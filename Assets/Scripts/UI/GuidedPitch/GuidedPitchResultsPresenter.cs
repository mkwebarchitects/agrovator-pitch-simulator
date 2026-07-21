using System;
using System.Globalization;
using Agrovator.PitchSimulator.Core;
using Agrovator.PitchSimulator.GuidedPitch;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    /// <summary>
    /// Statement-focused guided Results screen. It mirrors the final snapshot
    /// into four fixed part cards, the readable final pitch, honest improvement
    /// recognition, the transfer prompt, and the Submit/Resubmit/Retry actions.
    /// It never computes mastery or readiness itself: every displayed value
    /// comes from the domain <see cref="PitchAssessment"/> and draft snapshot,
    /// and it never shows scores, confidence, or plant-level labels.
    /// </summary>
    public sealed class GuidedPitchResultsPresenter : MonoBehaviour
    {
        private const int PartCount = 4;

        [SerializeField] private Text headingText;
        [SerializeField] private PitchResultPartView[] partViews = new PitchResultPartView[PartCount];
        [SerializeField] private Text readinessText;
        [SerializeField] private Image[] readinessSegments = Array.Empty<Image>();

        // Kept in step with GuidedPitchSceneBuilder.MeterFilled/MeterEmpty.
        private static readonly Color MeterFilled = new Color32(0xFF, 0xD1, 0x66, 0xFF);
        private static readonly Color MeterEmpty = new Color32(0x67, 0x6F, 0x77, 0xFF);
        [SerializeField] private Text improvementText;
        [SerializeField] private Text transferText;
        [SerializeField] private Text finalPitchHeadingText;
        [SerializeField] private Text finalPitchText;
        [SerializeField] private Text submissionStatusText;
        [SerializeField] private Button submitButton;
        [SerializeField] private Text submitButtonText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Text retryButtonText;
        [SerializeField] private ScrollRect resultsScroll;

        private GuidedPitchSessionController controller;
        private Action changed;
        private Func<string, string> localize;
        private int renderedAttempt = -1;
        private bool hasRenderedResult;
        private bool initialized;

        public void Initialize(GuidedPitchSessionController sessionController, Action onChanged,
            Func<string, string> localizeText)
        {
            if (sessionController == null) throw new ArgumentNullException(nameof(sessionController));
            if (localizeText == null) throw new ArgumentNullException(nameof(localizeText));
            if (!ValidateContract())
            {
                throw new InvalidOperationException("Guided results presenter references are incomplete.");
            }

            submitButton.onClick.RemoveListener(HandleSubmit);
            retryButton.onClick.RemoveListener(HandleRetry);
            controller = sessionController;
            changed = onChanged;
            localize = localizeText;
            renderedAttempt = -1;
            hasRenderedResult = false;
            submitButton.onClick.AddListener(HandleSubmit);
            retryButton.onClick.AddListener(HandleRetry);
            initialized = true;
        }

        public bool ValidateContract()
        {
            if (headingText == null || readinessText == null || improvementText == null ||
                transferText == null || finalPitchHeadingText == null || finalPitchText == null ||
                submissionStatusText == null || submitButton == null || submitButtonText == null ||
                retryButton == null || retryButtonText == null || resultsScroll == null ||
                resultsScroll.content == null || partViews == null || partViews.Length != PartCount)
            {
                return false;
            }

            for (var index = 0; index < PartCount; index++)
            {
                var view = partViews[index];
                if (view == null || !view.IsValid || view.Part != (PitchPart)index ||
                    !view.transform.IsChildOf(transform))
                {
                    return false;
                }
            }

            foreach (var component in new Component[]
            {
                headingText, readinessText, improvementText, transferText, finalPitchHeadingText,
                finalPitchText, submissionStatusText, submitButton, submitButtonText,
                retryButton, retryButtonText, resultsScroll,
            })
            {
                if (!component.transform.IsChildOf(transform))
                {
                    return false;
                }
            }

            return true;
        }

        public void Refresh(GuidedPitchSessionSnapshot snapshot)
        {
            if (!initialized)
            {
                return;
            }
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            headingText.text = localize("ui.results");
            transferText.text = localize("guided.transfer_prompt");
            finalPitchHeadingText.text = localize("guided.results.final_pitch");
            retryButtonText.text = localize("ui.retry");
            submitButtonText.text = localize(
                snapshot.SubmissionError != null ? "guided.results.resubmit" : "ui.submit_results");

            var phase = snapshot.Phase;
            var showResult = (phase == GuidedPitchPhase.Results ||
                phase == GuidedPitchPhase.Submitting ||
                phase == GuidedPitchPhase.Complete) && snapshot.Draft.IsComplete;
            if (showResult)
            {
                var isNewResult = !hasRenderedResult || renderedAttempt != snapshot.AttemptNumber;
                foreach (var part in PitchParts.Ordered)
                {
                    partViews[(int)part].Render(snapshot.Draft[part], localize);
                }

                readinessText.text = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}: {1}%",
                    localize("guided.pitch_readiness"),
                    snapshot.Assessment.PitchReadiness);
                RenderReadinessMeter(snapshot.Assessment.PitchReadiness);
                SetImprovement(snapshot.Assessment.ImprovedPartCount);
                finalPitchText.text = ComposeFinalPitch(snapshot);
                hasRenderedResult = true;
                renderedAttempt = snapshot.AttemptNumber;
                if (isNewResult)
                {
                    ResetScroll();
                }
            }
            else
            {
                ClearResult();
            }

            submissionStatusText.text = GetSubmissionStatus(snapshot);
            submitButton.interactable = phase == GuidedPitchPhase.Results;
            retryButton.interactable = phase == GuidedPitchPhase.Results ||
                phase == GuidedPitchPhase.Complete;
        }

        /// <summary>
        /// The router asks this presenter for the Results screen's own default
        /// focus target: Submit while resubmission is possible, Retry once the
        /// attempt is complete, and nothing while a submission is in flight.
        /// </summary>
        public Selectable GetDefaultSelectable(GuidedPitchPhase phase)
        {
            switch (phase)
            {
                case GuidedPitchPhase.Results:
                    return submitButton;
                case GuidedPitchPhase.Complete:
                    return retryButton;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Cycles Tab/Shift+Tab focus through this screen's active selectables
        /// (Submit and Retry), wrapping at both ends. The always-active router
        /// forwards Tab here while the Results screen is shown; during
        /// Submitting both actions are disabled, so the cycle is a safe no-op.
        /// </summary>
        public bool MoveFocus(bool backward)
        {
            return GuidedFocusCycle.MoveFocus(transform, backward);
        }

        private void OnDestroy()
        {
            if (submitButton != null) submitButton.onClick.RemoveListener(HandleSubmit);
            if (retryButton != null) retryButton.onClick.RemoveListener(HandleRetry);
        }

        /// <summary>
        /// Fills whole segments only, so the meter never implies a precision the
        /// readiness score does not carry. Applied instantly: there is no animation
        /// to gate, so the meter is identical with and without reduced motion.
        /// </summary>
        private void RenderReadinessMeter(int readiness)
        {
            if (readinessSegments == null || readinessSegments.Length == 0)
            {
                return;
            }

            var clamped = Mathf.Clamp(readiness, 0, 100);
            var filled = Mathf.RoundToInt(clamped / 100f * readinessSegments.Length);
            for (var index = 0; index < readinessSegments.Length; index++)
            {
                if (readinessSegments[index] == null) continue;
                readinessSegments[index].color = index < filled ? MeterFilled : MeterEmpty;
            }
        }

        private void SetImprovement(int improvedPartCount)
        {
            if (improvedPartCount <= 0)
            {
                improvementText.text = string.Empty;
                improvementText.gameObject.SetActive(false);
                return;
            }

            improvementText.text = improvedPartCount == 1
                ? localize("guided.results.strengthened.one")
                : localize("guided.results.strengthened.many").Replace(
                    "{count}", improvedPartCount.ToString(CultureInfo.InvariantCulture));
            improvementText.gameObject.SetActive(true);
        }

        private string ComposeFinalPitch(GuidedPitchSessionSnapshot snapshot)
        {
            return PitchPartVisuals.ComposeCurrentSentences(snapshot.Draft, localize);
        }

        private void ClearResult()
        {
            foreach (var view in partViews)
            {
                view.Clear();
            }

            readinessText.text = string.Empty;
            RenderReadinessMeter(0);
            improvementText.text = string.Empty;
            improvementText.gameObject.SetActive(false);
            finalPitchText.text = string.Empty;
            if (hasRenderedResult)
            {
                ResetScroll();
            }

            hasRenderedResult = false;
        }

        private void ResetScroll()
        {
            resultsScroll.verticalNormalizedPosition = 1f;
        }

        private string GetSubmissionStatus(GuidedPitchSessionSnapshot snapshot)
        {
            switch (snapshot.Phase)
            {
                case GuidedPitchPhase.Submitting:
                    return localize("lms.submission.submitting");
                case GuidedPitchPhase.Complete:
                    return localize("lms.submission.success");
                case GuidedPitchPhase.Results:
                    return snapshot.SubmissionError != null
                        ? localize(snapshot.SubmissionError.MessageKey)
                        : localize("lms.submission.ready");
                default:
                    return string.Empty;
            }
        }

        private void HandleSubmit()
        {
            if (!initialized)
            {
                return;
            }

            controller.SubmitResults();
            changed?.Invoke();
        }

        private void HandleRetry()
        {
            if (!initialized)
            {
                return;
            }

            controller.Retry();
            changed?.Invoke();
        }
    }
}
