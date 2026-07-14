using System;
using UnityEngine;

namespace Agrovator.PitchSimulator.Audio
{
    public enum AudioCue
    {
        MusicLoop,
        ButtonPress,
        ResponseSelected,
        TimerWarning,
        JudgeReaction,
        FeedbackOpen,
        ResultsReveal,
        CompletionSuccess,
        CompletionFailure,
    }

    [Serializable]
    public sealed class AudioCueBinding
    {
        [SerializeField] private AudioCue cue;
        [SerializeField] private AudioClip clip;

        public AudioCueBinding(AudioCue cue, AudioClip clip)
        {
            this.cue = cue;
            this.clip = clip;
        }

        public AudioCue Cue => cue;
        public AudioClip Clip => clip;
    }
}
