using System;
using Agrovator.PitchSimulator.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    public sealed class QuestionReviewItemView : MonoBehaviour
    {
        [SerializeField] private Text responseLabel;
        [SerializeField] private Text responseText;
        [SerializeField] private Text feedbackLabel;
        [SerializeField] private Text feedbackText;
        [SerializeField] private Text explanationLabel;
        [SerializeField] private Text explanationText;

        public string ResponseText => responseText == null ? string.Empty : responseText.text;

        public string FeedbackText => feedbackText == null ? string.Empty : feedbackText.text;

        public string ExplanationText => explanationText == null ? string.Empty : explanationText.text;

        public bool ValidateContract()
        {
            return responseLabel != null && responseText != null && feedbackLabel != null &&
                feedbackText != null && explanationLabel != null && explanationText != null &&
                responseLabel.transform.IsChildOf(transform) &&
                responseText.transform.IsChildOf(transform) &&
                feedbackLabel.transform.IsChildOf(transform) &&
                feedbackText.transform.IsChildOf(transform) &&
                explanationLabel.transform.IsChildOf(transform) &&
                explanationText.transform.IsChildOf(transform);
        }

        public void Refresh(PitchReviewEntry entry, Func<string, string> localize)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }
            if (localize == null)
            {
                throw new ArgumentNullException(nameof(localize));
            }
            if (!ValidateContract())
            {
                throw new InvalidOperationException("Question review item references are incomplete.");
            }

            responseLabel.text = localize("ui.selected_response");
            responseText.text = localize(entry.ResponseDisplayKey);
            feedbackLabel.text = localize("ui.feedback");
            feedbackText.text = localize(entry.FeedbackKey);
            explanationLabel.text = localize("ui.stronger_answer");
            explanationText.text = localize(entry.ExplanationKey);
            gameObject.SetActive(true);
        }

        public void Clear()
        {
            if (responseLabel != null) responseLabel.text = string.Empty;
            if (responseText != null) responseText.text = string.Empty;
            if (feedbackLabel != null) feedbackLabel.text = string.Empty;
            if (feedbackText != null) feedbackText.text = string.Empty;
            if (explanationLabel != null) explanationLabel.text = string.Empty;
            if (explanationText != null) explanationText.text = string.Empty;
            gameObject.SetActive(false);
        }
    }
}
