using System;
using System.Collections.Generic;
using Agrovator.PitchSimulator.GuidedPitch;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    [Serializable]
    public sealed class PitchBoardSlot
    {
        [SerializeField] private PitchPart part;
        [SerializeField] private GameObject root;
        [SerializeField] private Text labelText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image accentImage;
        [SerializeField] private Text sentenceText;
        [SerializeField] private Text emptyPromptText;
        [SerializeField] private Image revisionOutline;

        public PitchBoardSlot(PitchPart part, GameObject root, Text labelText, Image iconImage,
            Image accentImage, Text sentenceText, Text emptyPromptText, Image revisionOutline)
        {
            this.part = part;
            this.root = root;
            this.labelText = labelText;
            this.iconImage = iconImage;
            this.accentImage = accentImage;
            this.sentenceText = sentenceText;
            this.emptyPromptText = emptyPromptText;
            this.revisionOutline = revisionOutline;
        }

        public PitchPart Part => part;
        public GameObject Root => root;
        public Text LabelText => labelText;
        public Image IconImage => iconImage;
        public Image AccentImage => accentImage;
        public Text SentenceText => sentenceText;
        public Text EmptyPromptText => emptyPromptText;
        public bool IsRevisionSelected { get; private set; }

        internal bool IsValid => root != null && labelText != null && iconImage != null &&
            accentImage != null && sentenceText != null && emptyPromptText != null;

        internal void Render(PitchPartVisual visual, PitchSectionSnapshot section,
            bool revisionSelected, Func<string, string> localize)
        {
            labelText.text = localize(visual.LabelKey);
            accentImage.color = visual.Colour;
            var populated = section.IsPopulated;
            sentenceText.text = populated ? localize(section.CurrentResponseId) : string.Empty;
            sentenceText.gameObject.SetActive(populated);
            emptyPromptText.text = populated ? string.Empty : localize(visual.EmptyPromptKey);
            emptyPromptText.gameObject.SetActive(!populated);
            SetRevisionSelected(revisionSelected);
            root.SetActive(true);
        }

        internal void SetRevisionSelected(bool selected)
        {
            IsRevisionSelected = selected;
            if (revisionOutline != null)
            {
                revisionOutline.color = PitchPartVisuals.FocusGold;
                revisionOutline.raycastTarget = false;
                revisionOutline.gameObject.SetActive(selected);
            }
        }
    }

    public sealed class PitchBoardView : MonoBehaviour
    {
        private const int RequiredSlotCount = 4;

        [SerializeField] private PitchBoardSlot[] slots = new PitchBoardSlot[RequiredSlotCount];
        private PitchPart? revisionSelection;

        public IReadOnlyList<PitchBoardSlot> Slots => slots;

        public void Configure(PitchBoardSlot[] boardSlots)
        {
            slots = boardSlots == null ? null : (PitchBoardSlot[])boardSlots.Clone();
            ValidateSlots();
        }

        /// <summary>
        /// Renders the four fixed board slots from the draft snapshot.
        /// </summary>
        /// <param name="draft">Draft snapshot whose populated sections carry the selected response IDs.</param>
        /// <param name="localize">
        /// For populated slots this function receives the section's current response ID and must
        /// resolve it to display text (typically response ID -> option TextKey -> catalog value).
        /// Non-ID keys (part labels and empty prompts) are passed through unchanged and must
        /// resolve directly against the localization catalog.
        /// </param>
        public void Render(PitchDraftSnapshot draft, Func<string, string> localize)
        {
            if (draft == null) throw new ArgumentNullException(nameof(draft));
            if (localize == null) throw new ArgumentNullException(nameof(localize));
            ValidateSlots();

            foreach (var part in PitchParts.Ordered)
            {
                slots[(int)part].Render(PitchPartVisuals.Get(part), draft[part],
                    revisionSelection == part, localize);
            }
        }

        public void SetRevisionSelection(PitchPart? part)
        {
            if (part.HasValue)
            {
                PitchPartVisuals.Get(part.Value);
            }

            revisionSelection = part;
            if (slots == null) return;
            foreach (var slot in slots)
            {
                slot?.SetRevisionSelected(part.HasValue && slot.Part == part.Value);
            }
        }

        private void ValidateSlots()
        {
            if (slots == null || slots.Length != RequiredSlotCount)
            {
                throw new InvalidOperationException("Pitch Board requires exactly four prebuilt slots.");
            }

            var roots = new HashSet<GameObject>();
            for (var index = 0; index < slots.Length; index++)
            {
                if (slots[index] == null || !slots[index].IsValid || slots[index].Part != (PitchPart)index ||
                    !roots.Add(slots[index].Root))
                {
                    throw new InvalidOperationException("Pitch Board slots must be valid, distinct, and ordered.");
                }
            }
        }
    }
}
