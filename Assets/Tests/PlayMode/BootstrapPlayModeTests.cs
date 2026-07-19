using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Agrovator.PitchSimulator.GuidedPitch;
using Agrovator.PitchSimulator.LMS;
using Agrovator.PitchSimulator.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.Tests.PlayMode
{
    public sealed class BootstrapPlayModeTests
    {
        private const string RecoverySentence =
            "This pitch activity could not be loaded. Refresh and try again, or ask your teacher for help.";

        private readonly List<GameObject> roots = new List<GameObject>();
        private readonly List<string> capturedErrors = new List<string>();

        [SetUp]
        public void SetUp()
        {
            capturedErrors.Clear();
            Application.logMessageReceived += CaptureLog;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= CaptureLog;
            foreach (var root in roots.Where(root => root != null))
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            roots.Clear();
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            foreach (var bootstrapper in UnityEngine.Object.FindObjectsByType<Bootstrapper>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                UnityEngine.Object.Destroy(bootstrapper.gameObject);
            }
            yield return null;
            foreach (var sceneName in new[] { "Game", "Bootstrap" })
            {
                var scene = SceneManager.GetSceneByName(sceneName);
                if (scene.IsValid() && scene.isLoaded)
                {
                    yield return SceneManager.UnloadSceneAsync(scene);
                }
            }
        }

        [UnityTest]
        public IEnumerator BootstrapScene_BootsTheGuidedComposition_ThroughTitleIntoModeSelection()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Additive);
            Bootstrapper bootstrapper = null;
            var deadline = Time.realtimeSinceStartup + 60f;
            while (bootstrapper == null && Time.realtimeSinceStartup < deadline)
            {
                bootstrapper = UnityEngine.Object.FindFirstObjectByType<Bootstrapper>();
                yield return null;
            }
            Assert.That(bootstrapper, Is.Not.Null, "The Bootstrap scene must own one Bootstrapper.");
            while (!bootstrapper.IsInitialized && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(bootstrapper.IsInitialized, Is.True,
                "Bootstrap must compose the guided session against the generated scenes.");

            var gameScene = SceneManager.GetSceneByName("Game");
            Assert.That(gameScene.isLoaded, Is.True, "Bootstrap must load the Game scene additively.");
            var gameRoots = gameScene.GetRootGameObjects();
            var routers = gameRoots
                .SelectMany(root => root.GetComponentsInChildren<GuidedPitchScreenRouter>(true))
                .ToArray();
            Assert.That(routers, Has.Length.EqualTo(1));
            Assert.That(gameRoots.SelectMany(root => root.GetComponentsInChildren<Canvas>(true)).Count(),
                Is.EqualTo(1));
            Assert.That(gameRoots.SelectMany(root => root.GetComponentsInChildren<EventSystem>(true)).Count(),
                Is.EqualTo(1));

            var router = routers[0];
            Assert.That(router.IsInitialized, Is.True);
            Assert.That(router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Title));
            var canvas = router.transform;
            var title = canvas.Find("Title");
            var guided = canvas.Find("Guided Pitch");
            Assert.That(title, Is.Not.Null);
            Assert.That(guided, Is.Not.Null);
            Assert.That(title.gameObject.activeSelf, Is.True);
            Assert.That(guided.gameObject.activeSelf, Is.False);
            var start = title.Find("Content Frame/Start Button").GetComponent<Button>();
            var eventSystem = EventSystem.current;
            Assert.That(eventSystem, Is.Not.Null);
            Assert.That(eventSystem.currentSelectedGameObject, Is.SameAs(start.gameObject),
                "Title Start must receive the initial focus.");

            start.onClick.Invoke();
            yield return null;
            Assert.That(router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Briefing));
            var briefing = canvas.Find("Briefing");
            Assert.That(briefing.gameObject.activeSelf, Is.True);
            var briefingCopy = string.Join(" ", briefing
                .Find("Content Frame")
                .GetComponentsInChildren<Text>(false)
                .Where(line => line.name.StartsWith("Line", StringComparison.Ordinal))
                .Select(line => line.text));
            Assert.That(briefingCopy, Does.Contain("Judge Aya"));
            Assert.That(briefingCopy, Does.Contain("no timer"));
            Assert.That(briefingCopy, Does.Not.Contain("[[missing:"));

            briefing.Find("Content Frame/Continue Button").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(router.ActivePhase, Is.EqualTo(GuidedPitchPhase.ModeSelection));
            Assert.That(guided.gameObject.activeSelf, Is.True,
                "The guided panel must stay active behind the nested Mode Selection section.");
            var modeSelection = guided.Find(
                "Content Frame/Phase Scroll/Viewport/Content/Mode Selection");
            Assert.That(modeSelection, Is.Not.Null);
            Assert.That(modeSelection.gameObject.activeInHierarchy, Is.True,
                "The nested Mode Selection section must be visible during ModeSelection.");
            var modeView = modeSelection.GetComponent<ModeSelectionView>();
            Assert.That(modeView.Cards.Count, Is.EqualTo(2));
            Assert.That(eventSystem.currentSelectedGameObject,
                Is.SameAs(modeView.Cards[0].Button.gameObject),
                "The first mode card must take the ModeSelection default focus.");
            Assert.That(capturedErrors, Is.Empty,
                "A healthy boot must log no errors: " + string.Join(", ", capturedErrors));
        }

        [Test]
        public void Bootstrap_LoadsValidGuidedContent_AndRouterEntersTitle()
        {
            var bootstrap = CreateBootstrapper(
                GuidedRigFactory.ReadProjectFile("Content", "Scenarios", "guided-pitch-builder.en.json"),
                GuidedRigFactory.ReadProjectFile("Content", "Localization", "en.json"),
                GuidedRigFactory.ReadProjectFile("Content", "Localization", "ms.json"));

            Assert.That(bootstrap.TryLoadGuidedContent(out var content, out var catalog), Is.True);
            Assert.That(content, Is.Not.Null);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(content.Id, Is.EqualTo("smart-school-garden"));
            Assert.That(content.Version, Is.EqualTo(2));

            var rig = GuidedRigFactory.CreateRig(roots);
            var bridge = new MockLmsBridge(
                MockLmsBridgeMode.Success, GuidedRigFactory.CreateLaunch(content));
            Assert.That(bootstrap.TryPresentGuidedSession(rig.Router, bridge), Is.True);
            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Title));
            Assert.That(bootstrap.IsInitialized, Is.True);
            Assert.That(rig.TitlePanel.activeSelf, Is.True);
            Assert.That(rig.SafeFallbackPanel.activeSelf, Is.False);
            Assert.That(capturedErrors, Is.Empty);
        }

        [Test]
        public void Bootstrap_MissingGuidedContentReference_BlocksTheAttemptOnSafeFallback()
        {
            var bootstrap = CreateBootstrapper(
                null,
                GuidedRigFactory.ReadProjectFile("Content", "Localization", "en.json"),
                GuidedRigFactory.ReadProjectFile("Content", "Localization", "ms.json"));

            AssertBlockedOnSafeFallback(bootstrap, "guided_content_invalid");
        }

        [Test]
        public void Bootstrap_MalformedGuidedJson_BlocksTheAttemptOnSafeFallback()
        {
            var bootstrap = CreateBootstrapper(
                "{\"Id\":\"smart-school-garden\",",
                GuidedRigFactory.ReadProjectFile("Content", "Localization", "en.json"),
                GuidedRigFactory.ReadProjectFile("Content", "Localization", "ms.json"));

            AssertBlockedOnSafeFallback(bootstrap, "guided_content_invalid");
        }

        [Test]
        public void Bootstrap_InvalidGuidedRoute_BlocksTheAttemptOnSafeFallback()
        {
            var brokenRoutes = GuidedRigFactory
                .ReadProjectFile("Content", "Scenarios", "guided-pitch-builder.en.json")
                .Replace("\"Modes\"", "\"Modez\"");
            var bootstrap = CreateBootstrapper(
                brokenRoutes,
                GuidedRigFactory.ReadProjectFile("Content", "Localization", "en.json"),
                GuidedRigFactory.ReadProjectFile("Content", "Localization", "ms.json"));

            AssertBlockedOnSafeFallback(bootstrap, "guided_content_invalid");
        }

        [Test]
        public void Bootstrap_LocalizationMismatch_BlocksTheAttemptOnSafeFallback()
        {
            var sparseEnglish =
                "{\"locale\":\"en\",\"translationStatus\":\"reviewed\",\"entries\":[" +
                "{\"key\":\"ui.start\",\"value\":\"Start\"}]}";
            var sparseMalay =
                "{\"locale\":\"ms\",\"translationStatus\":\"pending_human_review\",\"entries\":[" +
                "{\"key\":\"ui.start\",\"value\":\"Start\"}]}";
            var bootstrap = CreateBootstrapper(
                GuidedRigFactory.ReadProjectFile("Content", "Scenarios", "guided-pitch-builder.en.json"),
                sparseEnglish,
                sparseMalay);

            AssertBlockedOnSafeFallback(bootstrap, "guided_content_invalid");
        }

        [Test]
        public void Bootstrap_LocalizationFailure_UsesTheExactEnglishRecoverySentence()
        {
            var bootstrap = CreateBootstrapper(
                GuidedRigFactory.ReadProjectFile("Content", "Scenarios", "guided-pitch-builder.en.json"),
                "{not-a-catalog",
                GuidedRigFactory.ReadProjectFile("Content", "Localization", "ms.json"));

            AssertBlockedOnSafeFallback(bootstrap, "guided_localization_invalid");
        }

        [Test]
        public void Bootstrap_PreloadedComposition_RejectedLaunch_LogsTheLaunchCodeExactlyOnce()
        {
            var bootstrap = CreateBootstrapper(
                GuidedRigFactory.ReadProjectFile("Content", "Scenarios", "guided-pitch-builder.en.json"),
                GuidedRigFactory.ReadProjectFile("Content", "Localization", "en.json"),
                GuidedRigFactory.ReadProjectFile("Content", "Localization", "ms.json"));
            Assert.That(bootstrap.TryLoadGuidedContent(out var content, out var catalog), Is.True);
            var badLaunch = GuidedRigFactory.CreateLaunch(content);
            badLaunch.ContentVersion = 1;
            var rig = GuidedRigFactory.CreateRig(roots);
            LogAssert.Expect(LogType.Error, "guided_launch_invalid");

            Assert.That(bootstrap.PresentLoadedGuidedSession(rig.Router, content, catalog,
                new MockLmsBridge(MockLmsBridgeMode.Success, badLaunch)), Is.False);

            Assert.That(bootstrap.IsInitialized, Is.False);
            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.SafeFallback));
            Assert.That(rig.SafeFallbackPanel.activeSelf, Is.True);
            Assert.That(rig.SafeFallbackText.text, Is.EqualTo(RecoverySentence));
            Assert.That(capturedErrors, Is.EqualTo(new[] { "guided_launch_invalid" }),
                "The shared composition path must log the stable code exactly once.");
        }

        private void AssertBlockedOnSafeFallback(Bootstrapper bootstrap, string expectedCode)
        {
            var rig = GuidedRigFactory.CreateRig(roots);
            LogAssert.Expect(LogType.Error, expectedCode);

            Assert.That(bootstrap.TryPresentGuidedSession(rig.Router), Is.False);
            Assert.That(bootstrap.IsInitialized, Is.False);
            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.SafeFallback));
            Assert.That(rig.SafeFallbackPanel.activeSelf, Is.True);
            Assert.That(rig.TitlePanel.activeSelf, Is.False);
            Assert.That(rig.GuidedPanel.activeSelf, Is.False);
            Assert.That(rig.SafeFallbackText.text, Is.EqualTo(RecoverySentence));

            Assert.That(capturedErrors, Has.Count.EqualTo(1),
                "Each blocked attempt must log its stable diagnostic code exactly once.");
            foreach (var error in capturedErrors)
            {
                Assert.That(error, Is.EqualTo(expectedCode),
                    "Failure logs must contain only the stable diagnostic code.");
                Assert.That(error, Does.Not.Contain("{"));
                Assert.That(error, Does.Not.Contain("local_learner"));
                Assert.That(error, Does.Not.Contain("lref_"));
                Assert.That(error, Does.Not.Contain("garden beds"));
            }
        }

        private Bootstrapper CreateBootstrapper(string guidedJson, string englishJson, string malayJson)
        {
            var root = new GameObject("Bootstrap Rig");
            root.SetActive(false);
            roots.Add(root);
            var bootstrap = root.AddComponent<Bootstrapper>();
            GuidedRigFactory.SetField(bootstrap, "guidedPitchContentJson", CreateAsset(guidedJson));
            GuidedRigFactory.SetField(bootstrap, "englishCatalogJson", CreateAsset(englishJson));
            GuidedRigFactory.SetField(bootstrap, "malayCatalogJson", CreateAsset(malayJson));
            return bootstrap;
        }

        private static TextAsset CreateAsset(string text)
        {
            return text == null ? null : new TextAsset(text);
        }

        private void CaptureLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                capturedErrors.Add(condition);
            }
        }
    }
}
