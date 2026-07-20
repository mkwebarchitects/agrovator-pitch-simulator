using System.Collections;
using System.Collections.Generic;
using Agrovator.PitchSimulator.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.Tests.PlayMode
{
    public sealed class JudgeReactionPlayModeTests
    {
        private readonly List<Object> owned = new List<Object>();

        [Test]
        public void TypedMapping_CoversAllElevenStates_AndFallsBackToEncouraging()
        {
            var sprites = CreateCompleteSpriteSet();

            foreach (JudgeReaction reaction in System.Enum.GetValues(typeof(JudgeReaction)))
            {
                Assert.That(sprites.Resolve(reaction), Is.Not.Null, reaction.ToString());
            }

            Assert.That(System.Enum.GetValues(typeof(JudgeReaction)), Has.Length.EqualTo(11));
            Assert.That(JudgeReactionMapper.Parse("Impressed"), Is.EqualTo(JudgeReaction.Impressed));
            Assert.That(JudgeReactionMapper.Parse("Curious"), Is.EqualTo(JudgeReaction.Interested));
            Assert.That(JudgeReactionMapper.Parse("not-authored"), Is.EqualTo(JudgeReaction.Encouraging));
            Assert.That(JudgeReactionMapper.Parse(null), Is.EqualTo(JudgeReaction.Encouraging));

            sprites.Set(JudgeReaction.Confused, null);
            Assert.That(sprites.Resolve(JudgeReaction.Confused),
                Is.SameAs(sprites.Resolve(JudgeReaction.Encouraging)));
        }

        [UnityTest]
        public IEnumerator View_GuardsTalkBlinkAndOneShotReaction_WithReducedMotionFallback()
        {
            var root = Track(new GameObject("Judge", typeof(RectTransform), typeof(Image),
                typeof(JudgeReactionView)));
            var image = root.GetComponent<Image>();
            var view = root.GetComponent<JudgeReactionView>();
            var sprites = CreateCompleteSpriteSet();
            view.Configure(
                image,
                sprites,
                blinkIntervalSeconds: 0.04f,
                blinkDurationSeconds: 0.02f,
                talkFrameSeconds: 0.01f,
                semanticHoldSeconds: 0.04f);

            view.Render(null, questionTextVisible: true, showSemanticReaction: false, reducedMotion: false);
            Assert.That(view.IsTalkLoopActive, Is.True);
            Assert.That(view.CurrentReaction, Is.EqualTo(JudgeReaction.Talk));
            yield return new WaitForSecondsRealtime(0.015f);
            Assert.That(view.CurrentReaction,
                Is.EqualTo(JudgeReaction.Idle).Or.EqualTo(JudgeReaction.Talk));

            view.Render(null, questionTextVisible: false, showSemanticReaction: false, reducedMotion: false);
            Assert.That(view.IsTalkLoopActive, Is.False);
            Assert.That(view.CurrentReaction, Is.EqualTo(JudgeReaction.Idle));
            yield return new WaitForSecondsRealtime(0.045f);
            Assert.That(view.CurrentReaction, Is.EqualTo(JudgeReaction.Blink));
            yield return new WaitForSecondsRealtime(0.025f);
            Assert.That(view.CurrentReaction, Is.EqualTo(JudgeReaction.Idle));

            view.Render("Celebrating", questionTextVisible: false, showSemanticReaction: true,
                reducedMotion: false);
            Assert.That(view.CurrentReaction, Is.EqualTo(JudgeReaction.Celebrating));
            yield return new WaitForSecondsRealtime(0.05f);
            Assert.That(view.CurrentReaction, Is.EqualTo(JudgeReaction.Encouraging));

            view.Render("Confused", questionTextVisible: false, showSemanticReaction: true,
                reducedMotion: true);
            Assert.That(view.CurrentReaction, Is.EqualTo(JudgeReaction.Confused));
            yield return new WaitForSecondsRealtime(0.06f);
            Assert.That(view.CurrentReaction, Is.EqualTo(JudgeReaction.Confused),
                "Reduced motion preserves the semantic static reaction.");

            view.Render("unknown-cue", questionTextVisible: false, showSemanticReaction: true,
                reducedMotion: true);
            Assert.That(view.CurrentReaction, Is.EqualTo(JudgeReaction.Encouraging));
            Assert.That(image.sprite, Is.SameAs(sprites.Resolve(JudgeReaction.Encouraging)));

            view.Render(null, questionTextVisible: true, showSemanticReaction: false, reducedMotion: true);
            Assert.That(view.IsTalkLoopActive, Is.False);
            Assert.That(view.CurrentReaction, Is.EqualTo(JudgeReaction.Idle));
        }

        /// <summary>
        /// The guided panel is saved inactive, and the router refreshes presenters
        /// before it activates the panel. Awake therefore runs after the resting
        /// face has already been applied, so it must not drop Aya onto a different
        /// portrait that the latch then refuses to correct.
        /// </summary>
        [UnityTest]
        public IEnumerator View_KeepsTheRestingFace_WhenAwakeRunsAfterTheFirstRender()
        {
            var panel = Track(new GameObject("Guided Panel"));
            panel.SetActive(false);
            var root = new GameObject("Judge", typeof(RectTransform), typeof(Image),
                typeof(JudgeReactionView));
            root.transform.SetParent(panel.transform, false);
            var image = root.GetComponent<Image>();
            var view = root.GetComponent<JudgeReactionView>();
            var sprites = CreateCompleteSpriteSet();
            view.Configure(image, sprites, 5f, 0.12f, 0.18f, semanticHoldSeconds: 2.5f);

            view.Render("Encouraging", questionTextVisible: false, showSemanticReaction: true,
                reducedMotion: false);
            panel.SetActive(true);
            yield return null;

            Assert.That(image.sprite, Is.SameAs(sprites.Resolve(JudgeReaction.Encouraging)),
                "Activating the panel must not drop Aya onto the Idle frame.");
            Assert.That(view.CurrentReaction, Is.EqualTo(JudgeReaction.Encouraging));
        }

        /// <summary>
        /// Settling to Encouraging must also release the latch that records which
        /// reaction is showing. Otherwise the same cue arriving twice in a row is
        /// silently swallowed as a repeat of a reaction that is no longer on screen.
        /// </summary>
        [UnityTest]
        public IEnumerator View_RepeatsAReactionThatAlreadySettled_InsteadOfSwallowingIt()
        {
            var root = Track(new GameObject("Judge", typeof(RectTransform), typeof(Image),
                typeof(JudgeReactionView)));
            var image = root.GetComponent<Image>();
            var view = root.GetComponent<JudgeReactionView>();
            var sprites = CreateCompleteSpriteSet();
            view.Configure(image, sprites, 5f, 0.02f, 0.01f, semanticHoldSeconds: 0.04f);

            view.Render("Concerned", questionTextVisible: false, showSemanticReaction: true,
                reducedMotion: false);
            Assert.That(view.CurrentReaction, Is.EqualTo(JudgeReaction.Concerned));
            yield return new WaitForSecondsRealtime(0.06f);
            Assert.That(view.CurrentReaction, Is.EqualTo(JudgeReaction.Encouraging));

            view.Render("Concerned", questionTextVisible: false, showSemanticReaction: true,
                reducedMotion: false);

            Assert.That(view.CurrentReaction, Is.EqualTo(JudgeReaction.Concerned),
                "A repeated cue must show again once the previous one has settled away.");
            Assert.That(image.sprite, Is.SameAs(sprites.Resolve(JudgeReaction.Concerned)));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (var index = owned.Count - 1; index >= 0; index--)
            {
                if (owned[index] != null) Object.Destroy(owned[index]);
            }
            owned.Clear();
            yield return null;
        }

        private JudgeReactionSpriteSet CreateCompleteSpriteSet()
        {
            var set = new JudgeReactionSpriteSet();
            foreach (JudgeReaction reaction in System.Enum.GetValues(typeof(JudgeReaction)))
            {
                set.Set(reaction, CreateSprite(reaction.ToString()));
            }
            return set;
        }

        private Sprite CreateSprite(string name)
        {
            var texture = Track(new Texture2D(2, 2, TextureFormat.RGBA32, false));
            texture.name = name + " Texture";
            var sprite = Track(Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f), 32f));
            sprite.name = name;
            return sprite;
        }

        private T Track<T>(T value) where T : Object
        {
            owned.Add(value);
            return value;
        }
    }
}
