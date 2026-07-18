using System;
using System.Collections.Generic;
using System.Linq;
using Agrovator.PitchSimulator.Core;
using Agrovator.PitchSimulator.GuidedPitch;
using Agrovator.PitchSimulator.LMS;
using Agrovator.PitchSimulator.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.Tests.PlayMode
{
    public sealed class GuidedPitchResultsPlayModeTests
    {
        private static readonly string[] ForbiddenResultLabels =
        {
            "Score",
            "Confidence",
            "Final confidence",
            "Seedling",
            "Sprouting",
            "Growing",
            "Thriving",
        };

        private readonly List<GameObject> roots = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var root in roots.Where(root => root != null))
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            roots.Clear();
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        [Test]
        public void Results_RevisedPrimaryPath_ShowsConstructiveStatementFocusedSummary()
        {
            var fixture = GuidedRigFactory.LoadAuthoredContent();
            var bridge = new MockLmsBridge(
                MockLmsBridgeMode.Success, GuidedRigFactory.CreateLaunch(fixture.Content));
            var controller = GuidedRigFactory.CreateController(fixture, bridge);
            Assert.That(controller.FinishLaunch(), Is.True);
            var rig = GuidedRigFactory.CreateRig(roots);
            rig.Router.Initialize(controller, fixture.Localize);
            EnterBuild(rig, LearnerMode.Primary);
            CompleteBuild(rig, fixture, LearnerMode.Primary,
                MasteryState.Developing, MasteryState.Clear,
                MasteryState.Developing, MasteryState.NeedsPractice);

            // Improve: revise Value without growth, then strengthen Problem to Clear.
            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Improve));
            var sameValue = fixture.Option(
                LearnerMode.Primary, PitchPart.Value, MasteryState.NeedsPractice);
            rig.StrengthenButtons[(int)PitchPart.Value].onClick.Invoke();
            ClickCard(rig, sameValue.Id);
            var developingProblem = fixture.Option(
                LearnerMode.Primary, PitchPart.Problem, MasteryState.Developing);
            var clearProblem = fixture.Option(
                LearnerMode.Primary, PitchPart.Problem, MasteryState.Clear);
            rig.StrengthenButtons[(int)PitchPart.Problem].onClick.Invoke();
            ClickCard(rig, clearProblem.Id);
            PresentAndAnswerFollowUp(rig, fixture, LearnerMode.Primary, MasteryState.Clear);

            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Results));
            Assert.That(rig.ResultsPanel.activeSelf, Is.True);
            Assert.That(rig.GuidedPanel.activeSelf, Is.False);

            var partViews = rig.ResultParts;
            Assert.That(partViews.Select(view => view.Part), Is.EqualTo(PitchParts.Ordered));
            Assert.That(partViews.Select(view => view.StatusText.text), Is.EqualTo(new[]
            {
                "Clear", "Clear", "Developing", "Needs Practice",
            }));
            Assert.That(rig.ReadinessText.text, Is.EqualTo("Pitch Readiness: 80%"));
            Assert.That(rig.ImprovementText.text,
                Is.EqualTo("You strengthened 1 part of this pitch."));
            Assert.That(rig.TransferText.text, Does.StartWith("Use these four parts when your team"));

            // Each card keeps the persistent part identity: icon, colour, label, sentence.
            foreach (var part in PitchParts.Ordered)
            {
                var visual = PitchPartVisuals.Get(part);
                var view = partViews[(int)part];
                Assert.That(view.IconText.text, Is.EqualTo(visual.IconGlyph), part.ToString());
                Assert.That(view.AccentImage.color, Is.EqualTo(visual.Colour), part.ToString());
                Assert.That(view.LabelText.text,
                    Is.EqualTo(fixture.Catalog.Resolve("en", visual.LabelKey)), part.ToString());
                Assert.That(view.SentenceText.text,
                    Is.EqualTo(fixture.Sentence(controller.Snapshot.Draft[part].CurrentResponseId)),
                    part.ToString());
            }

            // Improvement recognition per card: growth is named, plain revision is not oversold.
            Assert.That(partViews[0].RevisionNoteText.text, Is.EqualTo("Strengthened after feedback"));
            Assert.That(partViews[0].RevisionNoteText.gameObject.activeSelf, Is.True);
            Assert.That(partViews[3].RevisionNoteText.text, Is.EqualTo("Revised after feedback"));
            Assert.That(partViews[3].RevisionNoteText.gameObject.activeSelf, Is.True);
            Assert.That(partViews[1].RevisionNoteText.gameObject.activeSelf, Is.False,
                "An unrevised part must not carry a revision note.");
            Assert.That(partViews[2].RevisionNoteText.gameObject.activeSelf, Is.False);

            // The final pitch reads the CURRENT sentences in framework order.
            var localizedProblem = fixture.Sentence(clearProblem.Id);
            var initialWeakProblem = fixture.Sentence(developingProblem.Id);
            Assert.That(rig.FinalPitchText.text, Does.Contain(localizedProblem));
            Assert.That(rig.FinalPitchText.text, Does.Not.Contain(initialWeakProblem));
            var lastIndex = -1;
            foreach (var part in PitchParts.Ordered)
            {
                var sentence = fixture.Sentence(controller.Snapshot.Draft[part].CurrentResponseId);
                var position = rig.FinalPitchText.text.IndexOf(sentence, StringComparison.Ordinal);
                Assert.That(position, Is.GreaterThan(lastIndex), sentence);
                lastIndex = position;
            }
            Assert.That(rig.FinalPitchText.text, Does.Contain("\n\n"));
            Assert.That(rig.FinalPitchText.text, Does.Not.Contain("[[missing:"));

            // No judging labels anywhere on the Results screen.
            var resultsCopy = rig.ResultsPanel.GetComponentsInChildren<Text>(true)
                .Select(text => text.text).ToArray();
            foreach (var forbidden in ForbiddenResultLabels)
            {
                Assert.That(resultsCopy, Has.None.Contains(forbidden), forbidden);
            }

            Assert.That(rig.SubmissionStatusText.text, Is.EqualTo("Ready to submit."));
            Assert.That(rig.SubmitButtonLabel.text, Is.EqualTo("Submit results"));
            Assert.That(rig.SubmitButton.interactable, Is.True);
            Assert.That(rig.RetryButton.interactable, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.SameAs(rig.SubmitButton.gameObject),
                "Entering Results must focus the results screen's own default selectable.");
            controller.Dispose();
        }

        [Test]
        public void Submission_LifecyclePreservesTheCompletedResultAndAllowsResubmission()
        {
            var fixture = GuidedRigFactory.LoadAuthoredContent();
            var bridge = new DeferredGuidedBridge(GuidedRigFactory.CreateLaunch(fixture.Content));
            var controller = GuidedRigFactory.CreateController(fixture, bridge);
            Assert.That(controller.FinishLaunch(), Is.True);
            var rig = GuidedRigFactory.CreateRig(roots);
            rig.Router.Initialize(controller, fixture.Localize);
            EnterBuild(rig, LearnerMode.Primary);
            CompleteBuild(rig, fixture, LearnerMode.Primary,
                MasteryState.NeedsPractice, MasteryState.NeedsPractice,
                MasteryState.NeedsPractice, MasteryState.NeedsPractice);
            PresentAndAnswerFollowUp(rig, fixture, LearnerMode.Primary, MasteryState.NeedsPractice);

            // Completion is available for every complete, reviewed pitch regardless of readiness.
            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Results));
            Assert.That(rig.ReadinessText.text, Is.EqualTo("Pitch Readiness: 40%"));
            Assert.That(rig.ResultParts.Select(view => view.StatusText.text),
                Has.All.EqualTo("Needs Practice"));
            Assert.That(rig.SubmitButton.interactable, Is.True);
            var finalPitch = rig.FinalPitchText.text;
            Assert.That(finalPitch, Is.Not.Empty);

            rig.SubmitButton.onClick.Invoke();
            Assert.That(controller.Snapshot.Phase, Is.EqualTo(GuidedPitchPhase.Submitting));
            Assert.That(rig.SubmissionStatusText.text, Is.EqualTo("Submitting results..."));
            Assert.That(rig.SubmitButton.interactable, Is.False);
            Assert.That(rig.RetryButton.interactable, Is.False);
            Assert.That(rig.FinalPitchText.text, Is.EqualTo(finalPitch));

            bridge.Fail(new LmsSubmissionError(
                LmsSubmissionErrorCode.SubmissionFailed,
                "lms.submission.failed",
                controller.Snapshot.AttemptNumber));
            Assert.That(controller.Snapshot.Phase, Is.EqualTo(GuidedPitchPhase.Results));
            Assert.That(rig.SubmissionStatusText.text,
                Is.EqualTo("Submission failed. Please submit again."));
            Assert.That(rig.SubmitButton.interactable, Is.True);
            Assert.That(rig.SubmitButtonLabel.text, Is.EqualTo("Resubmit results"));
            AssertResultPreserved(rig, finalPitch);

            rig.SubmitButton.onClick.Invoke();
            Assert.That(bridge.SubmissionCount, Is.EqualTo(2));
            bridge.Fail(new LmsSubmissionError(
                LmsSubmissionErrorCode.SessionExpired,
                "lms.session.expired",
                controller.Snapshot.AttemptNumber));
            Assert.That(rig.SubmissionStatusText.text,
                Is.EqualTo("Your session expired. Please submit again."));
            AssertResultPreserved(rig, finalPitch);

            rig.SubmitButton.onClick.Invoke();
            bridge.Fail(new LmsSubmissionError(
                LmsSubmissionErrorCode.MissingConfiguration,
                "lms.configuration.missing",
                controller.Snapshot.AttemptNumber));
            Assert.That(rig.SubmissionStatusText.text,
                Is.EqualTo("Submission configuration is unavailable. Please try again."));
            Assert.That(rig.RetryButton.interactable, Is.True);
            AssertResultPreserved(rig, finalPitch);

            rig.SubmitButton.onClick.Invoke();
            bridge.Succeed();
            Assert.That(controller.Snapshot.Phase, Is.EqualTo(GuidedPitchPhase.Complete));
            Assert.That(rig.SubmissionStatusText.text,
                Is.EqualTo("Results submitted successfully."));
            Assert.That(rig.SubmitButton.interactable, Is.False);
            Assert.That(rig.RetryButton.interactable, Is.True);
            AssertResultPreserved(rig, finalPitch);
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.SameAs(rig.RetryButton.gameObject),
                "Completion must move focus onto the still-usable Retry action.");
            controller.Dispose();
        }

        [Test]
        public void Retry_ClearsTheFirstAttemptAndTheSecondAttemptRendersOnlyItsOwnPitch()
        {
            var fixture = GuidedRigFactory.LoadAuthoredContent();
            var bridge = new DeferredGuidedBridge(GuidedRigFactory.CreateLaunch(fixture.Content));
            var controller = GuidedRigFactory.CreateController(fixture, bridge);
            Assert.That(controller.FinishLaunch(), Is.True);
            var rig = GuidedRigFactory.CreateRig(roots);
            rig.Router.Initialize(controller, fixture.Localize);
            EnterBuild(rig, LearnerMode.Primary);
            CompleteBuild(rig, fixture, LearnerMode.Primary,
                MasteryState.Developing, MasteryState.Developing,
                MasteryState.Developing, MasteryState.Developing);
            PresentAndAnswerFollowUp(rig, fixture, LearnerMode.Primary, MasteryState.Developing);
            var firstAttemptProblem = fixture.Sentence(fixture.Option(
                LearnerMode.Primary, PitchPart.Problem, MasteryState.Developing).Id);
            Assert.That(rig.FinalPitchText.text, Does.Contain(firstAttemptProblem));

            // Leave a failed submission status and a scrolled view behind.
            rig.SubmitButton.onClick.Invoke();
            bridge.Fail(new LmsSubmissionError(
                LmsSubmissionErrorCode.SubmissionFailed,
                "lms.submission.failed",
                controller.Snapshot.AttemptNumber));
            rig.ResultsScroll.verticalNormalizedPosition = 0.2f;
            rig.Router.Refresh();
            Assert.That(rig.ResultsScroll.verticalNormalizedPosition, Is.EqualTo(0.2f).Within(0.001f),
                "A submission refresh must not reset the results scroll.");
            Assert.That(rig.SubmissionStatusText.text,
                Is.EqualTo("Submission failed. Please submit again."));

            rig.RetryButton.onClick.Invoke();

            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Briefing));
            Assert.That(rig.BriefingPanel.activeSelf, Is.True);
            Assert.That(rig.ResultsPanel.activeSelf, Is.False);
            Assert.That(controller.Snapshot.AttemptNumber, Is.EqualTo(2));
            Assert.That(controller.Snapshot.LearnerMode, Is.Null,
                "Retry must not retain the first attempt's learner mode.");
            Assert.That(controller.Snapshot.Draft.PopulatedCount, Is.Zero);
            Assert.That(controller.Snapshot.SubmissionError, Is.Null);
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.SameAs(rig.BriefingContinueButton.gameObject));

            rig.BriefingContinueButton.onClick.Invoke();
            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.ModeSelection),
                "The second attempt must ask for the learner mode again.");
            rig.ModeSelection.Cards[1].Button.onClick.Invoke();
            rig.ContinueButton.onClick.Invoke();
            CompleteBuild(rig, fixture, LearnerMode.Secondary,
                MasteryState.Clear, MasteryState.Clear,
                MasteryState.Clear, MasteryState.Clear);
            PresentAndAnswerFollowUp(rig, fixture, LearnerMode.Secondary, MasteryState.Clear);

            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Results));
            Assert.That(rig.ResultParts.Select(view => view.StatusText.text),
                Has.All.EqualTo("Clear"));
            Assert.That(rig.ReadinessText.text, Is.EqualTo("Pitch Readiness: 100%"));
            Assert.That(rig.ImprovementText.gameObject.activeSelf, Is.False,
                "A pitch with no strengthened part must not claim growth.");
            Assert.That(rig.FinalPitchText.text, Does.Not.Contain(firstAttemptProblem),
                "The second attempt must not display the first attempt's draft.");
            var secondProblem = fixture.Sentence(fixture.Option(
                LearnerMode.Secondary, PitchPart.Problem, MasteryState.Clear).Id);
            Assert.That(rig.FinalPitchText.text, Does.Contain(secondProblem));
            Assert.That(rig.SubmissionStatusText.text, Is.EqualTo("Ready to submit."),
                "The second attempt must not display the first attempt's submission status.");
            Assert.That(rig.SubmitButtonLabel.text, Is.EqualTo("Submit results"));
            Assert.That(rig.ResultsScroll.verticalNormalizedPosition, Is.EqualTo(1f).Within(0.001f),
                "A new attempt's results must start scrolled to the top.");
            Assert.That(rig.ResultParts.All(view => !view.RevisionNoteText.gameObject.activeSelf),
                Is.True);
            controller.Dispose();
        }

        [Test]
        public void Keyboard_RouterTab_CyclesSubmitAndRetryOnResultsAndIsSafeWhileSubmitting()
        {
            var fixture = GuidedRigFactory.LoadAuthoredContent();
            var bridge = new DeferredGuidedBridge(GuidedRigFactory.CreateLaunch(fixture.Content));
            var controller = GuidedRigFactory.CreateController(fixture, bridge);
            Assert.That(controller.FinishLaunch(), Is.True);
            var rig = GuidedRigFactory.CreateRig(roots);
            rig.Router.Initialize(controller, fixture.Localize);
            EnterBuild(rig, LearnerMode.Primary);
            CompleteBuild(rig, fixture, LearnerMode.Primary,
                MasteryState.Clear, MasteryState.Clear,
                MasteryState.Clear, MasteryState.Clear);
            PresentAndAnswerFollowUp(rig, fixture, LearnerMode.Primary, MasteryState.Clear);

            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Results));
            Assert.That(rig.Presenter.gameObject.activeInHierarchy, Is.False,
                "The guided presenter is inactive on Results, so the router must route Tab to the results screen.");
            var eventSystem = EventSystem.current;
            Assert.That(eventSystem.currentSelectedGameObject,
                Is.SameAs(rig.SubmitButton.gameObject));

            Assert.That(rig.Router.MoveFocus(false), Is.True,
                "Tab must cycle the Results screen's own selectables.");
            Assert.That(eventSystem.currentSelectedGameObject,
                Is.SameAs(rig.RetryButton.gameObject));
            Assert.That(rig.Router.MoveFocus(false), Is.True);
            Assert.That(eventSystem.currentSelectedGameObject,
                Is.SameAs(rig.SubmitButton.gameObject),
                "Tab must wrap back to Submit.");
            Assert.That(rig.Router.MoveFocus(true), Is.True);
            Assert.That(eventSystem.currentSelectedGameObject,
                Is.SameAs(rig.RetryButton.gameObject),
                "Shift+Tab must move backward through the cycle.");

            rig.SubmitButton.onClick.Invoke();
            Assert.That(controller.Snapshot.Phase, Is.EqualTo(GuidedPitchPhase.Submitting));
            var selectedWhileSubmitting = eventSystem.currentSelectedGameObject;
            Assert.That(rig.Router.MoveFocus(false), Is.False,
                "Submitting disables both actions, so Tab must be a safe no-op.");
            Assert.That(eventSystem.currentSelectedGameObject, Is.SameAs(selectedWhileSubmitting));
            controller.Dispose();
        }

        private static void EnterBuild(GuidedRig rig, LearnerMode mode)
        {
            rig.StartButton.onClick.Invoke();
            rig.BriefingContinueButton.onClick.Invoke();
            rig.ModeSelection.Cards[mode == LearnerMode.Primary ? 0 : 1].Button.onClick.Invoke();
            rig.ContinueButton.onClick.Invoke();
        }

        private static void CompleteBuild(GuidedRig rig, GuidedContentFixture fixture,
            LearnerMode mode, MasteryState problem, MasteryState evidence,
            MasteryState solution, MasteryState value)
        {
            var picks = new[]
            {
                (PitchPart.Problem, problem),
                (PitchPart.Evidence, evidence),
                (PitchPart.Solution, solution),
                (PitchPart.Value, value),
            };
            foreach (var (part, mastery) in picks)
            {
                ClickCard(rig, fixture.Option(mode, part, mastery).Id);
                rig.ContinueButton.onClick.Invoke();
            }
        }

        private static void PresentAndAnswerFollowUp(GuidedRig rig, GuidedContentFixture fixture,
            LearnerMode mode, MasteryState followUpMastery)
        {
            rig.PresentButton.onClick.Invoke();
            rig.ContinueButton.onClick.Invoke();
            ClickCard(rig, fixture.FollowUpOption(mode, followUpMastery).Id);
            rig.ContinueButton.onClick.Invoke();
        }

        private static void AssertResultPreserved(GuidedRig rig, string finalPitch)
        {
            Assert.That(rig.FinalPitchText.text, Is.EqualTo(finalPitch),
                "The final pitch must survive submission state changes.");
            Assert.That(rig.ResultParts.All(view => view.gameObject.activeSelf), Is.True,
                "All four part cards must survive submission state changes.");
            Assert.That(rig.ResultParts.All(view => view.SentenceText.text.Length > 0), Is.True);
            Assert.That(rig.ResultParts.All(view => view.StatusText.text.Length > 0), Is.True);
        }

        private static void ClickCard(GuidedRig rig, string responseId)
        {
            var card = rig.Cards.Cards.Single(candidate =>
                candidate.gameObject.activeSelf && candidate.ResponseId == responseId);
            card.Button.onClick.Invoke();
        }

        private sealed class DeferredGuidedBridge : ILmsBridge
        {
            private readonly LmsLaunchConfig launch;
            private Action onSuccess;
            private Action<LmsSubmissionError> onFailure;

            internal DeferredGuidedBridge(LmsLaunchConfig launch)
            {
                this.launch = launch;
            }

            internal int SubmissionCount { get; private set; }

            public LmsLaunchConfig GetLaunchConfig()
            {
                return launch;
            }

            public void SubmitCompletion(
                LmsCompletionPayload payload,
                Action success,
                Action<LmsSubmissionError> failure)
            {
                SubmissionCount++;
                onSuccess = success;
                onFailure = failure;
            }

            internal void Fail(LmsSubmissionError error)
            {
                var callback = onFailure;
                onSuccess = null;
                onFailure = null;
                callback?.Invoke(error);
            }

            internal void Succeed()
            {
                var callback = onSuccess;
                onSuccess = null;
                onFailure = null;
                callback?.Invoke();
            }
        }
    }
}
