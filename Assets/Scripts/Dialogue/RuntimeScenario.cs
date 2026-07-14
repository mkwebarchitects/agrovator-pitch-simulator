using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Agrovator.PitchSimulator.Dialogue
{
    public sealed class RuntimeScenario
    {
        private RuntimeScenario(ScenarioDefinitionDto source)
        {
            Id = source.Id;
            Version = source.Version;
            TitleKey = source.TitleKey;
            BriefingKey = source.BriefingKey;
            LearningObjectiveKeys = CopyStrings(source.LearningObjectiveKeys);
            EstimatedDurationMinutes = source.EstimatedDurationMinutes;
            Project = source.Project == null ? null : new RuntimeProjectDefinition(source.Project);
            Judge = source.Judge == null ? null : new RuntimeJudgeDefinition(source.Judge);
            Difficulty = source.Difficulty;
            InitialConfidence = source.InitialConfidence;
            OpeningNodeId = source.OpeningNodeId;
            SupportedLocales = CopyStrings(source.SupportedLocales);
            ContentChecksum = source.ContentChecksum;

            var nodes = new Dictionary<string, RuntimeDialogueNode>(StringComparer.Ordinal);
            foreach (var node in source.Nodes ?? Array.Empty<DialogueNodeDto>())
            {
                if (node == null || string.IsNullOrEmpty(node.Id))
                {
                    throw new InvalidOperationException("Runtime scenarios require every node to have an ID.");
                }

                if (nodes.ContainsKey(node.Id))
                {
                    throw new InvalidOperationException($"Runtime scenario contains duplicate node ID '{node.Id}'.");
                }

                nodes.Add(node.Id, new RuntimeDialogueNode(node));
            }

            if (string.IsNullOrEmpty(OpeningNodeId) || !nodes.TryGetValue(OpeningNodeId, out var openingNode))
            {
                throw new InvalidOperationException("Runtime scenario opening node does not exist.");
            }

            foreach (var node in nodes.Values)
            {
                foreach (var response in node.Responses)
                {
                    if (string.IsNullOrEmpty(response.NextNodeId) || !nodes.ContainsKey(response.NextNodeId))
                    {
                        throw new InvalidOperationException(
                            $"Response '{response.Id}' points to missing node '{response.NextNodeId}'.");
                    }
                }
            }

            Nodes = new ReadOnlyDictionary<string, RuntimeDialogueNode>(nodes);
            OpeningNode = openingNode;
        }

        public string Id { get; }

        public int Version { get; }

        public string TitleKey { get; }

        public string BriefingKey { get; }

        public IReadOnlyList<string> LearningObjectiveKeys { get; }

        public int EstimatedDurationMinutes { get; }

        public RuntimeProjectDefinition Project { get; }

        public RuntimeJudgeDefinition Judge { get; }

        public string Difficulty { get; }

        public int InitialConfidence { get; }

        public string OpeningNodeId { get; }

        public RuntimeDialogueNode OpeningNode { get; }

        public IReadOnlyDictionary<string, RuntimeDialogueNode> Nodes { get; }

        public IReadOnlyList<string> SupportedLocales { get; }

        public string ContentChecksum { get; }

        public static RuntimeScenario Compile(ScenarioDefinitionDto scenario)
        {
            if (scenario == null)
            {
                throw new ArgumentNullException(nameof(scenario));
            }

            return new RuntimeScenario(scenario);
        }

        internal static IReadOnlyList<string> CopyStrings(string[] values)
        {
            values = values ?? Array.Empty<string>();
            var copy = new string[values.Length];
            Array.Copy(values, copy, values.Length);
            return Array.AsReadOnly(copy);
        }
    }

    public sealed class RuntimeProjectDefinition
    {
        internal RuntimeProjectDefinition(ProjectDefinitionDto source)
        {
            Id = source.Id;
            NameKey = source.NameKey;
            DescriptionKey = source.DescriptionKey;
        }

        public string Id { get; }

        public string NameKey { get; }

        public string DescriptionKey { get; }
    }

    public sealed class RuntimeJudgeDefinition
    {
        internal RuntimeJudgeDefinition(JudgeDefinitionDto source)
        {
            Id = source.Id;
            NameKey = source.NameKey;
            RoleKey = source.RoleKey;
            PortraitId = source.PortraitId;
        }

        public string Id { get; }

        public string NameKey { get; }

        public string RoleKey { get; }

        public string PortraitId { get; }
    }

    public sealed class RuntimeDialogueNode
    {
        internal RuntimeDialogueNode(DialogueNodeDto source)
        {
            Id = source.Id;
            NodeType = source.NodeType;
            Speaker = source.Speaker;
            TextKey = source.TextKey;
            TimerSeconds = source.TimerSeconds;
            RequiredFlags = RuntimeScenario.CopyStrings(source.RequiredFlags);
            BlockedFlags = RuntimeScenario.CopyStrings(source.BlockedFlags);
            HasConfidenceRange = source.HasConfidenceRange;
            MinimumConfidence = source.MinimumConfidence;
            MaximumConfidence = source.MaximumConfidence;

            var sourceResponses = source.Responses ?? Array.Empty<ResponseOptionDto>();
            var responses = new RuntimeResponseOption[sourceResponses.Length];
            for (var index = 0; index < sourceResponses.Length; index++)
            {
                if (sourceResponses[index] == null)
                {
                    throw new InvalidOperationException($"Node '{source.Id}' contains a null response.");
                }

                responses[index] = new RuntimeResponseOption(sourceResponses[index]);
            }

            Responses = Array.AsReadOnly(responses);
        }

        public string Id { get; }

        public string NodeType { get; }

        public string Speaker { get; }

        public string TextKey { get; }

        public int TimerSeconds { get; }

        public IReadOnlyList<RuntimeResponseOption> Responses { get; }

        public IReadOnlyList<string> RequiredFlags { get; }

        public IReadOnlyList<string> BlockedFlags { get; }

        public bool HasConfidenceRange { get; }

        public int MinimumConfidence { get; }

        public int MaximumConfidence { get; }

        public bool IsTerminal => string.Equals(NodeType, "Terminal", StringComparison.Ordinal);
    }

    public sealed class RuntimeResponseOption
    {
        internal RuntimeResponseOption(ResponseOptionDto source)
        {
            Id = source.Id;
            TextKey = source.TextKey;
            QualityTier = source.QualityTier;
            ScoreDelta = new RuntimeResponseScoreDelta(source.ScoreDelta);
            ConfidenceDelta = source.ConfidenceDelta;
            CompetencyTags = RuntimeScenario.CopyStrings(source.CompetencyTags);
            ReactionCue = source.ReactionCue;
            FeedbackKey = source.FeedbackKey;
            ExplanationKey = source.ExplanationKey;
            NextNodeId = source.NextNodeId;
            SetFlags = RuntimeScenario.CopyStrings(source.SetFlags);
            RequiredFlags = RuntimeScenario.CopyStrings(source.RequiredFlags);
            BlockedFlags = RuntimeScenario.CopyStrings(source.BlockedFlags);
            HasConfidenceRange = source.HasConfidenceRange;
            MinimumConfidence = source.MinimumConfidence;
            MaximumConfidence = source.MaximumConfidence;
        }

        public string Id { get; }

        public string TextKey { get; }

        public string QualityTier { get; }

        public RuntimeResponseScoreDelta ScoreDelta { get; }

        public int ConfidenceDelta { get; }

        public IReadOnlyList<string> CompetencyTags { get; }

        public string ReactionCue { get; }

        public string FeedbackKey { get; }

        public string ExplanationKey { get; }

        public string NextNodeId { get; }

        public IReadOnlyList<string> SetFlags { get; }

        public IReadOnlyList<string> RequiredFlags { get; }

        public IReadOnlyList<string> BlockedFlags { get; }

        public bool HasConfidenceRange { get; }

        public int MinimumConfidence { get; }

        public int MaximumConfidence { get; }
    }

    public sealed class RuntimeResponseScoreDelta
    {
        internal RuntimeResponseScoreDelta(ResponseScoreDeltaDto source)
        {
            source = source ?? new ResponseScoreDeltaDto();
            ClearExplanation = source.ClearExplanation;
            Problem = source.Problem;
            Solution = source.Solution;
            Audience = source.Audience;
            Evidence = source.Evidence;
            Communication = source.Communication;
            TimeManagement = source.TimeManagement;
        }

        public int ClearExplanation { get; }

        public int Problem { get; }

        public int Solution { get; }

        public int Audience { get; }

        public int Evidence { get; }

        public int Communication { get; }

        public int TimeManagement { get; }
    }
}
