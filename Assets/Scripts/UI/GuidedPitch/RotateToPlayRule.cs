namespace Agrovator.PitchSimulator.UI
{
    /// <summary>
    /// Decides whether a stage is too upright to play on. The guided builder shows
    /// four parts side by side, which does not survive a phone held vertically, so a
    /// handheld in portrait is asked to turn rather than served a broken layout.
    /// Pure rule with no Unity dependency beyond the metrics struct, so the decision
    /// is testable without a scene.
    /// </summary>
    public static class RotateToPlayRule
    {
        /// <summary>
        /// Stages at or below this CSS width are treated as handhelds. Shared with
        /// <see cref="GuidedPitchResponsiveLayout"/> so the stage that compacts is
        /// exactly the stage that can be gated, and the two cannot drift apart.
        /// </summary>
        public const float HandheldWidthThreshold = 960f;

        public static bool ShouldPromptRotate(WebViewportMetrics metrics)
        {
            // Rotating a monitor is not a fix, so a wide portrait window - a tall
            // desktop browser - is left playable and the learner can resize. Only a
            // stage narrow enough to actually be a handheld is gated, or the prompt
            // becomes a dead end nobody can clear.
            return metrics.CssHeight > metrics.CssWidth &&
                metrics.CssWidth <= HandheldWidthThreshold;
        }
    }
}
