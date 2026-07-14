using System;
using Agrovator.PitchSimulator.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    public sealed class ResultsPresenter : MonoBehaviour
    {
        [SerializeField] private Text summaryText;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button retryButton;
        private PitchSessionController controller;
        private Action changed;
        private bool initialized;

        public void Initialize(PitchSessionController sessionController, Action onChanged)
        {
            if (sessionController == null) throw new ArgumentNullException(nameof(sessionController));
            submitButton.onClick.RemoveListener(Submit);
            retryButton.onClick.RemoveListener(Retry);
            controller = sessionController;
            changed = onChanged;
            submitButton.onClick.AddListener(Submit);
            retryButton.onClick.AddListener(Retry);
            initialized = true;
        }

        public void Refresh(PitchSessionSnapshot snapshot)
        {
            if (!initialized) return;
            summaryText.text = $"Pitch complete\nScore {snapshot.OverallScore}\nConfidence {snapshot.Confidence}";
            submitButton.interactable = snapshot.State == GameState.Results;
            retryButton.interactable = snapshot.State == GameState.Results || snapshot.State == GameState.Complete;
        }

        private void OnDestroy()
        {
            if (submitButton != null) submitButton.onClick.RemoveListener(Submit);
            if (retryButton != null) retryButton.onClick.RemoveListener(Retry);
        }

        private void Submit()
        {
            if (!initialized) return;
            controller.SubmitResults();
            changed?.Invoke();
        }

        private void Retry()
        {
            if (!initialized) return;
            controller.Retry();
            changed?.Invoke();
        }
    }
}
