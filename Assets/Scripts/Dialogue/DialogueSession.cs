using System;
using System.Collections.Generic;

namespace Agrovator.PitchSimulator.Dialogue
{
    public sealed class DialogueSession
    {
        private readonly RuntimeScenario _scenario;
        private readonly HashSet<string> _flags = new HashSet<string>(StringComparer.Ordinal);

        public DialogueSession(RuntimeScenario scenario)
        {
            _scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
            CurrentNode = scenario.OpeningNode;
        }

        public RuntimeDialogueNode CurrentNode { get; private set; }

        public string CurrentNodeId => CurrentNode.Id;

        public bool IsComplete => CurrentNode.IsTerminal;

        public bool HasFlag(string flag)
        {
            return flag != null && _flags.Contains(flag);
        }

        public IReadOnlyList<RuntimeResponseOption> GetAvailableResponses(int confidence)
        {
            var responses = new List<RuntimeResponseOption>();
            foreach (var response in CurrentNode.Responses)
            {
                if (CanSelect(response, confidence))
                {
                    responses.Add(response);
                }
            }

            return responses.AsReadOnly();
        }

        public DialogueSelectionResult Select(string responseId, int confidence)
        {
            RuntimeResponseOption selectedResponse = null;
            foreach (var response in CurrentNode.Responses)
            {
                if (string.Equals(response.Id, responseId, StringComparison.Ordinal))
                {
                    selectedResponse = response;
                    break;
                }
            }

            if (!CanSelect(selectedResponse, confidence))
            {
                return DialogueSelectionResult.Rejected();
            }

            foreach (var flag in selectedResponse.SetFlags)
            {
                _flags.Add(flag);
            }

            CurrentNode = _scenario.Nodes[selectedResponse.NextNodeId];
            return DialogueSelectionResult.Accepted(selectedResponse, CurrentNode);
        }

        private bool CanSelect(RuntimeResponseOption response, int confidence)
        {
            if (!ResponseAvailability.IsAvailable(response, _flags, confidence))
            {
                return false;
            }

            var projectedFlags = new HashSet<string>(_flags, StringComparer.Ordinal);
            foreach (var flag in response.SetFlags)
            {
                projectedFlags.Add(flag);
            }

            return _scenario.Nodes.TryGetValue(response.NextNodeId, out var destination)
                && ResponseAvailability.IsAvailable(destination, projectedFlags, confidence);
        }
    }

    public sealed class DialogueSelectionResult
    {
        private DialogueSelectionResult(
            bool isAccepted,
            RuntimeResponseOption selectedResponse,
            RuntimeDialogueNode newNode)
        {
            IsAccepted = isAccepted;
            SelectedResponse = selectedResponse;
            NewNode = newNode;
        }

        public bool IsAccepted { get; }

        public RuntimeResponseOption SelectedResponse { get; }

        public RuntimeDialogueNode NewNode { get; }

        internal static DialogueSelectionResult Accepted(
            RuntimeResponseOption selectedResponse,
            RuntimeDialogueNode newNode)
        {
            return new DialogueSelectionResult(true, selectedResponse, newNode);
        }

        internal static DialogueSelectionResult Rejected()
        {
            return new DialogueSelectionResult(false, null, null);
        }
    }
}
