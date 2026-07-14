using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Agrovator.PitchSimulator.LMS
{
    public enum WebGlLmsTransportFailure
    {
        SubmissionFailed,
        SessionExpired,
        MissingConfiguration,
    }

    public interface IWebGlLmsTransport
    {
        string GetLaunchConfigJson();

        void SubmitCompletion(
            string completionJson,
            Action onSuccess,
            Action<WebGlLmsTransportFailure> onFailure);
    }

    public sealed class WebGlLmsBridge : ILmsBridge
    {
        private readonly IWebGlLmsTransport transport;

        public WebGlLmsBridge()
            : this(new BrowserWebGlLmsTransport())
        {
        }

        public WebGlLmsBridge(IWebGlLmsTransport transport)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public LmsLaunchConfig GetLaunchConfig()
        {
            string json;
            try
            {
                json = transport.GetLaunchConfigJson();
            }
            catch
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                var config = LmsPayloadJson.DeserializeLaunchConfig(json);
                return LmsPayloadValidator.ValidateLaunch(config).Count == 0 ? config : null;
            }
            catch
            {
                return null;
            }
        }

        public void SubmitCompletion(
            LmsCompletionPayload payload,
            Action onSuccess,
            Action<LmsSubmissionError> onFailure)
        {
            var attemptNumber = payload == null ? 0 : payload.AttemptNumber;
            if (LmsPayloadValidator.ValidateCompletion(payload).Count > 0)
            {
                onFailure?.Invoke(Error(
                    LmsSubmissionErrorCode.InvalidPayload,
                    "lms.payload.invalid",
                    attemptNumber));
                return;
            }

            string json;
            try
            {
                json = LmsPayloadJson.SerializeCompletion(payload);
            }
            catch
            {
                onFailure?.Invoke(Error(
                    LmsSubmissionErrorCode.InvalidPayload,
                    "lms.payload.invalid",
                    attemptNumber));
                return;
            }

            var completed = false;
            try
            {
                transport.SubmitCompletion(
                    json,
                    () =>
                    {
                        if (completed) return;
                        completed = true;
                        onSuccess?.Invoke();
                    },
                    reason =>
                    {
                        if (completed) return;
                        completed = true;
                        onFailure?.Invoke(MapError(reason, attemptNumber));
                    });
            }
            catch
            {
                if (completed) return;
                completed = true;
                onFailure?.Invoke(Error(
                    LmsSubmissionErrorCode.SubmissionFailed,
                    "lms.submission.failed",
                    attemptNumber));
            }
        }

        private static LmsSubmissionError MapError(
            WebGlLmsTransportFailure failure,
            int attemptNumber)
        {
            switch (failure)
            {
                case WebGlLmsTransportFailure.SessionExpired:
                    return Error(
                        LmsSubmissionErrorCode.SessionExpired,
                        "lms.session.expired",
                        attemptNumber);

                case WebGlLmsTransportFailure.MissingConfiguration:
                    return Error(
                        LmsSubmissionErrorCode.MissingConfiguration,
                        "lms.configuration.missing",
                        attemptNumber);

                default:
                    return Error(
                        LmsSubmissionErrorCode.SubmissionFailed,
                        "lms.submission.failed",
                        attemptNumber);
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

    public sealed class LmsLaunchPoller
    {
        private readonly ILmsBridge bridge;
        private readonly float intervalSeconds;
        private bool hasPolled;
        private float lastPollTime;
        private float nextPollTime;

        public LmsLaunchPoller(ILmsBridge bridge, float intervalSeconds = 0.2f)
        {
            this.bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            if (float.IsNaN(intervalSeconds) || float.IsInfinity(intervalSeconds) || intervalSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(intervalSeconds));
            }
            this.intervalSeconds = intervalSeconds;
        }

        public bool LastCallPolledTransport { get; private set; }

        public bool TryPoll(float unscaledTime, out LmsLaunchConfig launch)
        {
            if (float.IsNaN(unscaledTime) || float.IsInfinity(unscaledTime))
            {
                throw new ArgumentOutOfRangeException(nameof(unscaledTime));
            }

            LastCallPolledTransport = false;
            if (hasPolled && unscaledTime >= lastPollTime && unscaledTime < nextPollTime)
            {
                launch = null;
                return false;
            }

            hasPolled = true;
            lastPollTime = unscaledTime;
            nextPollTime = unscaledTime + intervalSeconds;
            LastCallPolledTransport = true;
            launch = bridge.GetLaunchConfig();
            return launch != null;
        }
    }

    internal sealed class BrowserWebGlLmsTransport : IWebGlLmsTransport
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        private const string ReceiverName = "Pitch Simulator WebGL Bridge Receiver";
        private const float SubmissionTimeoutSeconds = 30f;
        private static readonly Dictionary<int, PendingSubmission> Pending = new();
        private static readonly List<int> ExpiredRequestIds = new();
        private static int nextRequestId;
        private static WebGlLmsCallbackReceiver receiver;

        [DllImport("__Internal")]
        private static extern string PitchSimulatorBridge_GetLaunchConfigJson();

        [DllImport("__Internal")]
        private static extern void PitchSimulatorBridge_SubmitCompletion(
            string completionJson,
            int requestId,
            string receiverName);

        [DllImport("__Internal")]
        private static extern void PitchSimulatorBridge_CancelSubmission(int requestId);
#endif

        public string GetLaunchConfigJson()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            EnsureReceiver();
            return PitchSimulatorBridge_GetLaunchConfigJson();
#else
            return null;
#endif
        }

        public void SubmitCompletion(
            string completionJson,
            Action onSuccess,
            Action<WebGlLmsTransportFailure> onFailure)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            EnsureReceiver();
            var requestId = ++nextRequestId;
            Pending[requestId] = new PendingSubmission(
                onSuccess,
                onFailure,
                Time.realtimeSinceStartup + SubmissionTimeoutSeconds);
            try
            {
                PitchSimulatorBridge_SubmitCompletion(completionJson, requestId, ReceiverName);
            }
            catch
            {
                Pending.Remove(requestId);
                TryCancelJavaScriptRequest(requestId);
                throw;
            }
#else
            onFailure?.Invoke(WebGlLmsTransportFailure.MissingConfiguration);
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private static void EnsureReceiver()
        {
            if (receiver != null) return;
            var host = new GameObject(ReceiverName);
            UnityEngine.Object.DontDestroyOnLoad(host);
            receiver = host.AddComponent<WebGlLmsCallbackReceiver>();
        }

        internal static void Complete(int requestId)
        {
            if (!Pending.Remove(requestId, out var submission)) return;
            submission.Success?.Invoke();
        }

        internal static void Fail(int requestId, WebGlLmsTransportFailure failure)
        {
            if (!Pending.Remove(requestId, out var submission)) return;
            submission.Failure?.Invoke(failure);
        }

        internal static void ExpireTimedOutSubmissions(float now)
        {
            ExpiredRequestIds.Clear();
            foreach (var pair in Pending)
            {
                if (now >= pair.Value.Deadline)
                {
                    ExpiredRequestIds.Add(pair.Key);
                }
            }

            foreach (var requestId in ExpiredRequestIds)
            {
                if (!Pending.Remove(requestId, out var submission)) continue;
                TryCancelJavaScriptRequest(requestId);
                submission.Failure?.Invoke(WebGlLmsTransportFailure.SubmissionFailed);
            }
            ExpiredRequestIds.Clear();
        }

        private static void TryCancelJavaScriptRequest(int requestId)
        {
            try
            {
                PitchSimulatorBridge_CancelSubmission(requestId);
            }
            catch
            {
                // C# ownership is already cleared; a stale browser callback is ignored.
            }
        }

        private sealed class PendingSubmission
        {
            public PendingSubmission(
                Action success,
                Action<WebGlLmsTransportFailure> failure,
                float deadline)
            {
                Success = success;
                Failure = failure;
                Deadline = deadline;
            }

            public Action Success { get; }

            public Action<WebGlLmsTransportFailure> Failure { get; }

            public float Deadline { get; }
        }
#endif
    }

}
