using System;
using Agrovator.PitchSimulator.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    public sealed class ResultsPresenter : MonoBehaviour
    {
        [SerializeField] private Text headingText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text overallText;
        [SerializeField] private Text confidenceText;
        [SerializeField] private Text pitchingText;
        [SerializeField] private Text communicationsText;
        [SerializeField] private Text strengthsHeadingText;
        [SerializeField] private Text[] strengthTexts = Array.Empty<Text>();
        [SerializeField] private Text improvementsHeadingText;
        [SerializeField] private Text[] improvementTexts = Array.Empty<Text>();
        [SerializeField] private Text reviewHeadingText;
        [SerializeField] private ScrollRect reviewScroll;
        [SerializeField] private QuestionReviewItemView[] reviewItems = Array.Empty<QuestionReviewItemView>();
        [SerializeField] private Text submissionStatusText;
        [SerializeField] private Button submitButton;
        [SerializeField] private Text submitButtonText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Text retryButtonText;

        private PitchSessionController controller;
        private Action changed;
        private Func<string, string> localize;
        private object renderedResultIdentity;
        private int renderedAttempt = -1;
        private bool initialized;

        public void Initialize(
            PitchSessionController sessionController,
            Action onChanged,
            Func<string, string> localizationResolver)
        {
            if (sessionController == null) throw new ArgumentNullException(nameof(sessionController));
            if (localizationResolver == null) throw new ArgumentNullException(nameof(localizationResolver));
            if (!ValidateContract()) throw new InvalidOperationException("Results presenter references are incomplete.");

            submitButton.onClick.RemoveListener(Submit);
            retryButton.onClick.RemoveListener(Retry);
            controller = sessionController;
            changed = onChanged;
            localize = localizationResolver;
            submitButton.onClick.AddListener(Submit);
            retryButton.onClick.AddListener(Retry);
            initialized = true;
        }

        public bool ValidateContract()
        {
            if (headingText == null || levelText == null || overallText == null || confidenceText == null ||
                pitchingText == null || communicationsText == null || strengthsHeadingText == null ||
                improvementsHeadingText == null || reviewHeadingText == null || submissionStatusText == null ||
                submitButton == null || submitButtonText == null || retryButton == null || retryButtonText == null ||
                strengthTexts == null || strengthTexts.Length != 2 ||
                improvementTexts == null || improvementTexts.Length != 2 ||
                reviewScroll == null || reviewScroll.content == null || !reviewScroll.vertical ||
                reviewScroll.horizontal || !(reviewScroll.verticalScrollbar is KeyboardReviewScrollbar) ||
                reviewItems == null || reviewItems.Length != 6)
            {
                return false;
            }

            foreach (var component in new Component[]
            {
                headingText,
                levelText,
                overallText,
                confidenceText,
                pitchingText,
                communicationsText,
                strengthsHeadingText,
                improvementsHeadingText,
                reviewHeadingText,
                reviewScroll,
                reviewScroll.verticalScrollbar,
                submissionStatusText,
                submitButton,
                submitButtonText,
                retryButton,
                retryButtonText,
            })
            {
                if (!component.transform.IsChildOf(transform)) return false;
            }

            foreach (var text in strengthTexts)
            {
                if (text == null || !text.transform.IsChildOf(transform)) return false;
            }
            foreach (var text in improvementTexts)
            {
                if (text == null || !text.transform.IsChildOf(transform)) return false;
            }
            foreach (var item in reviewItems)
            {
                if (item == null || !item.transform.IsChildOf(transform) || !item.ValidateContract()) return false;
            }

            return true;
        }

        public void Refresh(PitchSessionSnapshot snapshot)
        {
            if (!initialized || snapshot == null) return;

            headingText.text = localize("ui.results");
            strengthsHeadingText.text = localize("ui.strengths");
            improvementsHeadingText.text = localize("ui.improvements");
            reviewHeadingText.text = localize("ui.review");
            submitButtonText.text = localize("ui.submit_results");
            retryButtonText.text = localize("ui.retry");

            if (snapshot.Result == null)
            {
                ClearResult();
                renderedResultIdentity = null;
                renderedAttempt = snapshot.AttemptNumber;
                ResetReviewScroll();
            }
            else
            {
                var result = snapshot.Result;
                var isNewResult = !ReferenceEquals(renderedResultIdentity, result) ||
                    renderedAttempt != snapshot.AttemptNumber;
                levelText.text = localize(result.LevelLocalizationKey);
                overallText.text = $"{localize("ui.overall_score")} {result.OverallScore}";
                confidenceText.text = $"{localize("ui.final_confidence")} {snapshot.Confidence}";
                pitchingText.text = $"{localize("ui.pitching")} {result.PitchingScore}";
                communicationsText.text = $"{localize("ui.communications")} {result.CommunicationsScore}";
                RefreshList(strengthTexts, result.StrengthKeys);
                RefreshList(improvementTexts, result.ImprovementKeys);
                RefreshReview(snapshot);
                renderedResultIdentity = result;
                renderedAttempt = snapshot.AttemptNumber;
                if (isNewResult) ResetReviewScroll();
            }

            submissionStatusText.text = GetSubmissionStatus(snapshot);
            submitButton.interactable = snapshot.State == GameState.Results;
            retryButton.interactable = snapshot.State == GameState.Results || snapshot.State == GameState.Complete;
        }

        private void OnDestroy()
        {
            if (submitButton != null) submitButton.onClick.RemoveListener(Submit);
            if (retryButton != null) retryButton.onClick.RemoveListener(Retry);
        }

        private void RefreshList(Text[] fields, System.Collections.Generic.IReadOnlyList<string> keys)
        {
            for (var index = 0; index < fields.Length; index++)
            {
                fields[index].text = index < keys.Count ? localize(keys[index]) : string.Empty;
                fields[index].gameObject.SetActive(index < keys.Count);
            }
        }

        private void RefreshReview(PitchSessionSnapshot snapshot)
        {
            for (var index = 0; index < reviewItems.Length; index++)
            {
                if (index < snapshot.ReviewHistory.Count)
                {
                    reviewItems[index].Refresh(snapshot.ReviewHistory[index], localize);
                }
                else
                {
                    reviewItems[index].Clear();
                }
            }
        }

        private void ClearResult()
        {
            levelText.text = string.Empty;
            overallText.text = string.Empty;
            confidenceText.text = string.Empty;
            pitchingText.text = string.Empty;
            communicationsText.text = string.Empty;
            RefreshList(strengthTexts, Array.Empty<string>());
            RefreshList(improvementTexts, Array.Empty<string>());
            foreach (var item in reviewItems) item.Clear();
        }

        private void ResetReviewScroll()
        {
            reviewScroll.verticalNormalizedPosition = 1f;
        }

        private string GetSubmissionStatus(PitchSessionSnapshot snapshot)
        {
            if (snapshot.State == GameState.Submitting) return localize("lms.submission.submitting");
            if (snapshot.State == GameState.Complete) return localize("lms.submission.success");
            if (snapshot.SubmissionError != null) return localize(snapshot.SubmissionError.MessageKey);
            return snapshot.State == GameState.Results ? localize("lms.submission.ready") : string.Empty;
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
