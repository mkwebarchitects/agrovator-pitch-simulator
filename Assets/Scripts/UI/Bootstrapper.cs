using System;
using System.Collections;
using System.Linq;
using Agrovator.PitchSimulator.Accessibility;
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

        private static Bootstrapper instance;
        private PitchSessionController controller;

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

            if (!TryCreateController(out controller))
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

            routers[0].Initialize(controller);
            IsInitialized = true;
        }

        private void Update()
        {
            controller?.Tick(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            controller?.Dispose();
            controller = null;
            if (instance == this)
            {
                instance = null;
            }
        }

        private bool TryCreateController(out PitchSessionController result)
        {
            result = null;
            if (scenarioJson == null || englishCatalogJson == null || malayCatalogJson == null)
            {
                Debug.LogError("Bootstrap content references are incomplete.", this);
                return false;
            }

            try
            {
                var catalog = LocalizationCatalog.Load(englishCatalogJson.text, malayCatalogJson.text);
                var loaded = ScenarioJsonLoader.Load(scenarioJson.text, catalog.GetKeys("en"));
                if (!loaded.IsSuccess)
                {
                    Debug.LogError("Scenario content failed validated loading.", this);
                    return false;
                }

                var scenario = loaded.Scenario;
                var launch = new LmsLaunchConfig
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
                };
                result = new PitchSessionController(
                    scenario,
                    new ScoreAccumulator(),
                    new AccessibilitySettings(TimerMode.Normal, false, 0.8f, 0.8f, "en"),
                    new QuestionTimer(0d),
                    new MockLmsBridge(MockLmsBridgeMode.Success, launch),
                    () => DateTimeOffset.UtcNow,
                    Application.version);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Bootstrap composition failed: {exception.GetType().Name}.", this);
                return false;
            }
        }

        private static T[] FindInRoots<T>(GameObject[] roots) where T : Component
        {
            return roots.SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        }
    }
}
