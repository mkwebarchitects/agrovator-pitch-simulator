using System;

namespace Agrovator.PitchSimulator.LMS
{
    public interface ILmsBridge
    {
        LmsLaunchConfig GetLaunchConfig();

        void SubmitCompletion(
            LmsCompletionPayload payload,
            Action onSuccess,
            Action<LmsSubmissionError> onFailure);
    }

    public enum LmsSubmissionErrorCode
    {
        InvalidPayload,
        SubmissionFailed,
        SessionExpired,
        MissingConfiguration,
    }

    public sealed class LmsSubmissionError
    {
        public LmsSubmissionError(
            LmsSubmissionErrorCode code,
            string messageKey,
            int attemptNumber)
        {
            Code = code;
            MessageKey = messageKey;
            AttemptNumber = attemptNumber;
        }

        public LmsSubmissionErrorCode Code { get; }

        public string MessageKey { get; }

        public int AttemptNumber { get; }
    }
}
