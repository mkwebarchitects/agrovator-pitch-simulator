using System;
using System.Runtime.Serialization;

namespace Agrovator.PitchSimulator.GuidedPitch
{
    [Serializable]
    [DataContract]
    public sealed class GuidedPitchContentDto
    {
        [DataMember(Name = "Id", Order = 0)] public string Id;
        [DataMember(Name = "Version", Order = 1)] public int Version;
        [DataMember(Name = "EstimatedDurationMinutes", Order = 2)] public int EstimatedDurationMinutes;
        [DataMember(Name = "TitleKey", Order = 3)] public string TitleKey;
        [DataMember(Name = "BriefingKey", Order = 4)] public string BriefingKey;
        [DataMember(Name = "LearnIncompletePitchKey", Order = 5)] public string LearnIncompletePitchKey;
        [DataMember(Name = "LearnExplanationKey", Order = 6)] public string LearnExplanationKey;
        [DataMember(Name = "Modes", Order = 7)] public GuidedLearnerModeContentDto[] Modes = Array.Empty<GuidedLearnerModeContentDto>();
        [DataMember(Name = "SupportedLocales", Order = 8)] public string[] SupportedLocales = Array.Empty<string>();
        [DataMember(Name = "ContentChecksum", Order = 9)] public string ContentChecksum;
    }

    [Serializable]
    [DataContract]
    public sealed class GuidedLearnerModeContentDto
    {
        [DataMember(Name = "Mode", Order = 0)] public string Mode;
        [DataMember(Name = "PromptStyleKey", Order = 1)] public string PromptStyleKey;
        [DataMember(Name = "Parts", Order = 2)] public GuidedPitchPartContentDto[] Parts = Array.Empty<GuidedPitchPartContentDto>();
        [DataMember(Name = "FollowUp", Order = 3)] public GuidedFollowUpContentDto FollowUp;
    }

    [Serializable]
    [DataContract]
    public sealed class GuidedPitchPartContentDto
    {
        [DataMember(Name = "Part", Order = 0)] public string Part;
        [DataMember(Name = "LabelKey", Order = 1)] public string LabelKey;
        [DataMember(Name = "PlainPromptKey", Order = 2)] public string PlainPromptKey;
        [DataMember(Name = "QuestionKey", Order = 3)] public string QuestionKey;
        [DataMember(Name = "HintKey", Order = 4)] public string HintKey;
        [DataMember(Name = "Options", Order = 5)] public GuidedPitchOptionDto[] Options = Array.Empty<GuidedPitchOptionDto>();
    }

    [Serializable]
    [DataContract]
    public sealed class GuidedFollowUpContentDto
    {
        [DataMember(Name = "LabelKey", Order = 0)] public string LabelKey;
        [DataMember(Name = "QuestionKey", Order = 1)] public string QuestionKey;
        [DataMember(Name = "HintKey", Order = 2)] public string HintKey;
        [DataMember(Name = "Options", Order = 3)] public GuidedPitchOptionDto[] Options = Array.Empty<GuidedPitchOptionDto>();
    }

    [Serializable]
    [DataContract]
    public sealed class GuidedPitchOptionDto
    {
        [DataMember(Name = "Id", Order = 0)] public string Id;
        [DataMember(Name = "TextKey", Order = 1)] public string TextKey;
        [DataMember(Name = "Mastery", Order = 2)] public string Mastery;
        [DataMember(Name = "LegacyConfidenceDelta", Order = 3)] public int LegacyConfidenceDelta;
        [DataMember(Name = "ReactionCue", Order = 4)] public string ReactionCue;
        [DataMember(Name = "Feedback", Order = 5)] public GuidedPitchFeedbackDto Feedback;
    }

    [Serializable]
    [DataContract]
    public sealed class GuidedPitchFeedbackDto
    {
        [DataMember(Name = "WorkedKey", Order = 0)] public string WorkedKey;
        [DataMember(Name = "MissingKey", Order = 1)] public string MissingKey;
        [DataMember(Name = "ImproveKey", Order = 2)] public string ImproveKey;
    }
}
