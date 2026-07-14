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
            Assert.That(scenario.Version, Is.EqualTo(1));
            Assert.That(scenario.OpeningNodeId, Is.EqualTo("tutorial"));
            var tutorial = scenario.Nodes.Single(node => node.Id == scenario.OpeningNodeId);
            Assert.That(tutorial.NodeType, Is.EqualTo("Tutorial"));
            Assert.That(tutorial.TimerSeconds, Is.Zero);
            Assert.That(tutorial.Responses.Single().ConfidenceDelta, Is.Zero);
            Assert.That(ScoreValues(tutorial.Responses.Single().ScoreDelta), Has.All.Zero);
        }

        [Test]
        public void EveryPlayablePath_HasTutorialAndAtLeastFiveScoredQuestions()
        {
            var scenario = LoadDefinition();
            var paths = EnumerateQuestionCounts(scenario);

            Assert.That(paths, Is.Not.Empty);
            Assert.That(paths.Min(), Is.GreaterThanOrEqualTo(5));
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
        public void AnswerLength_DoesNotPredictStrongQuality()
        {
            var scenario = LoadDefinition();
            var catalog = LocalizationCatalog.Load(ReadCatalog("en"), ReadCatalog("ms"));
            var questions = scenario.Nodes.Where(node => node.NodeType == "Question").ToArray();
            var strongLengths = questions.Select(node => catalog.Resolve(
                "en",
                node.Responses.Single(response => response.QualityTier == "Strong").TextKey).Length).ToArray();
            var longestLengths = questions.Select(node => node.Responses.Max(response =>
                catalog.Resolve("en", response.TextKey).Length)).ToArray();
            var shortestLengths = questions.Select(node => node.Responses.Min(response =>
                catalog.Resolve("en", response.TextKey).Length)).ToArray();

            Assert.That(strongLengths.Zip(longestLengths, (strong, longest) => strong < longest).Count(value => value),
                Is.GreaterThanOrEqualTo(2));
            Assert.That(strongLengths.Zip(shortestLengths, (strong, shortest) => strong == shortest),
                Has.Some.True);
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

        private static IReadOnlyList<int> EnumerateQuestionCounts(ScenarioDefinitionDto scenario)
        {
            var nodes = scenario.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
            var counts = new List<int>();
            Visit(scenario.OpeningNodeId, new HashSet<string>(StringComparer.Ordinal), 0, 0);
            return counts;

            void Visit(string nodeId, HashSet<string> flags, int questionCount, int depth)
            {
                Assert.That(depth, Is.LessThan(20), "Scenario path appears cyclic.");
                var node = nodes[nodeId];
                if (!Available(node.RequiredFlags, node.BlockedFlags, flags))
                {
                    return;
                }

                if (node.NodeType == "Question")
                {
                    questionCount++;
                }

                if (node.NodeType == "Terminal")
                {
                    counts.Add(questionCount);
                    return;
                }

                foreach (var response in node.Responses)
                {
                    if (!Available(response.RequiredFlags, response.BlockedFlags, flags))
                    {
                        continue;
                    }

                    var nextFlags = new HashSet<string>(flags, StringComparer.Ordinal);
                    nextFlags.UnionWith(response.SetFlags);
                    Visit(response.NextNodeId, nextFlags, questionCount, depth + 1);
                }
            }
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
    }
}
