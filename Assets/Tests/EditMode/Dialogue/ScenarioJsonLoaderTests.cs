using System;
using System.Linq;
using Agrovator.PitchSimulator.Dialogue;
using NUnit.Framework;
using UnityEngine;

namespace Agrovator.PitchSimulator.Tests.EditMode.Dialogue
{
    public sealed class ScenarioJsonLoaderTests
    {
        private static readonly string[] Keys =
        {
            "scenario.title",
            "scenario.briefing",
            "project.name",
            "project.description",
            "judge.name",
            "judge.role",
            "node.opening",
            "node.terminal",
            "response.continue",
            "feedback.continue",
            "explanation.continue",
        };

        [Test]
        public void Load_ValidJsonReturnsCompiledRuntimeScenario()
        {
            var result = ScenarioJsonLoader.Load(ValidJson(), Keys);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.Scenario.Id, Is.EqualTo("loader-test"));
            Assert.That(result.Scenario.OpeningNode.Id, Is.EqualTo("opening"));
        }

        [Test]
        public void Load_ValidatesAgainstProvidedLocalizationKeysBeforeCompile()
        {
            var result = ScenarioJsonLoader.Load(ValidJson(), Keys.Where(key => key != "response.continue"));

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Scenario, Is.Null);
            Assert.That(result.Issues.Select(issue => issue.Code),
                Does.Contain("dialogue.localization_key_missing"));
        }

        [TestCase(null, "dialogue.json_missing")]
        [TestCase("", "dialogue.json_missing")]
        [TestCase("   ", "dialogue.json_missing")]
        [TestCase("{not-json}", "dialogue.json_malformed")]
        public void Load_InvalidInputReturnsSanitizedStructuredIssue(string json, string expectedCode)
        {
            var result = ScenarioJsonLoader.Load(json, Keys);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Scenario, Is.Null);
            Assert.That(result.Issues, Has.Count.EqualTo(1));
            Assert.That(result.Issues[0].Code, Is.EqualTo(expectedCode));
            Assert.That(result.Issues[0].Path, Is.EqualTo("$"));
            Assert.That(result.Issues[0].Severity, Is.EqualTo(ValidationSeverity.Error));
            Assert.That(result.Issues[0].Code + result.Issues[0].Path, Does.Not.Contain("not-json"));
        }

        [Test]
        public void Load_RejectsConcatenatedJsonWithoutLeakingContent()
        {
            var result = ScenarioJsonLoader.Load(ValidJson() + "{\"secret\":\"learner-data\"}", Keys);

            Assert.That(result.Issues.Single().Code, Is.EqualTo("dialogue.json_malformed"));
            Assert.That(result.Issues.Single().Code + result.Issues.Single().Path,
                Does.Not.Contain("secret").And.Not.Contain("learner-data"));
        }

        [Test]
        public void Load_CompileFailureIsReturnedAsSanitizedIssue()
        {
            var json = ValidJson().Replace("\"Id\":\"continue\"", "\"Id\":null");

            var result = ScenarioJsonLoader.Load(json, Keys);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Scenario, Is.Null);
            Assert.That(result.Issues.Select(issue => issue.Code), Does.Contain("dialogue.compile_failed"));
        }

        [Test]
        public void DataContractMetadata_PreservesUnityJsonUtilityPublicFieldRoundTrip()
        {
            var source = new ScenarioDefinitionDto
            {
                Id = "json-utility-compatible",
                Version = 3,
                OpeningNodeId = "opening",
                Nodes = new[]
                {
                    new DialogueNodeDto
                    {
                        Id = "opening",
                        NodeType = "Terminal",
                        Responses = Array.Empty<ResponseOptionDto>(),
                    },
                },
            };

            var copy = JsonUtility.FromJson<ScenarioDefinitionDto>(JsonUtility.ToJson(source));

            Assert.That(copy.Id, Is.EqualTo(source.Id));
            Assert.That(copy.Version, Is.EqualTo(source.Version));
            Assert.That(copy.OpeningNodeId, Is.EqualTo(source.OpeningNodeId));
            Assert.That(copy.Nodes.Single().Id, Is.EqualTo("opening"));
        }

        private static string ValidJson()
        {
            return "{"
                + "\"Id\":\"loader-test\","
                + "\"Version\":1,"
                + "\"TitleKey\":\"scenario.title\","
                + "\"BriefingKey\":\"scenario.briefing\","
                + "\"LearningObjectiveKeys\":[],"
                + "\"EstimatedDurationMinutes\":2,"
                + "\"Project\":{\"Id\":\"project\",\"NameKey\":\"project.name\",\"DescriptionKey\":\"project.description\"},"
                + "\"Judge\":{\"Id\":\"judge\",\"NameKey\":\"judge.name\",\"RoleKey\":\"judge.role\",\"PortraitId\":\"judge\"},"
                + "\"Difficulty\":\"Tutorial\","
                + "\"InitialConfidence\":50,"
                + "\"OpeningNodeId\":\"opening\","
                + "\"Nodes\":["
                + "{\"Id\":\"opening\",\"NodeType\":\"Question\",\"Speaker\":\"judge\",\"TextKey\":\"node.opening\",\"TimerSeconds\":15,\"Responses\":["
                + "{\"Id\":\"continue\",\"TextKey\":\"response.continue\",\"QualityTier\":\"Strong\",\"ScoreDelta\":{},\"ConfidenceDelta\":1,\"CompetencyTags\":[],\"ReactionCue\":\"Encouraging\",\"FeedbackKey\":\"feedback.continue\",\"ExplanationKey\":\"explanation.continue\",\"NextNodeId\":\"terminal\",\"SetFlags\":[],\"RequiredFlags\":[],\"BlockedFlags\":[]}]},"
                + "{\"Id\":\"terminal\",\"NodeType\":\"Terminal\",\"Speaker\":\"judge\",\"TextKey\":\"node.terminal\",\"TimerSeconds\":0,\"Responses\":[]}],"
                + "\"SupportedLocales\":[\"en\",\"ms\"],"
                + "\"ContentChecksum\":\"loader-test-v1\"}"
                ;
        }
    }
}
