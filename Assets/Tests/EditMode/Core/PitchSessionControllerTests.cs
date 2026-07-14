using System;
using System.Collections.Generic;
using System.Linq;
using Agrovator.PitchSimulator.Accessibility;
using Agrovator.PitchSimulator.Core;
using Agrovator.PitchSimulator.Dialogue;
using Agrovator.PitchSimulator.LMS;
using Agrovator.PitchSimulator.Scoring;
using NUnit.Framework;

namespace Agrovator.PitchSimulator.Tests.EditMode.Core
{
    public sealed class PitchSessionControllerTests
    {
        private static readonly DateTimeOffset StartedAt =
            new DateTimeOffset(2026, 7, 14, 2, 0, 0, TimeSpan.Zero);

        [Test]
        public void FinishLaunch_ValidConfigurationEntersTitle()
        {
            var fixture = CreateFixture();

            Assert.That(fixture.Controller.FinishLaunch(), Is.True);

            Assert.That(fixture.Controller.Snapshot.State, Is.EqualTo(GameState.Title));
            Assert.That(fixture.Controller.Snapshot.AttemptNumber, Is.EqualTo(2));
        }

        [Test]
        public void StartScenario_FromTitleEntersBriefingWithFreshTutorialNode()
        {
            var fixture = CreateFixture();
            fixture.Controller.FinishLaunch();

            Assert.That(fixture.Controller.StartScenario(), Is.True);

            Assert.That(fixture.Controller.Snapshot.State, Is.EqualTo(GameState.Briefing));
            Assert.That(fixture.Controller.Snapshot.CurrentNodeId, Is.EqualTo("tutorial"));
            Assert.That(fixture.Controller.Snapshot.OverallScore, Is.Zero);
            Assert.That(fixture.Controller.Snapshot.Confidence, Is.EqualTo(50));
        }

        [Test]
        public void TutorialResponse_TraversesWithoutScoreOrConfidenceChange()
        {
            var fixture = CreateStartedFixture();
            EnterAwaitingResponse(fixture.Controller);

            Assert.That(fixture.Controller.SelectResponse("tutorial-ready"), Is.True);

            Assert.That(fixture.Controller.Snapshot.State, Is.EqualTo(GameState.ShowingReaction));
            Assert.That(fixture.Controller.Snapshot.CurrentNodeId, Is.EqualTo("question"));
            Assert.That(fixture.Controller.Snapshot.OverallScore, Is.Zero);
            Assert.That(fixture.Controller.Snapshot.Confidence, Is.EqualTo(50));
        }

        [Test]
        public void ScoredResponse_AppliesRubricConfidenceAndPublishesReaction()
        {
            var fixture = CreateStartedFixture();
            AdvancePastTutorial(fixture.Controller);
            PitchSessionEvent published = null;
            fixture.Controller.EventPublished += value => published = value;

            Assert.That(fixture.Controller.SelectResponse("question-strong"), Is.True);

            Assert.That(fixture.Controller.Snapshot.OverallScore, Is.EqualTo(5));
            Assert.That(fixture.Controller.Snapshot.Confidence, Is.EqualTo(54));
            Assert.That(fixture.Controller.Snapshot.LastResponseId, Is.EqualTo("question-strong"));
            Assert.That(published.Type, Is.EqualTo(PitchSessionEventType.ReactionReady));
            Assert.That(published.ReactionCue, Is.EqualTo("Impressed"));
        }

        [Test]
        public void TimerExpiry_UsesNeutralTraversalWithoutAuthoredScoreConfidenceOrSelection()
        {
            var fixture = CreateStartedFixture();
            AdvancePastTutorial(fixture.Controller);
            var beforeScore = fixture.Controller.Snapshot.OverallScore;
            var beforeConfidence = fixture.Controller.Snapshot.Confidence;
            PitchSessionEvent published = null;
            fixture.Controller.EventPublished += value => published = value;

            fixture.Controller.Tick(5d);

            Assert.That(fixture.Controller.Snapshot.State, Is.EqualTo(GameState.ShowingReaction));
            Assert.That(fixture.Controller.Snapshot.CurrentNodeId, Is.EqualTo("standard"));
            Assert.That(fixture.Controller.Snapshot.OverallScore, Is.EqualTo(beforeScore));
            Assert.That(fixture.Controller.Snapshot.Confidence, Is.EqualTo(beforeConfidence));
            Assert.That(fixture.Controller.Snapshot.TimeoutCount, Is.EqualTo(1));
            Assert.That(fixture.Controller.Snapshot.LastResponseId, Is.Null);
            Assert.That(fixture.Controller.Snapshot.SelectedResponseIds, Is.Empty);
            Assert.That(published.Type, Is.EqualTo(PitchSessionEventType.TimeoutReactionReady));
            Assert.That(published.ReactionCue, Is.EqualTo("Neutral"));
        }

        [Test]
        public void ContinueAfterSelection_PublishesReactionBeforeFeedback()
        {
            var fixture = CreateStartedFixture();
            AdvancePastTutorial(fixture.Controller);
            var eventTypes = new List<PitchSessionEventType>();
            fixture.Controller.EventPublished += value => eventTypes.Add(value.Type);

            fixture.Controller.SelectResponse("question-strong");
            fixture.Controller.Continue();

            Assert.That(eventTypes, Is.EqualTo(new[]
            {
                PitchSessionEventType.ReactionReady,
                PitchSessionEventType.FeedbackReady,
            }));
            Assert.That(fixture.Controller.Snapshot.State, Is.EqualTo(GameState.ShowingFeedback));
            Assert.That(fixture.Controller.Snapshot.LastFeedbackKey, Is.EqualTo("feedback.strong"));
        }

        [Test]
        public void WeakResponse_FollowsConditionalRecoveryDestination()
        {
            var fixture = CreateStartedFixture();
            AdvancePastTutorial(fixture.Controller);

            fixture.Controller.SelectResponse("question-weak");

            Assert.That(fixture.Controller.Snapshot.CurrentNodeId, Is.EqualTo("recovery"));
            Assert.That(fixture.Controller.Snapshot.AvailableResponses.Select(value => value.Id),
                Is.EquivalentTo(new[] { "recovery-finish" }));
        }

        [Test]
        public void TerminalFeedback_CreatesValidResultsPayloadFromLaunchAndSessionState()
        {
            var fixture = CreateStartedFixture();
            AdvancePastTutorial(fixture.Controller);
            fixture.Controller.SelectResponse("question-strong");
            AdvanceFeedbackToNextQuestion(fixture.Controller);
            fixture.Controller.SelectResponse("standard-finish");

            fixture.Controller.Continue();
            fixture.Controller.Continue();

            var snapshot = fixture.Controller.Snapshot;
            Assert.That(snapshot.State, Is.EqualTo(GameState.Results));
            Assert.That(snapshot.Result, Is.Not.Null);
            Assert.That(snapshot.CompletionPayload, Is.Not.Null);
            Assert.That(snapshot.CompletionPayload.SessionId, Is.EqualTo("session-42"));
            Assert.That(snapshot.CompletionPayload.ScenarioId, Is.EqualTo("scenario"));
            Assert.That(snapshot.CompletionPayload.OverallScore, Is.EqualTo(snapshot.OverallScore));
            Assert.That(snapshot.CompletionPayload.FinalConfidence, Is.EqualTo(snapshot.Confidence));
            Assert.That(snapshot.CompletionPayload.SelectedResponseIds,
                Is.EqualTo(new[] { "question-strong", "standard-finish" }));
            Assert.That(snapshot.CompletionPayload.DurationSeconds, Is.EqualTo(0d));
            Assert.That(LmsPayloadValidator.ValidateCompletion(snapshot.CompletionPayload), Is.Empty);
        }

        [Test]
        public void SubmitResults_SuccessTransitionsThroughSubmittingToComplete()
        {
            var fixture = CreateAtResults();

            Assert.That(fixture.Controller.SubmitResults(), Is.True);

            Assert.That(fixture.Controller.Snapshot.State, Is.EqualTo(GameState.Complete));
            Assert.That(fixture.Bridge.LastSubmittedAttemptNumber, Is.EqualTo(2));
        }

        [Test]
        public void Retry_ClearsRuntimeStateAndIncrementsAttempt()
        {
            var fixture = CreateStartedFixture();
            AdvancePastTutorial(fixture.Controller);
            fixture.Controller.Tick(5d);
            fixture.Controller.Continue();
            fixture.Controller.Continue();
            EnterAwaitingResponse(fixture.Controller);
            fixture.Controller.SelectResponse("standard-finish");
            fixture.Controller.Continue();
            fixture.Controller.Continue();
            Assert.That(fixture.Controller.Snapshot.State, Is.EqualTo(GameState.Results));

            Assert.That(fixture.Controller.Retry(), Is.True);

            var snapshot = fixture.Controller.Snapshot;
            Assert.That(snapshot.State, Is.EqualTo(GameState.Briefing));
            Assert.That(snapshot.AttemptNumber, Is.EqualTo(3));
            Assert.That(snapshot.CurrentNodeId, Is.EqualTo("tutorial"));
            Assert.That(snapshot.OverallScore, Is.Zero);
            Assert.That(snapshot.Confidence, Is.EqualTo(50));
            Assert.That(snapshot.TimeoutCount, Is.Zero);
            Assert.That(snapshot.SelectedResponseIds, Is.Empty);
            Assert.That(snapshot.LastResponseId, Is.Null);
            Assert.That(snapshot.LastFeedbackKey, Is.Null);
            Assert.That(snapshot.Result, Is.Null);
            Assert.That(snapshot.CompletionPayload, Is.Null);
        }

        private static Fixture CreateAtResults()
        {
            var fixture = CreateStartedFixture();
            AdvancePastTutorial(fixture.Controller);
            fixture.Controller.SelectResponse("question-strong");
            AdvanceFeedbackToNextQuestion(fixture.Controller);
            fixture.Controller.SelectResponse("standard-finish");
            fixture.Controller.Continue();
            fixture.Controller.Continue();
            return fixture;
        }

        private static Fixture CreateStartedFixture()
        {
            var fixture = CreateFixture();
            fixture.Controller.FinishLaunch();
            fixture.Controller.StartScenario();
            return fixture;
        }

        private static void AdvancePastTutorial(PitchSessionController controller)
        {
            EnterAwaitingResponse(controller);
            controller.SelectResponse("tutorial-ready");
            AdvanceFeedbackToNextQuestion(controller);
        }

        private static void AdvanceFeedbackToNextQuestion(PitchSessionController controller)
        {
            Assert.That(controller.Continue(), Is.True);
            Assert.That(controller.Continue(), Is.True);
            Assert.That(controller.Snapshot.State, Is.EqualTo(GameState.AskingQuestion));
            Assert.That(controller.Continue(), Is.True);
            Assert.That(controller.Snapshot.State, Is.EqualTo(GameState.AwaitingResponse));
        }

        private static void EnterAwaitingResponse(PitchSessionController controller)
        {
            while (controller.Snapshot.State != GameState.AwaitingResponse)
            {
                Assert.That(controller.Continue(), Is.True);
            }
        }

        private static Fixture CreateFixture()
        {
            var bridge = new MockLmsBridge(MockLmsBridgeMode.Success, ValidLaunch());
            var controller = new PitchSessionController(
                BuildScenario(),
                new ScoreAccumulator(),
                new AccessibilitySettings(TimerMode.Normal, false, 0.75f, 0.8f, "en"),
                new QuestionTimer(0d),
                bridge,
                () => StartedAt,
                "0.1.0");
            return new Fixture(controller, bridge);
        }

        private static RuntimeScenario BuildScenario()
        {
            return RuntimeScenario.Compile(new ScenarioDefinitionDto
            {
                Id = "scenario",
                Version = 1,
                InitialConfidence = 50,
                OpeningNodeId = "tutorial",
                Nodes = new[]
                {
                    Node("tutorial", "Tutorial", 0, Response("tutorial-ready", "Strong", "question", 9, 9)),
                    Node("question", "Question", 5,
                        Response("question-strong", "Strong", "standard", 3, 2, 4, "Impressed", "feedback.strong"),
                        Response("question-neutral", "Developing", "standard", 2, 1, 1, "Curious", "feedback.neutral"),
                        Response("question-weak", "NeedsWork", "recovery", 0, 0, -3, "Concerned", "feedback.weak", "weak")),
                    Node("standard", "Question", 5,
                        Response("standard-finish", "Strong", "complete", 3, 2, 2)),
                    Node("recovery", "Question", 5,
                        Response("recovery-finish", "Strong", "complete", 3, 2, 3)),
                    Node("complete", "Terminal", 0),
                },
            });
        }

        private static DialogueNodeDto Node(
            string id,
            string nodeType,
            int timerSeconds,
            params ResponseOptionDto[] responses)
        {
            return new DialogueNodeDto
            {
                Id = id,
                NodeType = nodeType,
                TextKey = "node." + id,
                TimerSeconds = timerSeconds,
                Responses = responses,
            };
        }

        private static ResponseOptionDto Response(
            string id,
            string quality,
            string nextNodeId,
            int clarity,
            int communication,
            int confidence = 0,
            string reaction = "Encouraging",
            string feedback = "feedback.default",
            string flag = null)
        {
            return new ResponseOptionDto
            {
                Id = id,
                TextKey = "response." + id,
                QualityTier = quality,
                ScoreDelta = new ResponseScoreDeltaDto
                {
                    ClearExplanation = clarity,
                    Communication = communication,
                },
                ConfidenceDelta = confidence,
                CompetencyTags = new[] { "clear_explanation", "communication" },
                ReactionCue = reaction,
                FeedbackKey = feedback,
                ExplanationKey = "explanation." + id,
                NextNodeId = nextNodeId,
                SetFlags = flag == null ? Array.Empty<string>() : new[] { flag },
            };
        }

        private static LmsLaunchConfig ValidLaunch()
        {
            return new LmsLaunchConfig
            {
                PseudonymousLearnerId = "learner-7b9",
                SessionId = "session-42",
                CourseId = "course-garden",
                ModuleId = "module-pitching",
                LessonId = "lesson-scenario",
                ScenarioId = "scenario",
                Language = "en",
                AttemptNumber = 2,
                TimerMode = "normal",
                ReducedMotion = false,
                MusicVolume = 0.75f,
                SfxVolume = 0.8f,
                ContentVersion = 1,
                LaunchReference = "lref_opaque_ref_17",
            };
        }

        private sealed class Fixture
        {
            public Fixture(PitchSessionController controller, MockLmsBridge bridge)
            {
                Controller = controller;
                Bridge = bridge;
            }

            public PitchSessionController Controller { get; }

            public MockLmsBridge Bridge { get; }
        }
    }
}
