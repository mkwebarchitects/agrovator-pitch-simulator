using System;
using System.Collections.Generic;
using Agrovator.PitchSimulator.Dialogue;
using UnityEngine;

namespace Agrovator.PitchSimulator.UI
{
    public sealed class ResponseListView : MonoBehaviour
    {
        private const int RequiredSlotCount = 3;

        [SerializeField] private ResponseButtonView[] slots = new ResponseButtonView[RequiredSlotCount];

        private Action<string> selected;
        private IReadOnlyList<RuntimeResponseOption> renderedResponses;
        private bool initialized;

        public int SlotCount => slots == null ? 0 : slots.Length;

        public bool IsSelectionLocked { get; private set; }

        public void Configure(ResponseButtonView[] responseSlots)
        {
            if (responseSlots == null) throw new ArgumentNullException(nameof(responseSlots));
            slots = (ResponseButtonView[])responseSlots.Clone();
        }

        public void Initialize(Action<string> onSelected)
        {
            ValidateSlots();
            selected = onSelected ?? throw new ArgumentNullException(nameof(onSelected));
            for (var index = 0; index < slots.Length; index++)
            {
                slots[index].Initialize(HandleSelected);
            }
            initialized = true;
        }

        public void Render(
            IReadOnlyList<RuntimeResponseOption> responses,
            bool interactable,
            Func<string, string> resolveText = null)
        {
            if (!initialized)
            {
                throw new InvalidOperationException("Response list must be initialized before rendering.");
            }

            responses = responses ?? Array.Empty<RuntimeResponseOption>();
            if (responses.Count > RequiredSlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(responses), "At most three responses can be rendered.");
            }

            renderedResponses = responses;
            resolveText = resolveText ?? (value => value);
            IsSelectionLocked = !interactable;
            for (var index = 0; index < slots.Length; index++)
            {
                if (index < responses.Count)
                {
                    slots[index].Show(index, resolveText(responses[index].TextKey), interactable);
                }
                else
                {
                    slots[index].ClearAndHide();
                }
            }

            FocusNavigator.ConfigureVisible(slots, responses.Count);
            if (interactable)
            {
                FocusNavigator.FocusFirst(slots, responses.Count);
            }
        }

        private void ValidateSlots()
        {
            if (slots == null || slots.Length != RequiredSlotCount)
            {
                throw new InvalidOperationException("Response list requires exactly three prebuilt slots.");
            }

            var unique = new HashSet<ResponseButtonView>();
            foreach (var slot in slots)
            {
                if (slot == null || !unique.Add(slot))
                {
                    throw new InvalidOperationException("Response slots must be non-null and distinct.");
                }
            }
        }

        private void HandleSelected(int index)
        {
            if (IsSelectionLocked || renderedResponses == null ||
                index < 0 || index >= renderedResponses.Count)
            {
                return;
            }

            IsSelectionLocked = true;
            foreach (var slot in slots)
            {
                slot.SetInteractable(false);
            }

            selected(renderedResponses[index].Id);
        }
    }
}
