using System.Collections.Generic;

namespace Agrovator.PitchSimulator.Dialogue
{
    public static class ResponseAvailability
    {
        public static bool IsAvailable(
            RuntimeResponseOption response,
            ISet<string> flags,
            int confidence)
        {
            return response != null
                && IsAvailable(
                    response.RequiredFlags,
                    response.BlockedFlags,
                    response.HasConfidenceRange,
                    response.MinimumConfidence,
                    response.MaximumConfidence,
                    flags,
                    confidence);
        }

        public static bool IsAvailable(
            RuntimeDialogueNode node,
            ISet<string> flags,
            int confidence)
        {
            return node != null
                && IsAvailable(
                    node.RequiredFlags,
                    node.BlockedFlags,
                    node.HasConfidenceRange,
                    node.MinimumConfidence,
                    node.MaximumConfidence,
                    flags,
                    confidence);
        }

        private static bool IsAvailable(
            IReadOnlyList<string> requiredFlags,
            IReadOnlyList<string> blockedFlags,
            bool hasConfidenceRange,
            int minimumConfidence,
            int maximumConfidence,
            ISet<string> flags,
            int confidence)
        {
            if (flags == null)
            {
                return false;
            }

            if (hasConfidenceRange && (confidence < minimumConfidence || confidence > maximumConfidence))
            {
                return false;
            }

            foreach (var requiredFlag in requiredFlags)
            {
                if (!flags.Contains(requiredFlag))
                {
                    return false;
                }
            }

            foreach (var blockedFlag in blockedFlags)
            {
                if (flags.Contains(blockedFlag))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
