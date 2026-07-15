using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Agrovator.PitchSimulator.GuidedPitch
{
    public sealed class PitchAssessment
    {
        internal PitchAssessment(
            int pitchReadiness,
            int problemClarity,
            int evidenceQuality,
            int solutionFit,
            int audienceValue,
            int clearExplanation,
            int communication,
            int improvedPartCount,
            IReadOnlyDictionary<PitchPart, MasteryState> masteryByPart)
        {
            PitchReadiness = pitchReadiness;
            ProblemClarity = problemClarity;
            EvidenceQuality = evidenceQuality;
            SolutionFit = solutionFit;
            AudienceValue = audienceValue;
            ClearExplanation = clearExplanation;
            Communication = communication;
            ImprovedPartCount = improvedPartCount;
            MasteryByPart = masteryByPart;
        }

        public int PitchReadiness { get; }

        public int ProblemClarity { get; }

        public int EvidenceQuality { get; }

        public int SolutionFit { get; }

        public int AudienceValue { get; }

        public int ClearExplanation { get; }

        public int Communication { get; }

        public int ImprovedPartCount { get; }

        public IReadOnlyDictionary<PitchPart, MasteryState> MasteryByPart { get; }
    }

    public static class PitchAssessmentBuilder
    {
        public static PitchAssessment Build(PitchDraftSnapshot draft)
        {
            var masteryByPart = new Dictionary<PitchPart, MasteryState>();
            var pitchReadiness = 0;
            var competencyTotal = 0;
            var improvedPartCount = 0;

            foreach (var part in PitchParts.Ordered)
            {
                var section = draft[part];
                if (!section.CurrentMastery.HasValue)
                {
                    continue;
                }

                var mastery = section.CurrentMastery.Value;
                masteryByPart.Add(part, mastery);
                pitchReadiness += ReadinessFor(mastery);
                competencyTotal += CompetencyFor(mastery);

                if (section.WasRevised && mastery > section.InitialMastery.Value)
                {
                    improvedPartCount++;
                }
            }

            var populatedCount = masteryByPart.Count;
            var mean = populatedCount == 0
                ? 0
                : (int)Math.Round(
                    competencyTotal / (double)populatedCount,
                    MidpointRounding.AwayFromZero);
            var readOnlyMastery = new ReadOnlyDictionary<PitchPart, MasteryState>(masteryByPart);

            return new PitchAssessment(
                pitchReadiness,
                CompetencyFor(draft[PitchPart.Problem].CurrentMastery),
                CompetencyFor(draft[PitchPart.Evidence].CurrentMastery),
                CompetencyFor(draft[PitchPart.Solution].CurrentMastery),
                CompetencyFor(draft[PitchPart.Value].CurrentMastery),
                mean,
                mean,
                improvedPartCount,
                readOnlyMastery);
        }

        private static int ReadinessFor(MasteryState mastery)
        {
            switch (mastery)
            {
                case MasteryState.NeedsPractice:
                    return 10;
                case MasteryState.Developing:
                    return 20;
                case MasteryState.Clear:
                    return 25;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mastery), mastery, "Unknown mastery state.");
            }
        }

        private static int CompetencyFor(MasteryState? mastery)
        {
            if (!mastery.HasValue)
            {
                return 0;
            }

            return CompetencyFor(mastery.Value);
        }

        private static int CompetencyFor(MasteryState mastery)
        {
            switch (mastery)
            {
                case MasteryState.NeedsPractice:
                    return 40;
                case MasteryState.Developing:
                    return 70;
                case MasteryState.Clear:
                    return 100;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mastery), mastery, "Unknown mastery state.");
            }
        }
    }
}
