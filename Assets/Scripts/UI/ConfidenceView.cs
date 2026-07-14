using System;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    public sealed class ConfidenceView : MonoBehaviour
    {
        private static readonly string[] Labels =
        {
            "Getting Started",
            "Listening",
            "Curious",
            "Interested",
            "Convinced",
        };

        private static readonly string[] LabelKeys =
        {
            "ui.confidence.getting_started",
            "ui.confidence.listening",
            "ui.confidence.curious",
            "ui.confidence.interested",
            "ui.confidence.convinced",
        };

        private static readonly string[] Glyphs = { "[ ]", "[.]", "[:]", "[*]", "[#]" };

        [SerializeField] private Text stateLabel;
        [SerializeField] private Text iconLabel;
        [SerializeField] private Image fillImage;

        public void Configure(Text label, Text icon, Image fill)
        {
            stateLabel = label ?? throw new ArgumentNullException(nameof(label));
            iconLabel = icon ?? throw new ArgumentNullException(nameof(icon));
            fillImage = fill ?? throw new ArgumentNullException(nameof(fill));
        }

        public void Render(int confidence, Func<string, string> resolveText = null)
        {
            if (stateLabel == null || iconLabel == null || fillImage == null)
            {
                throw new InvalidOperationException("Confidence view references are incomplete.");
            }

            var value = Mathf.Clamp(confidence, 0, 100);
            var state = Math.Min(value / 20, Labels.Length - 1);
            stateLabel.text = resolveText == null ? Labels[state] : resolveText(LabelKeys[state]);
            iconLabel.text = Glyphs[state];
            fillImage.fillAmount = value / 100f;
        }
    }
}
