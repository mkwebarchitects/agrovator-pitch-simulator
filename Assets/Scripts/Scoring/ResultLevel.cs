namespace Agrovator.PitchSimulator.Scoring
{
    public enum ResultLevel
    {
        Seedling,
        Sprouting,
        Growing,
        Thriving,
    }

    public sealed class ResultLevelDefinition
    {
        public ResultLevelDefinition(ResultLevel level, int inclusiveMinimum, string localizationKey)
        {
            Level = level;
            InclusiveMinimum = inclusiveMinimum;
            LocalizationKey = localizationKey;
        }

        public ResultLevel Level { get; }

        public int InclusiveMinimum { get; }

        public string LocalizationKey { get; }
    }
}
