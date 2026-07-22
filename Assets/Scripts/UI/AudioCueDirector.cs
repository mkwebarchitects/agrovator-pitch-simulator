using System;
using Agrovator.PitchSimulator.Audio;
using Agrovator.PitchSimulator.Core;

namespace Agrovator.PitchSimulator.UI
{
    public sealed class AudioCueDirector
    {
        private readonly Action<AudioCue> play;
        private bool musicStarted;

        public AudioCueDirector(Action<AudioCue> playCue)
        {
            play = playCue ?? throw new ArgumentNullException(nameof(playCue));
        }

        public void HandleUserGesture()
        {
            EnsureUnlockedAndMusicStarted();
            PlayButtonPress();
        }

        /// <summary>
        /// Starts the music loop on the first call and is a no-op after that,
        /// without playing the click cue - for a gesture that only exists to
        /// satisfy the browser's audio-unlock requirement (any first click or
        /// keypress anywhere on the page), not an actual button press.
        /// </summary>
        public void EnsureUnlockedAndMusicStarted()
        {
            if (musicStarted)
            {
                return;
            }

            musicStarted = true;
            play(AudioCue.MusicLoop);
        }

        /// <summary>
        /// The plain click cue for controls that carry no richer semantic cue of
        /// their own (Continue, Present, strengthen, mode-select, Retry,
        /// Settings-close). Buttons that already trigger a domain event - the
        /// sentence cards' ResponseSelected, Submit's eventual
        /// CompletionSuccess/Failure - keep only that cue rather than layering
        /// this one on top.
        /// </summary>
        public void PlayButtonPress()
        {
            play(AudioCue.ButtonPress);
        }

        /// <summary>
        /// Maps guided session outcomes to the reserved audio cues. The guided
        /// path is untimed and must never reach <see cref="AudioCue.TimerWarning"/>.
        /// </summary>
        public void HandleGuidedSessionEvent(GuidedPitchSessionEvent sessionEvent)
        {
            if (sessionEvent == null)
            {
                return;
            }

            switch (sessionEvent.Type)
            {
                case GuidedPitchSessionEventType.ResponseSelected:
                    play(AudioCue.ResponseSelected);
                    play(ResolveJudgeReactionCue(sessionEvent.ReactionCue));
                    break;
                case GuidedPitchSessionEventType.FeedbackReady:
                    play(AudioCue.FeedbackOpen);
                    break;
                case GuidedPitchSessionEventType.ResultsReady:
                    play(AudioCue.ResultsReveal);
                    break;
                case GuidedPitchSessionEventType.SubmissionSucceeded:
                    play(AudioCue.CompletionSuccess);
                    break;
                case GuidedPitchSessionEventType.SubmissionFailed:
                    play(AudioCue.CompletionFailure);
                    break;
            }
        }

        /// <summary>
        /// Reuses the same typed mapping the portrait resolves through, so the
        /// sound always matches the face - including its Encouraging fallback
        /// for a cue that fails to parse, which plays the Impressed cue rather
        /// than nothing.
        /// </summary>
        private static AudioCue ResolveJudgeReactionCue(string reactionCue)
        {
            switch (JudgeReactionMapper.Parse(reactionCue))
            {
                case JudgeReaction.Concerned:
                    return AudioCue.JudgeReactionConcerned;
                case JudgeReaction.Interested:
                    return AudioCue.JudgeReactionInterested;
                default:
                    return AudioCue.JudgeReactionImpressed;
            }
        }
    }
}
