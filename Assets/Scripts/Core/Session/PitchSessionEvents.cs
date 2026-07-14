namespace Agrovator.PitchSimulator.Core
{
    public enum PitchSessionEventType
    {
        ReactionReady,
        TimeoutReactionReady,
        FeedbackReady,
        ResultsReady,
        SubmissionSucceeded,
        SubmissionFailed,
    }

    public sealed class PitchSessionEvent
    {
        public PitchSessionEvent(
            PitchSessionEventType type,
            string responseId = null,
            string reactionCue = null,
            string feedbackKey = null,
            string explanationKey = null,
            string messageKey = null)
        {
            Type = type;
            ResponseId = responseId;
            ReactionCue = reactionCue;
            FeedbackKey = feedbackKey;
            ExplanationKey = explanationKey;
            MessageKey = messageKey;
        }

        public PitchSessionEventType Type { get; }

        public string ResponseId { get; }

        public string ReactionCue { get; }

        public string FeedbackKey { get; }

        public string ExplanationKey { get; }

        public string MessageKey { get; }
    }
}
