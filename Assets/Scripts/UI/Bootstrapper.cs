using System;
using System.Collections;
using System.Linq;
using Agrovator.PitchSimulator.Accessibility;
using Agrovator.PitchSimulator.Audio;
using Agrovator.PitchSimulator.Core;
using Agrovator.PitchSimulator.Dialogue;
using Agrovator.PitchSimulator.LMS;
using Agrovator.PitchSimulator.Scoring;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    [DefaultExecutionOrder(-10000)]
    public sealed class Bootstrapper : MonoBehaviour
    {
        private const string GameSceneName = "Game";

        [SerializeField] private TextAsset scenarioJson;
        [SerializeField] private TextAsset englishCatalogJson;
        [SerializeField] private TextAsset malayCatalogJson;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioCueBinding[] audioCueBindings = Array.Empty<AudioCueBinding>();

        private static Bootstrapper instance;
        private PitchSessionController controller;
        private GameScreenRouter router;
        private Func<string, string> localize;
        private AudioService audioService;

        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator Start()
        {
            if (instance != this)
            {
                yield break;
            }

            var gameScene = SceneManager.GetSceneByName(GameSceneName);
            if (!gameScene.isLoaded)
            {
                var load = SceneManager.LoadSceneAsync(GameSceneName, LoadSceneMode.Additive);
                if (load == null)
                {
                    Debug.LogError("The Game scene could not be loaded.", this);
                    yield break;
                }

                yield return load;
                gameScene = SceneManager.GetSceneByName(GameSceneName);
            }

            var roots = gameScene.GetRootGameObjects();
            var routers = FindInRoots<GameScreenRouter>(roots);
            var canvases = FindInRoots<Canvas>(roots);
            var eventSystems = FindInRoots<EventSystem>(roots);
            if (routers.Length != 1 || canvases.Length != 1 || eventSystems.Length != 1)
            {
                Debug.LogError(
                    $"Game scene contract requires one Canvas, EventSystem and router; found " +
                    $"{canvases.Length}/{eventSystems.Length}/{routers.Length}.", this);
                yield break;
            }
            if (!routers[0].ValidateContract(out var contractError))
            {
                Debug.LogError($"Game router contract is invalid: {contractError}", this);
                yield break;
            }

            if (!TryLoadContent(out var scenario, out var catalog))
            {
                yield break;
            }

            var lmsBridge = CreateLmsBridge(scenario);
            var launchPoller = new LmsLaunchPoller(lmsBridge);
            LmsLaunchConfig launch;
#if UNITY_WEBGL && !UNITY_EDITOR
            while (!launchPoller.TryPoll(Time.realtimeSinceStartup, out launch))
            {
                yield return null;
            }
#else
            if (!launchPoller.TryPoll(Time.realtimeSinceStartup, out launch))
            {
                Debug.LogError("LMS launch configuration was not available.", this);
                yield break;
            }
#endif

            if (!TryCreateController(scenario, catalog, lmsBridge, launch, out controller))
            {
                yield break;
            }

            if (!controller.FinishLaunch())
            {
                Debug.LogError("Pitch session launch configuration was rejected.", this);
                controller.Dispose();
                controller = null;
                yield break;
            }

            ConfigureAudio(launch);
            router = routers[0];
            router.Initialize(controller, localize, HandleTitleUserGesture);
            IsInitialized = true;
        }

        private void Update()
        {
            if (controller == null)
            {
                return;
            }

            controller.Tick(Time.unscaledDeltaTime);
            router?.TickPresentation(controller.Snapshot);
        }

        private void OnDestroy()
        {
            controller?.Dispose();
            controller = null;
            router = null;
            audioService = null;
            if (instance == this)
            {
                instance = null;
            }
        }

        private void ConfigureAudio(LmsLaunchConfig launch)
        {
            var music = musicSource == null
                ? (IAudioPlaybackChannel)new SilentAudioPlaybackChannel()
                : new UnityAudioSourceChannel(musicSource);
            var sfx = sfxSource == null
                ? (IAudioPlaybackChannel)new SilentAudioPlaybackChannel()
                : new UnityAudioSourceChannel(sfxSource);
            audioService = new AudioService(
                music,
                sfx,
                audioCueBindings ?? Array.Empty<AudioCueBinding>(),
                new UnityAudioDiagnostics(this),
                Debug.isDebugBuild || Application.isEditor);
            audioService.SetMusicVolume(launch.MusicVolume);
            audioService.SetSfxVolume(launch.SfxVolume);
        }

        private void HandleTitleUserGesture()
        {
            if (audioService == null) return;
            audioService.UnlockAfterUserGesture();
            audioService.Play(AudioCue.ButtonPress);
        }

        private bool TryLoadContent(
            out RuntimeScenario scenario,
            out LocalizationCatalog catalog)
        {
            scenario = null;
            catalog = null;
            if (scenarioJson == null || englishCatalogJson == null || malayCatalogJson == null)
            {
                Debug.LogError("Bootstrap content references are incomplete.", this);
                return false;
            }

            try
            {
                catalog = LocalizationCatalog.Load(englishCatalogJson.text, malayCatalogJson.text);
                var loaded = ScenarioJsonLoader.Load(scenarioJson.text, catalog.GetKeys("en"));
                if (!loaded.IsSuccess)
                {
                    Debug.LogError("Scenario content failed validated loading.", this);
                    return false;
                }

                scenario = loaded.Scenario;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Bootstrap composition failed: {exception.GetType().Name}.", this);
                return false;
            }
        }

        private static ILmsBridge CreateLmsBridge(RuntimeScenario scenario)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new WebGlLmsBridge();
#else
            return new MockLmsBridge(MockLmsBridgeMode.Success, new LmsLaunchConfig
            {
                PseudonymousLearnerId = "local_learner",
                SessionId = "local_session",
                CourseId = "local_course",
                ModuleId = "local_module",
                LessonId = "local_lesson",
                ScenarioId = scenario.Id,
                Language = "en",
                AttemptNumber = 1,
                TimerMode = "Normal",
                ReducedMotion = false,
                MusicVolume = 0.8f,
                SfxVolume = 0.8f,
                ContentVersion = scenario.Version,
                LaunchReference = "lref_localLaunch01",
            });
#endif
        }

        private bool TryCreateController(
            RuntimeScenario scenario,
            LocalizationCatalog catalog,
            ILmsBridge lmsBridge,
            LmsLaunchConfig launch,
            out PitchSessionController result)
        {
            result = null;
            if (LmsPayloadValidator.ValidateLaunch(launch).Count > 0 ||
                !string.Equals(launch.ScenarioId, scenario.Id, StringComparison.Ordinal) ||
                launch.ContentVersion != scenario.Version)
            {
                Debug.LogError("LMS launch configuration was rejected.", this);
                return false;
            }

            Enum.TryParse(launch.TimerMode, true, out TimerMode timerMode);
            localize = key => catalog.Resolve(launch.Language, key);
            result = new PitchSessionController(
                scenario,
                new ScoreAccumulator(),
                new AccessibilitySettings(
                    timerMode,
                    launch.ReducedMotion,
                    launch.MusicVolume,
                    launch.SfxVolume,
                    launch.Language),
                new QuestionTimer(0d),
                lmsBridge,
                () => DateTimeOffset.UtcNow,
                Application.version);
            return true;
        }

        private static T[] FindInRoots<T>(GameObject[] roots) where T : Component
        {
            return roots.SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        }
    }
}
