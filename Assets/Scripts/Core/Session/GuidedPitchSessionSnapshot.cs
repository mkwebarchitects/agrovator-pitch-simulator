using System;
using System.Collections.Generic;
using Agrovator.PitchSimulator.GuidedPitch;
using Agrovator.PitchSimulator.LMS;

namespace Agrovator.PitchSimulator.Core
{
    public sealed class GuidedPitchSessionSnapshot
    {
        private readonly LmsCompletionPayload completionPayload;

        internal GuidedPitchSessionSnapshot(
            GuidedPitchPhase phase,
            LearnerMode? learnerMode,
            PitchPart? activePart,
            PitchDraftSnapshot draft,
            PitchAssessment assessment,
            IReadOnlyList<GuidedPitchOption> availableOptions,
            GuidedPitchFeedback feedback,
            string followUpResponseId,
            IReadOnlyList<string> selectionHistory,
            int attemptNumber,
            bool reducedMotion,
            LmsCompletionPayload completionPayload,
            LmsSubmissionError submissionError)
        {
            Phase = phase;
            LearnerMode = learnerMode;
            ActivePart = activePart;
            Draft = draft;
            Assessment = assessment;
            AvailableOptions = Copy(availableOptions);
            Feedback = feedback;
            FollowUpResponseId = followUpResponseId;
            SelectionHistory = Copy(selectionHistory);
            AttemptNumber = attemptNumber;
            ReducedMotion = reducedMotion;
            this.completionPayload = Clone(completionPayload);
            SubmissionError = submissionError;
        }

        public GuidedPitchPhase Phase { get; }

        public LearnerMode? LearnerMode { get; }

        public PitchPart? ActivePart { get; }

        public PitchDraftSnapshot Draft { get; }

        public PitchAssessment Assessment { get; }

        public IReadOnlyList<GuidedPitchOption> AvailableOptions { get; }

        public GuidedPitchFeedback Feedback { get; }

        public string FollowUpResponseId { get; }

        public IReadOnlyList<string> SelectionHistory { get; }

        public int AttemptNumber { get; }

        public bool ReducedMotion { get; }

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

            var sourceScores = source.CompetencyScores ?? Array.Empty<LmsCompetencyScore>();
            var scores = new LmsCompetencyScore[sourceScores.Length];
            for (var index = 0; index < sourceScores.Length; index++)
            {
                var sourceScore = sourceScores[index];
                scores[index] = sourceScore == null
                    ? null
                    : new LmsCompetencyScore
                    {
                        CompetencyId = sourceScore.CompetencyId,
                        Score = sourceScore.Score,
                    };
            }

            var sourceSelections = source.SelectedResponseIds ?? Array.Empty<string>();
            var selections = new string[sourceSelections.Length];
            Array.Copy(sourceSelections, selections, sourceSelections.Length);
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
                CompetencyScores = scores,
                FinalConfidence = source.FinalConfidence,
                SelectedResponseIds = selections,
                TimeoutCount = source.TimeoutCount,
                AttemptNumber = source.AttemptNumber,
                RecommendedFollowUpLessonId = source.RecommendedFollowUpLessonId,
            };
        }
    }
}
