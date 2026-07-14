using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

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

    [DisallowMultipleComponent]
    public sealed class WebGlLmsBridgeHost : MonoBehaviour
    {
        private const float LaunchPollIntervalSeconds = 0.2f;

        [SerializeField] private Text diagnosticsLabel;

        private WebGlLmsBridge bridge;
        private float nextLaunchPollTime;

        public bool IsConfigured { get; private set; }

        public Text DiagnosticsLabel => diagnosticsLabel;

        private void Start()
        {
            bridge ??= new WebGlLmsBridge();
            RefreshLaunchStatus();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextLaunchPollTime) return;
            RefreshLaunchStatus();
        }

        public void Initialize(IWebGlLmsTransport transport)
        {
            bridge = new WebGlLmsBridge(transport);
            RefreshLaunchStatus();
        }

        public void RefreshLaunchStatus()
        {
            bridge ??= new WebGlLmsBridge();
            nextLaunchPollTime = Time.unscaledTime + LaunchPollIntervalSeconds;
            var launch = bridge.GetLaunchConfig();
            IsConfigured = launch != null;
            if (diagnosticsLabel == null) return;

            diagnosticsLabel.text = IsConfigured
                ? $"LMS bridge ready (attempt {launch.AttemptNumber})."
                : "LMS bridge waiting for launch configuration.";
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
            PitchSimulatorBridge_SubmitCompletion(completionJson, requestId, ReceiverName);
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
                Fail(requestId, WebGlLmsTransportFailure.SubmissionFailed);
            }
            ExpiredRequestIds.Clear();
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

    public sealed class WebGlLmsCallbackReceiver : MonoBehaviour
    {
        private void Update()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            BrowserWebGlLmsTransport.ExpireTimedOutSubmissions(Time.realtimeSinceStartup);
#endif
        }

        public void OnLmsSubmissionSucceeded(string requestId)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (int.TryParse(requestId, out var parsed))
            {
                BrowserWebGlLmsTransport.Complete(parsed);
            }
#endif
        }

        public void OnLmsSubmissionFailed(string result)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (string.IsNullOrEmpty(result)) return;
            var separator = result.IndexOf('|');
            if (separator <= 0 || !int.TryParse(result.Substring(0, separator), out var requestId))
            {
                return;
            }

            var status = result.Substring(separator + 1);
            var failure = status == "expired"
                ? WebGlLmsTransportFailure.SessionExpired
                : status == "missing-config"
                    ? WebGlLmsTransportFailure.MissingConfiguration
                    : WebGlLmsTransportFailure.SubmissionFailed;
            BrowserWebGlLmsTransport.Fail(requestId, failure);
#endif
        }
    }
}
