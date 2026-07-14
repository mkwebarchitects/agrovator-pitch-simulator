using System;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    public sealed class TimerView : MonoBehaviour
    {
        private const double PulseThresholdSeconds = 5d;
        private const float PulseAmplitude = 0.025f;

        [SerializeField] private Text secondsLabel;
        [SerializeField] private Image fillImage;
        [SerializeField] private RectTransform pulseTarget;

        public int DisplayedSeconds { get; private set; }

        public bool IsPulsing { get; private set; }

        public void Configure(Text label, Image fill, RectTransform target)
        {
            secondsLabel = label ?? throw new ArgumentNullException(nameof(label));
            fillImage = fill ?? throw new ArgumentNullException(nameof(fill));
            pulseTarget = target ?? throw new ArgumentNullException(nameof(target));
        }

        public void Render(double remainingSeconds, double totalSeconds, bool reducedMotion)
        {
            if (double.IsNaN(remainingSeconds) || double.IsInfinity(remainingSeconds) ||
                double.IsNaN(totalSeconds) || double.IsInfinity(totalSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(remainingSeconds));
            }
            if (secondsLabel == null || fillImage == null || pulseTarget == null)
            {
                throw new InvalidOperationException("Timer view references are incomplete.");
            }

            var remaining = Math.Max(0d, remainingSeconds);
            var total = Math.Max(0d, totalSeconds);
            DisplayedSeconds = (int)Math.Ceiling(remaining);
            secondsLabel.text = DisplayedSeconds.ToString();
            fillImage.fillAmount = total <= 0d
                ? 0f
                : Mathf.Clamp01((float)(remaining / total));

            IsPulsing = !reducedMotion && total > 0d && remaining > 0d &&
                remaining <= PulseThresholdSeconds;
            if (!IsPulsing)
            {
                pulseTarget.localScale = Vector3.one;
                return;
            }

            var phase = (float)(remaining * Math.PI * 2d);
            var scale = 1f + PulseAmplitude * (0.5f + 0.5f * Mathf.Cos(phase));
            pulseTarget.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
