using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    /// <summary>
    /// Keeps one high-contrast outline synchronized with actual EventSystem selection.
    /// </summary>
    public sealed class SelectableFocusIndicator : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Selectable selectable;
        [SerializeField] private Outline indicator;

        public bool IsFocused { get; private set; }

        public void Configure(Selectable control, Outline focusOutline)
        {
            selectable = control ?? throw new ArgumentNullException(nameof(control));
            indicator = focusOutline ?? throw new ArgumentNullException(nameof(focusOutline));
            RefreshFromEventSystem();
        }

        public void OnSelect(BaseEventData eventData)
        {
            SetFocused(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            SetFocused(false);
        }

        private void OnEnable()
        {
            RefreshFromEventSystem();
        }

        private void OnDisable()
        {
            SetFocused(false);
        }

        private void RefreshFromEventSystem()
        {
            SetFocused(selectable != null && EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == selectable.gameObject);
        }

        private void SetFocused(bool focused)
        {
            IsFocused = focused;
            if (indicator != null)
            {
                indicator.enabled = focused;
            }
        }
    }
}
