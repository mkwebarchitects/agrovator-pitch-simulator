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

        private static readonly string[] CanonicalUtcTimestampFormats =
        {
            "yyyy-MM-dd'T'HH:mm:ss'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.f'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.ff'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.ffff'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.fffff'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'",
        };

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

            ValidateVolume(config.MusicVolume, "lms.music_volume.range", "MusicVolume", issues);
            ValidateVolume(config.SfxVolume, "lms.sfx_volume.range", "SfxVolume", issues);
            if (!IsValidLaunchReference(config.LaunchReference))
            {
                Add(issues, "lms.launch_reference.invalid", "LaunchReference");
            }

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
            if (string.IsNullOrWhiteSpace(value))
            {
                timestamp = default;
                return false;
            }

            return DateTimeOffset.TryParseExact(
                value,
                CanonicalUtcTimestampFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out timestamp);
        }

        private static void ValidateVolume(
            float volume,
            string code,
            string path,
            ICollection<LmsValidationIssue> issues)
        {
            if (float.IsNaN(volume) || float.IsInfinity(volume) || volume < 0f || volume > 1f)
            {
                Add(issues, code, path);
            }
        }

        private static bool IsValidLaunchReference(string value)
        {
            const string prefix = "lref_";
            if (string.IsNullOrEmpty(value) ||
                !value.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            var identifierLength = value.Length - prefix.Length;
            if (identifierLength < 12 || identifierLength > 60)
            {
                return false;
            }

            for (var index = prefix.Length; index < value.Length; index++)
            {
                var character = value[index];
                var isUrlSafe = character >= 'A' && character <= 'Z' ||
                                character >= 'a' && character <= 'z' ||
                                character >= '0' && character <= '9' ||
                                character == '_' ||
                                character == '-';
                if (!isUrlSafe)
                {
                    return false;
                }
            }

            return true;
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
