using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Agrovator.PitchSimulator.Core;
using Agrovator.PitchSimulator.GuidedPitch;
using Agrovator.PitchSimulator.LMS;
using Agrovator.PitchSimulator.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace Agrovator.PitchSimulator.Tests.PlayMode
{
    /// <summary>
    /// Judge Aya reacts to the pitch statement the learner just assembled, then
    /// settles. The authored cue travels Clear to Impressed, Developing to
    /// Curious/Interested, and Needs Practice to Concerned, in both learner modes,
    /// and no reaction survives a Retry.
    /// </summary>
    public sealed class GuidedPitchJudgeReactionPlayModeTests
    {
        private readonly List<GameObject> roots = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var root in roots.Where(root => root != null))
            {
                Object.DestroyImmediate(root);
            }

            roots.Clear();
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        [Test]
        public void Judge_ReactsToEveryPrimaryBuildStatement_AndReleasesTheFaceOnContinue()
        {
            var session = StartAtBuild(LearnerMode.Primary);

            Assert.That(session.Rig.Judge.IsConfigured, Is.True,
                "The guided scene must own a configured Judge Aya reaction view.");
            AssertReaction(session, JudgeReaction.Encouraging,
                "Aya waits on a still, encouraging face while the learner reads the question.");

            SelectByMastery(session, PitchPart.Problem, MasteryState.NeedsPractice);
            AssertReaction(session, JudgeReaction.Concerned,
                "A Needs Practice statement must draw the authored Concerned cue.");

            session.Rig.ContinueButton.onClick.Invoke();
            AssertReaction(session, JudgeReaction.Encouraging,
                "Advancing to the next part must release the previous reaction.");

            SelectByMastery(session, PitchPart.Evidence, MasteryState.Developing);
            AssertReaction(session, JudgeReaction.Interested,
                "The authored Curious cue must map to the Interested portrait.");

            session.Rig.ContinueButton.onClick.Invoke();
            SelectByMastery(session, PitchPart.Solution, MasteryState.Clear);
            AssertReaction(session, JudgeReaction.Impressed,
                "A Clear statement must draw the authored Impressed cue.");

            session.Controller.Dispose();
        }

        [Test]
        public void Judge_ReactsToSecondaryImproveAndFollowUpStatements()
        {
            var session = StartAtBuild(LearnerMode.Secondary);
            SelectByMastery(session, PitchPart.Problem, MasteryState.NeedsPractice);
            AssertReaction(session, JudgeReaction.Concerned);
            session.Rig.ContinueButton.onClick.Invoke();
            foreach (var part in new[] { PitchPart.Evidence, PitchPart.Solution, PitchPart.Value })
            {
                SelectByMastery(session, part, MasteryState.Clear);
                session.Rig.ContinueButton.onClick.Invoke();
            }

            Assert.That(session.Rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Improve));
            session.Rig.StrengthenButtons[0].onClick.Invoke();
            SelectByMastery(session, PitchPart.Problem, MasteryState.Clear);
            AssertReaction(session, JudgeReaction.Impressed,
                "A strengthened statement must draw its own authored reaction.");

            session.Rig.PresentButton.onClick.Invoke();
            session.Rig.ContinueButton.onClick.Invoke();
            Assert.That(session.Rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.FollowUp));

            var followUp = session.Fixture.FollowUpOption(
                LearnerMode.Secondary, MasteryState.NeedsPractice);
            ClickCard(session.Rig, followUp.Id);
            Assert.That(session.Rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.FollowUpFeedback));
            AssertReaction(session, JudgeReaction.Concerned,
                "The follow-up answer must draw a reaction too.");

            session.Controller.Dispose();
        }

        /// <summary>
        /// The guided screens are untimed reading surfaces, and the browser smoke
        /// advances only once the rendered canvas settles. An ambient blink or talk
        /// loop would both compete with a young learner's reading and leave the
        /// canvas permanently unsettled, so Aya must be still between statements.
        /// </summary>
        [UnityTest]
        public IEnumerator Judge_HoldsAStillPortraitBetweenStatements_WithNoAmbientLoop()
        {
            var session = StartAtBuild(LearnerMode.Primary);
            var resting = session.Rig.JudgeImage.sprite;

            var deadline = Time.unscaledTime + 6f;
            while (Time.unscaledTime < deadline)
            {
                yield return null;
                Assert.That(session.Rig.JudgeImage.sprite, Is.SameAs(resting),
                    "Aya must not blink or talk while the learner reads the question.");
                Assert.That(session.Rig.Judge.IsTalkLoopActive, Is.False);
            }

            SelectByMastery(session, PitchPart.Problem, MasteryState.Clear);
            AssertReaction(session, JudgeReaction.Impressed);

            var settleDeadline = Time.unscaledTime + 5f;
            while (session.Rig.Judge.CurrentReaction != JudgeReaction.Encouraging &&
                Time.unscaledTime < settleDeadline)
            {
                yield return null;
            }

            var settled = session.Rig.JudgeImage.sprite;
            var stillDeadline = Time.unscaledTime + 6f;
            while (Time.unscaledTime < stillDeadline)
            {
                yield return null;
                Assert.That(session.Rig.JudgeImage.sprite, Is.SameAs(settled),
                    "Aya must stay still after settling, not resume an idle loop.");
            }

            session.Controller.Dispose();
        }

        [UnityTest]
        public IEnumerator Judge_SettlesBackToEncouraging_InsteadOfLatchingTheConcernedFace()
        {
            var session = StartAtBuild(LearnerMode.Primary);
            SelectByMastery(session, PitchPart.Problem, MasteryState.NeedsPractice);
            AssertReaction(session, JudgeReaction.Concerned);

            var deadline = Time.unscaledTime + 5f;
            while (session.Rig.Judge.CurrentReaction != JudgeReaction.Encouraging &&
                Time.unscaledTime < deadline)
            {
                yield return null;
            }

            Assert.That(session.Rig.Judge.CurrentReaction, Is.EqualTo(JudgeReaction.Encouraging),
                "Aya must settle to an encouraging face rather than hold Concerned at the learner.");
            Assert.That(session.Rig.JudgeImage.sprite,
                Is.SameAs(GuidedRigFactory.JudgeSprite(JudgeReaction.Encouraging)));
            Assert.That(session.Rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.BuildFeedback),
                "Settling is a portrait change only; the feedback stays on screen.");

            session.Controller.Dispose();
        }

        [UnityTest]
        public IEnumerator Judge_ReducedMotion_HoldsTheSemanticFaceStaticAndSuppressesTheLoops()
        {
            var session = StartAtBuild(LearnerMode.Primary, reducedMotion: true);

            Assert.That(session.Controller.Snapshot.ReducedMotion, Is.True);
            Assert.That(session.Rig.Judge.CurrentReaction, Is.EqualTo(JudgeReaction.Encouraging),
                "Reduced motion rests on the same still, encouraging face.");
            Assert.That(session.Rig.Judge.IsTalkLoopActive, Is.False);

            SelectByMastery(session, PitchPart.Problem, MasteryState.NeedsPractice);
            AssertReaction(session, JudgeReaction.Concerned,
                "Reduced motion still shows the authored semantic reaction.");
            Assert.That(session.Rig.Judge.IsTalkLoopActive, Is.False);

            for (var frame = 0; frame < 30; frame++)
            {
                yield return null;
            }

            Assert.That(session.Rig.Judge.CurrentReaction, Is.EqualTo(JudgeReaction.Concerned),
                "Reduced motion must keep the semantic reaction static, with no settle animation.");
            Assert.That(session.Rig.JudgeImage.sprite,
                Is.SameAs(GuidedRigFactory.JudgeSprite(JudgeReaction.Concerned)));

            session.Rig.ContinueButton.onClick.Invoke();
            Assert.That(session.Rig.Judge.CurrentReaction, Is.EqualTo(JudgeReaction.Encouraging),
                "Advancing must still release the reaction under reduced motion.");
            Assert.That(session.Rig.Judge.IsTalkLoopActive, Is.False);

            session.Controller.Dispose();
        }

        [Test]
        public void Judge_Retry_ClearsTheReactionSoAttemptTwoNeverOpensOnAttemptOnesFace()
        {
            var session = StartAtBuild(LearnerMode.Primary);
            foreach (var part in PitchParts.Ordered)
            {
                SelectByMastery(session, part, MasteryState.Clear);
                session.Rig.ContinueButton.onClick.Invoke();
            }

            session.Rig.PresentButton.onClick.Invoke();
            session.Rig.ContinueButton.onClick.Invoke();
            ClickCard(session.Rig, session.Fixture
                .FollowUpOption(LearnerMode.Primary, MasteryState.NeedsPractice).Id);
            AssertReaction(session, JudgeReaction.Concerned);
            session.Rig.ContinueButton.onClick.Invoke();
            Assert.That(session.Rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Results));

            session.Rig.RetryButton.onClick.Invoke();

            Assert.That(session.Rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Briefing));
            Assert.That(session.Controller.Snapshot.LastReactionCue, Is.Null);
            Assert.That(session.Rig.Judge.CurrentReaction, Is.EqualTo(JudgeReaction.Encouraging),
                "Retry must clear the reaction before attempt two begins.");
            Assert.That(session.Rig.JudgeImage.sprite,
                Is.SameAs(GuidedRigFactory.JudgeSprite(JudgeReaction.Encouraging)));

            session.Rig.BriefingContinueButton.onClick.Invoke();
            session.Rig.ModeSelection.Cards[0].Button.onClick.Invoke();
            session.Rig.ContinueButton.onClick.Invoke();
            Assert.That(session.Rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Build));
            Assert.That(session.Rig.Judge.CurrentReaction, Is.EqualTo(JudgeReaction.Encouraging),
                "Attempt two must open on the neutral resting face.");

            session.Controller.Dispose();
        }

        private sealed class JudgeSession
        {
            internal GuidedContentFixture Fixture;
            internal GuidedPitchSessionController Controller;
            internal GuidedRig Rig;
        }

        private JudgeSession StartAtBuild(LearnerMode mode, bool reducedMotion = false)
        {
            var fixture = GuidedRigFactory.LoadAuthoredContent();
            var bridge = new MockLmsBridge(
                MockLmsBridgeMode.Success, GuidedRigFactory.CreateLaunch(fixture.Content));
            var controller = GuidedRigFactory.CreateController(fixture, bridge, reducedMotion);
            Assert.That(controller.FinishLaunch(), Is.True);
            var rig = GuidedRigFactory.CreateRig(roots);
            rig.Router.Initialize(controller, fixture.Localize);

            rig.StartButton.onClick.Invoke();
            rig.BriefingContinueButton.onClick.Invoke();
            rig.ModeSelection.Cards[mode == LearnerMode.Primary ? 0 : 1].Button.onClick.Invoke();
            rig.ContinueButton.onClick.Invoke();
            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Build));
            return new JudgeSession { Fixture = fixture, Controller = controller, Rig = rig };
        }

        private static void SelectByMastery(
            JudgeSession session, PitchPart part, MasteryState mastery)
        {
            var mode = session.Controller.Snapshot.LearnerMode.Value;
            ClickCard(session.Rig, session.Fixture.Option(mode, part, mastery).Id);
        }

        private static void AssertReaction(
            JudgeSession session, JudgeReaction expected, string because = null)
        {
            Assert.That(session.Rig.Judge.CurrentReaction, Is.EqualTo(expected), because);
            Assert.That(session.Rig.JudgeImage.sprite,
                Is.SameAs(GuidedRigFactory.JudgeSprite(expected)),
                "The portrait Image must actually swap to the reaction sprite.");
        }

        private static void ClickCard(GuidedRig rig, string responseId)
        {
            var card = rig.Cards.Cards.Single(candidate =>
                candidate.gameObject.activeSelf && candidate.ResponseId == responseId);
            card.Button.onClick.Invoke();
        }
    }
}
