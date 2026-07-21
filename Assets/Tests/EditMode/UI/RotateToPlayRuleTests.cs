using Agrovator.PitchSimulator.UI;
using NUnit.Framework;

namespace Agrovator.PitchSimulator.Tests.EditMode.UI
{
    public sealed class RotateToPlayRuleTests
    {
        // A handheld held upright cannot show the four-part board, so the learner is
        // asked to turn the device. A wide stage never is, whatever its size.
        [TestCase(390, 844, true, TestName = "Phone portrait asks for a turn")]
        [TestCase(844, 390, false, TestName = "Phone landscape plays")]
        [TestCase(768, 1024, true, TestName = "Small tablet portrait asks for a turn")]
        [TestCase(1024, 768, false, TestName = "Small tablet landscape plays")]
        [TestCase(1276, 918, false, TestName = "Desktop plays")]
        public void ShouldPromptRotate_TracksOrientationOnHandheldStages(
            int cssWidth, int cssHeight, bool expected)
        {
            var metrics = new WebViewportMetrics(cssWidth, cssHeight, 1f);
            Assert.That(RotateToPlayRule.ShouldPromptRotate(metrics), Is.EqualTo(expected));
        }

        // A tall narrow desktop window is portrait too, but rotating a monitor is not
        // the fix and the prompt would be a dead end. Only stages narrow enough to be
        // a handheld are gated; anything wider is left playable so the learner can
        // resize instead of being locked out.
        [TestCase(1200, 1600, TestName = "Tall desktop window is not gated")]
        [TestCase(961, 1600, TestName = "Just above the handheld width is not gated")]
        public void ShouldPromptRotate_LeavesWidePortraitStagesPlayable(int cssWidth, int cssHeight)
        {
            var metrics = new WebViewportMetrics(cssWidth, cssHeight, 1f);
            Assert.That(RotateToPlayRule.ShouldPromptRotate(metrics), Is.False);
        }

        // Exactly square is not portrait. Pinning the boundary keeps a future
        // refactor from flipping it to >= and gating a square stage.
        [Test]
        public void ShouldPromptRotate_TreatsSquareAsPlayable()
        {
            Assert.That(
                RotateToPlayRule.ShouldPromptRotate(new WebViewportMetrics(800, 800, 1f)),
                Is.False);
        }

        // Device pixel ratio must not participate: a high-density phone is still a
        // phone, and a DPR-only change must never flip the gate.
        [TestCase(1f)]
        [TestCase(2f)]
        [TestCase(3f)]
        public void ShouldPromptRotate_IgnoresDevicePixelRatio(float devicePixelRatio)
        {
            Assert.That(
                RotateToPlayRule.ShouldPromptRotate(
                    new WebViewportMetrics(390, 844, devicePixelRatio)),
                Is.True);
        }
    }
}
