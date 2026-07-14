using Agrovator.PitchSimulator.Core;
using NUnit.Framework;

namespace Agrovator.PitchSimulator.Tests.EditMode.Core
{
    public sealed class GameStateMachineTests
    {
        [Test]
        public void StartScenario_MovesTitleToBriefing()
        {
            var machine = new GameStateMachine(GameState.Title);

            Assert.That(machine.TryApply(GameCommand.StartScenario), Is.True);
            Assert.That(machine.Current, Is.EqualTo(GameState.Briefing));
        }

        [Test]
        public void SelectResponse_IsRejectedOutsideAwaitingResponse()
        {
            var machine = new GameStateMachine(GameState.Briefing);

            Assert.That(machine.TryApply(GameCommand.SelectResponse), Is.False);
            Assert.That(machine.Current, Is.EqualTo(GameState.Briefing));
        }

        [Test]
        public void ApprovedCommands_MoveThroughCompleteStatePath()
        {
            var machine = new GameStateMachine(GameState.Booting);
            var commands = new[]
            {
                GameCommand.FinishBooting,
                GameCommand.StartScenario,
                GameCommand.Continue,
                GameCommand.Continue,
                GameCommand.Continue,
                GameCommand.Continue,
                GameCommand.SelectResponse,
                GameCommand.Continue,
                GameCommand.FinishScenario,
                GameCommand.SubmitResults,
                GameCommand.SubmissionSucceeded,
            };
            var expectedStates = new[]
            {
                GameState.Title,
                GameState.Briefing,
                GameState.Tutorial,
                GameState.JudgeIntro,
                GameState.AskingQuestion,
                GameState.AwaitingResponse,
                GameState.ShowingReaction,
                GameState.ShowingFeedback,
                GameState.Results,
                GameState.Submitting,
                GameState.Complete,
            };

            for (var index = 0; index < commands.Length; index++)
            {
                Assert.That(machine.TryApply(commands[index]), Is.True);
                Assert.That(machine.Current, Is.EqualTo(expectedStates[index]));
            }
        }

        [Test]
        public void SubmissionFailure_ReturnsSubmittingToResults()
        {
            var machine = new GameStateMachine(GameState.Submitting);

            Assert.That(machine.TryApply(GameCommand.SubmissionFailed), Is.True);
            Assert.That(machine.Current, Is.EqualTo(GameState.Results));
        }

        [Test]
        public void Retry_ReturnsResultsToBriefing()
        {
            var machine = new GameStateMachine(GameState.Results);

            Assert.That(machine.TryApply(GameCommand.Retry), Is.True);
            Assert.That(machine.Current, Is.EqualTo(GameState.Briefing));
        }

        [Test]
        public void Retry_ReturnsCompleteToBriefing()
        {
            var machine = new GameStateMachine(GameState.Complete);

            Assert.That(machine.TryApply(GameCommand.Retry), Is.True);
            Assert.That(machine.Current, Is.EqualTo(GameState.Briefing));
        }
    }
}
