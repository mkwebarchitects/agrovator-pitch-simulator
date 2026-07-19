using System;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    /// <summary>
    /// Title screen binding expressed as callbacks so it depends on neither the
    /// legacy nor the guided session controller.
    /// </summary>
    public sealed class TitlePresenter : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        private Action start;
        private Action changed;
        private Action openSettings;
        private Action userGesture;
        private bool initialized;

        public void Initialize(Action start, Action changed, Action openSettings,
            Action onUserGesture = null)
        {
            if (start == null) throw new ArgumentNullException(nameof(start));
            startButton.onClick.RemoveListener(StartScenario);
            settingsButton.onClick.RemoveListener(OpenSettings);
            this.start = start;
            this.changed = changed;
            this.openSettings = openSettings;
            userGesture = onUserGesture;
            startButton.onClick.AddListener(StartScenario);
            settingsButton.onClick.AddListener(OpenSettings);
            initialized = true;
        }

        public void SetStartInteractable(bool value)
        {
            if (!initialized) return;
            startButton.interactable = value;
        }

        private void OnDestroy()
        {
            if (startButton != null) startButton.onClick.RemoveListener(StartScenario);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OpenSettings);
        }

        private void StartScenario()
        {
            if (!initialized) return;
            userGesture?.Invoke();
            start();
            changed?.Invoke();
        }

        private void OpenSettings()
        {
            if (!initialized) return;
            userGesture?.Invoke();
            openSettings?.Invoke();
        }
    }
}
