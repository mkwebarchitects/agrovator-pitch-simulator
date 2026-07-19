using System;
using System.Collections.Generic;
using Agrovator.PitchSimulator.GuidedPitch;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    [Serializable]
    public sealed class PitchProgressRailSlot
    {
        [SerializeField] private PitchPart part;
        [SerializeField] private GameObject root;
        [SerializeField] private Text labelText;
        [SerializeField] private Text iconText;
        [SerializeField] private Image accentImage;
        [SerializeField] private GameObject currentMarker;

        public PitchProgressRailSlot(PitchPart part, GameObject root, Text labelText, Text iconText,
            Image accentImage, GameObject currentMarker)
        {
            this.part = part;
            this.root = root;
            this.labelText = labelText;
            this.iconText = iconText;
            this.accentImage = accentImage;
            this.currentMarker = currentMarker;
        }

        public PitchPart Part => part;
        public GameObject Root => root;
        public Text LabelText => labelText;
        public Text IconText => iconText;
        public Image AccentImage => accentImage;
        public string Label => labelText == null ? string.Empty : labelText.text;
        public bool IsCurrent { get; private set; }

        internal bool IsComplete { get; private set; }

        internal void Render(PitchPartVisual visual, bool current, bool complete, Func<string, string> localize)
        {
            labelText.text = localize(visual.LabelKey);
            iconText.text = visual.IconGlyph;
            accentImage.color = visual.Colour;
            IsCurrent = current;
            IsComplete = complete;
            if (currentMarker != null) currentMarker.SetActive(current);
            if (root != null) root.SetActive(true);
        }

        internal bool IsValid => root != null && labelText != null && iconText != null && accentImage != null;
    }

    public sealed class PitchProgressRailView : MonoBehaviour
    {
        private const int RequiredSlotCount = 4;

        [SerializeField] private PitchProgressRailSlot[] slots = new PitchProgressRailSlot[RequiredSlotCount];

        public IReadOnlyList<PitchProgressRailSlot> Slots => slots;

        public void Configure(PitchProgressRailSlot[] railSlots)
        {
            slots = railSlots == null ? null : (PitchProgressRailSlot[])railSlots.Clone();
            ValidateSlots();
        }

        public void Render(PitchPart? current, PitchDraftSnapshot draft, Func<string, string> localize)
        {
            if (draft == null) throw new ArgumentNullException(nameof(draft));
            if (localize == null) throw new ArgumentNullException(nameof(localize));
            ValidateSlots();

            foreach (var part in PitchParts.Ordered)
            {
                slots[(int)part].Render(PitchPartVisuals.Get(part), current == part,
                    draft[part].IsPopulated, localize);
            }
        }

        private void ValidateSlots()
        {
            if (slots == null || slots.Length != RequiredSlotCount)
            {
                throw new InvalidOperationException("Pitch progress rail requires exactly four prebuilt slots.");
            }

            var roots = new HashSet<GameObject>();
            for (var index = 0; index < slots.Length; index++)
            {
                if (slots[index] == null || !slots[index].IsValid || slots[index].Part != (PitchPart)index ||
                    !roots.Add(slots[index].Root))
                {
                    throw new InvalidOperationException("Pitch progress rail slots must be valid, distinct, and ordered.");
                }
            }
        }
    }
}
