using System;
using System.Collections.Generic;
using System.Linq;
using Agrovator.PitchSimulator.GuidedPitch;
using Agrovator.PitchSimulator.LMS;
using Agrovator.PitchSimulator.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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
