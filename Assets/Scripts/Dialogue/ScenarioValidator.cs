using System;
using System.Collections.Generic;

namespace Agrovator.PitchSimulator.Dialogue
{
    public static class ScenarioValidator
    {
        private const string DestinationMissing = "dialogue.destination_missing";
        private const string LocalizationKeyMissing = "dialogue.localization_key_missing";
        private const string NodeIdDuplicate = "dialogue.node_id_duplicate";
        private const string NodeUnreachable = "dialogue.node_unreachable";
        private const string OpeningNodeMissing = "dialogue.opening_node_missing";
        private const string ResponseIdDuplicate = "dialogue.response_id_duplicate";
        private const string ScoreDeltaOutOfRange = "dialogue.score_delta_out_of_range";
        private const string TimerInvalid = "dialogue.timer_invalid";
        private const string ConfidenceDeltaOutOfRange = "dialogue.confidence_delta_out_of_range";

        public static IReadOnlyList<ValidationIssue> Validate(
            ScenarioDefinitionDto scenario,
            IEnumerable<string> localizationKeys)
        {
            var issues = new List<ValidationIssue>();
            if (scenario == null)
            {
                issues.Add(Error("dialogue.scenario_missing", "$"));
                return issues;
            }

            var keys = new HashSet<string>(localizationKeys ?? Array.Empty<string>(), StringComparer.Ordinal);
            ValidateLocalizationKey(scenario.TitleKey, "TitleKey", keys, issues);
            ValidateLocalizationKey(scenario.BriefingKey, "BriefingKey", keys, issues);
            ValidateLocalizationKeys(scenario.LearningObjectiveKeys, "LearningObjectiveKeys", keys, issues);
            ValidateProject(scenario.Project, keys, issues);
            ValidateJudge(scenario.Judge, keys, issues);

            var nodes = scenario.Nodes ?? Array.Empty<DialogueNodeDto>();
            var nodeById = BuildNodeLookup(nodes);
            var openingExists = !string.IsNullOrEmpty(scenario.OpeningNodeId)
                && nodeById.ContainsKey(scenario.OpeningNodeId);
            if (!openingExists)
            {
                issues.Add(Error(OpeningNodeMissing, "OpeningNodeId"));
            }

            var reachableNodeIds = openingExists
                ? FindReachableNodeIds(scenario.OpeningNodeId, nodeById)
                : new HashSet<string>(StringComparer.Ordinal);
            var seenNodeIds = new HashSet<string>(StringComparer.Ordinal);

            for (var nodeIndex = 0; nodeIndex < nodes.Length; nodeIndex++)
            {
                var node = nodes[nodeIndex];
                if (node == null)
                {
                    continue;
                }

                var nodePath = $"Nodes[{nodeIndex}]";
                if (!seenNodeIds.Add(node.Id))
                {
                    issues.Add(Error(NodeIdDuplicate, $"{nodePath}.Id"));
                }

                if (openingExists && !reachableNodeIds.Contains(node.Id))
                {
                    issues.Add(Error(NodeUnreachable, $"{nodePath}.Id"));
                }

                ValidateLocalizationKey(node.TextKey, $"{nodePath}.TextKey", keys, issues);
                if (node.TimerSeconds < 0)
                {
                    issues.Add(Error(TimerInvalid, $"{nodePath}.TimerSeconds"));
                }

                ValidateResponses(node.Responses, nodePath, nodeById, keys, issues);
            }

            return issues;
        }

        private static Dictionary<string, DialogueNodeDto> BuildNodeLookup(DialogueNodeDto[] nodes)
        {
            var nodeById = new Dictionary<string, DialogueNodeDto>(StringComparer.Ordinal);
            foreach (var node in nodes)
            {
                if (node != null && node.Id != null && !nodeById.ContainsKey(node.Id))
                {
                    nodeById.Add(node.Id, node);
                }
            }

            return nodeById;
        }

        private static HashSet<string> FindReachableNodeIds(
            string openingNodeId,
            IReadOnlyDictionary<string, DialogueNodeDto> nodeById)
        {
            var reachable = new HashSet<string>(StringComparer.Ordinal);
            var pending = new Queue<string>();
            pending.Enqueue(openingNodeId);

            while (pending.Count > 0)
            {
                var nodeId = pending.Dequeue();
                if (!reachable.Add(nodeId) || !nodeById.TryGetValue(nodeId, out var node))
                {
                    continue;
                }

                foreach (var response in node.Responses ?? Array.Empty<ResponseOptionDto>())
                {
                    if (response != null
                        && response.NextNodeId != null
                        && nodeById.ContainsKey(response.NextNodeId)
                        && !reachable.Contains(response.NextNodeId))
                    {
                        pending.Enqueue(response.NextNodeId);
                    }
                }
            }

            return reachable;
        }

        private static void ValidateResponses(
            ResponseOptionDto[] responses,
            string nodePath,
            IReadOnlyDictionary<string, DialogueNodeDto> nodeById,
            ISet<string> localizationKeys,
            ICollection<ValidationIssue> issues)
        {
            responses = responses ?? Array.Empty<ResponseOptionDto>();
            var seenResponseIds = new HashSet<string>(StringComparer.Ordinal);
            for (var responseIndex = 0; responseIndex < responses.Length; responseIndex++)
            {
                var response = responses[responseIndex];
                if (response == null)
                {
                    continue;
                }

                var responsePath = $"{nodePath}.Responses[{responseIndex}]";
                if (!seenResponseIds.Add(response.Id))
                {
                    issues.Add(Error(ResponseIdDuplicate, $"{responsePath}.Id"));
                }

                ValidateLocalizationKey(response.TextKey, $"{responsePath}.TextKey", localizationKeys, issues);
                ValidateLocalizationKey(response.FeedbackKey, $"{responsePath}.FeedbackKey", localizationKeys, issues);
                ValidateLocalizationKey(
                    response.ExplanationKey,
                    $"{responsePath}.ExplanationKey",
                    localizationKeys,
                    issues);

                if (string.IsNullOrEmpty(response.NextNodeId) || !nodeById.ContainsKey(response.NextNodeId))
                {
                    issues.Add(Error(DestinationMissing, $"{responsePath}.NextNodeId"));
                }

                if (response.ConfidenceDelta < -100 || response.ConfidenceDelta > 100)
                {
                    issues.Add(Error(ConfidenceDeltaOutOfRange, $"{responsePath}.ConfidenceDelta"));
                }

                ValidateScoreDelta(response.ScoreDelta, $"{responsePath}.ScoreDelta", issues);
            }
        }

        private static void ValidateScoreDelta(
            ResponseScoreDeltaDto delta,
            string scorePath,
            ICollection<ValidationIssue> issues)
        {
            if (delta == null)
            {
                return;
            }

            ValidateScore(delta.ClearExplanation, 20, $"{scorePath}.ClearExplanation", issues);
            ValidateScore(delta.Problem, 15, $"{scorePath}.Problem", issues);
            ValidateScore(delta.Solution, 15, $"{scorePath}.Solution", issues);
            ValidateScore(delta.Audience, 15, $"{scorePath}.Audience", issues);
            ValidateScore(delta.Evidence, 15, $"{scorePath}.Evidence", issues);
            ValidateScore(delta.Communication, 10, $"{scorePath}.Communication", issues);
            ValidateScore(delta.TimeManagement, 10, $"{scorePath}.TimeManagement", issues);
        }

        private static void ValidateScore(
            int value,
            int maximumMagnitude,
            string path,
            ICollection<ValidationIssue> issues)
        {
            if (value < -maximumMagnitude || value > maximumMagnitude)
            {
                issues.Add(Error(ScoreDeltaOutOfRange, path));
            }
        }

        private static void ValidateProject(
            ProjectDefinitionDto project,
            ISet<string> localizationKeys,
            ICollection<ValidationIssue> issues)
        {
            if (project == null)
            {
                return;
            }

            ValidateLocalizationKey(project.NameKey, "Project.NameKey", localizationKeys, issues);
            ValidateLocalizationKey(project.DescriptionKey, "Project.DescriptionKey", localizationKeys, issues);
        }

        private static void ValidateJudge(
            JudgeDefinitionDto judge,
            ISet<string> localizationKeys,
            ICollection<ValidationIssue> issues)
        {
            if (judge == null)
            {
                return;
            }

            ValidateLocalizationKey(judge.NameKey, "Judge.NameKey", localizationKeys, issues);
            ValidateLocalizationKey(judge.RoleKey, "Judge.RoleKey", localizationKeys, issues);
        }

        private static void ValidateLocalizationKeys(
            string[] values,
            string path,
            ISet<string> localizationKeys,
            ICollection<ValidationIssue> issues)
        {
            values = values ?? Array.Empty<string>();
            for (var index = 0; index < values.Length; index++)
            {
                ValidateLocalizationKey(values[index], $"{path}[{index}]", localizationKeys, issues);
            }
        }

        private static void ValidateLocalizationKey(
            string value,
            string path,
            ISet<string> localizationKeys,
            ICollection<ValidationIssue> issues)
        {
            if (string.IsNullOrEmpty(value) || !localizationKeys.Contains(value))
            {
                issues.Add(Error(LocalizationKeyMissing, path));
            }
        }

        private static ValidationIssue Error(string code, string path)
        {
            return new ValidationIssue(code, path, ValidationSeverity.Error);
        }
    }
}
