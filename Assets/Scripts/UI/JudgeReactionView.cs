using System;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    public enum JudgeReaction
    {
        Idle,
        Blink,
        Talk,
        Think,
        Smile,
        Interested,
        Confused,
        Concerned,
        Impressed,
        Encouraging,
        Celebrating,
    }

    public static class JudgeReactionMapper
    {
        public static JudgeReaction Parse(string cue)
        {
            if (string.Equals(cue, "Idle", StringComparison.OrdinalIgnoreCase)) return JudgeReaction.Idle;
            if (string.Equals(cue, "Blink", StringComparison.OrdinalIgnoreCase)) return JudgeReaction.Blink;
            if (string.Equals(cue, "Talk", StringComparison.OrdinalIgnoreCase)) return JudgeReaction.Talk;
            if (string.Equals(cue, "Think", StringComparison.OrdinalIgnoreCase)) return JudgeReaction.Think;
            if (string.Equals(cue, "Smile", StringComparison.OrdinalIgnoreCase)) return JudgeReaction.Smile;
            if (string.Equals(cue, "Curious", StringComparison.OrdinalIgnoreCase)) return JudgeReaction.Interested;
            if (string.Equals(cue, "Interested", StringComparison.OrdinalIgnoreCase)) return JudgeReaction.Interested;
            if (string.Equals(cue, "Confused", StringComparison.OrdinalIgnoreCase)) return JudgeReaction.Confused;
            if (string.Equals(cue, "Concerned", StringComparison.OrdinalIgnoreCase)) return JudgeReaction.Concerned;
            if (string.Equals(cue, "Impressed", StringComparison.OrdinalIgnoreCase)) return JudgeReaction.Impressed;
            if (string.Equals(cue, "Encouraging", StringComparison.OrdinalIgnoreCase)) return JudgeReaction.Encouraging;
            if (string.Equals(cue, "Celebrating", StringComparison.OrdinalIgnoreCase)) return JudgeReaction.Celebrating;
            return JudgeReaction.Encouraging;
        }
    }

    [Serializable]
    public sealed class JudgeReactionSpriteSet
    {
        [SerializeField] private Sprite idle;
        [SerializeField] private Sprite blink;
        [SerializeField] private Sprite talk;
        [SerializeField] private Sprite think;
        [SerializeField] private Sprite smile;
        [SerializeField] private Sprite interested;
        [SerializeField] private Sprite confused;
        [SerializeField] private Sprite concerned;
        [SerializeField] private Sprite impressed;
        [SerializeField] private Sprite encouraging;
        [SerializeField] private Sprite celebrating;

        public Sprite Resolve(JudgeReaction reaction)
        {
            var resolved = GetExact(reaction);
            return resolved != null ? resolved : encouraging;
        }

        public void Set(JudgeReaction reaction, Sprite sprite)
        {
            switch (reaction)
            {
                case JudgeReaction.Idle: idle = sprite; break;
                case JudgeReaction.Blink: blink = sprite; break;
                case JudgeReaction.Talk: talk = sprite; break;
                case JudgeReaction.Think: think = sprite; break;
                case JudgeReaction.Smile: smile = sprite; break;
                case JudgeReaction.Interested: interested = sprite; break;
                case JudgeReaction.Confused: confused = sprite; break;
                case JudgeReaction.Concerned: concerned = sprite; break;
                case JudgeReaction.Impressed: impressed = sprite; break;
                case JudgeReaction.Encouraging: encouraging = sprite; break;
                case JudgeReaction.Celebrating: celebrating = sprite; break;
            }
        }

        private Sprite GetExact(JudgeReaction reaction)
        {
            switch (reaction)
            {
                case JudgeReaction.Idle: return idle;
                case JudgeReaction.Blink: return blink;
                case JudgeReaction.Talk: return talk;
                case JudgeReaction.Think: return think;
                case JudgeReaction.Smile: return smile;
                case JudgeReaction.Interested: return interested;
                case JudgeReaction.Confused: return confused;
                case JudgeReaction.Concerned: return concerned;
                case JudgeReaction.Impressed: return impressed;
                case JudgeReaction.Encouraging: return encouraging;
                case JudgeReaction.Celebrating: return celebrating;
                default: return encouraging;
            }
        }
    }

    public sealed class JudgeReactionView : MonoBehaviour
    {
        private const float DefaultBlinkIntervalSeconds = 5f;
        private const float DefaultBlinkDurationSeconds = 0.12f;
        private const float DefaultTalkFrameSeconds = 0.18f;
        private const float DefaultSemanticHoldSeconds = 0.9f;

        /// <summary>
        /// The face Aya holds when she is not reacting to anything.
        /// </summary>
        private const JudgeReaction RestingReaction = JudgeReaction.Encouraging;

        [SerializeField] private Image portraitImage;
        [SerializeField] private JudgeReactionSpriteSet sprites = new JudgeReactionSpriteSet();
        [SerializeField, Min(1f)] private float blinkIntervalSeconds = DefaultBlinkIntervalSeconds;
        [SerializeField, Min(0.02f)] private float blinkDurationSeconds = DefaultBlinkDurationSeconds;
        [SerializeField, Min(0.04f)] private float talkFrameSeconds = DefaultTalkFrameSeconds;
        [SerializeField, Min(0.1f)] private float semanticHoldSeconds = DefaultSemanticHoldSeconds;

        private JudgeReaction currentReaction = JudgeReaction.Idle;
        private JudgeReaction latchedSemanticReaction = JudgeReaction.Encouraging;
        private float elapsed;
        private bool questionTextVisible;
        private bool reducedMotion;
        private bool semanticActive;
        private bool semanticLatched;
        private bool talkFrameVisible;

        public JudgeReaction CurrentReaction => currentReaction;

        public bool IsTalkLoopActive => questionTextVisible && !reducedMotion && !semanticLatched;

        public bool IsConfigured => portraitImage != null && sprites != null;

        public void Render(
            string cue,
            bool questionTextVisible,
            bool showSemanticReaction,
            bool reducedMotion)
        {
            this.reducedMotion = reducedMotion;

            if (showSemanticReaction)
            {
                this.questionTextVisible = false;
                var reaction = JudgeReactionMapper.Parse(cue);
                if (!semanticLatched || reaction != latchedSemanticReaction)
                {
                    semanticLatched = true;
                    semanticActive = !reducedMotion;
                    latchedSemanticReaction = reaction;
                    elapsed = 0f;
                    Apply(reaction);
                }
                else if (reducedMotion)
                {
                    semanticActive = false;
                    Apply(reaction);
                }
                return;
            }

            semanticLatched = false;
            semanticActive = false;
            this.questionTextVisible = questionTextVisible;
            elapsed = 0f;
            talkFrameVisible = questionTextVisible && !reducedMotion;
            Apply(talkFrameVisible ? JudgeReaction.Talk : JudgeReaction.Idle);
        }

        public void Configure(
            Image image,
            JudgeReactionSpriteSet spriteSet,
            float blinkIntervalSeconds,
            float blinkDurationSeconds,
            float talkFrameSeconds,
            float semanticHoldSeconds)
        {
            portraitImage = image;
            sprites = spriteSet ?? new JudgeReactionSpriteSet();
            this.blinkIntervalSeconds = Mathf.Max(0.001f, blinkIntervalSeconds);
            this.blinkDurationSeconds = Mathf.Max(0.001f, blinkDurationSeconds);
            this.talkFrameSeconds = Mathf.Max(0.001f, talkFrameSeconds);
            this.semanticHoldSeconds = Mathf.Max(0.001f, semanticHoldSeconds);
            elapsed = 0f;
            Apply(RestingReaction);
        }

        private void Awake()
        {
            // Awake can run after a presenter has already rendered, because a
            // screen is refreshed before its panel is activated. Landing on the
            // resting face keeps that ordering harmless; landing on anything else
            // would be frozen in place by the semantic latch.
            if (!semanticLatched && !questionTextVisible)
            {
                Apply(RestingReaction);
            }
        }

        private void Update()
        {
            if (!IsConfigured || reducedMotion)
            {
                return;
            }

            var delta = Time.unscaledDeltaTime;
            if (semanticActive)
            {
                elapsed += delta;
                if (elapsed >= semanticHoldSeconds)
                {
                    semanticActive = false;
                    elapsed = 0f;
                    // The settled face is now what is latched, so the same cue
                    // arriving again is a new reaction to show, not a repeat to skip.
                    latchedSemanticReaction = RestingReaction;
                    Apply(RestingReaction);
                }
                return;
            }

            if (semanticLatched)
            {
                return;
            }

            elapsed += delta;
            if (questionTextVisible)
            {
                if (elapsed >= talkFrameSeconds)
                {
                    elapsed = 0f;
                    talkFrameVisible = !talkFrameVisible;
                    Apply(talkFrameVisible ? JudgeReaction.Talk : JudgeReaction.Idle);
                }
                return;
            }

            if (currentReaction == JudgeReaction.Blink)
            {
                if (elapsed >= blinkDurationSeconds)
                {
                    elapsed = 0f;
                    Apply(JudgeReaction.Idle);
                }
            }
            else if (elapsed >= blinkIntervalSeconds)
            {
                elapsed = 0f;
                Apply(JudgeReaction.Blink);
            }
        }

        private void Apply(JudgeReaction reaction)
        {
            currentReaction = reaction;
            if (portraitImage != null && sprites != null)
            {
                portraitImage.sprite = sprites.Resolve(reaction);
            }
        }
    }
}
