using System;
using System.Collections.Generic;
using System.Linq;
using Agrovator.PitchSimulator.Dialogue;
using NUnit.Framework;

namespace Agrovator.PitchSimulator.Tests.EditMode.Dialogue
{
    public sealed class DialogueSessionTests
    {
        [Test]
        public void Constructor_SelectsOpeningNode()
        {
            var session = Session(Scenario());

            Assert.That(session.CurrentNode.Id, Is.EqualTo("opening"));
            Assert.That(session.IsComplete, Is.False);
        }

        [Test]
        public void Select_AvailableResponseSetsFlagsAndMovesToDestination()
        {
            var scenario = Scenario();
            scenario.Nodes[0].Responses[0].SetFlags = new[] { "introduced" };
            var session = Session(scenario);

            var result = session.Select("continue", 50);

            Assert.That(result.IsAccepted, Is.True);
            Assert.That(result.SelectedResponse.Id, Is.EqualTo("continue"));
            Assert.That(result.NewNode.Id, Is.EqualTo("terminal"));
            Assert.That(session.CurrentNode.Id, Is.EqualTo("terminal"));
            Assert.That(session.HasFlag("introduced"), Is.True);
        }

        [Test]
        public void GetAvailableResponses_RequiredFlagExposesResponse()
        {
            var scenario = Scenario();
            scenario.Nodes[0].Responses = new[]
            {
                Response("prepare", "question", setFlags: new[] { "prepared" }),
            };
            scenario.Nodes = new[]
            {
                scenario.Nodes[0],
                Node(
                    "question",
                    Response("base", "terminal"),
                    Response("prepared-answer", "terminal", requiredFlags: new[] { "prepared" })),
                scenario.Nodes[1],
            };
            var session = Session(scenario);

            session.Select("prepare", 50);

            Assert.That(
                session.GetAvailableResponses(50).Select(response => response.Id),
                Is.EquivalentTo(new[] { "base", "prepared-answer" }));
        }

        [Test]
        public void GetAvailableResponses_BlockedFlagHidesResponse()
        {
            var scenario = Scenario();
            scenario.Nodes[0].Responses = new[]
            {
                Response("commit", "question", setFlags: new[] { "committed" }),
            };
            scenario.Nodes = new[]
            {
                scenario.Nodes[0],
                Node(
                    "question",
                    Response("reconsider", "terminal", blockedFlags: new[] { "committed" }),
                    Response("proceed", "terminal")),
                scenario.Nodes[1],
            };
            var session = Session(scenario);

            session.Select("commit", 50);

            Assert.That(
                session.GetAvailableResponses(50).Select(response => response.Id),
                Is.EqualTo(new[] { "proceed" }));
        }

        [Test]
        public void GetAvailableResponses_ConfidenceRangeSelectsEligiblePath()
        {
            var scenario = Scenario(
                Response("low-confidence", "terminal", minimumConfidence: 0, maximumConfidence: 49),
                Response("high-confidence", "terminal", minimumConfidence: 50, maximumConfidence: 100));
            var lowSession = Session(scenario);
            var highSession = Session(scenario);

            var low = lowSession.GetAvailableResponses(49);
            var high = highSession.GetAvailableResponses(50);

            Assert.That(low.Select(response => response.Id), Is.EqualTo(new[] { "low-confidence" }));
            Assert.That(high.Select(response => response.Id), Is.EqualTo(new[] { "high-confidence" }));
        }

        [Test]
        public void Select_TerminalNodeCompletesSession()
        {
            var session = Session(Scenario());

            session.Select("continue", 50);

            Assert.That(session.IsComplete, Is.True);
            Assert.That(session.CurrentNode.Id, Is.EqualTo("terminal"));
        }

        [Test]
        public void CompleteSession_HidesAndRejectsAuthoredTerminalResponsesWithoutMutation()
        {
            var scenario = Scenario();
            scenario.Nodes[1].Responses = new[]
            {
                Response("leave-terminal", "opening", setFlags: new[] { "left-terminal" }),
            };
            var session = Session(scenario);
            session.Select("continue", 50);

            var available = session.GetAvailableResponses(50);
            var result = session.Select("leave-terminal", 50);

            Assert.That(available, Is.Empty);
            Assert.That(result.IsAccepted, Is.False);
            Assert.That(result.SelectedResponse, Is.Null);
            Assert.That(result.NewNode, Is.Null);
            Assert.That(session.CurrentNode.Id, Is.EqualTo("terminal"));
            Assert.That(session.HasFlag("left-terminal"), Is.False);
        }

        [Test]
        public void Select_UnknownResponseIsRejectedWithoutMutation()
        {
            var scenario = Scenario();
            scenario.Nodes[0].Responses[0].SetFlags = new[] { "selected" };
            var session = Session(scenario);

            var result = session.Select("Continue", 50);

            Assert.That(result.IsAccepted, Is.False);
            Assert.That(result.SelectedResponse, Is.Null);
            Assert.That(result.NewNode, Is.Null);
            Assert.That(session.CurrentNode.Id, Is.EqualTo("opening"));
            Assert.That(session.HasFlag("selected"), Is.False);
        }

        [Test]
        public void Select_NullResponseIdIsRejectedWithoutMutation()
        {
            var scenario = Scenario();
            scenario.Nodes[0].Responses[0].SetFlags = new[] { "selected" };
            var session = Session(scenario);

            var result = session.Select(null, 50);

            Assert.That(result.IsAccepted, Is.False);
            Assert.That(session.CurrentNode.Id, Is.EqualTo("opening"));
            Assert.That(session.HasFlag("selected"), Is.False);
        }

        [Test]
        public void Select_UnavailableResponseIsRejectedWithoutMutation()
        {
            var scenario = Scenario(Response(
                "locked",
                "terminal",
                setFlags: new[] { "selected" },
                requiredFlags: new[] { "missing" }));
            var session = Session(scenario);

            var result = session.Select("locked", 50);

            Assert.That(result.IsAccepted, Is.False);
            Assert.That(session.CurrentNode.Id, Is.EqualTo("opening"));
            Assert.That(session.HasFlag("selected"), Is.False);
        }

        [Test]
        public void Select_RecoveryFlagIsAuthoredOnlyAfterWeakThenEvidenceResponses()
        {
            var scenario = Scenario();
            scenario.Nodes[0].Responses = new[]
            {
                Response("plain-one", "evidence", setFlags: new[] { "weak-answer" }),
            };
            scenario.Nodes = new[]
            {
                scenario.Nodes[0],
                Node(
                    "evidence",
                    Response(
                        "plain-two",
                        "terminal",
                        setFlags: new[] { "recovered" },
                        requiredFlags: new[] { "weak-answer" })),
                scenario.Nodes[1],
            };
            var session = Session(scenario);

            var weakResult = session.Select("plain-one", 50);

            Assert.That(weakResult.IsAccepted, Is.True);
            Assert.That(session.HasFlag("weak-answer"), Is.True);
            Assert.That(session.HasFlag("recovered"), Is.False);

            var evidenceResult = session.Select("plain-two", 50);

            Assert.That(evidenceResult.IsAccepted, Is.True);
            Assert.That(session.HasFlag("recovered"), Is.True);
        }

        [Test]
        public void Compile_CopiesDtoCollectionsAndValues()
        {
            var scenario = Scenario();
            scenario.Nodes[0].RequiredFlags = new[] { "original-node-flag" };
            scenario.Nodes[0].Responses[0].SetFlags = new[] { "original-response-flag" };
            var runtime = RuntimeScenario.Compile(scenario);

            scenario.OpeningNodeId = "terminal";
            scenario.Nodes[0].Id = "changed";
            scenario.Nodes[0].RequiredFlags[0] = "changed";
            scenario.Nodes[0].Responses[0].Id = "changed";
            scenario.Nodes[0].Responses[0].SetFlags[0] = "changed";
            scenario.Nodes[0].Responses = Array.Empty<ResponseOptionDto>();

            Assert.That(runtime.OpeningNode.Id, Is.EqualTo("opening"));
            Assert.That(runtime.OpeningNode.RequiredFlags, Is.EqualTo(new[] { "original-node-flag" }));
            Assert.That(runtime.OpeningNode.Responses.Single().Id, Is.EqualTo("continue"));
            Assert.That(
                runtime.OpeningNode.Responses.Single().SetFlags,
                Is.EqualTo(new[] { "original-response-flag" }));
        }

        [TestCase(null)]
        [TestCase("")]
        public void Compile_RejectsMissingResponseId(string responseId)
        {
            var scenario = Scenario();
            scenario.Nodes[0].Responses[0].Id = responseId;

            Assert.Throws<InvalidOperationException>(() => RuntimeScenario.Compile(scenario));
        }

        [Test]
        public void Compile_RejectsDuplicateResponseIdsWithinNode()
        {
            var scenario = Scenario(
                Response("duplicate", "terminal"),
                Response("duplicate", "terminal"));

            Assert.Throws<InvalidOperationException>(() => RuntimeScenario.Compile(scenario));
        }

        [Test]
        public void Compile_ResponseIdIdentityIsOrdinalCaseSensitive()
        {
            var scenario = Scenario(
                Response("answer", "terminal"),
                Response("Answer", "terminal"));

            var runtime = RuntimeScenario.Compile(scenario);

            Assert.That(
                runtime.OpeningNode.Responses.Select(response => response.Id),
                Is.EqualTo(new[] { "answer", "Answer" }));
        }

        [Test]
        public void Select_ResponseSetFlagCanUnlockDestination()
        {
            var scenario = Scenario(Response(
                "unlock",
                "terminal",
                setFlags: new[] { "destination-unlocked" }));
            scenario.Nodes[1].RequiredFlags = new[] { "destination-unlocked" };
            var session = Session(scenario);

            var result = session.Select("unlock", 50);

            Assert.That(result.IsAccepted, Is.True);
            Assert.That(session.CurrentNode.Id, Is.EqualTo("terminal"));
            Assert.That(session.HasFlag("destination-unlocked"), Is.True);
        }

        [Test]
        public void Select_ResponseSetFlagBlockedByDestinationIsRejectedWithoutMutation()
        {
            var scenario = Scenario(Response(
                "block",
                "terminal",
                setFlags: new[] { "destination-blocked" }));
            scenario.Nodes[1].BlockedFlags = new[] { "destination-blocked" };
            var session = Session(scenario);

            var result = session.Select("block", 50);

            Assert.That(result.IsAccepted, Is.False);
            Assert.That(session.CurrentNode.Id, Is.EqualTo("opening"));
            Assert.That(session.HasFlag("destination-blocked"), Is.False);
        }

        [Test]
        public void GetAvailableResponses_UsesOrdinalCaseSensitiveFlagIdentity()
        {
            var scenario = Scenario();
            scenario.Nodes[0].Responses = new[]
            {
                Response("set-lowercase", "question", setFlags: new[] { "proof" }),
            };
            scenario.Nodes = new[]
            {
                scenario.Nodes[0],
                Node("question", Response("needs-uppercase", "terminal", requiredFlags: new[] { "Proof" })),
                scenario.Nodes[1],
            };
            var session = Session(scenario);

            session.Select("set-lowercase", 50);

            Assert.That(session.HasFlag("proof"), Is.True);
            Assert.That(session.HasFlag("Proof"), Is.False);
            Assert.That(session.GetAvailableResponses(50), Is.Empty);
        }

        private static DialogueSession Session(ScenarioDefinitionDto scenario)
        {
            return new DialogueSession(RuntimeScenario.Compile(scenario));
        }

        private static ScenarioDefinitionDto Scenario(params ResponseOptionDto[] openingResponses)
        {
            return new ScenarioDefinitionDto
            {
                Id = "runtime-test",
                Version = 1,
                TitleKey = "scenario.title",
                BriefingKey = "scenario.briefing",
                OpeningNodeId = "opening",
                Nodes = new[]
                {
                    Node(
                        "opening",
                        openingResponses.Length == 0
                            ? new[] { Response("continue", "terminal") }
                            : openingResponses),
                    Terminal("terminal"),
                },
            };
        }

        private static DialogueNodeDto Node(string id, params ResponseOptionDto[] responses)
        {
            return new DialogueNodeDto
            {
                Id = id,
                NodeType = "Question",
                Speaker = "judge",
                TextKey = $"node.{id}",
                TimerSeconds = 15,
                Responses = responses,
            };
        }

        private static DialogueNodeDto Terminal(string id)
        {
            return new DialogueNodeDto
            {
                Id = id,
                NodeType = "Terminal",
                Speaker = "judge",
                TextKey = $"node.{id}",
                Responses = Array.Empty<ResponseOptionDto>(),
            };
        }

        private static ResponseOptionDto Response(
            string id,
            string destination,
            string[] setFlags = null,
            string[] requiredFlags = null,
            string[] blockedFlags = null,
            int? minimumConfidence = null,
            int? maximumConfidence = null)
        {
            return new ResponseOptionDto
            {
                Id = id,
                TextKey = $"response.{id}",
                QualityTier = "Strong",
                FeedbackKey = $"feedback.{id}",
                ExplanationKey = $"explanation.{id}",
                NextNodeId = destination,
                SetFlags = setFlags ?? Array.Empty<string>(),
                RequiredFlags = requiredFlags ?? Array.Empty<string>(),
                BlockedFlags = blockedFlags ?? Array.Empty<string>(),
                HasConfidenceRange = minimumConfidence.HasValue || maximumConfidence.HasValue,
                MinimumConfidence = minimumConfidence ?? 0,
                MaximumConfidence = maximumConfidence ?? 100,
            };
        }
    }
}
