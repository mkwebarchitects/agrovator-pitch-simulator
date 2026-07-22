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
        // The single generic judge-reaction cue, renamed in place rather than
        // removed: Unity serializes AudioCueBinding.cue by its underlying int,
        // and committed scenes already bind real clips to these positions by
        // index. Renaming keeps every existing binding (this one included)
        // pointed at the same clip; the two new reactions below are appended
        // rather than inserted so nothing already bound silently shifts onto
        // the wrong cue.
        JudgeReactionImpressed,
        FeedbackOpen,
        ResultsReveal,
        CompletionSuccess,
        CompletionFailure,
        // One cue per remaining judge portrait reaction, not one shared cue for
        // every response, so approval and disapproval sound distinct as well as
        // look distinct. Appended at the end (see above) rather than grouped
        // with JudgeReactionImpressed above.
        JudgeReactionInterested,
        JudgeReactionConcerned,
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
