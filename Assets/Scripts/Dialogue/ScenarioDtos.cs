using System;
using System.Runtime.Serialization;

namespace Agrovator.PitchSimulator.Dialogue
{
    [Serializable]
    [DataContract]
    public sealed class ScenarioDefinitionDto
    {
        [DataMember(Name = "Id", Order = 0)]
        public string Id;
        [DataMember(Name = "Version", Order = 1)]
        public int Version;
        [DataMember(Name = "TitleKey", Order = 2)]
        public string TitleKey;
        [DataMember(Name = "BriefingKey", Order = 3)]
        public string BriefingKey;
        [DataMember(Name = "LearningObjectiveKeys", Order = 4)]
        public string[] LearningObjectiveKeys = Array.Empty<string>();
        [DataMember(Name = "EstimatedDurationMinutes", Order = 5)]
        public int EstimatedDurationMinutes;
        [DataMember(Name = "Project", Order = 6)]
        public ProjectDefinitionDto Project;
        [DataMember(Name = "Judge", Order = 7)]
        public JudgeDefinitionDto Judge;
        [DataMember(Name = "Difficulty", Order = 8)]
        public string Difficulty;
        [DataMember(Name = "InitialConfidence", Order = 9)]
        public int InitialConfidence;
        [DataMember(Name = "OpeningNodeId", Order = 10)]
        public string OpeningNodeId;
        [DataMember(Name = "Nodes", Order = 11)]
        public DialogueNodeDto[] Nodes = Array.Empty<DialogueNodeDto>();
        [DataMember(Name = "SupportedLocales", Order = 12)]
        public string[] SupportedLocales = Array.Empty<string>();
        [DataMember(Name = "ContentChecksum", Order = 13)]
        public string ContentChecksum;
    }

    [Serializable]
    [DataContract]
    public sealed class ProjectDefinitionDto
    {
        [DataMember(Name = "Id", Order = 0)]
        public string Id;
        [DataMember(Name = "NameKey", Order = 1)]
        public string NameKey;
        [DataMember(Name = "DescriptionKey", Order = 2)]
        public string DescriptionKey;
    }

    [Serializable]
    [DataContract]
    public sealed class JudgeDefinitionDto
    {
        [DataMember(Name = "Id", Order = 0)]
        public string Id;
        [DataMember(Name = "NameKey", Order = 1)]
        public string NameKey;
        [DataMember(Name = "RoleKey", Order = 2)]
        public string RoleKey;
        [DataMember(Name = "PortraitId", Order = 3)]
        public string PortraitId;
    }

    [Serializable]
    [DataContract]
    public sealed class DialogueNodeDto
    {
        [DataMember(Name = "Id", Order = 0)]
        public string Id;
        [DataMember(Name = "NodeType", Order = 1)]
        public string NodeType;
        [DataMember(Name = "Speaker", Order = 2)]
        public string Speaker;
        [DataMember(Name = "TextKey", Order = 3)]
        public string TextKey;
        [DataMember(Name = "TimerSeconds", Order = 4)]
        public int TimerSeconds;
        [DataMember(Name = "Responses", Order = 5)]
        public ResponseOptionDto[] Responses = Array.Empty<ResponseOptionDto>();
        [DataMember(Name = "RequiredFlags", Order = 6)]
        public string[] RequiredFlags = Array.Empty<string>();
        [DataMember(Name = "BlockedFlags", Order = 7)]
        public string[] BlockedFlags = Array.Empty<string>();
        [DataMember(Name = "HasConfidenceRange", Order = 8)]
        public bool HasConfidenceRange;
        [DataMember(Name = "MinimumConfidence", Order = 9)]
        public int MinimumConfidence;
        [DataMember(Name = "MaximumConfidence", Order = 10)]
        public int MaximumConfidence = 100;
    }

    [Serializable]
    [DataContract]
    public sealed class ResponseOptionDto
    {
        [DataMember(Name = "Id", Order = 0)]
        public string Id;
        [DataMember(Name = "TextKey", Order = 1)]
        public string TextKey;
        [DataMember(Name = "QualityTier", Order = 2)]
        public string QualityTier;
        [DataMember(Name = "ScoreDelta", Order = 3)]
        public ResponseScoreDeltaDto ScoreDelta = new ResponseScoreDeltaDto();
        [DataMember(Name = "ConfidenceDelta", Order = 4)]
        public int ConfidenceDelta;
        [DataMember(Name = "CompetencyTags", Order = 5)]
        public string[] CompetencyTags = Array.Empty<string>();
        [DataMember(Name = "ReactionCue", Order = 6)]
        public string ReactionCue;
        [DataMember(Name = "FeedbackKey", Order = 7)]
        public string FeedbackKey;
        [DataMember(Name = "ExplanationKey", Order = 8)]
        public string ExplanationKey;
        [DataMember(Name = "NextNodeId", Order = 9)]
        public string NextNodeId;
        [DataMember(Name = "SetFlags", Order = 10)]
        public string[] SetFlags = Array.Empty<string>();
        [DataMember(Name = "RequiredFlags", Order = 11)]
        public string[] RequiredFlags = Array.Empty<string>();
        [DataMember(Name = "BlockedFlags", Order = 12)]
        public string[] BlockedFlags = Array.Empty<string>();
        [DataMember(Name = "HasConfidenceRange", Order = 13)]
        public bool HasConfidenceRange;
        [DataMember(Name = "MinimumConfidence", Order = 14)]
        public int MinimumConfidence;
        [DataMember(Name = "MaximumConfidence", Order = 15)]
        public int MaximumConfidence = 100;
    }

    [Serializable]
    [DataContract]
    public sealed class ResponseScoreDeltaDto
    {
        [DataMember(Name = "ClearExplanation", Order = 0)]
        public int ClearExplanation;
        [DataMember(Name = "Problem", Order = 1)]
        public int Problem;
        [DataMember(Name = "Solution", Order = 2)]
        public int Solution;
        [DataMember(Name = "Audience", Order = 3)]
        public int Audience;
        [DataMember(Name = "Evidence", Order = 4)]
        public int Evidence;
        [DataMember(Name = "Communication", Order = 5)]
        public int Communication;
        [DataMember(Name = "TimeManagement", Order = 6)]
        public int TimeManagement;
    }
}
