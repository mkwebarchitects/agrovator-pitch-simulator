namespace Agrovator.PitchSimulator.Scoring
{
    public sealed class ConfidenceMeter
    {
        public const int Minimum = 0;
        public const int Maximum = 100;

        public ConfidenceMeter(int initialValue)
        {
            Value = Clamp(initialValue);
        }

        public int Value { get; private set; }

        public void Apply(int delta)
        {
            var candidate = (long)Value + delta;
            Value = candidate < Minimum
                ? Minimum
                : candidate > Maximum
                    ? Maximum
                    : (int)candidate;
        }

        private static int Clamp(int value)
        {
            if (value < Minimum)
            {
                return Minimum;
            }

            return value > Maximum ? Maximum : value;
        }
    }
}
