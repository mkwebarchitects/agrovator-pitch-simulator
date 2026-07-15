using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Agrovator.PitchSimulator.Accessibility;
using Agrovator.PitchSimulator.GuidedPitch;
using Agrovator.PitchSimulator.LMS;

namespace Agrovator.PitchSimulator.Core
{
    public sealed class GuidedPitchSessionController : IDisposable
    {
        private readonly GuidedPitchContent content;
        private readonly AccessibilitySettings accessibility;
        private readonly ILmsBridge lmsBridge;
        private readonly Func<DateTimeOffset> utcNow;
        private readonly string gameVersion;
        private readonly PitchDraft draft = new PitchDraft();
        private readonly List<string> selectionHistory = new List<string>();

        private GuidedPitchPhase phase = GuidedPitchPhase.Booting;
        private LearnerMode? learnerMode;
        private PitchPart? activePart;
        private GuidedPitchFeedback feedback;
        private GuidedPitchOption followUpOption;
        private LmsLaunchConfig launch;
        private LmsCompletionPayload completionPayload;
        private LmsSubmissionError submissionError;
        private DateTimeOffset startedAt;
        private double durationSeconds;
        private int attemptNumber;
        private long submissionGeneration;
        private long activeSubmissionGeneration;
        private bool isDisposed;
        private GuidedPitchSessionSnapshot snapshot;

        public GuidedPitchSessionController(
            GuidedPitchContent content,
            AccessibilitySettings accessibilitySettings,
            ILmsBridge lmsBridge,
            Func<DateTimeOffset> utcNow,
            string gameVersion)
        {
            this.content = content ?? throw new ArgumentNullException(nameof(content));
            accessibility = accessibilitySettings ?? throw new ArgumentNullException(nameof(accessibilitySettings));
            this.lmsBridge = lmsBridge ?? throw new ArgumentNullException(nameof(lmsBridge));
            this.utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
            this.gameVersion = string.IsNullOrWhiteSpace(gameVersion)
                ? throw new ArgumentException("Game version is required.", nameof(gameVersion))
                : gameVersion;
            RefreshSnapshot();
        }

        public event Action<GuidedPitchSessionEvent> EventPublished;

        public GuidedPitchSessionSnapshot Snapshot => snapshot;

        public bool FinishLaunch()
        {
            if (isDisposed || phase != GuidedPitchPhase.Booting)
            {
                return false;
            }

            var candidate = lmsBridge.GetLaunchConfig();
            if (LmsPayloadValidator.ValidateLaunch(candidate).Count > 0 ||
                !string.Equals(candidate.ScenarioId, content.Id, StringComparison.Ordinal) ||
                candidate.ContentVersion != content.Version)
            {
                return false;
            }

            launch = candidate;
            attemptNumber = launch.AttemptNumber;
            phase = GuidedPitchPhase.Title;
            RefreshSnapshot();
            return true;
        }

        public bool StartScenario()
        {
            if (isDisposed || phase != GuidedPitchPhase.Title)
            {
                return false;
            }

            ResetRun();
            phase = GuidedPitchPhase.Briefing;
            RefreshSnapshot();
            return true;
        }

        public bool Continue()
        {
            if (isDisposed)
            {
                return false;
            }

            switch (phase)
            {
                case GuidedPitchPhase.Briefing:
                    phase = GuidedPitchPhase.ModeSelection;
                    break;
                case GuidedPitchPhase.Learn:
                    phase = GuidedPitchPhase.Build;
                    activePart = PitchPart.Problem;
                    break;
                case GuidedPitchPhase.BuildFeedback:
                    PublishFeedback();
                    feedback = null;
                    if (activePart == PitchPart.Value)
                    {
                        activePart = null;
                        phase = GuidedPitchPhase.Improve;
                    }
                    else
                    {
                        activePart = (PitchPart)((int)activePart.Value + 1);
                        phase = GuidedPitchPhase.Build;
                    }
                    break;
                case GuidedPitchPhase.Present:
                    phase = GuidedPitchPhase.FollowUp;
                    break;
                case GuidedPitchPhase.FollowUpFeedback:
                    PublishFeedback();
                    feedback = null;
                    phase = GuidedPitchPhase.Results;
                    completionPayload = BuildCompletionPayload();
                    RefreshSnapshot();
                    Publish(new GuidedPitchSessionEvent(GuidedPitchSessionEventType.ResultsReady));
                    return true;
                default:
                    return false;
            }

            RefreshSnapshot();
            return true;
        }

        public bool SelectLearnerMode(LearnerMode mode)
        {
            if (isDisposed || phase != GuidedPitchPhase.ModeSelection || !content.Modes.ContainsKey(mode))
            {
                return false;
            }

            learnerMode = mode;
            phase = GuidedPitchPhase.Learn;
            RefreshSnapshot();
            return true;
        }

        public bool SelectPitchResponse(string responseId)
        {
            if (isDisposed || phase != GuidedPitchPhase.Build ||
                !TryFindActivePartOption(responseId, out var option))
            {
                return false;
            }

            if (!draft.TrySelectInitial(activePart.Value, option.Id, option.Mastery))
            {
                return false;
            }

            feedback = option.Feedback;
            selectionHistory.Add(option.Id);
            phase = GuidedPitchPhase.BuildFeedback;
            RefreshSnapshot();
            PublishSelection(option);
            return true;
        }

        public bool BeginRevision(PitchPart part)
        {
            if (isDisposed || phase != GuidedPitchPhase.Improve || !IsKnownPart(part))
            {
                return false;
            }

            var section = draft.Snapshot[part];
            if (!section.IsPopulated || section.CurrentMastery == MasteryState.Clear)
            {
                return false;
            }

            activePart = part;
            feedback = null;
            RefreshSnapshot();
            return true;
        }

        public bool ReplacePitchResponse(string responseId)
        {
            if (isDisposed || phase != GuidedPitchPhase.Improve || !activePart.HasValue ||
                !TryFindActivePartOption(responseId, out var option))
            {
                return false;
            }

            if (!draft.TryRevise(activePart.Value, option.Id, option.Mastery))
            {
                return false;
            }

            feedback = option.Feedback;
            selectionHistory.Add(option.Id);
            RefreshSnapshot();
            PublishSelection(option);
            return true;
        }

        public bool PresentPitch()
        {
            if (isDisposed || phase != GuidedPitchPhase.Improve || !draft.Snapshot.IsComplete)
            {
                return false;
            }

            phase = GuidedPitchPhase.Present;
            activePart = null;
            feedback = null;
            RefreshSnapshot();
            return true;
        }

        public bool SelectFollowUpResponse(string responseId)
        {
            if (isDisposed || phase != GuidedPitchPhase.FollowUp || !learnerMode.HasValue)
            {
                return false;
            }

            var option = content.Modes[learnerMode.Value].FollowUp.Options.FirstOrDefault(
                candidate => string.Equals(candidate.Id, responseId, StringComparison.Ordinal));
            if (option == null)
            {
                return false;
            }

            followUpOption = option;
            feedback = option.Feedback;
            selectionHistory.Add(option.Id);
            phase = GuidedPitchPhase.FollowUpFeedback;
            RefreshSnapshot();
            PublishSelection(option);
            return true;
        }

        public bool SubmitResults()
        {
            if (isDisposed || phase != GuidedPitchPhase.Results || completionPayload == null)
            {
                return false;
            }

            submissionError = null;
            phase = GuidedPitchPhase.Submitting;
            var generation = ++submissionGeneration;
            activeSubmissionGeneration = generation;
            var submittedAttempt = attemptNumber;
            RefreshSnapshot();
            lmsBridge.SubmitCompletion(
                completionPayload,
                () => CompleteSubmissionSuccess(generation, submittedAttempt),
                error => CompleteSubmissionFailure(generation, submittedAttempt, error));
            return true;
        }

        public bool Retry()
        {
            if (isDisposed || (phase != GuidedPitchPhase.Results && phase != GuidedPitchPhase.Complete))
            {
                return false;
            }

            attemptNumber++;
            ResetRun();
            phase = GuidedPitchPhase.Briefing;
            RefreshSnapshot();
            return true;
        }

        public void Tick(double seconds)
        {
            if (seconds < 0d || double.IsNaN(seconds) || double.IsInfinity(seconds))
            {
                throw new ArgumentOutOfRangeException(nameof(seconds));
            }

            if (isDisposed)
            {
                return;
            }

            if (phase >= GuidedPitchPhase.Briefing && phase < GuidedPitchPhase.Results)
            {
                durationSeconds += seconds;
                RefreshSnapshot();
            }
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            activeSubmissionGeneration = 0;
            EventPublished = null;
        }

        private void ResetRun()
        {
            activeSubmissionGeneration = 0;
            learnerMode = null;
            activePart = null;
            feedback = null;
            followUpOption = null;
            draft.Reset();
            selectionHistory.Clear();
            completionPayload = null;
            submissionError = null;
            durationSeconds = 0d;
            startedAt = utcNow().ToUniversalTime();
        }

        private bool TryFindActivePartOption(string responseId, out GuidedPitchOption option)
        {
            option = null;
            if (!learnerMode.HasValue || !activePart.HasValue)
            {
                return false;
            }

            var part = content.Modes[learnerMode.Value].Parts.First(
                candidate => candidate.Part == activePart.Value);
            option = part.Options.FirstOrDefault(
                candidate => string.Equals(candidate.Id, responseId, StringComparison.Ordinal));
            return option != null;
        }

        private LmsCompletionPayload BuildCompletionPayload()
        {
            var assessment = PitchAssessmentBuilder.Build(draft.Snapshot);
            return new LmsCompletionPayload
            {
                PseudonymousLearnerId = launch.PseudonymousLearnerId,
                SessionId = launch.SessionId,
                CourseId = launch.CourseId,
                ModuleId = launch.ModuleId,
                LessonId = launch.LessonId,
                ScenarioId = content.Id,
                GameVersion = gameVersion,
                ContentVersion = content.Version,
                CompletionStatus = "completed",
                StartedAtUtc = FormatUtc(startedAt),
                CompletedAtUtc = FormatUtc(utcNow().ToUniversalTime()),
                DurationSeconds = durationSeconds,
                OverallScore = assessment.PitchReadiness,
                CompetencyScores = new[]
                {
                    Score("problem", assessment.ProblemClarity),
                    Score("evidence", assessment.EvidenceQuality),
                    Score("solution", assessment.SolutionFit),
                    Score("audience", assessment.AudienceValue),
                    Score("clear_explanation", assessment.ClearExplanation),
                    Score("communication", assessment.Communication),
                },
                FinalConfidence = BuildLegacyConfidence(),
                SelectedResponseIds = selectionHistory.ToArray(),
                TimeoutCount = 0,
                AttemptNumber = attemptNumber,
                RecommendedFollowUpLessonId = null,
            };
        }

        private int BuildLegacyConfidence()
        {
            var confidence = 50;
            foreach (var part in PitchParts.Ordered)
            {
                var responseId = draft.Snapshot[part].CurrentResponseId;
                var partContent = content.Modes[learnerMode.Value].Parts.First(candidate => candidate.Part == part);
                confidence += partContent.Options.First(option => option.Id == responseId).LegacyConfidenceDelta;
            }

            confidence += followUpOption.LegacyConfidenceDelta;
            return Math.Max(0, Math.Min(100, confidence));
        }

        private void CompleteSubmissionSuccess(long generation, int submittedAttempt)
        {
            if (!TryAcceptSubmissionCallback(generation, submittedAttempt))
            {
                return;
            }

            phase = GuidedPitchPhase.Complete;
            RefreshSnapshot();
            Publish(new GuidedPitchSessionEvent(GuidedPitchSessionEventType.SubmissionSucceeded));
        }

        private void CompleteSubmissionFailure(
            long generation,
            int submittedAttempt,
            LmsSubmissionError error)
        {
            if (!TryAcceptSubmissionCallback(generation, submittedAttempt))
            {
                return;
            }

            submissionError = error ?? new LmsSubmissionError(
                LmsSubmissionErrorCode.SubmissionFailed,
                "lms.submission.failed",
                submittedAttempt);
            phase = GuidedPitchPhase.Results;
            RefreshSnapshot();
            Publish(new GuidedPitchSessionEvent(
                GuidedPitchSessionEventType.SubmissionFailed,
                messageKey: submissionError.MessageKey));
        }

        private bool TryAcceptSubmissionCallback(long generation, int submittedAttempt)
        {
            if (isDisposed || activeSubmissionGeneration != generation ||
                attemptNumber != submittedAttempt || phase != GuidedPitchPhase.Submitting)
            {
                return false;
            }

            activeSubmissionGeneration = 0;
            return true;
        }

        private void RefreshSnapshot()
        {
            var draftSnapshot = draft.Snapshot;
            snapshot = new GuidedPitchSessionSnapshot(
                phase,
                learnerMode,
                activePart,
                draftSnapshot,
                PitchAssessmentBuilder.Build(draftSnapshot),
                GetAvailableOptions(),
                feedback,
                followUpOption?.Id,
                selectionHistory.AsReadOnly(),
                attemptNumber,
                accessibility.ReducedMotion,
                completionPayload,
                submissionError);
        }

        private IReadOnlyList<GuidedPitchOption> GetAvailableOptions()
        {
            if (!learnerMode.HasValue)
            {
                return Array.AsReadOnly(Array.Empty<GuidedPitchOption>());
            }

            if (phase == GuidedPitchPhase.Build || (phase == GuidedPitchPhase.Improve && activePart.HasValue))
            {
                return content.Modes[learnerMode.Value].Parts.First(
                    candidate => candidate.Part == activePart.Value).Options;
            }

            return phase == GuidedPitchPhase.FollowUp
                ? content.Modes[learnerMode.Value].FollowUp.Options
                : Array.AsReadOnly(Array.Empty<GuidedPitchOption>());
        }

        private void PublishSelection(GuidedPitchOption option)
        {
            Publish(new GuidedPitchSessionEvent(
                GuidedPitchSessionEventType.ResponseSelected,
                option.Id,
                option.ReactionCue));
        }

        private void PublishFeedback()
        {
            if (feedback != null)
            {
                Publish(new GuidedPitchSessionEvent(
                    GuidedPitchSessionEventType.FeedbackReady,
                    messageKey: feedback.ImproveKey));
            }
        }

        private void Publish(GuidedPitchSessionEvent sessionEvent)
        {
            EventPublished?.Invoke(sessionEvent);
        }

        private static bool IsKnownPart(PitchPart part)
        {
            return part >= PitchPart.Problem && part <= PitchPart.Value;
        }

        private static LmsCompetencyScore Score(string id, int value)
        {
            return new LmsCompetencyScore { CompetencyId = id, Score = value };
        }

        private static string FormatUtc(DateTimeOffset value)
        {
            return value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        }
    }
}
