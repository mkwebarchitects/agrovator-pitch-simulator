using System;
using System.Collections.Generic;

namespace Agrovator.PitchSimulator.Scoring
{
    public sealed class ResultBuilder
    {
        public const int SeedlingMinimum = 0;
        public const int SproutingMinimum = 40;
        public const int GrowingMinimum = 60;
        public const int ThrivingMinimum = 80;

        public const int ClearExplanationStrengthThreshold = 12;
        public const int ProblemStrengthThreshold = 9;
        public const int SolutionStrengthThreshold = 9;
        public const int AudienceStrengthThreshold = 9;
        public const int EvidenceStrengthThreshold = 9;
        public const int CommunicationStrengthThreshold = 6;
        public const int TimeManagementStrengthThreshold = 6;

        private const int MaximumStrengths = 2;
        private const int MaximumImprovements = 2;

        private static readonly ResultLevelDefinition[] Levels =
        {
            new ResultLevelDefinition(ResultLevel.Seedling, SeedlingMinimum, "result.level.seedling"),
            new ResultLevelDefinition(ResultLevel.Sprouting, SproutingMinimum, "result.level.sprouting"),
            new ResultLevelDefinition(ResultLevel.Growing, GrowingMinimum, "result.level.growing"),
            new ResultLevelDefinition(ResultLevel.Thriving, ThrivingMinimum, "result.level.thriving"),
        };

        public static ResultLevelDefinition GetLevel(int overallScore)
        {
            for (var index = Levels.Length - 1; index >= 0; index--)
            {
                if (overallScore >= Levels[index].InclusiveMinimum)
                {
                    return Levels[index];
                }
            }

            return Levels[0];
        }

        public static int GetStrengthThreshold(ScoreCategory category)
        {
            switch (category)
            {
                case ScoreCategory.ClearExplanation:
                    return ClearExplanationStrengthThreshold;
                case ScoreCategory.Problem:
                    return ProblemStrengthThreshold;
                case ScoreCategory.Solution:
                    return SolutionStrengthThreshold;
                case ScoreCategory.Audience:
                    return AudienceStrengthThreshold;
                case ScoreCategory.Evidence:
                    return EvidenceStrengthThreshold;
                case ScoreCategory.Communication:
                    return CommunicationStrengthThreshold;
                case ScoreCategory.TimeManagement:
                    return TimeManagementStrengthThreshold;
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown score category.");
            }
        }

        public ResultSummary Build(ScoreAccumulator scores, bool recovered)
        {
            if (scores == null)
            {
                throw new ArgumentNullException(nameof(scores));
            }

            var strengths = BuildStrengthKeys(scores, recovered);
            var improvements = BuildImprovementKeys(scores);
            var level = GetLevel(scores.OverallScore);
            return new ResultSummary(scores, level, strengths, improvements);
        }

        private static IReadOnlyList<string> BuildStrengthKeys(ScoreAccumulator scores, bool recovered)
        {
            var keys = new List<string>(MaximumStrengths);
            if (recovered)
            {
                keys.Add("result.strength.recovery");
            }

            var ranked = RankCategories(scores, descending: true);
            foreach (var category in ranked)
            {
                if (keys.Count == MaximumStrengths)
                {
                    break;
                }

                if (category.Score >= GetStrengthThreshold(category.Category))
                {
                    keys.Add(GetCategoryKey("strength", category.Category));
                }
            }

            return keys.AsReadOnly();
        }

        private static IReadOnlyList<string> BuildImprovementKeys(ScoreAccumulator scores)
        {
            var keys = new List<string>(MaximumImprovements);
            var ranked = RankCategories(scores, descending: false);
            foreach (var category in ranked)
            {
                if (keys.Count == MaximumImprovements)
                {
                    break;
                }

                if (category.Score < GetStrengthThreshold(category.Category))
                {
                    keys.Add(GetCategoryKey("improvement", category.Category));
                }
            }

            return keys.AsReadOnly();
        }

        private static List<CategoryRank> RankCategories(ScoreAccumulator scores, bool descending)
        {
            var ranked = new List<CategoryRank>(ScoreAccumulator.Categories.Count);
            for (var index = 0; index < ScoreAccumulator.Categories.Count; index++)
            {
                var category = ScoreAccumulator.Categories[index];
                ranked.Add(new CategoryRank(category, scores[category], ScoreAccumulator.GetMaximum(category), index));
            }

            ranked.Sort((left, right) =>
            {
                var comparison = descending
                    ? (right.Score * left.Maximum).CompareTo(left.Score * right.Maximum)
                    : (left.Score * right.Maximum).CompareTo(right.Score * left.Maximum);
                return comparison != 0 ? comparison : left.StableIndex.CompareTo(right.StableIndex);
            });
            return ranked;
        }

        private static string GetCategoryKey(string kind, ScoreCategory category)
        {
            switch (category)
            {
                case ScoreCategory.ClearExplanation:
                    return $"result.{kind}.clear_explanation";
                case ScoreCategory.Problem:
                    return $"result.{kind}.problem";
                case ScoreCategory.Solution:
                    return $"result.{kind}.solution";
                case ScoreCategory.Audience:
                    return $"result.{kind}.audience";
                case ScoreCategory.Evidence:
                    return $"result.{kind}.evidence";
                case ScoreCategory.Communication:
                    return $"result.{kind}.communication";
                case ScoreCategory.TimeManagement:
                    return $"result.{kind}.time_management";
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown score category.");
            }
        }

        private sealed class CategoryRank
        {
            public CategoryRank(ScoreCategory category, int score, int maximum, int stableIndex)
            {
                Category = category;
                Score = score;
                Maximum = maximum;
                StableIndex = stableIndex;
            }

            public ScoreCategory Category { get; }

            public int Score { get; }

            public int Maximum { get; }

            public int StableIndex { get; }
        }
    }

    public sealed class ResultSummary
    {
        internal ResultSummary(
            ScoreAccumulator scores,
            ResultLevelDefinition level,
            IReadOnlyList<string> strengthKeys,
            IReadOnlyList<string> improvementKeys)
        {
            OverallScore = scores.OverallScore;
            PitchingScore = scores.PitchingScore;
            CommunicationsScore = scores.CommunicationsScore;
            Level = level.Level;
            LevelLocalizationKey = level.LocalizationKey;
            StrengthKeys = strengthKeys;
            ImprovementKeys = improvementKeys;

            var allKeys = new List<string>(1 + strengthKeys.Count + improvementKeys.Count)
            {
                LevelLocalizationKey,
            };
            allKeys.AddRange(strengthKeys);
            allKeys.AddRange(improvementKeys);
            AllLocalizationKeys = allKeys.AsReadOnly();
        }

        public int OverallScore { get; }

        public int PitchingScore { get; }

        public int CommunicationsScore { get; }

        public ResultLevel Level { get; }

        public string LevelLocalizationKey { get; }

        public IReadOnlyList<string> StrengthKeys { get; }

        public IReadOnlyList<string> ImprovementKeys { get; }

        public IReadOnlyList<string> AllLocalizationKeys { get; }
    }
}
