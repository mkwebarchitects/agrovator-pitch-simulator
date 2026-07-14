using System;
using System.Collections.Generic;
using System.Globalization;
using Agrovator.PitchSimulator.Accessibility;
using Agrovator.PitchSimulator.Dialogue;
using Agrovator.PitchSimulator.LMS;
using Agrovator.PitchSimulator.Scoring;

namespace Agrovator.PitchSimulator.Core
{
    public sealed class PitchSessionController : IDisposable
    {
        private const string TimeoutReactionCue = "Neutral";
        private const string TimeoutFeedbackKey = "session.timeout.feedback";
        private const string TimeoutExplanationKey = "session.timeout.explanation";

        private readonly RuntimeScenario scenario;
        private readonly ScoreAccumulator scores;
        private readonly AccessibilitySettings accessibility;
        private readonly QuestionTimer timer;
        private readonly ILmsBridge lmsBridge;
        private readonly Func<DateTimeOffset> utcNow;
        private readonly string gameVersion;
        private readonly GameStateMachine stateMachine = new GameStateMachine(GameState.Booting);
        private readonly List<string> selectedResponseIds = new List<string>();

        private DialogueSession dialogue;
        private ConfidenceMeter confidence;
        private LmsLaunchConfig launch;
        private ResultSummary result;
        private LmsCompletionPayload completionPayload;
        private LmsSubmissionError submissionError;
        private DateTimeOffset startedAt;
        private double durationSeconds;
        private int timeoutCount;
        private int attemptNumber;
        private long submissionGeneration;
        private long activeSubmissionGeneration;
        private bool isDisposed;
        private string lastResponseId;
        private string lastReactionCue;
        private string lastFeedbackKey;
        private string lastExplanationKey;

        public PitchSessionController(
            RuntimeScenario scenario,
            ScoreAccumulator scoreService,
            AccessibilitySettings accessibilitySettings,
            QuestionTimer questionTimer,
            ILmsBridge lmsBridge,
            Func<DateTimeOffset> utcNow,
            string gameVersion)
        {
            this.scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
            scores = scoreService ?? throw new ArgumentNullException(nameof(scoreService));
            accessibility = accessibilitySettings ?? throw new ArgumentNullException(nameof(accessibilitySettings));
            timer = questionTimer ?? throw new ArgumentNullException(nameof(questionTimer));
            this.lmsBridge = lmsBridge ?? throw new ArgumentNullException(nameof(lmsBridge));
            this.utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
            this.gameVersion = string.IsNullOrWhiteSpace(gameVersion)
                ? throw new ArgumentException("Game version is required.", nameof(gameVersion))
                : gameVersion;

            dialogue = new DialogueSession(scenario);
            confidence = new ConfidenceMeter(scenario.InitialConfidence);
            timer.Expired += HandleTimeout;
            RefreshSnapshot();
        }

        public event Action<PitchSessionEvent> EventPublished;

        public PitchSessionSnapshot Snapshot { get; private set; }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            activeSubmissionGeneration = 0;
            timer.Expired -= HandleTimeout;
            EventPublished = null;
        }

        public bool FinishLaunch()
        {
            if (isDisposed || stateMachine.Current != GameState.Booting)
            {
                return false;
            }

            var candidate = lmsBridge.GetLaunchConfig();
            if (LmsPayloadValidator.ValidateLaunch(candidate).Count > 0 ||
                !string.Equals(candidate.ScenarioId, scenario.Id, StringComparison.Ordinal) ||
                candidate.ContentVersion != scenario.Version)
            {
                return false;
            }

            launch = candidate;
            attemptNumber = launch.AttemptNumber;
            var applied = stateMachine.TryApply(GameCommand.FinishBooting);
            RefreshSnapshot();
            return applied;
        }

        public bool StartScenario()
        {
            if (isDisposed || !stateMachine.TryApply(GameCommand.StartScenario))
            {
                return false;
            }

            ResetRun();
            RefreshSnapshot();
            return true;
        }

        public bool Continue()
        {
            if (isDisposed)
            {
                return false;
            }

            var current = stateMachine.Current;
            if (current == GameState.ShowingFeedback)
            {
                return ContinueAfterFeedback();
            }

            if (!stateMachine.TryApply(GameCommand.Continue))
            {
                return false;
            }

            if (current == GameState.AskingQuestion)
            {
                var isTutorial = string.Equals(dialogue.CurrentNode.NodeType, "Tutorial", StringComparison.Ordinal);
                var duration = accessibility.GetEffectiveDuration(dialogue.CurrentNode.TimerSeconds, isTutorial);
                timer.Reset(duration);
            }

            RefreshSnapshot();
            if (current == GameState.ShowingReaction)
            {
                Publish(new PitchSessionEvent(
                    PitchSessionEventType.FeedbackReady,
                    lastResponseId,
                    lastReactionCue,
                    lastFeedbackKey,
                    lastExplanationKey));
            }
            return true;
        }

        public bool SelectResponse(string responseId)
        {
            if (isDisposed || stateMachine.Current != GameState.AwaitingResponse)
            {
                return false;
            }

            var isTutorial = string.Equals(dialogue.CurrentNode.NodeType, "Tutorial", StringComparison.Ordinal);
            var selection = dialogue.Select(responseId, confidence.Value);
            if (!selection.IsAccepted || !stateMachine.TryApply(GameCommand.SelectResponse))
            {
                return false;
            }

            timer.Pause();
            var response = selection.SelectedResponse;
            if (!isTutorial)
            {
                scores.Apply(response.ScoreDelta, response.CompetencyTags);
                confidence.Apply(response.ConfidenceDelta);
                selectedResponseIds.Add(response.Id);
            }

            lastResponseId = response.Id;
            lastReactionCue = response.ReactionCue;
            lastFeedbackKey = response.FeedbackKey;
            lastExplanationKey = response.ExplanationKey;
            RefreshSnapshot();
            Publish(new PitchSessionEvent(
                PitchSessionEventType.ReactionReady,
                response.Id,
                response.ReactionCue,
                response.FeedbackKey,
                response.ExplanationKey));
            return true;
        }

        public void Tick(double seconds)
        {
            if (isDisposed)
            {
                return;
            }

            if (seconds < 0d || double.IsNaN(seconds) || double.IsInfinity(seconds))
            {
                throw new ArgumentOutOfRangeException(nameof(seconds));
            }

            if (stateMachine.Current >= GameState.Briefing && stateMachine.Current < GameState.Results)
            {
                durationSeconds += seconds;
            }

            if (stateMachine.Current == GameState.AwaitingResponse)
            {
                timer.Tick(seconds);
            }

            RefreshSnapshot();
        }

        public bool SubmitResults()
        {
            if (isDisposed || completionPayload == null || !stateMachine.TryApply(GameCommand.SubmitResults))
            {
                return false;
            }

            submissionError = null;
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
            if (isDisposed || !stateMachine.TryApply(GameCommand.Retry))
            {
                return false;
            }

            attemptNumber++;
            ResetRun();
            RefreshSnapshot();
            return true;
        }

        private bool ContinueAfterFeedback()
        {
            if (dialogue.IsComplete)
            {
                if (!stateMachine.TryApply(GameCommand.FinishScenario))
                {
                    return false;
                }

                result = new ResultBuilder().Build(
                    scores,
                    dialogue.HasFlag("recovered_after_weak_answer"));
                completionPayload = BuildCompletionPayload();
                RefreshSnapshot();
                Publish(new PitchSessionEvent(PitchSessionEventType.ResultsReady));
                return true;
            }

            if (!stateMachine.TryApply(GameCommand.Continue))
            {
                return false;
            }

            ClearPresentationOutcome();
            RefreshSnapshot();
            return true;
        }

        private void HandleTimeout()
        {
            if (isDisposed || stateMachine.Current != GameState.AwaitingResponse)
            {
                return;
            }

            var selection = SelectTimeoutTraversal(preferDeveloping: true);
            if (selection == null)
            {
                selection = SelectTimeoutTraversal(preferDeveloping: false);
            }

            if (!stateMachine.TryApply(GameCommand.SelectResponse))
            {
                return;
            }

            timeoutCount++;
            lastResponseId = null;
            lastReactionCue = TimeoutReactionCue;
            lastFeedbackKey = TimeoutFeedbackKey;
            lastExplanationKey = TimeoutExplanationKey;
            RefreshSnapshot();
            Publish(new PitchSessionEvent(
                PitchSessionEventType.TimeoutReactionReady,
                reactionCue: TimeoutReactionCue,
                feedbackKey: TimeoutFeedbackKey,
                explanationKey: TimeoutExplanationKey));
        }

        private DialogueSelectionResult SelectTimeoutTraversal(bool preferDeveloping)
        {
            foreach (var response in dialogue.CurrentNode.Responses)
            {
                var isDeveloping = string.Equals(response.QualityTier, "Developing", StringComparison.Ordinal);
                if (isDeveloping != preferDeveloping)
                {
                    continue;
                }

                var selection = dialogue.SelectForTimeout(response.Id, confidence.Value);
                if (selection.IsAccepted)
                {
                    return selection;
                }
            }

            return null;
        }

        private void ResetRun()
        {
            activeSubmissionGeneration = 0;
            scores.Reset();
            dialogue = new DialogueSession(scenario);
            confidence = new ConfidenceMeter(scenario.InitialConfidence);
            selectedResponseIds.Clear();
            timeoutCount = 0;
            durationSeconds = 0d;
            result = null;
            completionPayload = null;
            submissionError = null;
            startedAt = utcNow().ToUniversalTime();
            timer.Reset(0d);
            ClearPresentationOutcome();
        }

        private void CompleteSubmissionSuccess(long generation, int submittedAttempt)
        {
            if (!TryAcceptSubmissionCallback(generation, submittedAttempt))
            {
                return;
            }

            stateMachine.TryApply(GameCommand.SubmissionSucceeded);
            RefreshSnapshot();
            Publish(new PitchSessionEvent(PitchSessionEventType.SubmissionSucceeded));
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
            stateMachine.TryApply(GameCommand.SubmissionFailed);
            RefreshSnapshot();
            Publish(new PitchSessionEvent(
                PitchSessionEventType.SubmissionFailed,
                messageKey: submissionError.MessageKey));
        }

        private bool TryAcceptSubmissionCallback(long generation, int submittedAttempt)
        {
            if (activeSubmissionGeneration != generation ||
                isDisposed ||
                attemptNumber != submittedAttempt ||
                stateMachine.Current != GameState.Submitting)
            {
                return false;
            }

            activeSubmissionGeneration = 0;
            return true;
        }

        private void ClearPresentationOutcome()
        {
            lastResponseId = null;
            lastReactionCue = null;
            lastFeedbackKey = null;
            lastExplanationKey = null;
        }

        private LmsCompletionPayload BuildCompletionPayload()
        {
            var competencyScores = new LmsCompetencyScore[ScoreAccumulator.Categories.Count];
            for (var index = 0; index < ScoreAccumulator.Categories.Count; index++)
            {
                var category = ScoreAccumulator.Categories[index];
                competencyScores[index] = new LmsCompetencyScore
                {
                    CompetencyId = GetCompetencyId(category),
                    Score = scores[category] * 100 / ScoreAccumulator.GetMaximum(category),
                };
            }

            return new LmsCompletionPayload
            {
                PseudonymousLearnerId = launch.PseudonymousLearnerId,
                SessionId = launch.SessionId,
                CourseId = launch.CourseId,
                ModuleId = launch.ModuleId,
                LessonId = launch.LessonId,
                ScenarioId = scenario.Id,
                GameVersion = gameVersion,
                ContentVersion = scenario.Version,
                CompletionStatus = "completed",
                StartedAtUtc = FormatUtc(startedAt),
                CompletedAtUtc = FormatUtc(utcNow().ToUniversalTime()),
                DurationSeconds = durationSeconds,
                OverallScore = scores.OverallScore,
                CompetencyScores = competencyScores,
                FinalConfidence = confidence.Value,
                SelectedResponseIds = selectedResponseIds.ToArray(),
                TimeoutCount = timeoutCount,
                AttemptNumber = attemptNumber,
                RecommendedFollowUpLessonId = null,
            };
        }

        private void RefreshSnapshot()
        {
            var available = dialogue == null
                ? Array.AsReadOnly(Array.Empty<RuntimeResponseOption>())
                : dialogue.GetAvailableResponses(confidence.Value);
            Snapshot = new PitchSessionSnapshot(
                stateMachine.Current,
                dialogue?.CurrentNode,
                available,
                scores.OverallScore,
                confidence.Value,
                timeoutCount,
                attemptNumber,
                selectedResponseIds.AsReadOnly(),
                lastResponseId,
                lastReactionCue,
                lastFeedbackKey,
                lastExplanationKey,
                result,
                completionPayload,
                submissionError);
        }

        private void Publish(PitchSessionEvent sessionEvent)
        {
            EventPublished?.Invoke(sessionEvent);
        }

        private static string FormatUtc(DateTimeOffset value)
        {
            return value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        }

        private static string GetCompetencyId(ScoreCategory category)
        {
            switch (category)
            {
                case ScoreCategory.ClearExplanation:
                    return "clear_explanation";
                case ScoreCategory.Problem:
                    return "problem";
                case ScoreCategory.Solution:
                    return "solution";
                case ScoreCategory.Audience:
                    return "audience";
                case ScoreCategory.Evidence:
                    return "evidence";
                case ScoreCategory.Communication:
                    return "communication";
                case ScoreCategory.TimeManagement:
                    return "time_management";
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown score category.");
            }
        }
    }
}
