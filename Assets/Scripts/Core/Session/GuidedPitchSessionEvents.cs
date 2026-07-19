namespace Agrovator.PitchSimulator.Core
{
    public enum GuidedPitchSessionEventType
    {
        ResponseSelected,
        FeedbackReady,
        ResultsReady,
        SubmissionSucceeded,
        SubmissionFailed,
    }

    public sealed class GuidedPitchSessionEvent
    {
        public GuidedPitchSessionEvent(
            GuidedPitchSessionEventType type,
            string responseId = null,
            string reactionCue = null,
            string messageKey = null)
        {
            Type = type;
            ResponseId = responseId;
            ReactionCue = reactionCue;
            MessageKey = messageKey;
        }

        public GuidedPitchSessionEventType Type { get; }

        public string ResponseId { get; }

        public string ReactionCue { get; }

        public string MessageKey { get; }
    }
}
