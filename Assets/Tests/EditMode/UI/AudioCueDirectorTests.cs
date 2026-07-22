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
        public void EnsureUnlockedAndMusicStarted_StartsMusicOnceWithoutAnyButtonPress()
        {
            var played = new List<AudioCue>();
            var director = new AudioCueDirector(played.Add);

            director.EnsureUnlockedAndMusicStarted();
            director.EnsureUnlockedAndMusicStarted();

            Assert.That(played, Is.EqualTo(new[] { AudioCue.MusicLoop }),
                "A background interaction that only unlocks audio must start music once and never " +
                "play a click cue - it did not come from pressing an actual button.");
        }

        [Test]
        public void EnsureUnlockedAndMusicStarted_ThenHandleUserGesture_MusicStaysSingleButClickStillPlays()
        {
            var played = new List<AudioCue>();
            var director = new AudioCueDirector(played.Add);

            director.EnsureUnlockedAndMusicStarted();
            director.HandleUserGesture();

            Assert.That(played, Is.EqualTo(new[] { AudioCue.MusicLoop, AudioCue.ButtonPress }),
                "Music must not restart when the learner goes on to click Start or Settings, but that " +
                "real button press must still play its own click cue.");
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
                AudioCue.JudgeReactionImpressed,
                AudioCue.FeedbackOpen,
                AudioCue.ResultsReveal,
                AudioCue.CompletionSuccess,
                AudioCue.CompletionFailure,
            }));
            Assert.That(played, Has.None.EqualTo(AudioCue.TimerWarning),
                "The guided session path must never play the timer warning cue.");
        }

        [Test]
        public void ResponseSelected_PlaysTheJudgeReactionCueMatchingTheAuthoredPortrait()
        {
            var played = new List<AudioCue>();
            var director = new AudioCueDirector(played.Add);

            director.HandleGuidedSessionEvent(new GuidedPitchSessionEvent(
                GuidedPitchSessionEventType.ResponseSelected, "clear", "Impressed"));
            director.HandleGuidedSessionEvent(new GuidedPitchSessionEvent(
                GuidedPitchSessionEventType.ResponseSelected, "developing", "Curious"));
            director.HandleGuidedSessionEvent(new GuidedPitchSessionEvent(
                GuidedPitchSessionEventType.ResponseSelected, "needs-practice", "Concerned"));
            director.HandleGuidedSessionEvent(new GuidedPitchSessionEvent(
                GuidedPitchSessionEventType.ResponseSelected, "unknown", "not-authored"));

            Assert.That(played, Is.EqualTo(new[]
            {
                AudioCue.ResponseSelected, AudioCue.JudgeReactionImpressed,
                AudioCue.ResponseSelected, AudioCue.JudgeReactionInterested,
                AudioCue.ResponseSelected, AudioCue.JudgeReactionConcerned,
                AudioCue.ResponseSelected, AudioCue.JudgeReactionImpressed,
            }), "Each portrait reaction must play its own distinct cue, resolved through the same " +
                "typed mapping the portrait uses, so an unparseable cue falls back the same way the " +
                "portrait does (to Encouraging/Impressed) rather than silently playing nothing.");
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
