using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Agrovator.PitchSimulator.GuidedPitch
{
    public enum GuidedPitchContentIssueSeverity
    {
        Warning,
        Error,
    }

    public sealed class GuidedPitchContentIssue
    {
        public GuidedPitchContentIssue(string code, string path, GuidedPitchContentIssueSeverity severity)
        {
            Code = code;
            Path = path;
            Severity = severity;
        }

        public string Code { get; }
        public string Path { get; }
        public GuidedPitchContentIssueSeverity Severity { get; }
    }

    public static class GuidedPitchContentValidator
    {
        private const string LocalizationKeyMissing = "guided.localization_key_missing";

        public static IReadOnlyList<GuidedPitchContentIssue> Validate(
            GuidedPitchContentDto content,
            IEnumerable<string> localizationKeys)
        {
            var keys = new HashSet<string>(localizationKeys ?? Array.Empty<string>(), StringComparer.Ordinal);
            return ValidateCore(content, keys, null);
        }

        public static IReadOnlyList<GuidedPitchContentIssue> ValidateWithLocalizationValues(
            GuidedPitchContentDto content,
            IReadOnlyDictionary<string, string> localizationValues)
        {
            var values = localizationValues ?? new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>(StringComparer.Ordinal));
            return ValidateCore(
                content,
                new HashSet<string>(values.Keys, StringComparer.Ordinal),
                values);
        }

        private static IReadOnlyList<GuidedPitchContentIssue> ValidateCore(
            GuidedPitchContentDto content,
            ISet<string> localizationKeys,
            IReadOnlyDictionary<string, string> localizationValues)
        {
            var issues = new List<GuidedPitchContentIssue>();
            if (content == null)
            {
                issues.Add(Error("guided.content_missing", "$"));
                return issues.AsReadOnly();
            }

            if (!string.Equals(content.Id, "smart-school-garden", StringComparison.Ordinal))
            {
                issues.Add(Error("guided.scenario_id_invalid", "Id"));
            }

            if (content.Version != 2)
            {
                issues.Add(Error("guided.version_invalid", "Version"));
            }

            if (content.EstimatedDurationMinutes < 8 || content.EstimatedDurationMinutes > 10)
            {
                issues.Add(Error("guided.duration_invalid", "EstimatedDurationMinutes"));
            }

            ValidateLocalizationKey(content.TitleKey, "TitleKey", localizationKeys, issues);
            ValidateLocalizationKey(content.BriefingKey, "BriefingKey", localizationKeys, issues);
            ValidateLocalizationKey(
                content.LearnIncompletePitchKey,
                "LearnIncompletePitchKey",
                localizationKeys,
                issues);
            ValidateLocalizationKey(
                content.LearnExplanationKey,
                "LearnExplanationKey",
                localizationKeys,
                issues);

            var modes = content.Modes ?? Array.Empty<GuidedLearnerModeContentDto>();
            var seenModes = new HashSet<LearnerMode>();
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            for (var modeIndex = 0; modeIndex < modes.Length; modeIndex++)
            {
                var path = $"Modes[{modeIndex}]";
                var mode = modes[modeIndex];
                if (mode == null)
                {
                    issues.Add(Error("guided.mode_missing", path));
                    continue;
                }

                if (!TryParseDefined(mode.Mode, out LearnerMode parsedMode))
                {
                    issues.Add(Error("guided.mode_invalid", path + ".Mode"));
                    continue;
                }

                if (!seenModes.Add(parsedMode))
                {
                    issues.Add(Error("guided.mode_duplicate", path + ".Mode"));
                }

                ValidateLocalizationKey(mode.PromptStyleKey, path + ".PromptStyleKey", localizationKeys, issues);
                ValidateParts(
                    mode.Parts,
                    parsedMode,
                    path + ".Parts",
                    localizationKeys,
                    localizationValues,
                    seenIds,
                    issues);
                ValidateFollowUp(
                    mode.FollowUp,
                    parsedMode,
                    path + ".FollowUp",
                    localizationKeys,
                    localizationValues,
                    seenIds,
                    issues);
            }

            foreach (LearnerMode requiredMode in Enum.GetValues(typeof(LearnerMode)))
            {
                if (!seenModes.Contains(requiredMode))
                {
                    issues.Add(Error("guided.mode_missing", "Modes"));
                }
            }

            ValidateSupportedLocales(content.SupportedLocales, issues);
            if (string.IsNullOrWhiteSpace(content.ContentChecksum))
            {
                issues.Add(Error("guided.checksum_missing", "ContentChecksum"));
            }

            return issues.AsReadOnly();
        }

        private static void ValidateParts(
            GuidedPitchPartContentDto[] parts,
            LearnerMode mode,
            string path,
            ISet<string> localizationKeys,
            IReadOnlyDictionary<string, string> localizationValues,
            ISet<string> seenIds,
            ICollection<GuidedPitchContentIssue> issues)
        {
            parts = parts ?? Array.Empty<GuidedPitchPartContentDto>();
            if (parts.Length != PitchParts.Ordered.Count)
            {
                issues.Add(Error("guided.part_order_invalid", path));
            }

            for (var index = 0; index < parts.Length; index++)
            {
                var partPath = $"{path}[{index}]";
                var part = parts[index];
                if (part == null)
                {
                    issues.Add(Error("guided.part_order_invalid", partPath));
                    continue;
                }

                if (!TryParseDefined(part.Part, out PitchPart parsedPart) ||
                    index >= PitchParts.Ordered.Count || parsedPart != PitchParts.Ordered[index])
                {
                    issues.Add(Error("guided.part_order_invalid", partPath + ".Part"));
                }

                ValidateLocalizationKey(part.LabelKey, partPath + ".LabelKey", localizationKeys, issues);
                ValidateLocalizationKey(part.PlainPromptKey, partPath + ".PlainPromptKey", localizationKeys, issues);
                ValidateLocalizationKey(part.QuestionKey, partPath + ".QuestionKey", localizationKeys, issues);
                ValidateLocalizationKey(part.HintKey, partPath + ".HintKey", localizationKeys, issues);
                ValidateOptions(
                    part.Options,
                    mode,
                    partPath + ".Options",
                    localizationKeys,
                    localizationValues,
                    seenIds,
                    issues);
            }
        }

        private static void ValidateFollowUp(
            GuidedFollowUpContentDto followUp,
            LearnerMode mode,
            string path,
            ISet<string> localizationKeys,
            IReadOnlyDictionary<string, string> localizationValues,
            ISet<string> seenIds,
            ICollection<GuidedPitchContentIssue> issues)
        {
            if (followUp == null)
            {
                issues.Add(Error("guided.follow_up_missing", path));
                return;
            }

            ValidateLocalizationKey(followUp.LabelKey, path + ".LabelKey", localizationKeys, issues);
            ValidateLocalizationKey(followUp.QuestionKey, path + ".QuestionKey", localizationKeys, issues);
            ValidateLocalizationKey(followUp.HintKey, path + ".HintKey", localizationKeys, issues);
            ValidateOptions(
                followUp.Options,
                mode,
                path + ".Options",
                localizationKeys,
                localizationValues,
                seenIds,
                issues);
        }

        private static void ValidateOptions(
            GuidedPitchOptionDto[] options,
            LearnerMode mode,
            string path,
            ISet<string> localizationKeys,
            IReadOnlyDictionary<string, string> localizationValues,
            ISet<string> seenIds,
            ICollection<GuidedPitchContentIssue> issues)
        {
            options = options ?? Array.Empty<GuidedPitchOptionDto>();
            if (options.Length != 3)
            {
                issues.Add(Error("guided.option_count_invalid", path));
            }

            var seenMastery = new HashSet<MasteryState>();
            for (var index = 0; index < options.Length; index++)
            {
                var optionPath = $"{path}[{index}]";
                var option = options[index];
                if (option == null)
                {
                    issues.Add(Error("guided.option_missing", optionPath));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(option.Id))
                {
                    issues.Add(Error("guided.id_missing", optionPath + ".Id"));
                }
                else if (!seenIds.Add(option.Id))
                {
                    issues.Add(Error("guided.id_duplicate", optionPath + ".Id"));
                }

                ValidateLocalizationKey(option.TextKey, optionPath + ".TextKey", localizationKeys, issues);
                if (localizationValues != null &&
                    option.TextKey != null && localizationValues.TryGetValue(option.TextKey, out var text))
                {
                    var wordCount = CountWords(text);
                    if (mode == LearnerMode.Primary && (wordCount < 12 || wordCount > 16))
                    {
                        issues.Add(Error("guided.primary_word_count_invalid", optionPath + ".TextKey"));
                    }
                    else if (mode == LearnerMode.Secondary && wordCount > 32)
                    {
                        issues.Add(Error("guided.secondary_word_count_invalid", optionPath + ".TextKey"));
                    }
                }

                if (!TryParseDefined(option.Mastery, out MasteryState mastery))
                {
                    issues.Add(Error("guided.mastery_invalid", optionPath + ".Mastery"));
                }
                else
                {
                    if (!seenMastery.Add(mastery))
                    {
                        issues.Add(Error("guided.mastery_set_invalid", optionPath + ".Mastery"));
                    }

                    if (option.LegacyConfidenceDelta != ExpectedDelta(mastery))
                    {
                        issues.Add(Error("guided.legacy_confidence_delta_invalid", optionPath + ".LegacyConfidenceDelta"));
                    }

                    if (!string.Equals(option.ReactionCue, ExpectedReaction(mastery), StringComparison.Ordinal))
                    {
                        issues.Add(Error("guided.reaction_cue_invalid", optionPath + ".ReactionCue"));
                    }
                }

                ValidateFeedback(option.Feedback, option.Id, optionPath + ".Feedback", localizationKeys, issues);
            }

            if (seenMastery.Count != 3)
            {
                issues.Add(Error("guided.mastery_set_invalid", path));
            }
        }

        private static void ValidateFeedback(
            GuidedPitchFeedbackDto feedback,
            string optionId,
            string path,
            ISet<string> localizationKeys,
            ICollection<GuidedPitchContentIssue> issues)
        {
            if (feedback == null)
            {
                issues.Add(Error("guided.feedback_key_missing", path));
                return;
            }

            ValidateFeedbackKey(feedback.WorkedKey, optionId, "worked", path + ".WorkedKey", localizationKeys, issues);
            ValidateFeedbackKey(feedback.MissingKey, optionId, "missing", path + ".MissingKey", localizationKeys, issues);
            ValidateFeedbackKey(feedback.ImproveKey, optionId, "improve", path + ".ImproveKey", localizationKeys, issues);
        }

        private static void ValidateFeedbackKey(
            string value,
            string optionId,
            string suffix,
            string path,
            ISet<string> localizationKeys,
            ICollection<GuidedPitchContentIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                issues.Add(Error("guided.feedback_key_missing", path));
                return;
            }

            var expected = $"guided.feedback.{optionId}.{suffix}";
            if (!string.Equals(value, expected, StringComparison.Ordinal))
            {
                issues.Add(Error("guided.feedback_key_pattern_invalid", path));
            }

            if (!localizationKeys.Contains(value))
            {
                issues.Add(Error(LocalizationKeyMissing, path));
            }
        }

        private static void ValidateLocalizationKey(
            string value,
            string path,
            ISet<string> localizationKeys,
            ICollection<GuidedPitchContentIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(value) || !localizationKeys.Contains(value))
            {
                issues.Add(Error(LocalizationKeyMissing, path));
            }
        }

        private static void ValidateSupportedLocales(
            string[] locales,
            ICollection<GuidedPitchContentIssue> issues)
        {
            // English is the only shipped locale. A second entry would claim a
            // translation the game does not have.
            if (locales == null || locales.Length != 1 ||
                !string.Equals(locales[0], "en", StringComparison.Ordinal))
            {
                issues.Add(Error("guided.supported_locales_invalid", "SupportedLocales"));
            }
        }

        private static bool TryParseDefined<TEnum>(string value, out TEnum parsed) where TEnum : struct
        {
            return Enum.TryParse(value, false, out parsed) && Enum.IsDefined(typeof(TEnum), parsed);
        }

        private static int ExpectedDelta(MasteryState mastery)
        {
            return mastery == MasteryState.Clear ? 4 : mastery == MasteryState.Developing ? 1 : -4;
        }

        private static string ExpectedReaction(MasteryState mastery)
        {
            return mastery == MasteryState.Clear ? "Impressed" : mastery == MasteryState.Developing ? "Curious" : "Concerned";
        }

        private static int CountWords(string value)
        {
            return (value ?? string.Empty).Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private static GuidedPitchContentIssue Error(string code, string path)
        {
            return new GuidedPitchContentIssue(code, path, GuidedPitchContentIssueSeverity.Error);
        }
    }
}
