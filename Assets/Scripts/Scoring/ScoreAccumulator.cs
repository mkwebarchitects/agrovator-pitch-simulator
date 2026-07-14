using System;
using System.Collections.Generic;
using Agrovator.PitchSimulator.Dialogue;

namespace Agrovator.PitchSimulator.Scoring
{
    public sealed class ScoreAccumulator
    {
        public const int ClearExplanationMaximum = 20;
        public const int ProblemMaximum = 15;
        public const int SolutionMaximum = 15;
        public const int AudienceMaximum = 15;
        public const int EvidenceMaximum = 15;
        public const int CommunicationMaximum = 10;
        public const int TimeManagementMaximum = 10;
        public const int OverallMaximum = 100;

        private static readonly ScoreCategory[] OrderedCategories =
        {
            ScoreCategory.ClearExplanation,
            ScoreCategory.Problem,
            ScoreCategory.Solution,
            ScoreCategory.Audience,
            ScoreCategory.Evidence,
            ScoreCategory.Communication,
            ScoreCategory.TimeManagement,
        };

        private static readonly IReadOnlyList<ScoreCategory> ReadOnlyCategories =
            Array.AsReadOnly(OrderedCategories);

        private readonly int[] scores = new int[OrderedCategories.Length];
        private readonly List<string> competencyTags = new List<string>();
        private readonly HashSet<string> competencyTagSet = new HashSet<string>(StringComparer.Ordinal);

        public static IReadOnlyList<ScoreCategory> Categories => ReadOnlyCategories;

        public IReadOnlyList<string> CompetencyTags => competencyTags.AsReadOnly();

        public int this[ScoreCategory category] => scores[GetIndex(category)];

        public int PitchingScore =>
            this[ScoreCategory.ClearExplanation]
            + this[ScoreCategory.Problem]
            + this[ScoreCategory.Solution]
            + this[ScoreCategory.Audience]
            + this[ScoreCategory.Evidence];

        public int CommunicationsScore =>
            this[ScoreCategory.Communication] + this[ScoreCategory.TimeManagement];

        public int OverallScore => PitchingScore + CommunicationsScore;

        public static int GetMaximum(ScoreCategory category)
        {
            switch (category)
            {
                case ScoreCategory.ClearExplanation:
                    return ClearExplanationMaximum;
                case ScoreCategory.Problem:
                    return ProblemMaximum;
                case ScoreCategory.Solution:
                    return SolutionMaximum;
                case ScoreCategory.Audience:
                    return AudienceMaximum;
                case ScoreCategory.Evidence:
                    return EvidenceMaximum;
                case ScoreCategory.Communication:
                    return CommunicationMaximum;
                case ScoreCategory.TimeManagement:
                    return TimeManagementMaximum;
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown score category.");
            }
        }

        public void Apply(ResponseScoreDeltaDto delta, IEnumerable<string> competencyTags = null)
        {
            if (delta == null)
            {
                throw new ArgumentNullException(nameof(delta));
            }

            ApplyValues(
                delta.ClearExplanation,
                delta.Problem,
                delta.Solution,
                delta.Audience,
                delta.Evidence,
                delta.Communication,
                delta.TimeManagement,
                competencyTags);
        }

        public void Apply(RuntimeResponseScoreDelta delta, IEnumerable<string> competencyTags = null)
        {
            if (delta == null)
            {
                throw new ArgumentNullException(nameof(delta));
            }

            ApplyValues(
                delta.ClearExplanation,
                delta.Problem,
                delta.Solution,
                delta.Audience,
                delta.Evidence,
                delta.Communication,
                delta.TimeManagement,
                competencyTags);
        }

        private void ApplyValues(
            int clearExplanation,
            int problem,
            int solution,
            int audience,
            int evidence,
            int communication,
            int timeManagement,
            IEnumerable<string> tags)
        {
            Add(ScoreCategory.ClearExplanation, clearExplanation);
            Add(ScoreCategory.Problem, problem);
            Add(ScoreCategory.Solution, solution);
            Add(ScoreCategory.Audience, audience);
            Add(ScoreCategory.Evidence, evidence);
            Add(ScoreCategory.Communication, communication);
            Add(ScoreCategory.TimeManagement, timeManagement);
            RecordTags(tags);
        }

        private void Add(ScoreCategory category, int delta)
        {
            var index = GetIndex(category);
            var candidate = (long)scores[index] + delta;
            var maximum = GetMaximum(category);
            scores[index] = candidate < 0
                ? 0
                : candidate > maximum
                    ? maximum
                    : (int)candidate;
        }

        private void RecordTags(IEnumerable<string> tags)
        {
            if (tags == null)
            {
                return;
            }

            foreach (var tag in tags)
            {
                if (!string.IsNullOrEmpty(tag) && competencyTagSet.Add(tag))
                {
                    competencyTags.Add(tag);
                }
            }
        }

        private static int GetIndex(ScoreCategory category)
        {
            var index = (int)category;
            if (index < 0 || index >= OrderedCategories.Length || OrderedCategories[index] != category)
            {
                throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown score category.");
            }

            return index;
        }
    }
}
