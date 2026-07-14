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
