using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Agrovator.PitchSimulator.Dialogue;
using NUnit.Framework;

namespace Agrovator.PitchSimulator.Tests.EditMode.Dialogue
{
    public sealed class ScenarioValidatorTests
    {
        [Test]
        public void Validate_AcceptsMinimalScenario()
        {
            var issues = ScenarioValidator.Validate(ScenarioFactory.Minimal(), ScenarioFactory.Keys());

            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void Validate_ReportsDuplicateNodeIds()
        {
            var scenario = ScenarioFactory.Minimal();
            scenario.Nodes = Append(scenario.Nodes, ScenarioFactory.TerminalNode("terminal"));

            var issues = ScenarioValidator.Validate(scenario, ScenarioFactory.Keys());

            Assert.That(issues, Has.Some.Matches<ValidationIssue>(
                issue => issue.Code == "dialogue.node_id_duplicate"));
        }

        [Test]
        public void Validate_ReportsMissingOpeningNode()
        {
            var scenario = ScenarioFactory.Minimal();
            scenario.OpeningNodeId = "missing";

            var issues = ScenarioValidator.Validate(scenario, ScenarioFactory.Keys());

            Assert.That(issues, Has.Some.Matches<ValidationIssue>(
                issue => issue.Code == "dialogue.opening_node_missing"));
        }

        [Test]
        public void Validate_ReportsMissingDestination()
        {
            var scenario = ScenarioFactory.Minimal();
            scenario.Nodes[0].Responses[0].NextNodeId = "missing";

            var issues = ScenarioValidator.Validate(scenario, ScenarioFactory.Keys());

            Assert.That(issues, Has.Some.Matches<ValidationIssue>(
                issue => issue.Code == "dialogue.destination_missing"));
        }

        [Test]
        public void Validate_ReportsUnreachableNode()
        {
            var scenario = ScenarioFactory.Minimal();
            scenario.Nodes = Append(scenario.Nodes, ScenarioFactory.TerminalNode("orphan"));

            var issues = ScenarioValidator.Validate(scenario, ScenarioFactory.Keys());

            Assert.That(issues, Has.Some.Matches<ValidationIssue>(
                issue => issue.Code == "dialogue.node_unreachable" && issue.Path == "Nodes[2].Id"));
        }

        [Test]
        public void Validate_ReportsDuplicateResponseIdsWithinNode()
        {
            var scenario = ScenarioFactory.Minimal();
            scenario.Nodes[0].Responses = Append(
                scenario.Nodes[0].Responses,
                ScenarioFactory.Response("continue", "terminal"));

            var issues = ScenarioValidator.Validate(scenario, ScenarioFactory.Keys());

            Assert.That(issues, Has.Some.Matches<ValidationIssue>(
                issue => issue.Code == "dialogue.response_id_duplicate"));
        }

        [Test]
        public void Validate_ReportsTimerBelowZero()
        {
            var scenario = ScenarioFactory.Minimal();
            scenario.Nodes[0].TimerSeconds = -1;

            var issues = ScenarioValidator.Validate(scenario, ScenarioFactory.Keys());

            Assert.That(issues, Has.Some.Matches<ValidationIssue>(
                issue => issue.Code == "dialogue.timer_invalid"));
        }

        [Test]
        public void Validate_ReportsScoreDeltaOutsideCategoryRange()
        {
            var scenario = ScenarioFactory.Minimal();
            scenario.Nodes[0].Responses[0].ScoreDelta.ClearExplanation = 21;

            var issues = ScenarioValidator.Validate(scenario, ScenarioFactory.Keys());

            Assert.That(issues, Has.Some.Matches<ValidationIssue>(
                issue => issue.Code == "dialogue.score_delta_out_of_range"
                    && issue.Path == "Nodes[0].Responses[0].ScoreDelta.ClearExplanation"));
        }

        [Test]
        public void Validate_ReportsMissingLocalizationKey()
        {
            var scenario = ScenarioFactory.Minimal();
            scenario.Nodes[0].TextKey = "missing.key";

            var issues = ScenarioValidator.Validate(scenario, ScenarioFactory.Keys());

            Assert.That(issues, Has.Some.Matches<ValidationIssue>(
                issue => issue.Code == "dialogue.localization_key_missing"));
        }

        [TestCase(-101)]
        [TestCase(101)]
        public void Validate_ReportsConfidenceDeltaOutsideInclusiveRange(int confidenceDelta)
        {
            var scenario = ScenarioFactory.Minimal();
            scenario.Nodes[0].Responses[0].ConfidenceDelta = confidenceDelta;

            var issues = ScenarioValidator.Validate(scenario, ScenarioFactory.Keys());

            Assert.That(issues, Has.Some.Matches<ValidationIssue>(
                issue => issue.Code == "dialogue.confidence_delta_out_of_range"));
        }

        [Test]
        public void Validate_ReturnsAllIssuesInNodeAndResponseOrder()
        {
            var scenario = ScenarioFactory.Minimal();
            scenario.Nodes[0].Responses[0].TextKey = "missing.first";
            scenario.Nodes[0].Responses = Append(
                scenario.Nodes[0].Responses,
                ScenarioFactory.Response("second", "missing"));
            scenario.Nodes[1].TimerSeconds = -1;

            var firstRun = ScenarioValidator.Validate(scenario, ScenarioFactory.Keys());
            var secondRun = ScenarioValidator.Validate(scenario, ScenarioFactory.Keys());
            var expectedPaths = new[]
            {
                "Nodes[0].Responses[0].TextKey",
                "Nodes[0].Responses[1].NextNodeId",
                "Nodes[1].TimerSeconds",
            };

            Assert.That(firstRun.Select(issue => issue.Path), Is.EqualTo(expectedPaths));
            Assert.That(
                secondRun.Select(issue => $"{issue.Code}|{issue.Path}|{issue.Severity}"),
                Is.EqualTo(firstRun.Select(issue => $"{issue.Code}|{issue.Path}|{issue.Severity}")));
        }

        [Test]
        public void ValidationIssue_ExposesOnlyStructuredDiagnosticFields()
        {
            var propertyNames = typeof(ValidationIssue)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(property => property.Name);

            Assert.That(propertyNames, Is.EquivalentTo(new[] { "Code", "Path", "Severity" }));
        }

        private static T[] Append<T>(T[] source, T value)
        {
            var result = new T[source.Length + 1];
            Array.Copy(source, result, source.Length);
            result[result.Length - 1] = value;
            return result;
        }

        private static class ScenarioFactory
        {
            public static ScenarioDefinitionDto Minimal()
            {
                return new ScenarioDefinitionDto
                {
                    Id = "minimal",
                    Version = 1,
                    TitleKey = "scenario.title",
                    BriefingKey = "scenario.briefing",
                    LearningObjectiveKeys = new[] { "scenario.objective" },
                    OpeningNodeId = "opening",
                    Nodes = new[]
                    {
                        new DialogueNodeDto
                        {
                            Id = "opening",
                            NodeType = "Question",
                            Speaker = "judge",
                            TextKey = "node.opening",
                            TimerSeconds = 15,
                            Responses = new[] { Response("continue", "terminal") },
                        },
                        TerminalNode("terminal"),
                    },
                    SupportedLocales = new[] { "en" },
                };
            }

            public static HashSet<string> Keys()
            {
                return new HashSet<string>(StringComparer.Ordinal)
                {
                    "scenario.title",
                    "scenario.briefing",
                    "scenario.objective",
                    "node.opening",
                    "node.terminal",
                    "node.orphan",
                    "response.continue",
                    "response.second",
                    "feedback.continue",
                    "feedback.second",
                    "explanation.continue",
                    "explanation.second",
                };
            }

            public static DialogueNodeDto TerminalNode(string id)
            {
                return new DialogueNodeDto
                {
                    Id = id,
                    NodeType = "Terminal",
                    Speaker = "judge",
                    TextKey = $"node.{id}",
                    TimerSeconds = 0,
                    Responses = Array.Empty<ResponseOptionDto>(),
                };
            }

            public static ResponseOptionDto Response(string id, string destination)
            {
                return new ResponseOptionDto
                {
                    Id = id,
                    TextKey = $"response.{id}",
                    QualityTier = "Strong",
                    ScoreDelta = new ResponseScoreDeltaDto(),
                    ConfidenceDelta = 0,
                    FeedbackKey = $"feedback.{id}",
                    ExplanationKey = $"explanation.{id}",
                    NextNodeId = destination,
                };
            }
        }
    }
}
