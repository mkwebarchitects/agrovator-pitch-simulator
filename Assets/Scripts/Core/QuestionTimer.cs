using System;

namespace Agrovator.PitchSimulator.Core
{
    public sealed class QuestionTimer
    {
        private bool isEnabled;

        public QuestionTimer(double durationSeconds)
        {
            Reset(durationSeconds);
        }

        public event Action Expired;

        public double RemainingSeconds { get; private set; }

        public bool IsPaused { get; private set; }

        public bool HasExpired { get; private set; }

        public void Reset(double durationSeconds)
        {
            if (durationSeconds < 0d || double.IsNaN(durationSeconds) || double.IsInfinity(durationSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            }

            RemainingSeconds = durationSeconds;
            isEnabled = durationSeconds > 0d;
            IsPaused = false;
            HasExpired = false;
        }

        public void Pause()
        {
            IsPaused = true;
        }

        public void Resume()
        {
            IsPaused = false;
        }

        public void Tick(double seconds)
        {
            if (seconds < 0d || double.IsNaN(seconds) || double.IsInfinity(seconds))
            {
                throw new ArgumentOutOfRangeException(nameof(seconds));
            }

            if (!isEnabled || IsPaused || HasExpired)
            {
                return;
            }

            if (seconds < RemainingSeconds)
            {
                RemainingSeconds -= seconds;
                return;
            }

            RemainingSeconds = 0d;
            HasExpired = true;
            Expired?.Invoke();
        }
    }
}
