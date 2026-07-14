using System;
using Agrovator.PitchSimulator.Audio;
using Agrovator.PitchSimulator.Core;

namespace Agrovator.PitchSimulator.UI
{
    public sealed class AudioCueDirector
    {
        private readonly Action<AudioCue> play;
        private bool musicStarted;
        private bool timerWarningPlayed;

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

        public void HandleSessionEvent(PitchSessionEvent sessionEvent)
        {
            if (sessionEvent == null)
            {
                return;
            }

            switch (sessionEvent.Type)
            {
                case PitchSessionEventType.ReactionReady:
                    play(AudioCue.ResponseSelected);
                    play(AudioCue.JudgeReaction);
                    break;
                case PitchSessionEventType.TimeoutReactionReady:
                    play(AudioCue.JudgeReaction);
                    break;
                case PitchSessionEventType.FeedbackReady:
                    play(AudioCue.FeedbackOpen);
                    break;
                case PitchSessionEventType.ResultsReady:
                    play(AudioCue.ResultsReveal);
                    break;
                case PitchSessionEventType.SubmissionSucceeded:
                    play(AudioCue.CompletionSuccess);
                    break;
                case PitchSessionEventType.SubmissionFailed:
                    play(AudioCue.CompletionFailure);
                    break;
            }
        }

        public void UpdateTimer(GameState state, double remainingSeconds, double totalSeconds)
        {
            if (state != GameState.AwaitingResponse || totalSeconds <= 0d || remainingSeconds <= 0d)
            {
                timerWarningPlayed = false;
                return;
            }

            if (!timerWarningPlayed && remainingSeconds <= 5d)
            {
                timerWarningPlayed = true;
                play(AudioCue.TimerWarning);
            }
        }
    }
}
