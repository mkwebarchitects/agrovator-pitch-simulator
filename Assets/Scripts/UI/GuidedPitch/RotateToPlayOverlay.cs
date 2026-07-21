using System;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    /// <summary>
    /// Thin presentation bridge over <see cref="RotateToPlayRule"/>. Shows a prompt
    /// asking the learner to turn a handheld that is being held upright, because the
    /// guided builder lays four parts side by side and cannot be played vertically.
    /// There is no orientation lock to call: the Screen Orientation API needs
    /// fullscreen on Chrome for Android and does not exist on iOS Safari, so the
    /// gate is one the learner clears by turning the device.
    /// </summary>
    public sealed class RotateToPlayOverlay : MonoBehaviour
    {
        public const string TitleKey = "ui.rotate.title";
        public const string BodyKey = "ui.rotate.body";

        // Baked into the generated scene so it reads correctly before a catalog is
        // available; the launch locale replaces both through ApplyLocalization.
        public const string EnglishTitle = "Turn your device sideways";
        public const string EnglishBody =
            "This game needs a wider screen. Turn your device to keep playing.";

        [SerializeField] private GameObject panel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;

        private Func<WebViewportMetrics> viewportMetricsReader = WebViewportMetrics.Read;
        private WebViewportMetrics? observedViewportMetrics;

        public bool IsPrompting => panel != null && panel.activeSelf;

        public void Configure(GameObject overlayPanel, Text title = null, Text body = null)
        {
            panel = overlayPanel ?? throw new ArgumentNullException(nameof(overlayPanel));
            titleText = title;
            bodyText = body;
            observedViewportMetrics = null;
            // A playable stage must never flash the prompt, so the resting state is
            // hidden and only a measured portrait handheld turns it on.
            panel.SetActive(false);
        }

        /// <summary>
        /// Supplies viewport display metrics for embedded hosts and deterministic tests.
        /// </summary>
        public void ConfigureViewportMetricsReader(Func<WebViewportMetrics> reader)
        {
            viewportMetricsReader = reader ?? throw new ArgumentNullException(nameof(reader));
            observedViewportMetrics = null;
        }

        public bool ValidateContract(out string reason)
        {
            if (panel == null)
            {
                reason = "rotate_overlay_panel_missing";
                return false;
            }

            reason = null;
            return true;
        }

        /// <summary>
        /// Replaces the baked English with the launch locale. Safe to call with a
        /// null resolver so a catalog failure leaves the readable English standing
        /// rather than blanking the only instruction the learner can act on.
        /// </summary>
        public void ApplyLocalization(Func<string, string> localize)
        {
            if (localize == null)
            {
                return;
            }

            SetIfResolved(titleText, localize(TitleKey));
            SetIfResolved(bodyText, localize(BodyKey));
        }

        private static void SetIfResolved(Text target, string value)
        {
            // A missing key resolves to a visible [[missing:...]] token. Showing that
            // instead of the instruction would strand the learner, so keep English.
            if (target == null || string.IsNullOrEmpty(value) || value.StartsWith("[[missing:"))
            {
                return;
            }

            target.text = value;
        }

        /// <summary>
        /// Returns true only when the prompt's visibility changed, so polling every
        /// frame with an unchanged viewport costs nothing.
        /// </summary>
        public bool Apply(WebViewportMetrics metrics)
        {
            if (panel == null)
            {
                throw new InvalidOperationException("The rotate prompt has no panel.");
            }

            if (observedViewportMetrics.HasValue && observedViewportMetrics.Value == metrics)
            {
                return false;
            }

            observedViewportMetrics = metrics;
            var prompt = RotateToPlayRule.ShouldPromptRotate(metrics);
            if (panel.activeSelf == prompt)
            {
                return false;
            }

            panel.SetActive(prompt);
            return true;
        }

        private void OnEnable()
        {
            ApplyCurrentViewport();
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyCurrentViewport();
        }

        private void Update()
        {
            ApplyCurrentViewport();
        }

        private void ApplyCurrentViewport()
        {
            if (!isActiveAndEnabled || panel == null)
            {
                return;
            }

            Apply(viewportMetricsReader());
        }
    }
}
