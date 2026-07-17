using System;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    public sealed class LearnPitchView : MonoBehaviour
    {
        [SerializeField] private Text incompletePitchText;
        [SerializeField] private Text explanationText;

        public Text IncompletePitchText => incompletePitchText;
        public Text ExplanationText => explanationText;

        public void Configure(Text incompletePitch, Text explanation)
        {
            incompletePitchText = incompletePitch ?? throw new ArgumentNullException(nameof(incompletePitch));
            explanationText = explanation ?? throw new ArgumentNullException(nameof(explanation));
        }

        public void Render(Func<string, string> localize)
        {
            if (localize == null) throw new ArgumentNullException(nameof(localize));
            if (incompletePitchText == null || explanationText == null)
            {
                throw new InvalidOperationException("Learn view references are incomplete.");
            }

            incompletePitchText.text = localize("guided.learn.incomplete_pitch");
            explanationText.text = localize("guided.learn.explanation");
        }
    }
}
