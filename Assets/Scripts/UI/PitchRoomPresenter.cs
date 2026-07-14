using System;
using Agrovator.PitchSimulator.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    public sealed class PitchRoomPresenter : MonoBehaviour
    {
        [SerializeField] private Text promptText;
        [SerializeField] private Text statusText;
        [SerializeField] private ResponseListView responseList;
        [SerializeField] private TimerView timerView;
        [SerializeField] private ConfidenceView confidenceView;
        [SerializeField] private Button continueButton;

        private PitchSessionController controller;
        private Action changed;
        private Func<string, string> resolveText;
        private bool initialized;

        public void Initialize(
            PitchSessionController sessionController,
            Action onChanged,
            Func<string, string> localize = null)
        {
            if (sessionController == null) throw new ArgumentNullException(nameof(sessionController));
            RemoveListeners();
            controller = sessionController;
            changed = onChanged;
            resolveText = localize ?? (value => value);
            responseList.Initialize(Select);
            continueButton.onClick.AddListener(Continue);
            initialized = true;
        }

        public void Refresh(PitchSessionSnapshot snapshot)
        {
            if (!initialized) return;
            promptText.text = snapshot.CurrentNode?.TextKey ?? "Preparing your pitch...";
            statusText.text = $"Score {snapshot.OverallScore}";
            responseList.Render(
                snapshot.AvailableResponses,
                snapshot.State == GameState.AwaitingResponse,
                resolveText);
            timerView.Render(
                snapshot.TimerRemainingSeconds,
                snapshot.TimerTotalSeconds,
                snapshot.ReducedMotion);
            confidenceView.Render(snapshot.Confidence, resolveText);

            continueButton.gameObject.SetActive(snapshot.State != GameState.AwaitingResponse);
            continueButton.interactable = snapshot.State != GameState.AwaitingResponse;
            if (continueButton.gameObject.activeInHierarchy && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
            }
        }

        private void OnDestroy()
        {
            RemoveListeners();
        }

        private void RemoveListeners()
        {
            if (continueButton != null) continueButton.onClick.RemoveListener(Continue);
        }

        private void Select(string responseId)
        {
            if (!initialized || string.IsNullOrEmpty(responseId)) return;
            controller.SelectResponse(responseId);
            changed?.Invoke();
        }

        private void Continue()
        {
            if (!initialized) return;
            controller.Continue();
            changed?.Invoke();
        }
    }
}
