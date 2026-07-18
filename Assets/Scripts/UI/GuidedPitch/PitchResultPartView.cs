using System;
using Agrovator.PitchSimulator.GuidedPitch;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    /// <summary>
    /// One fixed Results status card for a single pitch part. It mirrors the
    /// persistent part identity (icon, colour, label), the learner's current
    /// sentence, the mastery statement, and an honest revision note: growth is
    /// named only when the revised sentence outranks the initial one.
    /// </summary>
    public sealed class PitchResultPartView : MonoBehaviour
    {
        [SerializeField] private PitchPart part;
        [SerializeField] private Text labelText;
        [SerializeField] private Text iconText;
        [SerializeField] private Image accentImage;
        [SerializeField] private Text sentenceText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text revisionNoteText;

        public PitchPart Part => part;
        public Text LabelText => labelText;
        public Text IconText => iconText;
        public Image AccentImage => accentImage;
        public Text SentenceText => sentenceText;
        public Text StatusText => statusText;
        public Text RevisionNoteText => revisionNoteText;

        internal bool IsValid => labelText != null && iconText != null && accentImage != null &&
            sentenceText != null && statusText != null && revisionNoteText != null;

        public void Configure(PitchPart cardPart, Text label, Text icon, Image accent,
            Text sentence, Text status, Text revisionNote)
        {
            PitchPartVisuals.Get(cardPart);
            part = cardPart;
            labelText = label;
            iconText = icon;
            accentImage = accent;
            sentenceText = sentence;
            statusText = status;
            revisionNoteText = revisionNote;
            if (!IsValid)
            {
                throw new InvalidOperationException("Result part view references are incomplete.");
            }
        }

        /// <summary>
        /// Renders one populated draft section. <paramref name="localize"/> must
        /// resolve the section's current response ID to display text (response
        /// ID -> option TextKey -> catalog) and pass plain keys through to the
        /// catalog, matching the composite localizer used by the Pitch Board.
        /// </summary>
        internal void Render(PitchSectionSnapshot section, Func<string, string> localize)
        {
            if (section == null) throw new ArgumentNullException(nameof(section));
            if (localize == null) throw new ArgumentNullException(nameof(localize));
            if (!IsValid || section.Part != part || !section.IsPopulated)
            {
                throw new InvalidOperationException("Result part view cannot render this section.");
            }

            var visual = PitchPartVisuals.Get(part);
            labelText.text = localize(visual.LabelKey);
            iconText.text = visual.IconGlyph;
            accentImage.color = visual.Colour;
            sentenceText.text = localize(section.CurrentResponseId);
            statusText.text = localize(PitchPartVisuals.MasteryLabelKey(section.CurrentMastery.Value));
            var revisionNoteKey = RevisionNoteKey(section);
            revisionNoteText.text = revisionNoteKey == null ? string.Empty : localize(revisionNoteKey);
            revisionNoteText.gameObject.SetActive(revisionNoteKey != null);
            gameObject.SetActive(true);
        }

        internal void Clear()
        {
            if (sentenceText != null) sentenceText.text = string.Empty;
            if (statusText != null) statusText.text = string.Empty;
            if (revisionNoteText != null)
            {
                revisionNoteText.text = string.Empty;
                revisionNoteText.gameObject.SetActive(false);
            }

            gameObject.SetActive(false);
        }

        private static string RevisionNoteKey(PitchSectionSnapshot section)
        {
            if (!section.WasRevised || !section.InitialMastery.HasValue)
            {
                return null;
            }

            return section.CurrentMastery.Value > section.InitialMastery.Value
                ? "guided.results.part.strengthened"
                : "guided.results.part.revised";
        }
    }
}
