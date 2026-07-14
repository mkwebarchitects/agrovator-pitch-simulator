using System;
using Agrovator.PitchSimulator.Core;
using Agrovator.PitchSimulator.Dialogue;
using UnityEngine;
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
        [SerializeField] private JudgeReactionView judgeReactionView;
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
            var showingOutcome = snapshot.State == GameState.ShowingReaction ||
                snapshot.State == GameState.ShowingFeedback;
            var promptKey = snapshot.State == GameState.ShowingReaction
                ? snapshot.LastFeedbackKey
                : snapshot.State == GameState.ShowingFeedback
                    ? snapshot.LastExplanationKey
                    : snapshot.CurrentNode?.TextKey;
            promptText.text = string.IsNullOrEmpty(promptKey)
                ? "Preparing your pitch..."
                : resolveText(promptKey);
            statusText.text = $"Score {snapshot.OverallScore}";
            responseList.Render(
                showingOutcome
                    ? Array.Empty<RuntimeResponseOption>()
                    : snapshot.AvailableResponses,
                snapshot.State == GameState.AwaitingResponse,
                resolveText);
            RefreshTimer(snapshot);
            confidenceView.Render(snapshot.Confidence, resolveText);
            if (judgeReactionView != null)
            {
                var questionVisible = snapshot.State == GameState.AskingQuestion ||
                    snapshot.State == GameState.AwaitingResponse;
                judgeReactionView.Render(
                    snapshot.LastReactionCue,
                    questionVisible,
                    snapshot.State == GameState.ShowingReaction,
                    snapshot.ReducedMotion);
            }

            continueButton.gameObject.SetActive(snapshot.State != GameState.AwaitingResponse);
            continueButton.interactable = snapshot.State != GameState.AwaitingResponse;
        }

        public void RefreshTimer(PitchSessionSnapshot snapshot)
        {
            if (!initialized || snapshot == null) return;
            timerView.Render(
                snapshot.TimerRemainingSeconds,
                snapshot.TimerTotalSeconds,
                snapshot.ReducedMotion,
                snapshot.State == GameState.AwaitingResponse);
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
