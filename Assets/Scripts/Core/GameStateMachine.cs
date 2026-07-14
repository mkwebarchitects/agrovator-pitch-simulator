using System.Collections.Generic;

namespace Agrovator.PitchSimulator.Core
{
    public sealed class GameStateMachine
    {
        private static readonly IReadOnlyDictionary<(GameState State, GameCommand Command), GameState> Transitions =
            new Dictionary<(GameState State, GameCommand Command), GameState>
            {
                [(GameState.Booting, GameCommand.FinishBooting)] = GameState.Title,
                [(GameState.Title, GameCommand.StartScenario)] = GameState.Briefing,
                [(GameState.Briefing, GameCommand.Continue)] = GameState.Tutorial,
                [(GameState.Tutorial, GameCommand.Continue)] = GameState.JudgeIntro,
                [(GameState.JudgeIntro, GameCommand.Continue)] = GameState.AskingQuestion,
                [(GameState.AskingQuestion, GameCommand.Continue)] = GameState.AwaitingResponse,
                [(GameState.AwaitingResponse, GameCommand.SelectResponse)] = GameState.ShowingReaction,
                [(GameState.ShowingReaction, GameCommand.Continue)] = GameState.ShowingFeedback,
                [(GameState.ShowingFeedback, GameCommand.FinishScenario)] = GameState.Results,
                [(GameState.Results, GameCommand.SubmitResults)] = GameState.Submitting,
                [(GameState.Results, GameCommand.Retry)] = GameState.Briefing,
                [(GameState.Submitting, GameCommand.SubmissionSucceeded)] = GameState.Complete,
                [(GameState.Submitting, GameCommand.SubmissionFailed)] = GameState.Results,
            };

        public GameStateMachine(GameState initialState)
        {
            Current = initialState;
        }

        public GameState Current { get; private set; }

        public bool TryApply(GameCommand command)
        {
            if (!Transitions.TryGetValue((Current, command), out var nextState))
            {
                return false;
            }

            Current = nextState;
            return true;
        }
    }
}
