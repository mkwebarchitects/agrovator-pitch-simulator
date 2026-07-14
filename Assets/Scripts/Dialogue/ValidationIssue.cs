namespace Agrovator.PitchSimulator.Dialogue
{
    public enum ValidationSeverity
    {
        Warning,
        Error,
    }

    public sealed class ValidationIssue
    {
        public ValidationIssue(string code, string path, ValidationSeverity severity)
        {
            Code = code;
            Path = path;
            Severity = severity;
        }

        public string Code { get; }

        public string Path { get; }

        public ValidationSeverity Severity { get; }
    }
}
