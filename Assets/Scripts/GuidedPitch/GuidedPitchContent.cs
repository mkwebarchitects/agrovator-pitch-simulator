using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Agrovator.PitchSimulator.GuidedPitch
{
    public sealed class GuidedPitchContent
    {
        private GuidedPitchContent(
            string id,
            int version,
            int estimatedDurationMinutes,
            string titleKey,
            string briefingKey,
            string learnIncompletePitchKey,
            string learnExplanationKey,
            IDictionary<LearnerMode, GuidedLearnerModeContent> modes,
            IEnumerable<string> supportedLocales,
            string contentChecksum)
        {
            Id = id;
            Version = version;
            EstimatedDurationMinutes = estimatedDurationMinutes;
            TitleKey = titleKey;
            BriefingKey = briefingKey;
            LearnIncompletePitchKey = learnIncompletePitchKey;
            LearnExplanationKey = learnExplanationKey;
            Modes = new ReadOnlyDictionary<LearnerMode, GuidedLearnerModeContent>(
                new Dictionary<LearnerMode, GuidedLearnerModeContent>(modes));
            SupportedLocales = new ReadOnlyCollection<string>(new List<string>(supportedLocales));
            ContentChecksum = contentChecksum;
        }

        public string Id { get; }
        public int Version { get; }
        public int EstimatedDurationMinutes { get; }
        public string TitleKey { get; }
        public string BriefingKey { get; }
        public string LearnIncompletePitchKey { get; }
        public string LearnExplanationKey { get; }
        public IReadOnlyDictionary<LearnerMode, GuidedLearnerModeContent> Modes { get; }
        public IReadOnlyList<string> SupportedLocales { get; }
        public string ContentChecksum { get; }

        internal static GuidedPitchContent Compile(GuidedPitchContentDto dto)
        {
            var modes = new Dictionary<LearnerMode, GuidedLearnerModeContent>();
            foreach (var mode in dto.Modes)
            {
                var parsedMode = ParseEnum<LearnerMode>(mode.Mode);
                modes.Add(parsedMode, GuidedLearnerModeContent.Compile(parsedMode, mode));
            }

            return new GuidedPitchContent(
                dto.Id,
                dto.Version,
                dto.EstimatedDurationMinutes,
                dto.TitleKey,
                dto.BriefingKey,
                dto.LearnIncompletePitchKey,
                dto.LearnExplanationKey,
                modes,
                dto.SupportedLocales,
                dto.ContentChecksum);
        }

        internal static TEnum ParseEnum<TEnum>(string value) where TEnum : struct
        {
            return (TEnum)Enum.Parse(typeof(TEnum), value, false);
        }
    }

    public sealed class GuidedLearnerModeContent
    {
        private GuidedLearnerModeContent(
            LearnerMode mode,
            string promptStyleKey,
            IEnumerable<GuidedPitchPartContent> parts,
            GuidedFollowUpContent followUp)
        {
            Mode = mode;
            PromptStyleKey = promptStyleKey;
            Parts = new ReadOnlyCollection<GuidedPitchPartContent>(new List<GuidedPitchPartContent>(parts));
            FollowUp = followUp;
        }

        public LearnerMode Mode { get; }
        public string PromptStyleKey { get; }
        public IReadOnlyList<GuidedPitchPartContent> Parts { get; }
        public GuidedFollowUpContent FollowUp { get; }

        internal static GuidedLearnerModeContent Compile(LearnerMode mode, GuidedLearnerModeContentDto dto)
        {
            var parts = new List<GuidedPitchPartContent>();
            foreach (var part in dto.Parts)
            {
                parts.Add(GuidedPitchPartContent.Compile(part));
            }

            return new GuidedLearnerModeContent(
                mode,
                dto.PromptStyleKey,
                parts,
                GuidedFollowUpContent.Compile(dto.FollowUp));
        }
    }

    public sealed class GuidedPitchPartContent
    {
        private GuidedPitchPartContent(
            PitchPart part,
            string labelKey,
            string plainPromptKey,
            string questionKey,
            string hintKey,
            IEnumerable<GuidedPitchOption> options)
        {
            Part = part;
            LabelKey = labelKey;
            PlainPromptKey = plainPromptKey;
            QuestionKey = questionKey;
            HintKey = hintKey;
            Options = new ReadOnlyCollection<GuidedPitchOption>(new List<GuidedPitchOption>(options));
        }

        public PitchPart Part { get; }
        public string LabelKey { get; }
        public string PlainPromptKey { get; }
        public string QuestionKey { get; }
        public string HintKey { get; }
        public IReadOnlyList<GuidedPitchOption> Options { get; }

        internal static GuidedPitchPartContent Compile(GuidedPitchPartContentDto dto)
        {
            return new GuidedPitchPartContent(
                GuidedPitchContent.ParseEnum<PitchPart>(dto.Part),
                dto.LabelKey,
                dto.PlainPromptKey,
                dto.QuestionKey,
                dto.HintKey,
                CompileOptions(dto.Options));
        }

        internal static IEnumerable<GuidedPitchOption> CompileOptions(GuidedPitchOptionDto[] options)
        {
            var compiled = new List<GuidedPitchOption>();
            foreach (var option in options)
            {
                compiled.Add(GuidedPitchOption.Compile(option));
            }

            return compiled;
        }
    }

    public sealed class GuidedFollowUpContent
    {
        private GuidedFollowUpContent(
            string labelKey,
            string questionKey,
            string hintKey,
            IEnumerable<GuidedPitchOption> options)
        {
            LabelKey = labelKey;
            QuestionKey = questionKey;
            HintKey = hintKey;
            Options = new ReadOnlyCollection<GuidedPitchOption>(new List<GuidedPitchOption>(options));
        }

        public string LabelKey { get; }
        public string QuestionKey { get; }
        public string HintKey { get; }
        public IReadOnlyList<GuidedPitchOption> Options { get; }

        internal static GuidedFollowUpContent Compile(GuidedFollowUpContentDto dto)
        {
            return new GuidedFollowUpContent(
                dto.LabelKey,
                dto.QuestionKey,
                dto.HintKey,
                GuidedPitchPartContent.CompileOptions(dto.Options));
        }
    }

    public sealed class GuidedPitchOption
    {
        private GuidedPitchOption(
            string id,
            string textKey,
            MasteryState mastery,
            int legacyConfidenceDelta,
            string reactionCue,
            GuidedPitchFeedback feedback)
        {
            Id = id;
            TextKey = textKey;
            Mastery = mastery;
            LegacyConfidenceDelta = legacyConfidenceDelta;
            ReactionCue = reactionCue;
            Feedback = feedback;
        }

        public string Id { get; }
        public string TextKey { get; }
        public MasteryState Mastery { get; }
        public int LegacyConfidenceDelta { get; }
        public string ReactionCue { get; }
        public GuidedPitchFeedback Feedback { get; }

        internal static GuidedPitchOption Compile(GuidedPitchOptionDto dto)
        {
            return new GuidedPitchOption(
                dto.Id,
                dto.TextKey,
                GuidedPitchContent.ParseEnum<MasteryState>(dto.Mastery),
                dto.LegacyConfidenceDelta,
                dto.ReactionCue,
                new GuidedPitchFeedback(
                    dto.Feedback.WorkedKey,
                    dto.Feedback.MissingKey,
                    dto.Feedback.ImproveKey));
        }
    }

    public sealed class GuidedPitchFeedback
    {
        internal GuidedPitchFeedback(string workedKey, string missingKey, string improveKey)
        {
            WorkedKey = workedKey;
            MissingKey = missingKey;
            ImproveKey = improveKey;
        }

        public string WorkedKey { get; }
        public string MissingKey { get; }
        public string ImproveKey { get; }
    }
}
