using UnityEngine;

namespace Agrovator.PitchSimulator.LMS
{
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
