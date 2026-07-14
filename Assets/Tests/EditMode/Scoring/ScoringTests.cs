using System;
using System.Linq;
using Agrovator.PitchSimulator.Dialogue;
using Agrovator.PitchSimulator.Scoring;
using NUnit.Framework;

namespace Agrovator.PitchSimulator.Tests.EditMode.Scoring
{
    public sealed class ScoringTests
    {
        [TestCase(ScoreCategory.ClearExplanation, 20)]
        [TestCase(ScoreCategory.Problem, 15)]
        [TestCase(ScoreCategory.Solution, 15)]
        [TestCase(ScoreCategory.Audience, 15)]
        [TestCase(ScoreCategory.Evidence, 15)]
        [TestCase(ScoreCategory.Communication, 10)]
        [TestCase(ScoreCategory.TimeManagement, 10)]
        public void CategoryCaps_AreExplicit(ScoreCategory category, int expected)
        {
            Assert.That(ScoreAccumulator.GetMaximum(category), Is.EqualTo(expected));
        }

        [Test]
        public void Apply_ClampsAllSevenCategoriesAndOverallAtOneHundred()
        {
            var scores = new ScoreAccumulator();

            scores.Apply(Delta(100, 100, 100, 100, 100, 100, 100));

            Assert.That(scores[ScoreCategory.ClearExplanation], Is.EqualTo(20));
            Assert.That(scores[ScoreCategory.Problem], Is.EqualTo(15));
            Assert.That(scores[ScoreCategory.Solution], Is.EqualTo(15));
            Assert.That(scores[ScoreCategory.Audience], Is.EqualTo(15));
            Assert.That(scores[ScoreCategory.Evidence], Is.EqualTo(15));
            Assert.That(scores[ScoreCategory.Communication], Is.EqualTo(10));
            Assert.That(scores[ScoreCategory.TimeManagement], Is.EqualTo(10));
            Assert.That(scores.OverallScore, Is.EqualTo(100));
        }

        [Test]
        public void Apply_NeverAllowsNegativeCategoryTotals()
        {
            var scores = new ScoreAccumulator();
            scores.Apply(Delta(5, 5, 5, 5, 5, 5, 5));

            scores.Apply(Delta(-50, -50, -50, -50, -50, -50, -50));

            Assert.That(
                ScoreAccumulator.Categories.Select(category => scores[category]),
                Is.All.EqualTo(0));
        }

        [Test]
        public void Apply_RecordsUniqueCompetencyTagsInAuthoredOrder()
        {
            var scores = new ScoreAccumulator();

            scores.Apply(Delta(), new[] { "problem-framing", "evidence", "problem-framing" });
            scores.Apply(Delta(), new[] { null, string.Empty, "audience-awareness" });

            Assert.That(
                scores.CompetencyTags,
                Is.EqualTo(new[] { "problem-framing", "evidence", "audience-awareness" }));
        }

        [Test]
        public void Apply_AcceptsImmutableRuntimeResponseScoreDeltaForSessionOrchestration()
        {
            var scores = new ScoreAccumulator();
            var runtimeDelta = RuntimeDelta(Delta(clearExplanation: 4, evidence: 3));

            scores.Apply(runtimeDelta, new[] { "evidence" });

            Assert.That(scores[ScoreCategory.ClearExplanation], Is.EqualTo(4));
            Assert.That(scores[ScoreCategory.Evidence], Is.EqualTo(3));
            Assert.That(scores.CompetencyTags, Is.EqualTo(new[] { "evidence" }));
        }

        [Test]
        public void TimeManagement_UsesOnlyTheAuthoredDelta()
        {
            var scores = new ScoreAccumulator();

            scores.Apply(Delta(clearExplanation: 13, timeManagement: 0));

            Assert.That(scores[ScoreCategory.TimeManagement], Is.Zero);
            Assert.That(scores.OverallScore, Is.EqualTo(13));
        }

        [Test]
        public void AuthoredResponseQuality_DominatesMaximumAuthoredTimeDelta()
        {
            var weaker = new ScoreAccumulator();
            var stronger = new ScoreAccumulator();
            weaker.Apply(Delta(clearExplanation: 2, timeManagement: 10));
            stronger.Apply(Delta(clearExplanation: 13));

            Assert.That(stronger.OverallScore, Is.GreaterThan(weaker.OverallScore));
        }

        [Test]
        public void Rollups_SeparatePitchingFromCommunicationAndTime()
        {
            var scores = new ScoreAccumulator();
            scores.Apply(Delta(20, 10, 8, 7, 5, 8, 3));

            Assert.That(scores.PitchingScore, Is.EqualTo(50));
            Assert.That(scores.CommunicationsScore, Is.EqualTo(11));
            Assert.That(scores.OverallScore, Is.EqualTo(61));
        }

        [TestCase(-5, 0)]
        [TestCase(101, 100)]
        [TestCase(60, 60)]
        public void Confidence_Clamps(int value, int expected)
        {
            Assert.That(new ConfidenceMeter(value).Value, Is.EqualTo(expected));
        }

        [Test]
        public void Confidence_ApplyClampsCumulativeDelta()
        {
            var confidence = new ConfidenceMeter(95);

            confidence.Apply(10);
            Assert.That(confidence.Value, Is.EqualTo(100));

            confidence.Apply(-150);
            Assert.That(confidence.Value, Is.Zero);
        }

        [TestCase(0, ResultLevel.Seedling, "result.level.seedling")]
        [TestCase(39, ResultLevel.Seedling, "result.level.seedling")]
        [TestCase(40, ResultLevel.Sprouting, "result.level.sprouting")]
        [TestCase(59, ResultLevel.Sprouting, "result.level.sprouting")]
        [TestCase(60, ResultLevel.Growing, "result.level.growing")]
        [TestCase(79, ResultLevel.Growing, "result.level.growing")]
        [TestCase(80, ResultLevel.Thriving, "result.level.thriving")]
        [TestCase(100, ResultLevel.Thriving, "result.level.thriving")]
        public void ResultLevel_UsesExactInclusiveBoundaries(
            int overall,
            ResultLevel expectedLevel,
            string expectedKey)
        {
            var level = ResultBuilder.GetLevel(overall);

            Assert.That(level.Level, Is.EqualTo(expectedLevel));
            Assert.That(level.LocalizationKey, Is.EqualTo(expectedKey));
        }

        [Test]
        public void ResultThresholds_AreExplicitForEveryCategory()
        {
            Assert.That(ResultBuilder.GetStrengthThreshold(ScoreCategory.ClearExplanation), Is.EqualTo(12));
            Assert.That(ResultBuilder.GetStrengthThreshold(ScoreCategory.Problem), Is.EqualTo(9));
            Assert.That(ResultBuilder.GetStrengthThreshold(ScoreCategory.Solution), Is.EqualTo(9));
            Assert.That(ResultBuilder.GetStrengthThreshold(ScoreCategory.Audience), Is.EqualTo(9));
            Assert.That(ResultBuilder.GetStrengthThreshold(ScoreCategory.Evidence), Is.EqualTo(9));
            Assert.That(ResultBuilder.GetStrengthThreshold(ScoreCategory.Communication), Is.EqualTo(6));
            Assert.That(ResultBuilder.GetStrengthThreshold(ScoreCategory.TimeManagement), Is.EqualTo(6));
        }

        [Test]
        public void Build_SelectsHighestStrengthsAndLowestImprovementsByNormalizedScore()
        {
            var scores = new ScoreAccumulator();
            scores.Apply(Delta(16, 10, 12, 3, 0, 6, 5));

            var result = new ResultBuilder().Build(scores, recovered: false);

            Assert.That(
                result.StrengthKeys,
                Is.EqualTo(new[]
                {
                    "result.strength.clear_explanation",
                    "result.strength.solution",
                }));
            Assert.That(
                result.ImprovementKeys,
                Is.EqualTo(new[]
                {
                    "result.improvement.evidence",
                    "result.improvement.audience",
                }));
        }

        [Test]
        public void Build_UsesStableCategoryOrderToBreakNormalizedTies()
        {
            var scores = new ScoreAccumulator();
            scores.Apply(Delta(12, 9, 9, 9, 9, 6, 6));

            var result = new ResultBuilder().Build(scores, recovered: false);

            Assert.That(
                result.StrengthKeys,
                Is.EqualTo(new[]
                {
                    "result.strength.clear_explanation",
                    "result.strength.problem",
                }));
            Assert.That(result.ImprovementKeys, Is.Empty);
        }

        [Test]
        public void Build_ExactCategoryThresholdIsAStrengthNotAnImprovement()
        {
            var scores = new ScoreAccumulator();
            scores.Apply(Delta(12, 9, 9, 9, 9, 6, 6));

            var result = new ResultBuilder().Build(scores, recovered: false);

            Assert.That(result.ImprovementKeys, Is.Empty);
        }

        [Test]
        public void Build_RecoveryCreatesEncouragingLocalizedStrength()
        {
            var scores = new ScoreAccumulator();
            scores.Apply(Delta(clearExplanation: 20, problem: 15));

            var result = new ResultBuilder().Build(scores, recovered: true);

            Assert.That(
                result.StrengthKeys,
                Is.EqualTo(new[]
                {
                    "result.strength.recovery",
                    "result.strength.clear_explanation",
                }));
            Assert.That(result.AllLocalizationKeys, Is.All.StartsWith("result."));
        }

        private static ResponseScoreDeltaDto Delta(
            int clearExplanation = 0,
            int problem = 0,
            int solution = 0,
            int audience = 0,
            int evidence = 0,
            int communication = 0,
            int timeManagement = 0)
        {
            return new ResponseScoreDeltaDto
            {
                ClearExplanation = clearExplanation,
                Problem = problem,
                Solution = solution,
                Audience = audience,
                Evidence = evidence,
                Communication = communication,
                TimeManagement = timeManagement,
            };
        }

        private static RuntimeResponseScoreDelta RuntimeDelta(ResponseScoreDeltaDto delta)
        {
            var scenario = new ScenarioDefinitionDto
            {
                Id = "score-runtime-test",
                Version = 1,
                TitleKey = "scenario.title",
                BriefingKey = "scenario.briefing",
                OpeningNodeId = "opening",
                Nodes = new[]
                {
                    new DialogueNodeDto
                    {
                        Id = "opening",
                        NodeType = "Question",
                        Speaker = "judge",
                        TextKey = "node.opening",
                        Responses = new[]
                        {
                            new ResponseOptionDto
                            {
                                Id = "answer",
                                TextKey = "response.answer",
                                ScoreDelta = delta,
                                FeedbackKey = "feedback.answer",
                                ExplanationKey = "explanation.answer",
                                NextNodeId = "terminal",
                            },
                        },
                    },
                    new DialogueNodeDto
                    {
                        Id = "terminal",
                        NodeType = "Terminal",
                        Speaker = "judge",
                        TextKey = "node.terminal",
                        Responses = Array.Empty<ResponseOptionDto>(),
                    },
                },
            };

            return RuntimeScenario.Compile(scenario).OpeningNode.Responses[0].ScoreDelta;
        }
    }
}
