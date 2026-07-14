using System;
using Agrovator.PitchSimulator.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    public sealed class TitlePresenter : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        private PitchSessionController controller;
        private Action changed;
        private Action openSettings;
        private Action userGesture;
        private bool initialized;

        public void Initialize(PitchSessionController sessionController, Action onChanged, Action onOpenSettings,
            Action onUserGesture = null)
        {
            if (sessionController == null) throw new ArgumentNullException(nameof(sessionController));
            startButton.onClick.RemoveListener(StartScenario);
            settingsButton.onClick.RemoveListener(OpenSettings);
            controller = sessionController;
            changed = onChanged;
            openSettings = onOpenSettings;
            userGesture = onUserGesture;
            startButton.onClick.AddListener(StartScenario);
            settingsButton.onClick.AddListener(OpenSettings);
            initialized = true;
        }

        public void Refresh(PitchSessionSnapshot snapshot)
        {
            if (!initialized) return;
            startButton.interactable = snapshot.State == GameState.Title;
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
            controller.StartScenario();
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
