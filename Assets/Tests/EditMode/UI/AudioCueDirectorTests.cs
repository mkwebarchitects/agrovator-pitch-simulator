using System.Collections.Generic;
using Agrovator.PitchSimulator.Audio;
using Agrovator.PitchSimulator.Core;
using Agrovator.PitchSimulator.UI;
using NUnit.Framework;

namespace Agrovator.PitchSimulator.Tests.EditMode.UI
{
    public sealed class AudioCueDirectorTests
    {
        [Test]
        public void UserGesture_StartsMusicOnceAndPlaysEveryHandledButtonPress()
        {
            var played = new List<AudioCue>();
            var director = new AudioCueDirector(played.Add);

            director.HandleUserGesture();
            director.HandleUserGesture();

            Assert.That(played, Is.EqualTo(new[]
            {
                AudioCue.MusicLoop,
                AudioCue.ButtonPress,
                AudioCue.ButtonPress,
            }));
        }

        [Test]
        public void SessionEvents_MapToEveryReservedOutcomeCue()
        {
            var played = new List<AudioCue>();
            var director = new AudioCueDirector(played.Add);

            director.HandleSessionEvent(new PitchSessionEvent(PitchSessionEventType.ReactionReady));
            director.HandleSessionEvent(new PitchSessionEvent(PitchSessionEventType.TimeoutReactionReady));
            director.HandleSessionEvent(new PitchSessionEvent(PitchSessionEventType.FeedbackReady));
            director.HandleSessionEvent(new PitchSessionEvent(PitchSessionEventType.ResultsReady));
            director.HandleSessionEvent(new PitchSessionEvent(PitchSessionEventType.SubmissionSucceeded));
            director.HandleSessionEvent(new PitchSessionEvent(PitchSessionEventType.SubmissionFailed));

            Assert.That(played, Is.EqualTo(new[]
            {
                AudioCue.ResponseSelected,
                AudioCue.JudgeReaction,
                AudioCue.JudgeReaction,
                AudioCue.FeedbackOpen,
                AudioCue.ResultsReveal,
                AudioCue.CompletionSuccess,
                AudioCue.CompletionFailure,
            }));
        }

        [Test]
        public void GuidedSessionEvents_MapToTheReservedOutcomeCues()
        {
            var played = new List<AudioCue>();
            var director = new AudioCueDirector(played.Add);

            director.HandleGuidedSessionEvent(new GuidedPitchSessionEvent(
                GuidedPitchSessionEventType.ResponseSelected, "primary-problem-clear", "Impressed"));
            director.HandleGuidedSessionEvent(new GuidedPitchSessionEvent(
                GuidedPitchSessionEventType.FeedbackReady, messageKey: "guided.feedback.improve"));
            director.HandleGuidedSessionEvent(new GuidedPitchSessionEvent(
                GuidedPitchSessionEventType.ResultsReady));
            director.HandleGuidedSessionEvent(new GuidedPitchSessionEvent(
                GuidedPitchSessionEventType.SubmissionSucceeded));
            director.HandleGuidedSessionEvent(new GuidedPitchSessionEvent(
                GuidedPitchSessionEventType.SubmissionFailed, messageKey: "lms.submission.failed"));

            Assert.That(played, Is.EqualTo(new[]
            {
                AudioCue.ResponseSelected,
                AudioCue.JudgeReaction,
                AudioCue.FeedbackOpen,
                AudioCue.ResultsReveal,
                AudioCue.CompletionSuccess,
                AudioCue.CompletionFailure,
            }));
            Assert.That(played, Has.None.EqualTo(AudioCue.TimerWarning),
                "The guided session path must never play the timer warning cue.");
        }

        [Test]
        public void GuidedSessionEvents_IgnoreNullEventsWithoutPlayingAnyCue()
        {
            var played = new List<AudioCue>();
            var director = new AudioCueDirector(played.Add);

            director.HandleGuidedSessionEvent(null);

            Assert.That(played, Is.Empty);
        }

        [Test]
        public void TimerWarning_PlaysOnceInFinalFiveAndResetsForNextQuestion()
        {
            var played = new List<AudioCue>();
            var director = new AudioCueDirector(played.Add);

            director.UpdateTimer(GameState.AwaitingResponse, 7d, 10d);
            director.UpdateTimer(GameState.AwaitingResponse, 5d, 10d);
            director.UpdateTimer(GameState.AwaitingResponse, 4d, 10d);
            director.UpdateTimer(GameState.AwaitingResponse, 1d, 10d);
            director.UpdateTimer(GameState.ShowingReaction, 1d, 10d);
            director.UpdateTimer(GameState.AskingQuestion, 10d, 10d);
            director.UpdateTimer(GameState.AwaitingResponse, 5d, 10d);

            Assert.That(played, Is.EqualTo(new[]
            {
                AudioCue.TimerWarning,
                AudioCue.TimerWarning,
            }));
        }

        [TestCase(0d, 10d)]
        [TestCase(5d, 0d)]
        [TestCase(-1d, 10d)]
        public void TimerWarning_IgnoresInactiveTimers(double remainingSeconds, double totalSeconds)
        {
            var played = new List<AudioCue>();
            var director = new AudioCueDirector(played.Add);

            director.UpdateTimer(GameState.AwaitingResponse, remainingSeconds, totalSeconds);

            Assert.That(played, Is.Empty);
        }
    }
}
