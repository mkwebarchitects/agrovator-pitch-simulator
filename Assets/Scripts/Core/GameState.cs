namespace Agrovator.PitchSimulator.Core
{
    public enum GameState
    {
        Booting,
        Title,
        Briefing,
        Tutorial,
        JudgeIntro,
        AskingQuestion,
        AwaitingResponse,
        ShowingReaction,
        ShowingFeedback,
        Results,
        Submitting,
        Complete,
        SafeFallback,
    }
}
