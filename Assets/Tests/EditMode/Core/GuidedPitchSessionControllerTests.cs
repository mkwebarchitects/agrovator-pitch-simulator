using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Agrovator.PitchSimulator.Accessibility;
using Agrovator.PitchSimulator.Core;
using Agrovator.PitchSimulator.GuidedPitch;
using Agrovator.PitchSimulator.LMS;
using NUnit.Framework;

namespace Agrovator.PitchSimulator.Tests.EditMode.Core
{
    public sealed class GuidedPitchSessionControllerTests
    {
        private static readonly DateTimeOffset StartedAt =
            new DateTimeOffset(2026, 7, 15, 3, 0, 0, TimeSpan.Zero);

        [Test]
        public void SuccessfulPrimaryPath_ExposesEveryPhaseAndCreatesPrivacyMinimizedPayload()
        {
            var fixture = CreateFixture();
            var events = new List<GuidedPitchSessionEvent>();
            fixture.Controller.EventPublished += events.Add;

            AssertSnapshot(fixture.Controller, GuidedPitchPhase.Booting, null, null);
            Assert.That(fixture.Controller.FinishLaunch(), Is.True);
            AssertSnapshot(fixture.Controller, GuidedPitchPhase.Title, null, null);
            Assert.That(fixture.Controller.StartScenario(), Is.True);
            AssertSnapshot(fixture.Controller, GuidedPitchPhase.Briefing, null, null);
            fixture.Controller.Tick(12.5d);
            Assert.That(fixture.Controller.Continue(), Is.True);
            AssertSnapshot(fixture.Controller, GuidedPitchPhase.ModeSelection, null, null);
            Assert.That(fixture.Controller.SelectLearnerMode(LearnerMode.Primary), Is.True);
            AssertSnapshot(fixture.Controller, GuidedPitchPhase.Learn, LearnerMode.Primary, null);
            Assert.That(fixture.Controller.Continue(), Is.True);
            AssertBuild(fixture.Controller, PitchPart.Problem, "primary-problem-clear", "primary-problem-developing", "primary-problem-needs-practice");

            SelectAndAssert(
                fixture.Controller,
                PitchPart.Problem,
                "primary-problem-needs-practice",
                MasteryState.NeedsPractice,
                10,
                "guided.feedback.primary-problem-needs-practice.worked",
                "guided.feedback.primary-problem-needs-practice.missing",
                "guided.feedback.primary-problem-needs-practice.improve");
            Assert.That(fixture.Controller.Continue(), Is.True);
            AssertBuild(fixture.Controller, PitchPart.Evidence, "primary-evidence-clear", "primary-evidence-developing", "primary-evidence-needs-practice");

            SelectAndAssert(fixture.Controller, PitchPart.Evidence, "primary-evidence-clear", MasteryState.Clear, 35,
                "guided.feedback.primary-evidence-clear.worked", "guided.feedback.primary-evidence-clear.missing", "guided.feedback.primary-evidence-clear.improve");
            Assert.That(fixture.Controller.Continue(), Is.True);
            AssertBuild(fixture.Controller, PitchPart.Solution, "primary-solution-clear", "primary-solution-developing", "primary-solution-needs-practice");

            SelectAndAssert(fixture.Controller, PitchPart.Solution, "primary-solution-clear", MasteryState.Clear, 60,
                "guided.feedback.primary-solution-clear.worked", "guided.feedback.primary-solution-clear.missing", "guided.feedback.primary-solution-clear.improve");
            Assert.That(fixture.Controller.Continue(), Is.True);
            AssertBuild(fixture.Controller, PitchPart.Value, "primary-value-clear", "primary-value-developing", "primary-value-needs-practice");

            SelectAndAssert(fixture.Controller, PitchPart.Value, "primary-value-clear", MasteryState.Clear, 85,
                "guided.feedback.primary-value-clear.worked", "guided.feedback.primary-value-clear.missing", "guided.feedback.primary-value-clear.improve");
            Assert.That(fixture.Controller.Continue(), Is.True);
            AssertSnapshot(fixture.Controller, GuidedPitchPhase.Improve, LearnerMode.Primary, null);
            Assert.That(fixture.Controller.Snapshot.AvailableOptions, Is.Empty);

            Assert.That(fixture.Controller.BeginRevision(PitchPart.Problem), Is.True);
            AssertSnapshot(fixture.Controller, GuidedPitchPhase.Improve, LearnerMode.Primary, PitchPart.Problem);
            Assert.That(fixture.Controller.Snapshot.AvailableOptions.Select(option => option.Id), Is.EqualTo(new[]
            {
                "primary-problem-clear", "primary-problem-developing", "primary-problem-needs-practice",
            }));
            Assert.That(fixture.Controller.ReplacePitchResponse("primary-problem-clear"), Is.True);
            Assert.That(fixture.Controller.Snapshot.Draft[PitchPart.Problem].InitialMastery, Is.EqualTo(MasteryState.NeedsPractice));
            Assert.That(fixture.Controller.Snapshot.Draft[PitchPart.Problem].CurrentMastery, Is.EqualTo(MasteryState.Clear));
            Assert.That(fixture.Controller.Snapshot.Assessment.PitchReadiness, Is.EqualTo(100));
            Assert.That(fixture.Controller.Snapshot.Assessment.ImprovedPartCount, Is.EqualTo(1));

            Assert.That(fixture.Controller.PresentPitch(), Is.True);
            AssertSnapshot(fixture.Controller, GuidedPitchPhase.Present, LearnerMode.Primary, null);
            Assert.That(fixture.Controller.Continue(), Is.True);
            AssertSnapshot(fixture.Controller, GuidedPitchPhase.FollowUp, LearnerMode.Primary, null);
            Assert.That(fixture.Controller.Snapshot.AvailableOptions.Select(option => option.Id), Is.EqualTo(new[]
            {
                "primary-cost-clear", "primary-cost-developing", "primary-cost-needs-practice",
            }));
            Assert.That(fixture.Controller.SelectFollowUpResponse("primary-cost-clear"), Is.True);
            AssertSnapshot(fixture.Controller, GuidedPitchPhase.FollowUpFeedback, LearnerMode.Primary, null);
            Assert.That(fixture.Controller.Snapshot.FollowUpResponseId, Is.EqualTo("primary-cost-clear"));
            Assert.That(fixture.Controller.Snapshot.Feedback.WorkedKey, Is.EqualTo("guided.feedback.primary-cost-clear.worked"));
            fixture.Now = StartedAt.AddMinutes(2);
            Assert.That(fixture.Controller.Continue(), Is.True);

            var snapshot = fixture.Controller.Snapshot;
            AssertSnapshot(fixture.Controller, GuidedPitchPhase.Results, LearnerMode.Primary, null);
            Assert.That(snapshot.Assessment.PitchReadiness, Is.EqualTo(100));
            Assert.That(snapshot.SelectionHistory, Is.EqualTo(new[]
            {
                "primary-problem-needs-practice", "primary-evidence-clear", "primary-solution-clear",
                "primary-value-clear", "primary-problem-clear", "primary-cost-clear",
            }));
            var payload = snapshot.CompletionPayload;
            Assert.That(payload.ContentVersion, Is.EqualTo(2));
            Assert.That(payload.OverallScore, Is.EqualTo(snapshot.Assessment.PitchReadiness));
            Assert.That(payload.CompetencyScores.Select(score => score.CompetencyId), Is.EqualTo(new[]
            {
                "problem", "evidence", "solution", "audience", "clear_explanation", "communication",
            }));
            Assert.That(payload.CompetencyScores, Has.None.Matches<LmsCompetencyScore>(score =>
                score.CompetencyId == "time_management"));
            Assert.That(payload.FinalConfidence, Is.EqualTo(70));
            Assert.That(payload.SelectedResponseIds, Is.EqualTo(snapshot.SelectionHistory));
            Assert.That(payload.TimeoutCount, Is.Zero);
            Assert.That(payload.DurationSeconds, Is.EqualTo(12.5d));
            Assert.That(payload.StartedAtUtc, Is.EqualTo("2026-07-15T03:00:00Z"));
            Assert.That(payload.CompletedAtUtc, Is.EqualTo("2026-07-15T03:02:00Z"));
            Assert.That(LmsPayloadValidator.ValidateCompletion(payload), Is.Empty);
            Assert.That(LmsPayloadValidator.ValidateCompletionPrivacyShape(), Is.Empty);
            Assert.That(LmsPayloadJson.SerializeCompletion(payload), Does.Not.Contain("Our garden beds"));

            Assert.That(events.Select(value => value.Type), Is.EqualTo(new[]
            {
                GuidedPitchSessionEventType.ResponseSelected, GuidedPitchSessionEventType.FeedbackReady,
                GuidedPitchSessionEventType.ResponseSelected, GuidedPitchSessionEventType.FeedbackReady,
                GuidedPitchSessionEventType.ResponseSelected, GuidedPitchSessionEventType.FeedbackReady,
                GuidedPitchSessionEventType.ResponseSelected, GuidedPitchSessionEventType.FeedbackReady,
                GuidedPitchSessionEventType.ResponseSelected,
                GuidedPitchSessionEventType.ResponseSelected, GuidedPitchSessionEventType.FeedbackReady,
                GuidedPitchSessionEventType.ResultsReady,
            }));
            Assert.That(events[0].ResponseId, Is.EqualTo("primary-problem-needs-practice"));
            Assert.That(events[0].ReactionCue, Is.EqualTo("Concerned"));
            Assert.That(events[1].MessageKey, Is.EqualTo("guided.feedback.primary-problem-needs-practice.improve"));
        }

        [Test]
        public void InvalidCommandsAndMismatchedResponses_ReturnFalseWithoutMutation()
        {
            var fixture = CreateFixture();
            AssertRejectedWithoutMutation(fixture.Controller, () => fixture.Controller.StartScenario());
            AssertRejectedWithoutMutation(fixture.Controller, () => fixture.Controller.Continue());
            AssertRejectedWithoutMutation(fixture.Controller, () => fixture.Controller.SelectLearnerMode(LearnerMode.Primary));
            AssertRejectedWithoutMutation(fixture.Controller, () => fixture.Controller.SelectPitchResponse("primary-problem-clear"));
            fixture.Controller.FinishLaunch();
            fixture.Controller.StartScenario();
            fixture.Controller.Continue();
            AssertRejectedWithoutMutation(fixture.Controller, () => fixture.Controller.SelectLearnerMode((LearnerMode)99));
            fixture.Controller.SelectLearnerMode(LearnerMode.Primary);
            fixture.Controller.Continue();

            AssertRejectedWithoutMutation(fixture.Controller, () => fixture.Controller.PresentPitch());
            AssertRejectedWithoutMutation(fixture.Controller, () => fixture.Controller.SelectPitchResponse("primary-evidence-clear"));
            AssertRejectedWithoutMutation(fixture.Controller, () => fixture.Controller.SelectPitchResponse("intro-clear-problem"));
            AssertRejectedWithoutMutation(fixture.Controller, () => fixture.Controller.SelectPitchResponse("missing"));
            fixture.Controller.SelectPitchResponse("primary-problem-developing");
            AssertRejectedWithoutMutation(fixture.Controller, () => fixture.Controller.SelectPitchResponse("primary-problem-clear"));
            fixture.Controller.Continue();
            fixture.Controller.SelectPitchResponse("primary-evidence-clear");
            fixture.Controller.Continue();
            fixture.Controller.SelectPitchResponse("primary-solution-clear");
            fixture.Controller.Continue();
            fixture.Controller.SelectPitchResponse("primary-value-clear");
            fixture.Controller.Continue();

            AssertRejectedWithoutMutation(fixture.Controller, () => fixture.Controller.BeginRevision(PitchPart.Evidence));
            AssertRejectedWithoutMutation(fixture.Controller, () => fixture.Controller.BeginRevision((PitchPart)99));
            AssertRejectedWithoutMutation(fixture.Controller, () => fixture.Controller.ReplacePitchResponse("primary-problem-needs-practice"));
            Assert.That(fixture.Controller.BeginRevision(PitchPart.Problem), Is.True);
            AssertRejectedWithoutMutation(fixture.Controller, () => fixture.Controller.ReplacePitchResponse("primary-evidence-clear"));
            AssertRejectedWithoutMutation(fixture.Controller, () => fixture.Controller.ReplacePitchResponse("intro-clear-problem"));

            Assert.That(fixture.Controller.ReplacePitchResponse("primary-problem-needs-practice"), Is.True,
                "A weaker response is accepted after the learner explicitly selects that revision part.");
            Assert.That(fixture.Controller.Snapshot.Draft[PitchPart.Problem].InitialMastery, Is.EqualTo(MasteryState.Developing));
            Assert.That(fixture.Controller.Snapshot.Draft[PitchPart.Problem].CurrentMastery, Is.EqualTo(MasteryState.NeedsPractice));
            Assert.That(fixture.Controller.Snapshot.Assessment.ImprovedPartCount, Is.Zero);
            Assert.That(fixture.Controller.Snapshot.Assessment.PitchReadiness, Is.EqualTo(85));
        }

        [Test]
        public void SubmissionFailure_PreservesResultsAndIgnoresDuplicateOrStaleCallbacks()
        {
            var bridge = new DeferredBridge(ValidLaunch());
            var fixture = CreateFixture(bridge);
            MoveRevisedToResults(fixture.Controller);
            var before = fixture.Controller.Snapshot;
            var beforePayload = before.CompletionPayload;
            var eventTypes = new List<GuidedPitchSessionEventType>();
            fixture.Controller.EventPublished += value => eventTypes.Add(value.Type);

            Assert.That(fixture.Controller.SubmitResults(), Is.True);
            Assert.That(fixture.Controller.Snapshot.Phase, Is.EqualTo(GuidedPitchPhase.Submitting));
            bridge.Fail(new LmsSubmissionError(LmsSubmissionErrorCode.SubmissionFailed, "lms.submission.failed", 2));

            var failed = fixture.Controller.Snapshot;
            Assert.That(failed.Phase, Is.EqualTo(GuidedPitchPhase.Results));
            Assert.That(failed.Draft[PitchPart.Problem].CurrentResponseId, Is.EqualTo(before.Draft[PitchPart.Problem].CurrentResponseId));
            Assert.That(failed.Assessment.PitchReadiness, Is.EqualTo(before.Assessment.PitchReadiness));
            Assert.That(failed.SelectionHistory, Is.EqualTo(before.SelectionHistory));
            AssertPayloadEqual(failed.CompletionPayload, beforePayload);
            Assert.That(failed.SubmissionError.MessageKey, Is.EqualTo("lms.submission.failed"));
            bridge.Fail(new LmsSubmissionError(LmsSubmissionErrorCode.SessionExpired, "lms.session.expired", 2));
            bridge.Succeed();
            Assert.That(fixture.Controller.Snapshot.SubmissionError.MessageKey, Is.EqualTo("lms.submission.failed"));
            Assert.That(eventTypes, Is.EqualTo(new[] { GuidedPitchSessionEventType.SubmissionFailed }));

            Assert.That(fixture.Controller.SubmitResults(), Is.True);
            bridge.Succeed();
            Assert.That(fixture.Controller.Snapshot.Phase, Is.EqualTo(GuidedPitchPhase.Complete));
            bridge.Fail(null);
            Assert.That(eventTypes, Is.EqualTo(new[]
            {
                GuidedPitchSessionEventType.SubmissionFailed,
                GuidedPitchSessionEventType.SubmissionSucceeded,
            }));
        }

        [Test]
        public void Retry_ClearsAttemptStateAndMakesPriorSubmissionCallbacksStale()
        {
            var bridge = new DeferredBridge(ValidLaunch());
            var fixture = CreateFixture(bridge);
            MoveRevisedToResults(fixture.Controller);
            fixture.Controller.SubmitResults();
            bridge.Fail(null);
            Assert.That(fixture.Controller.Snapshot.SubmissionError, Is.Not.Null);

            Assert.That(fixture.Controller.Retry(), Is.True);

            var snapshot = fixture.Controller.Snapshot;
            AssertSnapshot(fixture.Controller, GuidedPitchPhase.Briefing, null, null);
            Assert.That(snapshot.AttemptNumber, Is.EqualTo(3));
            Assert.That(snapshot.Draft.IsComplete, Is.False);
            Assert.That(snapshot.Draft.PopulatedCount, Is.Zero);
            foreach (var part in PitchParts.Ordered)
            {
                Assert.That(snapshot.Draft[part].InitialResponseId, Is.Null);
                Assert.That(snapshot.Draft[part].CurrentResponseId, Is.Null);
                Assert.That(snapshot.Draft[part].WasRevised, Is.False);
            }
            Assert.That(snapshot.Assessment.PitchReadiness, Is.Zero);
            Assert.That(snapshot.Feedback, Is.Null);
            Assert.That(snapshot.FollowUpResponseId, Is.Null);
            Assert.That(snapshot.SelectionHistory, Is.Empty);
            Assert.That(snapshot.CompletionPayload, Is.Null);
            Assert.That(snapshot.SubmissionError, Is.Null);
            bridge.Succeed();
            bridge.Fail(null);
            Assert.That(fixture.Controller.Snapshot.Phase, Is.EqualTo(GuidedPitchPhase.Briefing));
        }

        [Test]
        public void SnapshotAndPayloadCollections_AreDefensiveAndDisposeRejectsCommands()
        {
            var fixture = CreateFixture();
            MoveToResults(fixture.Controller);
            var first = fixture.Controller.Snapshot.CompletionPayload;
            first.SelectedResponseIds[0] = "mutated";
            first.CompetencyScores[0].CompetencyId = "mutated";
            var second = fixture.Controller.Snapshot.CompletionPayload;

            Assert.That(second.SelectedResponseIds[0], Is.EqualTo("primary-problem-clear"));
            Assert.That(second.CompetencyScores[0].CompetencyId, Is.EqualTo("problem"));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<string>)fixture.Controller.Snapshot.SelectionHistory).Add("mutated"));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<GuidedPitchOption>)fixture.Controller.Snapshot.AvailableOptions).Add(null));

            fixture.Controller.Dispose();
            fixture.Controller.Dispose();
            Assert.That(fixture.Controller.SubmitResults(), Is.False);
            Assert.That(fixture.Controller.Retry(), Is.False);
            Assert.DoesNotThrow(() => fixture.Controller.Tick(1d));
        }

        [TestCase(-1d)]
        [TestCase(double.NaN)]
        [TestCase(double.NegativeInfinity)]
        [TestCase(double.PositiveInfinity)]
        public void Tick_RejectsInvalidDurations(double seconds)
        {
            var fixture = CreateFixture();

            Assert.Throws<ArgumentOutOfRangeException>(() => fixture.Controller.Tick(seconds));
        }

        [Test]
        public void Tick_DuringSessionAccumulatesPayloadDurationWithoutReplacingObservableSnapshot()
        {
            var fixture = CreateFixture();
            fixture.Controller.FinishLaunch();
            fixture.Controller.StartScenario();
            var beforeTick = fixture.Controller.Snapshot;

            fixture.Controller.Tick(1.25d);

            Assert.That(fixture.Controller.Snapshot, Is.SameAs(beforeTick));
            CompleteFromBriefing(fixture.Controller);
            Assert.That(fixture.Controller.Snapshot.CompletionPayload.DurationSeconds, Is.EqualTo(1.25d));
        }

        [Test]
        public void LastReactionCue_MirrorsTheSelectedOptionAndClearsWhenTheFeedbackDoes()
        {
            var fixture = CreateFixture();
            EnterFirstBuild(fixture.Controller);
            Assert.That(fixture.Controller.Snapshot.LastReactionCue, Is.Null,
                "A Build prompt must not open on a previous reaction.");

            Assert.That(fixture.Controller.SelectPitchResponse("primary-problem-needs-practice"), Is.True);
            Assert.That(fixture.Controller.Snapshot.LastReactionCue, Is.EqualTo("Concerned"));
            Assert.That(fixture.Controller.Continue(), Is.True);
            Assert.That(fixture.Controller.Snapshot.LastReactionCue, Is.Null,
                "Advancing past Build feedback must release the reaction.");

            Assert.That(fixture.Controller.SelectPitchResponse("primary-evidence-developing"), Is.True);
            Assert.That(fixture.Controller.Snapshot.LastReactionCue, Is.EqualTo("Curious"));
            fixture.Controller.Continue();
            Assert.That(fixture.Controller.SelectPitchResponse("primary-solution-clear"), Is.True);
            Assert.That(fixture.Controller.Snapshot.LastReactionCue, Is.EqualTo("Impressed"));
            fixture.Controller.Continue();
            fixture.Controller.SelectPitchResponse("primary-value-clear");
            fixture.Controller.Continue();

            Assert.That(fixture.Controller.Snapshot.Phase, Is.EqualTo(GuidedPitchPhase.Improve));
            Assert.That(fixture.Controller.Snapshot.LastReactionCue, Is.Null);
            Assert.That(fixture.Controller.BeginRevision(PitchPart.Problem), Is.True);
            Assert.That(fixture.Controller.Snapshot.LastReactionCue, Is.Null);
            Assert.That(fixture.Controller.ReplacePitchResponse("primary-problem-clear"), Is.True);
            Assert.That(fixture.Controller.Snapshot.LastReactionCue, Is.EqualTo("Impressed"),
                "An Improve replacement must react to the new statement.");
            Assert.That(fixture.Controller.BeginRevision(PitchPart.Evidence), Is.True);
            Assert.That(fixture.Controller.Snapshot.LastReactionCue, Is.Null,
                "Reopening a revision list must release the previous reaction.");
            Assert.That(fixture.Controller.ReplacePitchResponse("primary-evidence-needs-practice"), Is.True);
            Assert.That(fixture.Controller.Snapshot.LastReactionCue, Is.EqualTo("Concerned"));

            Assert.That(fixture.Controller.PresentPitch(), Is.True);
            Assert.That(fixture.Controller.Snapshot.LastReactionCue, Is.Null,
                "Present must not stage the pitch under the last revision reaction.");
            Assert.That(fixture.Controller.Continue(), Is.True);
            Assert.That(fixture.Controller.SelectFollowUpResponse("primary-cost-needs-practice"), Is.True);
            Assert.That(fixture.Controller.Snapshot.LastReactionCue, Is.EqualTo("Concerned"));
            Assert.That(fixture.Controller.Continue(), Is.True);

            Assert.That(fixture.Controller.Snapshot.Phase, Is.EqualTo(GuidedPitchPhase.Results));
            Assert.That(fixture.Controller.Snapshot.LastReactionCue, Is.Null);
            Assert.That(LmsPayloadJson.SerializeCompletion(fixture.Controller.Snapshot.CompletionPayload),
                Does.Not.Contain("Concerned"),
                "A reaction cue is presentation state and must never reach the LMS payload.");

            Assert.That(fixture.Controller.Retry(), Is.True);
            Assert.That(fixture.Controller.Snapshot.LastReactionCue, Is.Null,
                "Retry must clear the reaction so attempt two never opens on attempt one's face.");
        }

        [Test]
        public void LastReactionCue_ClearsOnRetryFromAnUnsubmittedFollowUpReaction()
        {
            var fixture = CreateFixture();
            MoveToFollowUpFeedback(fixture.Controller);
            Assert.That(fixture.Controller.Snapshot.LastReactionCue, Is.EqualTo("Impressed"));

            Assert.That(fixture.Controller.Continue(), Is.True);
            Assert.That(fixture.Controller.Retry(), Is.True);

            Assert.That(fixture.Controller.Snapshot.Phase, Is.EqualTo(GuidedPitchPhase.Briefing));
            Assert.That(fixture.Controller.Snapshot.LastReactionCue, Is.Null);
        }

        [Test]
        public void BuildFeedbackEvent_PublishesAfterTransitionAndRefreshBeforeSynchronousListener()
        {
            var fixture = CreateFixture();
            EnterFirstBuild(fixture.Controller);
            fixture.Controller.SelectPitchResponse("primary-problem-clear");
            GuidedPitchPhase? observedPhase = null;
            PitchPart? observedPart = null;
            bool? reentrantResult = null;
            var handled = false;
            fixture.Controller.EventPublished += sessionEvent =>
            {
                if (sessionEvent.Type != GuidedPitchSessionEventType.FeedbackReady || handled)
                {
                    return;
                }

                handled = true;
                observedPhase = fixture.Controller.Snapshot.Phase;
                observedPart = fixture.Controller.Snapshot.ActivePart;
                reentrantResult = fixture.Controller.Continue();
            };

            Assert.That(fixture.Controller.Continue(), Is.True);

            Assert.That(observedPhase, Is.EqualTo(GuidedPitchPhase.Build));
            Assert.That(observedPart, Is.EqualTo(PitchPart.Evidence));
            Assert.That(reentrantResult, Is.False);
            Assert.That(fixture.Controller.Snapshot.Phase, Is.EqualTo(GuidedPitchPhase.Build));
            Assert.That(fixture.Controller.Snapshot.ActivePart, Is.EqualTo(PitchPart.Evidence));
        }

        [Test]
        public void FollowUpFeedbackEvent_PublishesAfterResultsSnapshotBeforeSynchronousListener()
        {
            var fixture = CreateFixture();
            MoveToFollowUpFeedback(fixture.Controller);
            GuidedPitchPhase? observedPhase = null;
            bool? reentrantResult = null;
            var resultsReadyCount = 0;
            var handled = false;
            fixture.Controller.EventPublished += sessionEvent =>
            {
                if (sessionEvent.Type == GuidedPitchSessionEventType.ResultsReady)
                {
                    resultsReadyCount++;
                }

                if (sessionEvent.Type != GuidedPitchSessionEventType.FeedbackReady || handled)
                {
                    return;
                }

                handled = true;
                observedPhase = fixture.Controller.Snapshot.Phase;
                reentrantResult = fixture.Controller.Continue();
            };

            Assert.That(fixture.Controller.Continue(), Is.True);

            Assert.That(observedPhase, Is.EqualTo(GuidedPitchPhase.Results));
            Assert.That(reentrantResult, Is.False);
            Assert.That(resultsReadyCount, Is.EqualTo(1));
            Assert.That(fixture.Controller.Snapshot.CompletionPayload, Is.Not.Null);
        }

        private static void SelectAndAssert(
            GuidedPitchSessionController controller,
            PitchPart part,
            string responseId,
            MasteryState mastery,
            int readiness,
            string workedKey,
            string missingKey,
            string improveKey)
        {
            Assert.That(controller.SelectPitchResponse(responseId), Is.True);
            AssertSnapshot(controller, GuidedPitchPhase.BuildFeedback, LearnerMode.Primary, part);
            Assert.That(controller.Snapshot.AvailableOptions, Is.Empty);
            Assert.That(controller.Snapshot.Draft[part].CurrentResponseId, Is.EqualTo(responseId));
            Assert.That(controller.Snapshot.Draft[part].CurrentMastery, Is.EqualTo(mastery));
            Assert.That(controller.Snapshot.Assessment.PitchReadiness, Is.EqualTo(readiness));
            Assert.That(controller.Snapshot.Feedback.WorkedKey, Is.EqualTo(workedKey));
            Assert.That(controller.Snapshot.Feedback.MissingKey, Is.EqualTo(missingKey));
            Assert.That(controller.Snapshot.Feedback.ImproveKey, Is.EqualTo(improveKey));
        }

        private static void AssertBuild(GuidedPitchSessionController controller, PitchPart part, params string[] optionIds)
        {
            AssertSnapshot(controller, GuidedPitchPhase.Build, LearnerMode.Primary, part);
            Assert.That(controller.Snapshot.AvailableOptions.Select(option => option.Id), Is.EqualTo(optionIds));
            Assert.That(controller.Snapshot.Feedback, Is.Null);
        }

        private static void AssertSnapshot(
            GuidedPitchSessionController controller,
            GuidedPitchPhase phase,
            LearnerMode? mode,
            PitchPart? activePart)
        {
            Assert.That(controller.Snapshot.Phase, Is.EqualTo(phase));
            Assert.That(controller.Snapshot.LearnerMode, Is.EqualTo(mode));
            Assert.That(controller.Snapshot.ActivePart, Is.EqualTo(activePart));
            Assert.That(controller.Snapshot.ReducedMotion, Is.False);
        }

        private static void AssertRejectedWithoutMutation(GuidedPitchSessionController controller, Func<bool> command)
        {
            var before = controller.Snapshot;
            Assert.That(command(), Is.False);
            var after = controller.Snapshot;
            Assert.That(after, Is.SameAs(before));
        }

        private static void MoveToResults(GuidedPitchSessionController controller)
        {
            controller.FinishLaunch();
            controller.StartScenario();
            controller.Continue();
            controller.SelectLearnerMode(LearnerMode.Primary);
            controller.Continue();
            foreach (var responseId in new[]
            {
                "primary-problem-clear", "primary-evidence-clear", "primary-solution-clear", "primary-value-clear",
            })
            {
                controller.SelectPitchResponse(responseId);
                controller.Continue();
            }

            controller.PresentPitch();
            controller.Continue();
            controller.SelectFollowUpResponse("primary-cost-clear");
            controller.Continue();
            Assert.That(controller.Snapshot.Phase, Is.EqualTo(GuidedPitchPhase.Results));
        }

        private static void EnterFirstBuild(GuidedPitchSessionController controller)
        {
            controller.FinishLaunch();
            controller.StartScenario();
            controller.Continue();
            controller.SelectLearnerMode(LearnerMode.Primary);
            controller.Continue();
            Assert.That(controller.Snapshot.Phase, Is.EqualTo(GuidedPitchPhase.Build));
            Assert.That(controller.Snapshot.ActivePart, Is.EqualTo(PitchPart.Problem));
        }

        private static void CompleteFromBriefing(GuidedPitchSessionController controller)
        {
            controller.Continue();
            controller.SelectLearnerMode(LearnerMode.Primary);
            controller.Continue();
            CompleteBuildAndFollowUp(controller, stopAtFollowUpFeedback: false);
        }

        private static void MoveToFollowUpFeedback(GuidedPitchSessionController controller)
        {
            EnterFirstBuild(controller);
            CompleteBuildAndFollowUp(controller, stopAtFollowUpFeedback: true);
        }

        private static void CompleteBuildAndFollowUp(
            GuidedPitchSessionController controller,
            bool stopAtFollowUpFeedback)
        {
            foreach (var responseId in new[]
            {
                "primary-problem-clear", "primary-evidence-clear", "primary-solution-clear", "primary-value-clear",
            })
            {
                controller.SelectPitchResponse(responseId);
                controller.Continue();
            }

            controller.PresentPitch();
            controller.Continue();
            controller.SelectFollowUpResponse("primary-cost-clear");
            if (!stopAtFollowUpFeedback)
            {
                controller.Continue();
            }
        }

        private static void MoveRevisedToResults(GuidedPitchSessionController controller)
        {
            controller.FinishLaunch();
            controller.StartScenario();
            controller.Continue();
            controller.SelectLearnerMode(LearnerMode.Primary);
            controller.Continue();
            foreach (var responseId in new[]
            {
                "primary-problem-needs-practice", "primary-evidence-clear",
                "primary-solution-clear", "primary-value-clear",
            })
            {
                controller.SelectPitchResponse(responseId);
                controller.Continue();
            }

            controller.BeginRevision(PitchPart.Problem);
            controller.ReplacePitchResponse("primary-problem-clear");
            controller.PresentPitch();
            controller.Continue();
            controller.SelectFollowUpResponse("primary-cost-clear");
            controller.Continue();
            Assert.That(controller.Snapshot.Draft[PitchPart.Problem].WasRevised, Is.True);
            Assert.That(controller.Snapshot.Phase, Is.EqualTo(GuidedPitchPhase.Results));
        }

        private static void AssertPayloadEqual(LmsCompletionPayload actual, LmsCompletionPayload expected)
        {
            Assert.That(actual.SessionId, Is.EqualTo(expected.SessionId));
            Assert.That(actual.OverallScore, Is.EqualTo(expected.OverallScore));
            Assert.That(actual.SelectedResponseIds, Is.EqualTo(expected.SelectedResponseIds));
            Assert.That(actual.CompetencyScores.Select(score => (score.CompetencyId, score.Score)),
                Is.EqualTo(expected.CompetencyScores.Select(score => (score.CompetencyId, score.Score))));
        }

        private static Fixture CreateFixture(ILmsBridge bridge = null)
        {
            var fixture = new Fixture();
            fixture.Now = StartedAt;
            fixture.Controller = new GuidedPitchSessionController(
                LoadContent(),
                new AccessibilitySettings(TimerMode.Normal, false, 0.75f, 0.8f, "en"),
                bridge ?? new MockLmsBridge(MockLmsBridgeMode.Success, ValidLaunch()),
                () => fixture.Now,
                "0.2.0");
            return fixture;
        }

        private static GuidedPitchContent LoadContent()
        {
            var catalog = LocalizationCatalog.Load(ReadCatalog("en"), ReadCatalog("ms"));
            var values = catalog.GetKeys("en").ToDictionary(
                key => key,
                key => catalog.Resolve("en", key),
                StringComparer.Ordinal);
            var result = GuidedPitchContentLoader.LoadWithLocalizationValues(
                File.ReadAllText(Path.Combine("Assets", "Content", "Scenarios", "guided-pitch-builder.en.json")),
                values);
            Assert.That(result.IsSuccess, Is.True);
            return result.Content;
        }

        private static string ReadCatalog(string locale)
        {
            return File.ReadAllText(Path.Combine("Assets", "Content", "Localization", locale + ".json"));
        }

        private static LmsLaunchConfig ValidLaunch()
        {
            return new LmsLaunchConfig
            {
                PseudonymousLearnerId = "learner-7b9",
                SessionId = "session-42",
                CourseId = "course-garden",
                ModuleId = "module-pitching",
                LessonId = "lesson-smart-school-garden",
                ScenarioId = "smart-school-garden",
                Language = "en",
                AttemptNumber = 2,
                TimerMode = "normal",
                ReducedMotion = false,
                MusicVolume = 0.75f,
                SfxVolume = 0.8f,
                ContentVersion = 2,
                LaunchReference = "lref_opaque_ref_17",
            };
        }

        private sealed class Fixture
        {
            public GuidedPitchSessionController Controller { get; set; }
            public DateTimeOffset Now { get; set; }
        }

        private sealed class DeferredBridge : ILmsBridge
        {
            private readonly LmsLaunchConfig launch;
            private Action success;
            private Action<LmsSubmissionError> failure;

            public DeferredBridge(LmsLaunchConfig launch)
            {
                this.launch = launch;
            }

            public LmsLaunchConfig GetLaunchConfig()
            {
                return launch;
            }

            public void SubmitCompletion(
                LmsCompletionPayload payload,
                Action onSuccess,
                Action<LmsSubmissionError> onFailure)
            {
                success = onSuccess;
                failure = onFailure;
            }

            public void Succeed()
            {
                success?.Invoke();
            }

            public void Fail(LmsSubmissionError error)
            {
                failure?.Invoke(error);
            }
        }
    }
}
