using System;

namespace Agrovator.PitchSimulator.LMS
{
    public enum MockLmsBridgeMode
    {
        Success,
        Failure,
        Expired,
        MissingConfiguration,
    }

    public sealed class MockLmsBridge : ILmsBridge
    {
        private readonly MockLmsBridgeMode mode;
        private readonly LmsLaunchConfig launchConfig;

        public MockLmsBridge(MockLmsBridgeMode mode, LmsLaunchConfig launchConfig)
        {
            this.mode = mode;
            this.launchConfig = launchConfig;
        }

        public int? LastSubmittedAttemptNumber { get; private set; }

        public LmsLaunchConfig GetLaunchConfig()
        {
            return mode == MockLmsBridgeMode.MissingConfiguration ? null : launchConfig;
        }

        public void SubmitCompletion(
            LmsCompletionPayload payload,
            Action onSuccess,
            Action<LmsSubmissionError> onFailure)
        {
            var attemptNumber = payload == null ? 0 : payload.AttemptNumber;
            LastSubmittedAttemptNumber = attemptNumber;

            if (LmsPayloadValidator.ValidateCompletion(payload).Count > 0)
            {
                onFailure?.Invoke(Error(
                    LmsSubmissionErrorCode.InvalidPayload,
                    "lms.payload.invalid",
                    attemptNumber));
                return;
            }

            switch (mode)
            {
                case MockLmsBridgeMode.Success:
                    onSuccess?.Invoke();
                    break;

                case MockLmsBridgeMode.Failure:
                    onFailure?.Invoke(Error(
                        LmsSubmissionErrorCode.SubmissionFailed,
                        "lms.submission.failed",
                        attemptNumber));
                    break;

                case MockLmsBridgeMode.Expired:
                    onFailure?.Invoke(Error(
                        LmsSubmissionErrorCode.SessionExpired,
                        "lms.session.expired",
                        attemptNumber));
                    break;

                case MockLmsBridgeMode.MissingConfiguration:
                    onFailure?.Invoke(Error(
                        LmsSubmissionErrorCode.MissingConfiguration,
                        "lms.configuration.missing",
                        attemptNumber));
                    break;

                default:
                    onFailure?.Invoke(Error(
                        LmsSubmissionErrorCode.SubmissionFailed,
                        "lms.submission.failed",
                        attemptNumber));
                    break;
            }
        }

        private static LmsSubmissionError Error(
            LmsSubmissionErrorCode code,
            string messageKey,
            int attemptNumber)
        {
            return new LmsSubmissionError(code, messageKey, attemptNumber);
        }
    }
}
