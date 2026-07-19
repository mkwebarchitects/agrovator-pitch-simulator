using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using Agrovator.PitchSimulator.Accessibility;
using Agrovator.PitchSimulator.Dialogue;
using NUnit.Framework;

namespace Agrovator.PitchSimulator.Tests.EditMode.Dialogue
{
    public sealed class SmartSchoolGardenContentTests
    {
        private const string ScenarioPath = "Assets/Content/Scenarios/smart-school-garden.en.json";

        private static readonly string[] RequiredCompetencies =
        {
            "clear_explanation",
            "problem",
            "solution",
            "audience",
            "evidence",
            "communication",
            "time_management",
        };

        private static readonly string[] RequiredSpecialTags =
        {
            "unsupported_impressive_claim",
            "poorly_communicated_useful_fact",
            "audience_aware",
            "honest_uncertainty_next_step",
            "evidence_recovery",
            "final_value",
        };

        [Test]
        public void AuthoredScenario_HasExpectedIdentityAndOpeningTutorial()
        {
            var scenario = LoadDefinition();

            Assert.That(scenario.Id, Is.EqualTo("smart-school-garden"));
            Assert.That(scenario.Version, Is.EqualTo(2));
            Assert.That(scenario.OpeningNodeId, Is.EqualTo("tutorial"));
            var tutorial = scenario.Nodes.Single(node => node.Id == scenario.OpeningNodeId);
            Assert.That(tutorial.NodeType, Is.EqualTo("Tutorial"));
            Assert.That(tutorial.TimerSeconds, Is.Zero);
            Assert.That(tutorial.Responses.Single().ConfidenceDelta, Is.Zero);
            Assert.That(ScoreValues(tutorial.Responses.Single().ScoreDelta), Has.All.Zero);
        }

        [Test]
        public void EveryPlayablePath_TerminatesWithSixQuestionsWithoutDeadEndsOrChoiceLoss()
        {
            var scenario = LoadDefinition();
            var catalog = LocalizationCatalog.Load(ReadCatalog("en"), ReadCatalog("ms"));
            var analysis = AnalyzePaths(scenario, catalog);

            Assert.That(analysis.Paths, Has.Count.EqualTo(729));
            Assert.That(analysis.Paths, Has.All.Matches<PathResult>(path => path.QuestionCount == 6));
            Assert.That(analysis.DeadEndCount, Is.Zero);
            Assert.That(analysis.MissingDestinationCount, Is.Zero);
            Assert.That(analysis.UnavailableSelectedDestinationCount, Is.Zero);
            Assert.That(analysis.ChoiceLossCount, Is.Zero);
            Assert.That(scenario.Nodes.Count(node => node.NodeType == "Tutorial"), Is.EqualTo(1));
        }

        [Test]
        public void EveryScoredQuestion_HasExactlyThreeRespectfulChoices()
        {
            var scenario = LoadDefinition();
            var questions = scenario.Nodes.Where(node => node.NodeType == "Question").ToArray();

            Assert.That(questions.Length, Is.GreaterThanOrEqualTo(5));
            Assert.That(questions, Has.All.Matches<DialogueNodeDto>(node => node.Responses.Length == 3));
        }

        [Test]
        public void Scenario_HasConditionalBranchesAndRestrictedRecoveryFlag()
        {
            var scenario = LoadDefinition();
            var conditionalBehaviors = scenario.Nodes.Count(node =>
                node.RequiredFlags.Length > 0 || node.BlockedFlags.Length > 0)
                + scenario.Nodes.SelectMany(node => node.Responses).Count(response =>
                    response.RequiredFlags.Length > 0 || response.BlockedFlags.Length > 0);
            var recoveryResponses = scenario.Nodes.SelectMany(node => node.Responses)
                .Where(response => response.SetFlags.Contains("recovered_after_weak_answer"))
                .ToArray();

            Assert.That(conditionalBehaviors, Is.GreaterThanOrEqualTo(2));
            Assert.That(recoveryResponses, Has.Length.EqualTo(1));
            Assert.That(recoveryResponses[0].RequiredFlags, Does.Contain("weak_claim_made"));
            Assert.That(recoveryResponses[0].CompetencyTags, Does.Contain("evidence_recovery"));
        }

        [Test]
        public void TimersAndStrongAnswerPositions_VaryAsDesigned()
        {
            var questions = LoadDefinition().Nodes.Where(node => node.NodeType == "Question").ToArray();
            var timers = questions.Select(node => node.TimerSeconds).ToArray();
            var strongPositions = questions.Select(node => Array.FindIndex(
                node.Responses,
                response => response.QualityTier == "Strong")).ToArray();

            Assert.That(timers, Does.Contain(20));
            Assert.That(timers, Does.Contain(15));
            Assert.That(timers, Does.Contain(12));
            Assert.That(strongPositions, Has.None.EqualTo(-1));
            Assert.That(strongPositions.Distinct().Count(), Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void AuthoredResponses_ContainRequiredCompetencyAndSpecialTags()
        {
            var tags = LoadDefinition().Nodes.SelectMany(node => node.Responses)
                .SelectMany(response => response.CompetencyTags)
                .ToArray();

            Assert.That(tags, Is.SupersetOf(RequiredCompetencies));
            Assert.That(tags, Is.SupersetOf(RequiredSpecialTags));
        }

        [Test]
        public void EveryAuthoredTextKey_ResolvesInBothCatalogsWithExactParity()
        {
            var scenario = LoadDefinition();
            var catalog = LocalizationCatalog.Load(ReadCatalog("en"), ReadCatalog("ms"));
            var textKeys = GetTextKeys(scenario).ToArray();

            Assert.That(catalog.GetKeys("ms"), Is.EquivalentTo(catalog.GetKeys("en")));
            Assert.That(catalog.GetTranslationStatus("ms"), Is.EqualTo("pending_human_review"));
            foreach (var key in textKeys)
            {
                Assert.That(catalog.Resolve("en", key), Does.Not.StartWith("[[missing:"), key);
                Assert.That(catalog.Resolve("ms", key), Does.Not.StartWith("[[missing:"), key);
            }

            if (catalog.GetTranslationStatus("ms") == "pending_human_review")
            {
                foreach (var key in catalog.GetKeys("en"))
                {
                    Assert.That(catalog.Resolve("ms", key), Is.EqualTo(catalog.Resolve("en", key)), key);
                }
            }
        }

        [Test]
        public void AuthoredScenario_ProductionLoaderValidatesAndCompilesEveryNode()
        {
            var catalog = LocalizationCatalog.Load(ReadCatalog("en"), ReadCatalog("ms"));
            var result = ScenarioJsonLoader.Load(File.ReadAllText(ScenarioPath), catalog.GetKeys("en"));

            Assert.That(result.IsSuccess, Is.True, string.Join(", ", result.Issues.Select(issue =>
                issue.Code + "@" + issue.Path)));
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.Scenario.Nodes, Has.Count.EqualTo(10));
        }

        [Test]
        public void StrongLengthRanks_AreDistributedAcrossAuthoredQuestions()
        {
            var scenario = LoadDefinition();
            var catalog = LocalizationCatalog.Load(ReadCatalog("en"), ReadCatalog("ms"));
            var questions = scenario.Nodes.Where(node => node.NodeType == "Question").ToArray();
            var ranks = questions.Select(node => CreateLengthProfile(node.Responses, catalog).StrongRank).ToArray();

            foreach (LengthRank rank in Enum.GetValues(typeof(LengthRank)))
            {
                Assert.That(ranks.Count(value => value == rank), Is.InRange(2, 3), rank.ToString());
            }
        }

        [Test]
        public void QualityTierMeanLengths_AreCloseAcrossAuthoredQuestions()
        {
            var scenario = LoadDefinition();
            var catalog = LocalizationCatalog.Load(ReadCatalog("en"), ReadCatalog("ms"));
            var means = scenario.Nodes.Where(node => node.NodeType == "Question")
                .SelectMany(node => node.Responses)
                .GroupBy(response => response.QualityTier, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Average(response => catalog.Resolve("en", response.TextKey).Length),
                    StringComparer.Ordinal);

            Assert.That(means.Keys, Is.EquivalentTo(new[] { "Strong", "Developing", "NeedsWork" }));
            Assert.That(means.Values.Max() - means.Values.Min(), Is.LessThanOrEqualTo(12.0));
        }

        [Test]
        public void EveryPlayableRoute_HasTwoStrongAnswersAtEachLengthRankAndCloseTierMeans()
        {
            var scenario = LoadDefinition();
            var catalog = LocalizationCatalog.Load(ReadCatalog("en"), ReadCatalog("ms"));
            var analysis = AnalyzePaths(scenario, catalog);

            Assert.That(analysis.Paths, Is.Not.Empty);
            Assert.That(analysis.Paths.Min(path => path.StrongShortestCount), Is.EqualTo(2));
            Assert.That(analysis.Paths.Max(path => path.StrongShortestCount), Is.EqualTo(2));
            Assert.That(analysis.Paths.Min(path => path.StrongMiddleCount), Is.EqualTo(2));
            Assert.That(analysis.Paths.Max(path => path.StrongMiddleCount), Is.EqualTo(2));
            Assert.That(analysis.Paths.Min(path => path.StrongLongestCount), Is.EqualTo(2));
            Assert.That(analysis.Paths.Max(path => path.StrongLongestCount), Is.EqualTo(2));
            Assert.That(analysis.Paths.Max(path => path.TierMeanSpread), Is.LessThanOrEqualTo(12.0));
        }

        private static ScenarioDefinitionDto LoadDefinition()
        {
            Assert.That(File.Exists(ScenarioPath), Is.True, $"Missing authored scenario: {ScenarioPath}");
            var serializer = new DataContractJsonSerializer(typeof(ScenarioDefinitionDto));
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(File.ReadAllText(ScenarioPath))))
            {
                return (ScenarioDefinitionDto)serializer.ReadObject(stream);
            }
        }

        private static string ReadCatalog(string locale)
        {
            return File.ReadAllText(Path.Combine("Assets", "Content", "Localization", locale + ".json"));
        }

        private static IEnumerable<string> GetTextKeys(ScenarioDefinitionDto scenario)
        {
            yield return scenario.TitleKey;
            yield return scenario.BriefingKey;
            foreach (var key in scenario.LearningObjectiveKeys)
            {
                yield return key;
            }

            yield return scenario.Project.NameKey;
            yield return scenario.Project.DescriptionKey;
            yield return scenario.Judge.NameKey;
            yield return scenario.Judge.RoleKey;
            foreach (var node in scenario.Nodes)
            {
                yield return node.TextKey;
                foreach (var response in node.Responses)
                {
                    yield return response.TextKey;
                    yield return response.FeedbackKey;
                    yield return response.ExplanationKey;
                }
            }
        }

        private static PathAnalysis AnalyzePaths(ScenarioDefinitionDto scenario, LocalizationCatalog catalog)
        {
            var nodes = scenario.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
            var analysis = new PathAnalysis();
            Visit(scenario.OpeningNodeId, new HashSet<string>(StringComparer.Ordinal), 0, 0, 0, 0, 0, 0, 0, 0);
            return analysis;

            void Visit(
                string nodeId,
                HashSet<string> flags,
                int questionCount,
                int strongShortestCount,
                int strongMiddleCount,
                int strongLongestCount,
                int strongLengthTotal,
                int developingLengthTotal,
                int needsWorkLengthTotal,
                int depth)
            {
                Assert.That(depth, Is.LessThan(20), "Scenario path appears cyclic.");
                if (!nodes.TryGetValue(nodeId, out var node))
                {
                    analysis.MissingDestinationCount++;
                    return;
                }

                if (!Available(node.RequiredFlags, node.BlockedFlags, flags))
                {
                    analysis.UnavailableSelectedDestinationCount++;
                    return;
                }

                var availableResponses = node.Responses
                    .Where(response => Available(response.RequiredFlags, response.BlockedFlags, flags))
                    .ToArray();
                if (node.NodeType == "Question")
                {
                    questionCount++;
                    if (availableResponses.Length != 3)
                    {
                        analysis.ChoiceLossCount++;
                    }

                    if (availableResponses.Length > 0)
                    {
                        var profile = CreateLengthProfile(availableResponses, catalog);
                        switch (profile.StrongRank)
                        {
                            case LengthRank.Shortest:
                                strongShortestCount++;
                                break;
                            case LengthRank.Middle:
                                strongMiddleCount++;
                                break;
                            case LengthRank.Longest:
                                strongLongestCount++;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        strongLengthTotal += profile.GetLength("Strong");
                        developingLengthTotal += profile.GetLength("Developing");
                        needsWorkLengthTotal += profile.GetLength("NeedsWork");
                    }
                }

                if (node.NodeType == "Terminal")
                {
                    analysis.Paths.Add(new PathResult(
                        questionCount,
                        strongShortestCount,
                        strongMiddleCount,
                        strongLongestCount,
                        strongLengthTotal,
                        developingLengthTotal,
                        needsWorkLengthTotal));
                    return;
                }

                if (availableResponses.Length == 0)
                {
                    analysis.DeadEndCount++;
                    return;
                }

                foreach (var response in availableResponses)
                {
                    var nextFlags = new HashSet<string>(flags, StringComparer.Ordinal);
                    nextFlags.UnionWith(response.SetFlags);
                    if (!nodes.TryGetValue(response.NextNodeId, out var destination))
                    {
                        analysis.MissingDestinationCount++;
                        continue;
                    }

                    if (!Available(destination.RequiredFlags, destination.BlockedFlags, nextFlags))
                    {
                        analysis.UnavailableSelectedDestinationCount++;
                        continue;
                    }

                    Visit(
                        response.NextNodeId,
                        nextFlags,
                        questionCount,
                        strongShortestCount,
                        strongMiddleCount,
                        strongLongestCount,
                        strongLengthTotal,
                        developingLengthTotal,
                        needsWorkLengthTotal,
                        depth + 1);
                }
            }
        }

        private static ResponseLengthProfile CreateLengthProfile(
            IEnumerable<ResponseOptionDto> responses,
            LocalizationCatalog catalog)
        {
            return new ResponseLengthProfile(responses, catalog);
        }

        private static bool Available(
            IEnumerable<string> required,
            IEnumerable<string> blocked,
            ISet<string> flags)
        {
            return required.All(flags.Contains) && blocked.All(flag => !flags.Contains(flag));
        }

        private static int[] ScoreValues(ResponseScoreDeltaDto score)
        {
            return new[]
            {
                score.ClearExplanation,
                score.Problem,
                score.Solution,
                score.Audience,
                score.Evidence,
                score.Communication,
                score.TimeManagement,
            };
        }

        private sealed class PathAnalysis
        {
            public List<PathResult> Paths { get; } = new List<PathResult>();

            public int DeadEndCount { get; set; }

            public int MissingDestinationCount { get; set; }

            public int UnavailableSelectedDestinationCount { get; set; }

            public int ChoiceLossCount { get; set; }
        }

        private sealed class PathResult
        {
            public PathResult(
                int questionCount,
                int strongShortestCount,
                int strongMiddleCount,
                int strongLongestCount,
                int strongLengthTotal,
                int developingLengthTotal,
                int needsWorkLengthTotal)
            {
                QuestionCount = questionCount;
                StrongShortestCount = strongShortestCount;
                StrongMiddleCount = strongMiddleCount;
                StrongLongestCount = strongLongestCount;
                var means = new[]
                {
                    (double)strongLengthTotal / questionCount,
                    (double)developingLengthTotal / questionCount,
                    (double)needsWorkLengthTotal / questionCount,
                };
                TierMeanSpread = means.Max() - means.Min();
            }

            public int QuestionCount { get; }

            public int StrongShortestCount { get; }

            public int StrongMiddleCount { get; }

            public int StrongLongestCount { get; }

            public double TierMeanSpread { get; }
        }

        private sealed class ResponseLengthProfile
        {
            private readonly IReadOnlyDictionary<string, int> _lengths;

            public ResponseLengthProfile(
                IEnumerable<ResponseOptionDto> responses,
                LocalizationCatalog catalog)
            {
                var choices = responses.Select(response => new
                {
                    Tier = response.QualityTier,
                    Length = catalog.Resolve("en", response.TextKey).Length,
                }).OrderBy(choice => choice.Length).ToArray();
                Assert.That(choices.Select(choice => choice.Tier),
                    Is.EquivalentTo(new[] { "Strong", "Developing", "NeedsWork" }));
                Assert.That(choices.Select(choice => choice.Length).Distinct().Count(), Is.EqualTo(3));
                _lengths = choices.ToDictionary(choice => choice.Tier, choice => choice.Length, StringComparer.Ordinal);
                StrongRank = (LengthRank)Array.FindIndex(choices, choice => choice.Tier == "Strong");
            }

            public LengthRank StrongRank { get; }

            public int GetLength(string qualityTier)
            {
                return _lengths[qualityTier];
            }
        }

        private enum LengthRank
        {
            Shortest,
            Middle,
            Longest,
        }
    }
}
