using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Agrovator.PitchSimulator.UI
{
    /// <summary>
    /// Read-only display metrics for the CSS-sized WebGL canvas. These values do not
    /// contain or transmit learner, launch, content, or completion data.
    /// </summary>
    public readonly struct WebViewportMetrics : IEquatable<WebViewportMetrics>
    {
        public WebViewportMetrics(int cssWidth, int cssHeight, float devicePixelRatio)
        {
            if (cssWidth <= 0) throw new ArgumentOutOfRangeException(nameof(cssWidth));
            if (cssHeight <= 0) throw new ArgumentOutOfRangeException(nameof(cssHeight));
            if (devicePixelRatio <= 0f || float.IsNaN(devicePixelRatio) ||
                float.IsInfinity(devicePixelRatio))
            {
                throw new ArgumentOutOfRangeException(nameof(devicePixelRatio));
            }

            CssWidth = cssWidth;
            CssHeight = cssHeight;
            DevicePixelRatio = devicePixelRatio;
        }

        public int CssWidth { get; }

        public int CssHeight { get; }

        public float DevicePixelRatio { get; }

        public Vector2 CssSize => new Vector2(CssWidth, CssHeight);

        public static WebViewportMetrics Read()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new WebViewportMetrics(
                PitchSimulatorViewportWidth(),
                PitchSimulatorViewportHeight(),
                PitchSimulatorDevicePixelRatioTimes100() / 100f);
#else
            return new WebViewportMetrics(
                Mathf.Max(1, Screen.width),
                Mathf.Max(1, Screen.height),
                1f);
#endif
        }

        public bool Equals(WebViewportMetrics other)
        {
            return CssWidth == other.CssWidth &&
                CssHeight == other.CssHeight &&
                DevicePixelRatio.Equals(other.DevicePixelRatio);
        }

        public override bool Equals(object obj)
        {
            return obj is WebViewportMetrics other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = CssWidth;
                hash = (hash * 397) ^ CssHeight;
                hash = (hash * 397) ^ DevicePixelRatio.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(WebViewportMetrics left, WebViewportMetrics right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(WebViewportMetrics left, WebViewportMetrics right)
        {
            return !left.Equals(right);
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int PitchSimulatorViewportWidth();

        [DllImport("__Internal")]
        private static extern int PitchSimulatorViewportHeight();

        [DllImport("__Internal")]
        private static extern int PitchSimulatorDevicePixelRatioTimes100();
#endif
    }
}
