using Agrovator.PitchSimulator.UI;
using NUnit.Framework;
using UnityEngine;

namespace Agrovator.PitchSimulator.Tests.EditMode.UI
{
    public sealed class RotateToPlayOverlayTests
    {
        private GameObject host;
        private GameObject panel;
        private RotateToPlayOverlay overlay;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("Rotate To Play", typeof(RotateToPlayOverlay));
            panel = new GameObject("Panel");
            panel.transform.SetParent(host.transform, false);
            overlay = host.GetComponent<RotateToPlayOverlay>();
            overlay.Configure(panel);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
        }

        [Test]
        public void Apply_ShowsThePromptOnAHandheldHeldUpright()
        {
            overlay.Apply(new WebViewportMetrics(390, 844, 3f));
            Assert.That(overlay.IsPrompting, Is.True);
            Assert.That(panel.activeSelf, Is.True);
        }

        [Test]
        public void Apply_HidesThePromptOnceTheDeviceIsTurned()
        {
            overlay.Apply(new WebViewportMetrics(390, 844, 3f));
            overlay.Apply(new WebViewportMetrics(844, 390, 3f));
            Assert.That(overlay.IsPrompting, Is.False);
            Assert.That(panel.activeSelf, Is.False);
        }

        [Test]
        public void Apply_LeavesADesktopStageAlone()
        {
            overlay.Apply(new WebViewportMetrics(1276, 918, 1f));
            Assert.That(overlay.IsPrompting, Is.False);
        }

        // The prompt must never be the state a learner lands in on a stage that can
        // play, so the serialized default is hidden and only a measured portrait
        // handheld turns it on.
        [Test]
        public void Configure_LeavesThePromptHiddenUntilAViewportIsMeasured()
        {
            Assert.That(overlay.IsPrompting, Is.False);
            Assert.That(panel.activeSelf, Is.False);
        }

        [Test]
        public void ValidateContract_FailsWithoutItsPanel()
        {
            var bare = new GameObject("Bare", typeof(RotateToPlayOverlay));
            try
            {
                Assert.That(bare.GetComponent<RotateToPlayOverlay>()
                    .ValidateContract(out var reason), Is.False);
                Assert.That(reason, Is.EqualTo("rotate_overlay_panel_missing"));
            }
            finally
            {
                Object.DestroyImmediate(bare);
            }
        }

        [Test]
        public void ValidateContract_PassesOnceConfigured()
        {
            Assert.That(overlay.ValidateContract(out var reason), Is.True, reason);
        }

        // Repeating the same viewport must not churn the active state, because the
        // overlay is polled every frame like the responsive layout.
        [Test]
        public void Apply_IsIdempotentForAnUnchangedViewport()
        {
            Assert.That(overlay.Apply(new WebViewportMetrics(390, 844, 3f)), Is.True,
                "The first measurement changes the prompt.");
            Assert.That(overlay.Apply(new WebViewportMetrics(390, 844, 3f)), Is.False,
                "Repeating a viewport must be a no-op.");
        }
    }
}
