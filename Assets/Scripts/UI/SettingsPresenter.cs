using System;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    public sealed class SettingsPresenter : MonoBehaviour
    {
        [SerializeField] private Button closeButton;
        private Action close;
        private Action buttonPress;
        private bool initialized;

        public void Initialize(Action onClose, Action onButtonPress = null)
        {
            closeButton.onClick.RemoveListener(Close);
            close = onClose;
            buttonPress = onButtonPress;
            closeButton.onClick.AddListener(Close);
            initialized = true;
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }

        private void Close()
        {
            if (!initialized) return;
            buttonPress?.Invoke();
            close?.Invoke();
        }
    }
}
