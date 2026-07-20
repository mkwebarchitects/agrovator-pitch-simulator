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
    }
}
