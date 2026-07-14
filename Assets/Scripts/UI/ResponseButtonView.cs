using System;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    public sealed class ResponseButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text label;

        private Action<int> selected;
        private int optionIndex;
        private bool initialized;

        public Button Button => button;

        public string DisplayText => label == null ? string.Empty : label.text;

        public void Configure(Button responseButton, Text responseLabel)
        {
            button = responseButton ?? throw new ArgumentNullException(nameof(responseButton));
            label = responseLabel ?? throw new ArgumentNullException(nameof(responseLabel));
        }

        public void Initialize(Action<int> onSelected)
        {
            if (button == null || label == null)
            {
                throw new InvalidOperationException("Response button references are incomplete.");
            }

            if (initialized)
            {
                button.onClick.RemoveListener(HandleSelected);
            }

            selected = onSelected ?? throw new ArgumentNullException(nameof(onSelected));
            button.onClick.AddListener(HandleSelected);
            initialized = true;
        }

        public void Show(int zeroBasedIndex, string responseText, bool interactable)
        {
            optionIndex = zeroBasedIndex;
            label.text = $"{zeroBasedIndex + 1}. {responseText ?? string.Empty}";
            gameObject.SetActive(true);
            button.interactable = interactable;
        }

        public void ClearAndHide()
        {
            optionIndex = -1;
            if (label != null) label.text = string.Empty;
            if (button != null) button.interactable = false;
            gameObject.SetActive(false);
        }

        public void SetInteractable(bool value)
        {
            if (button != null) button.interactable = value;
        }

        private void OnDestroy()
        {
            if (initialized && button != null)
            {
                button.onClick.RemoveListener(HandleSelected);
            }
        }

        private void HandleSelected()
        {
            selected?.Invoke(optionIndex);
        }
    }
}
