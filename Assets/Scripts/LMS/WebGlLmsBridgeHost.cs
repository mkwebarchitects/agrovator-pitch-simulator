using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.LMS
{
    [DisallowMultipleComponent]
    public sealed class WebGlLmsBridgeHost : MonoBehaviour
    {
        [SerializeField] private Text diagnosticsLabel;

        private LmsLaunchPoller launchPoller;

        public bool IsConfigured { get; private set; }

        public Text DiagnosticsLabel => diagnosticsLabel;

        private void Start()
        {
            launchPoller ??= new LmsLaunchPoller(new WebGlLmsBridge());
            RefreshLaunchStatus();
        }

        private void Update()
        {
            RefreshLaunchStatus();
        }

        public void Initialize(IWebGlLmsTransport transport)
        {
            launchPoller = new LmsLaunchPoller(new WebGlLmsBridge(transport));
            RefreshLaunchStatus();
        }

        public void RefreshLaunchStatus()
        {
            launchPoller ??= new LmsLaunchPoller(new WebGlLmsBridge());
            var available = launchPoller.TryPoll(Time.unscaledTime, out var launch);
            if (!launchPoller.LastCallPolledTransport) return;

            IsConfigured = available;
            if (diagnosticsLabel == null) return;
            diagnosticsLabel.text = available
                ? $"LMS bridge ready (attempt {launch.AttemptNumber})."
                : "LMS bridge waiting for launch configuration.";
        }
    }
}
