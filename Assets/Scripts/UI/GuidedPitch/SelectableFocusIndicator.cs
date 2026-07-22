using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    /// <summary>
    /// Keeps one high-contrast outline synchronized with keyboard/gamepad
    /// EventSystem selection and mouse hover, so every generated Selectable
    /// gives the same visual feedback for both input styles - previously only
    /// SentenceCardView's separate hand-rolled outline reacted to hover.
    /// </summary>
    public sealed class SelectableFocusIndicator : MonoBehaviour, ISelectHandler, IDeselectHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Selectable selectable;
        [SerializeField] private Outline indicator;

        private bool keyboardFocused;
        private bool hovered;

        public bool IsFocused { get; private set; }

        public void Configure(Selectable control, Outline focusOutline)
        {
            selectable = control ?? throw new ArgumentNullException(nameof(control));
            indicator = focusOutline ?? throw new ArgumentNullException(nameof(focusOutline));
            RefreshFromEventSystem();
        }

        public void OnSelect(BaseEventData eventData)
        {
            keyboardFocused = true;
            Refresh();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            keyboardFocused = false;
            Refresh();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            Refresh();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            Refresh();
        }

        private void OnEnable()
        {
            RefreshFromEventSystem();
        }

        private void OnDisable()
        {
            keyboardFocused = false;
            hovered = false;
            Refresh();
        }

        private void RefreshFromEventSystem()
        {
            keyboardFocused = selectable != null && EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == selectable.gameObject;
            Refresh();
        }

        private void Refresh()
        {
            var interactable = selectable != null && selectable.IsInteractable();
            IsFocused = interactable && (keyboardFocused || hovered);
            if (indicator != null)
            {
                indicator.enabled = IsFocused;
            }
        }
    }
}
