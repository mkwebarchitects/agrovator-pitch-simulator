using System;

namespace Agrovator.PitchSimulator.Dialogue
{
    [Serializable]
    public sealed class ScenarioDefinitionDto
    {
        public string Id;
        public int Version;
        public string TitleKey;
        public string BriefingKey;
        public string[] LearningObjectiveKeys = Array.Empty<string>();
        public int EstimatedDurationMinutes;
        public ProjectDefinitionDto Project;
        public JudgeDefinitionDto Judge;
        public string Difficulty;
        public int InitialConfidence;
        public string OpeningNodeId;
        public DialogueNodeDto[] Nodes = Array.Empty<DialogueNodeDto>();
        public string[] SupportedLocales = Array.Empty<string>();
        public string ContentChecksum;
    }

    [Serializable]
    public sealed class ProjectDefinitionDto
    {
        public string Id;
        public string NameKey;
        public string DescriptionKey;
    }

    [Serializable]
    public sealed class JudgeDefinitionDto
    {
        public string Id;
        public string NameKey;
        public string RoleKey;
        public string PortraitId;
    }

    [Serializable]
    public sealed class DialogueNodeDto
    {
        public string Id;
        public string NodeType;
        public string Speaker;
        public string TextKey;
        public int TimerSeconds;
        public ResponseOptionDto[] Responses = Array.Empty<ResponseOptionDto>();
        public string[] RequiredFlags = Array.Empty<string>();
        public string[] BlockedFlags = Array.Empty<string>();
        public bool HasConfidenceRange;
        public int MinimumConfidence;
        public int MaximumConfidence = 100;
    }

    [Serializable]
    public sealed class ResponseOptionDto
    {
        public string Id;
        public string TextKey;
        public string QualityTier;
        public ResponseScoreDeltaDto ScoreDelta = new ResponseScoreDeltaDto();
        public int ConfidenceDelta;
        public string[] CompetencyTags = Array.Empty<string>();
        public string ReactionCue;
        public string FeedbackKey;
        public string ExplanationKey;
        public string NextNodeId;
        public string[] SetFlags = Array.Empty<string>();
        public string[] RequiredFlags = Array.Empty<string>();
        public string[] BlockedFlags = Array.Empty<string>();
        public bool HasConfidenceRange;
        public int MinimumConfidence;
        public int MaximumConfidence = 100;
    }

    [Serializable]
    public sealed class ResponseScoreDeltaDto
    {
        public int ClearExplanation;
        public int Problem;
        public int Solution;
        public int Audience;
        public int Evidence;
        public int Communication;
        public int TimeManagement;
    }
}
