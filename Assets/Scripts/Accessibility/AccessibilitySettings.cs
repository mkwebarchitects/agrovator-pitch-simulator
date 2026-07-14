using System;

namespace Agrovator.PitchSimulator.Accessibility
{
    public sealed class AccessibilitySettings
    {
        public const string EnglishLocale = "en";
        public const string MalayLocale = "ms";

        public AccessibilitySettings(
            TimerMode timerMode,
            bool reducedMotion,
            float musicVolume,
            float sfxVolume,
            string locale)
        {
            TimerMode = NormalizeTimerMode(timerMode);
            ReducedMotion = reducedMotion;
            MusicVolume = ClampVolume(musicVolume);
            SfxVolume = ClampVolume(sfxVolume);
            Locale = NormalizeLocale(locale);
        }

        public TimerMode TimerMode { get; }

        public bool ReducedMotion { get; }

        public float MusicVolume { get; }

        public float SfxVolume { get; }

        public string Locale { get; }

        public double GetEffectiveDuration(double authoredDurationSeconds, bool isTutorial)
        {
            if (authoredDurationSeconds < 0d ||
                double.IsNaN(authoredDurationSeconds) ||
                double.IsInfinity(authoredDurationSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(authoredDurationSeconds));
            }

            if (isTutorial || TimerMode == TimerMode.Off)
            {
                return 0d;
            }

            return TimerMode == TimerMode.Extended
                ? authoredDurationSeconds * 1.5d
                : authoredDurationSeconds;
        }

        private static TimerMode NormalizeTimerMode(TimerMode timerMode)
        {
            return timerMode == TimerMode.Extended || timerMode == TimerMode.Off
                ? timerMode
                : TimerMode.Normal;
        }

        private static float ClampVolume(float value)
        {
            if (float.IsNaN(value) || value <= 0f)
            {
                return 0f;
            }

            return value >= 1f ? 1f : value;
        }

        private static string NormalizeLocale(string locale)
        {
            return locale == MalayLocale ? MalayLocale : EnglishLocale;
        }
    }
}
