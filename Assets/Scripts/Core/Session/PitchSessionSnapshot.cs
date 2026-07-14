using System;
using System.Collections.Generic;
using Agrovator.PitchSimulator.Dialogue;
using Agrovator.PitchSimulator.LMS;
using Agrovator.PitchSimulator.Scoring;

namespace Agrovator.PitchSimulator.Core
{
    public sealed class PitchSessionSnapshot
    {
        private readonly LmsCompletionPayload completionPayload;

        internal PitchSessionSnapshot(
            GameState state,
            RuntimeDialogueNode currentNode,
            IReadOnlyList<RuntimeResponseOption> availableResponses,
            int overallScore,
            int confidence,
            double timerRemainingSeconds,
            double timerTotalSeconds,
            bool reducedMotion,
            int timeoutCount,
            int attemptNumber,
            IReadOnlyList<string> selectedResponseIds,
            string lastResponseId,
            string lastReactionCue,
            string lastFeedbackKey,
            string lastExplanationKey,
            ResultSummary result,
            LmsCompletionPayload completionPayload,
            LmsSubmissionError submissionError)
        {
            State = state;
            CurrentNode = currentNode;
            AvailableResponses = Copy(availableResponses);
            OverallScore = overallScore;
            Confidence = confidence;
            TimerRemainingSeconds = timerRemainingSeconds;
            TimerTotalSeconds = timerTotalSeconds;
            ReducedMotion = reducedMotion;
            TimeoutCount = timeoutCount;
            AttemptNumber = attemptNumber;
            SelectedResponseIds = Copy(selectedResponseIds);
            LastResponseId = lastResponseId;
            LastReactionCue = lastReactionCue;
            LastFeedbackKey = lastFeedbackKey;
            LastExplanationKey = lastExplanationKey;
            Result = result;
            this.completionPayload = Clone(completionPayload);
            SubmissionError = submissionError;
        }

        public GameState State { get; }

        public RuntimeDialogueNode CurrentNode { get; }

        public string CurrentNodeId => CurrentNode?.Id;

        public IReadOnlyList<RuntimeResponseOption> AvailableResponses { get; }

        public int OverallScore { get; }

        public int Confidence { get; }

        public double TimerRemainingSeconds { get; }

        public double TimerTotalSeconds { get; }

        public bool ReducedMotion { get; }

        public int TimeoutCount { get; }

        public int AttemptNumber { get; }

        public IReadOnlyList<string> SelectedResponseIds { get; }

        public string LastResponseId { get; }

        public string LastReactionCue { get; }

        public string LastFeedbackKey { get; }

        public string LastExplanationKey { get; }

        public ResultSummary Result { get; }

        public LmsCompletionPayload CompletionPayload => Clone(completionPayload);

        public LmsSubmissionError SubmissionError { get; }

        private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.AsReadOnly(Array.Empty<T>());
            }

            var copy = new T[source.Count];
            for (var index = 0; index < source.Count; index++)
            {
                copy[index] = source[index];
            }

            return Array.AsReadOnly(copy);
        }

        private static LmsCompletionPayload Clone(LmsCompletionPayload source)
        {
            if (source == null)
            {
                return null;
            }

            var competencyScores = source.CompetencyScores ?? Array.Empty<LmsCompetencyScore>();
            var competencyCopy = new LmsCompetencyScore[competencyScores.Length];
            for (var index = 0; index < competencyScores.Length; index++)
            {
                var score = competencyScores[index];
                competencyCopy[index] = score == null
                    ? null
                    : new LmsCompetencyScore
                    {
                        CompetencyId = score.CompetencyId,
                        Score = score.Score,
                    };
            }

            var selected = source.SelectedResponseIds ?? Array.Empty<string>();
            var selectedCopy = new string[selected.Length];
            Array.Copy(selected, selectedCopy, selected.Length);
            return new LmsCompletionPayload
            {
                PseudonymousLearnerId = source.PseudonymousLearnerId,
                SessionId = source.SessionId,
                CourseId = source.CourseId,
                ModuleId = source.ModuleId,
                LessonId = source.LessonId,
                ScenarioId = source.ScenarioId,
                GameVersion = source.GameVersion,
                ContentVersion = source.ContentVersion,
                CompletionStatus = source.CompletionStatus,
                StartedAtUtc = source.StartedAtUtc,
                CompletedAtUtc = source.CompletedAtUtc,
                DurationSeconds = source.DurationSeconds,
                OverallScore = source.OverallScore,
                CompetencyScores = competencyCopy,
                FinalConfidence = source.FinalConfidence,
                SelectedResponseIds = selectedCopy,
                TimeoutCount = source.TimeoutCount,
                AttemptNumber = source.AttemptNumber,
                RecommendedFollowUpLessonId = source.RecommendedFollowUpLessonId,
            };
        }
    }
}
