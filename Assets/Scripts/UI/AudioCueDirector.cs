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
            if (!musicStarted)
            {
                musicStarted = true;
                play(AudioCue.MusicLoop);
            }

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
                    play(AudioCue.JudgeReaction);
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
    }
}
