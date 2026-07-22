using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    public enum SentenceCardVisualState
    {
        Normal,
        Hover,
        Selected,
        Disabled,
        KeyboardFocus,
    }

    public sealed class SentenceCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Text label;
        [SerializeField] private Image background;
        [SerializeField] private Image focusOutline;

        private Action<SentenceCardView> selected;
        private bool listening;
        private bool hovered;
        private bool focused;
        private bool isSelected;

        public Button Button => button;
        public Text Label => label;
        public Image Background => background;
        public string ResponseId { get; private set; }
        public SentenceCardVisualState State { get; private set; }
        public float TargetHeight => transform is RectTransform rect ? rect.rect.height : 0f;

        public void Configure(Button cardButton, Text cardLabel, Image cardBackground, Image cardFocusOutline)
        {
            RemoveClickListener();
            button = cardButton ?? throw new ArgumentNullException(nameof(cardButton));
            label = cardLabel ?? throw new ArgumentNullException(nameof(cardLabel));
            background = cardBackground ?? throw new ArgumentNullException(nameof(cardBackground));
            focusOutline = cardFocusOutline ?? throw new ArgumentNullException(nameof(cardFocusOutline));
            button.targetGraphic = background;
            ApplyBaseStyle();
        }

        internal void Show(string responseId, string text, bool selectedValue, bool interactable,
            Action<SentenceCardView> onSelected)
        {
            ValidateReferences();
            RemoveClickListener();
            ResponseId = responseId;
            label.text = text ?? string.Empty;
            selected = onSelected;
            isSelected = selectedValue;
            hovered = false;
            focused = false;
            button.interactable = interactable;
            button.onClick.AddListener(HandleClick);
            listening = true;
            gameObject.SetActive(true);
            RefreshState();
        }

        internal void ClearAndHide()
        {
            RemoveClickListener();
            selected = null;
            ResponseId = null;
            isSelected = false;
            hovered = false;
            focused = false;
            if (label != null) label.text = string.Empty;
            //if (button != null)
            //{
            //    button.interactable = false;
            //    var navigation = button.navigation;
            //    navigation.mode = Navigation.Mode.Explicit;
            //    navigation.selectOnUp = null;
            //    navigation.selectOnDown = null;
            //    navigation.selectOnLeft = null;
            //    navigation.selectOnRight = null;
            //    button.navigation = navigation;
            //}
            if (focusOutline != null) focusOutline.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        internal void SetInteractable(bool value)
        {
            if (button != null) button.interactable = value;
            RefreshState();
        }

        internal void SetSelected(bool value)
        {
            isSelected = value;
            RefreshState();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            RefreshState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            RefreshState();
        }

        public void OnSelect(BaseEventData eventData)
        {
            focused = true;
            RefreshState();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            focused = false;
            RefreshState();
        }

        private void OnDestroy()
        {
            RemoveClickListener();
        }

        private void HandleClick()
        {
            selected?.Invoke(this);
        }

        private void RefreshState()
        {
            if (background == null || button == null) return;
            State = isSelected
                ? SentenceCardVisualState.Selected
                : !button.interactable
                    ? SentenceCardVisualState.Disabled
                    : focused
                        ? SentenceCardVisualState.KeyboardFocus
                        : hovered
                            ? SentenceCardVisualState.Hover
                            : SentenceCardVisualState.Normal;

            background.color = PitchPartVisuals.CardCream;
            if (label != null) label.color = PitchPartVisuals.CardText;
            if (focusOutline != null)
            {
                focusOutline.color = PitchPartVisuals.FocusGold;
                focusOutline.raycastTarget = false;
                focusOutline.gameObject.SetActive(State == SentenceCardVisualState.Hover ||
                    State == SentenceCardVisualState.Selected || State == SentenceCardVisualState.KeyboardFocus);
            }
        }

        private void ApplyBaseStyle()
        {
            var colours = button.colors;
            colours.normalColor = Color.white;
            colours.highlightedColor = Color.white;
            colours.selectedColor = Color.white;
            colours.pressedColor = Color.white;
            colours.disabledColor = new Color(1f, 1f, 1f, 0.68f);
            button.colors = colours;
            background.color = PitchPartVisuals.CardCream;
            label.color = PitchPartVisuals.CardText;
            focusOutline.color = PitchPartVisuals.FocusGold;
            focusOutline.raycastTarget = false;
            focusOutline.gameObject.SetActive(false);
            State = SentenceCardVisualState.Normal;
        }

        private void ValidateReferences()
        {
            if (button == null || label == null || background == null || focusOutline == null ||
                button.targetGraphic != background)
            {
                throw new InvalidOperationException("Sentence card references are incomplete.");
            }
        }

        private void RemoveClickListener()
        {
            if (listening && button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
            listening = false;
        }
    }
}
