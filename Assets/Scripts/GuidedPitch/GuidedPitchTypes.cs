using System;
using System.Collections.Generic;

namespace Agrovator.PitchSimulator.GuidedPitch
{
    public enum LearnerMode
    {
        Primary,
        Secondary,
    }

    public enum PitchPart
    {
        Problem,
        Evidence,
        Solution,
        Value,
    }

    public enum MasteryState
    {
        NeedsPractice,
        Developing,
        Clear,
    }

    public enum GuidedPitchPhase
    {
        Booting,
        Title,
        Briefing,
        ModeSelection,
        Learn,
        Build,
        BuildFeedback,
        Improve,
        Present,
        FollowUp,
        FollowUpFeedback,
        Results,
        Submitting,
        Complete,
        SafeFallback,
    }

    public static class PitchParts
    {
        public static IReadOnlyList<PitchPart> Ordered { get; } =
            Array.AsReadOnly(new[]
            {
                PitchPart.Problem,
                PitchPart.Evidence,
                PitchPart.Solution,
                PitchPart.Value,
            });
    }
}
