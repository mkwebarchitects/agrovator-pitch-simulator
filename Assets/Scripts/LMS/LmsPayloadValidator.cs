using System;
using System.Collections.Generic;
using System.Globalization;

namespace Agrovator.PitchSimulator.LMS
{
    public enum LmsValidationSeverity
    {
        Warning,
        Error,
    }

    public sealed class LmsValidationIssue
    {
        public LmsValidationIssue(string code, string path, LmsValidationSeverity severity)
        {
            Code = code;
            Path = path;
            Severity = severity;
        }

        public string Code { get; }

        public string Path { get; }

        public LmsValidationSeverity Severity { get; }
    }

    public static class LmsPayloadValidator
    {
        public const int SupportedContentVersion = 1;

        private static readonly string[] ForbiddenCompletionFieldFragments =
        {
            "name",
            "email",
            "school",
            "token",
            "nonce",
            "answertext",
            "responsetext",
            "openended",
            "freeform",
        };

        public static IReadOnlyList<LmsValidationIssue> ValidateLaunch(LmsLaunchConfig config)
        {
            var issues = new List<LmsValidationIssue>();
            if (config == null)
            {
                Add(issues, "lms.launch.required", string.Empty);
                return issues;
            }

            ValidateRequiredIdentity(
                config.SessionId,
                config.ScenarioId,
                config.ContentVersion,
                config.AttemptNumber,
                issues);
            return issues;
        }

        public static IReadOnlyList<LmsValidationIssue> ValidateCompletion(
            LmsCompletionPayload payload)
        {
            var issues = new List<LmsValidationIssue>();
            if (payload == null)
            {
                Add(issues, "lms.payload.required", string.Empty);
                return issues;
            }

            ValidateRequiredIdentity(
                payload.SessionId,
                payload.ScenarioId,
                payload.ContentVersion,
                payload.AttemptNumber,
                issues);
            ValidateInclusiveScore(payload.OverallScore, "lms.overall_score.range", "OverallScore", issues);
            ValidateInclusiveScore(payload.FinalConfidence, "lms.final_confidence.range", "FinalConfidence", issues);

            var competencyScores = payload.CompetencyScores ?? Array.Empty<LmsCompetencyScore>();
            for (var index = 0; index < competencyScores.Length; index++)
            {
                var competency = competencyScores[index];
                if (competency == null)
                {
                    Add(issues, "lms.competency.required", $"CompetencyScores[{index}]");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(competency.CompetencyId))
                {
                    Add(issues, "lms.competency_id.required", $"CompetencyScores[{index}].CompetencyId");
                }

                ValidateInclusiveScore(
                    competency.Score,
                    "lms.competency_score.range",
                    $"CompetencyScores[{index}].Score",
                    issues);
            }

            DateTimeOffset startedAt;
            DateTimeOffset completedAt;
            var hasValidStart = TryParseUtc(payload.StartedAtUtc, out startedAt);
            var hasValidCompletion = TryParseUtc(payload.CompletedAtUtc, out completedAt);
            if (!hasValidStart)
            {
                Add(issues, "lms.timestamp.utc", "StartedAtUtc");
            }

            if (!hasValidCompletion)
            {
                Add(issues, "lms.timestamp.utc", "CompletedAtUtc");
            }

            if (hasValidStart && hasValidCompletion && completedAt < startedAt)
            {
                Add(issues, "lms.timestamp.order", "CompletedAtUtc");
            }

            if (payload.DurationSeconds < 0d ||
                double.IsNaN(payload.DurationSeconds) ||
                double.IsInfinity(payload.DurationSeconds))
            {
                Add(issues, "lms.duration.invalid", "DurationSeconds");
            }

            if (payload.TimeoutCount < 0)
            {
                Add(issues, "lms.timeout_count.invalid", "TimeoutCount");
            }

            issues.AddRange(ValidateCompletionPrivacyShape());
            return issues;
        }

        public static IReadOnlyList<LmsValidationIssue> ValidateCompletionPrivacyShape()
        {
            var issues = new List<LmsValidationIssue>();
            var fields = typeof(LmsCompletionPayload).GetFields();
            foreach (var field in fields)
            {
                var normalizedName = field.Name.ToLowerInvariant();
                foreach (var forbiddenFragment in ForbiddenCompletionFieldFragments)
                {
                    if (normalizedName.Contains(forbiddenFragment))
                    {
                        Add(issues, "lms.privacy.forbidden_field", field.Name);
                        break;
                    }
                }
            }

            return issues;
        }

        private static void ValidateRequiredIdentity(
            string sessionId,
            string scenarioId,
            int contentVersion,
            int attemptNumber,
            ICollection<LmsValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                Add(issues, "lms.session.required", "SessionId");
            }

            if (string.IsNullOrWhiteSpace(scenarioId))
            {
                Add(issues, "lms.scenario.required", "ScenarioId");
            }

            if (contentVersion != SupportedContentVersion)
            {
                Add(issues, "lms.content_version.unsupported", "ContentVersion");
            }

            if (attemptNumber < 0)
            {
                Add(issues, "lms.attempt.invalid", "AttemptNumber");
            }
        }

        private static void ValidateInclusiveScore(
            int score,
            string code,
            string path,
            ICollection<LmsValidationIssue> issues)
        {
            if (score < 0 || score > 100)
            {
                Add(issues, code, path);
            }
        }

        private static bool TryParseUtc(string value, out DateTimeOffset timestamp)
        {
            timestamp = default;
            if (string.IsNullOrWhiteSpace(value) ||
                !value.EndsWith("Z", StringComparison.Ordinal))
            {
                return false;
            }

            return DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out timestamp);
        }

        private static void Add(
            ICollection<LmsValidationIssue> issues,
            string code,
            string path)
        {
            issues.Add(new LmsValidationIssue(code, path, LmsValidationSeverity.Error));
        }
    }
}
