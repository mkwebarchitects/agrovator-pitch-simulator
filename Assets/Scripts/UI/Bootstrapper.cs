using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Agrovator.PitchSimulator.Accessibility;
using Agrovator.PitchSimulator.Audio;
using Agrovator.PitchSimulator.Core;
using Agrovator.PitchSimulator.GuidedPitch;
using Agrovator.PitchSimulator.LMS;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Agrovator.PitchSimulator.UI
{
    /// <summary>
    /// Composition root for the guided pitch builder. Loads localization first,
    /// then the versioned guided content, and blocks the attempt on the safe
    /// fallback screen when either is unusable. Failure logs carry only stable
    /// diagnostic codes - never JSON, launch payloads, learner IDs, or response
    /// text.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class Bootstrapper : MonoBehaviour
    {
        private const string GameSceneName = "Game";
        private const string ContentInvalidCode = "guided_content_invalid";
        private const string LocalizationInvalidCode = "guided_localization_invalid";
        private const string LaunchInvalidCode = "guided_launch_invalid";
        private const string SceneContractInvalidCode = "guided_scene_contract_invalid";

        [SerializeField] private TextAsset guidedPitchContentJson;
        [SerializeField] private TextAsset englishCatalogJson;
        [SerializeField] private TextAsset malayCatalogJson;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioCueBinding[] audioCueBindings = Array.Empty<AudioCueBinding>();

        private static Bootstrapper instance;
        private GuidedPitchSessionController controller;
        private GuidedPitchScreenRouter router;
        private Func<string, string> localize;
        private AudioService audioService;
        private AudioCueDirector audioCueDirector;

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
                    Debug.LogError(SceneContractInvalidCode, this);
                    yield break;
                }

                yield return load;
                gameScene = SceneManager.GetSceneByName(GameSceneName);
            }

            var roots = gameScene.GetRootGameObjects();
            var routers = FindInRoots<GuidedPitchScreenRouter>(roots);
            var canvases = FindInRoots<Canvas>(roots);
            var eventSystems = FindInRoots<EventSystem>(roots);
            if (routers.Length != 1 || canvases.Length != 1 || eventSystems.Length != 1 ||
                !routers[0].ValidateContract(out _))
            {
                Debug.LogError(SceneContractInvalidCode, this);
                yield break;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            if (!TryLoadGuidedContent(out var loadedContent, out var loadedCatalog))
            {
                EnterSafeFallback(routers[0], loadedCatalog);
                yield break;
            }

            var launchBridge = CreateLmsBridge(loadedContent);
            var launchPoller = new LmsLaunchPoller(launchBridge);
            while (!launchPoller.TryPoll(Time.realtimeSinceStartup, out _))
            {
                yield return null;
            }

            PresentLoadedGuidedSession(routers[0], loadedContent, loadedCatalog, launchBridge);
#else
            TryPresentGuidedSession(routers[0]);
#endif
        }

        private void Update()
        {
            controller?.Tick(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            if (controller != null && audioCueDirector != null)
            {
                controller.EventPublished -= audioCueDirector.HandleGuidedSessionEvent;
            }
            controller?.Dispose();
            controller = null;
            router = null;
            audioCueDirector = null;
            audioService = null;
            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>
        /// Loads localization first, then the versioned guided content validated
        /// against the English catalog keys. Failures log one stable diagnostic
        /// code and never any content, payload, or learner data.
        /// </summary>
        public bool TryLoadGuidedContent(out GuidedPitchContent content, out LocalizationCatalog catalog)
        {
            content = null;
            catalog = null;
            if (englishCatalogJson == null || malayCatalogJson == null)
            {
                Debug.LogError(LocalizationInvalidCode, this);
                return false;
            }

            try
            {
                catalog = LocalizationCatalog.Load(englishCatalogJson.text, malayCatalogJson.text);
            }
            catch (FormatException)
            {
                catalog = null;
                Debug.LogError(LocalizationInvalidCode, this);
                return false;
            }

            if (guidedPitchContentJson == null)
            {
                Debug.LogError(ContentInvalidCode, this);
                return false;
            }

            var loaded = GuidedPitchContentLoader.Load(guidedPitchContentJson.text, catalog.GetKeys("en"));
            if (!loaded.IsSuccess)
            {
                Debug.LogError(ContentInvalidCode, this);
                return false;
            }

            content = loaded.Content;
            return true;
        }

        /// <summary>
        /// Composes the guided session against a validated scene router. On any
        /// content, localization, or launch failure the attempt is blocked on the
        /// SafeFallback screen and this returns false.
        /// </summary>
        public bool TryPresentGuidedSession(GuidedPitchScreenRouter sceneRouter,
            ILmsBridge bridgeOverride = null)
        {
            if (sceneRouter == null) throw new ArgumentNullException(nameof(sceneRouter));

            if (!TryLoadGuidedContent(out var content, out var catalog))
            {
                EnterSafeFallback(sceneRouter, catalog);
                return false;
            }

            return PresentLoadedGuidedSession(sceneRouter, content, catalog, bridgeOverride);
        }

        /// <summary>
        /// Composes the guided session from already-loaded content so callers
        /// that had to load the content earlier (the WebGL launch poll) never
        /// parse or log twice. On a launch failure the attempt is blocked on the
        /// SafeFallback screen with one stable diagnostic code and this returns
        /// false.
        /// </summary>
        public bool PresentLoadedGuidedSession(
            GuidedPitchScreenRouter sceneRouter,
            GuidedPitchContent content,
            LocalizationCatalog catalog,
            ILmsBridge bridgeOverride = null)
        {
            if (sceneRouter == null) throw new ArgumentNullException(nameof(sceneRouter));
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));

            var lmsBridge = bridgeOverride ?? CreateLmsBridge(content);
            var launch = lmsBridge.GetLaunchConfig();
            if (launch == null)
            {
                Debug.LogError(LaunchInvalidCode, this);
                EnterSafeFallback(sceneRouter, catalog);
                return false;
            }

            localize = BuildGuidedLocalizer(content, catalog, launch.Language);
            Enum.TryParse(launch.TimerMode, true, out TimerMode timerMode);
            var sessionController = new GuidedPitchSessionController(
                content,
                new AccessibilitySettings(
                    timerMode,
                    launch.ReducedMotion,
                    launch.MusicVolume,
                    launch.SfxVolume,
                    launch.Language),
                lmsBridge,
                () => DateTimeOffset.UtcNow,
                Application.version);
            if (!sessionController.FinishLaunch())
            {
                sessionController.Dispose();
                Debug.LogError(LaunchInvalidCode, this);
                EnterSafeFallback(sceneRouter, catalog);
                return false;
            }

            controller = sessionController;
            ConfigureAudio(launch);
            router = sceneRouter;
            router.Initialize(controller, localize, HandleTitleUserGesture);
            IsInitialized = true;
            return true;
        }

        private void EnterSafeFallback(GuidedPitchScreenRouter sceneRouter, LocalizationCatalog catalog)
        {
            sceneRouter.ShowSafeFallback(
                catalog == null ? (Func<string, string>)null : key => catalog.Resolve("en", key));
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
            audioCueDirector = new AudioCueDirector(cue => audioService.Play(cue));
            controller.EventPublished += audioCueDirector.HandleGuidedSessionEvent;
        }

        private void HandleTitleUserGesture()
        {
            if (audioService == null) return;
            audioService.UnlockAfterUserGesture();
            audioCueDirector?.HandleUserGesture();
        }

        private static Func<string, string> BuildGuidedLocalizer(
            GuidedPitchContent content,
            LocalizationCatalog catalog,
            string language)
        {
            var textKeysByResponseId = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var mode in content.Modes.Values)
            {
                foreach (var option in mode.Parts.SelectMany(part => part.Options)
                             .Concat(mode.FollowUp.Options))
                {
                    textKeysByResponseId[option.Id] = option.TextKey;
                }
            }

            return key => textKeysByResponseId.TryGetValue(key, out var textKey)
                ? catalog.Resolve(language, textKey)
                : catalog.Resolve(language, key);
        }

        private static ILmsBridge CreateLmsBridge(GuidedPitchContent content)
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
                ScenarioId = content.Id,
                Language = "en",
                AttemptNumber = 1,
                TimerMode = "Normal",
                ReducedMotion = false,
                MusicVolume = 0.8f,
                SfxVolume = 0.8f,
                ContentVersion = content.Version,
                LaunchReference = "lref_localLaunch01",
            });
#endif
        }

        private static T[] FindInRoots<T>(GameObject[] roots) where T : Component
        {
            return roots.SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        }
    }
}
