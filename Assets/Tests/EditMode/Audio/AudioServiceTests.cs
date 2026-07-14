using System;
using System.Collections.Generic;
using System.Linq;
using Agrovator.PitchSimulator.Audio;
using NUnit.Framework;
using UnityEngine;

namespace Agrovator.PitchSimulator.Tests.EditMode.Audio
{
    public sealed class AudioServiceTests
    {
        private readonly List<AudioClip> clips = new List<AudioClip>();

        [Test]
        public void CueEnum_ContainsExactlyTheNineAuthoredHooks()
        {
            Assert.That(Enum.GetNames(typeof(AudioCue)), Is.EqualTo(new[]
            {
                "MusicLoop", "ButtonPress", "ResponseSelected", "TimerWarning",
                "JudgeReaction", "FeedbackOpen", "ResultsReveal", "CompletionSuccess",
                "CompletionFailure",
            }));
        }

        [Test]
        public void Play_BeforeUnlock_IsInertAndDoesNotQueueSurprisePlayback()
        {
            var music = new FakeChannel();
            var sfx = new FakeChannel();
            var service = CreateService(music, sfx, Binding(AudioCue.ButtonPress));

            Assert.That(service.Play(AudioCue.ButtonPress), Is.False);
            Assert.That(music.Plays, Is.Empty);
            Assert.That(sfx.Plays, Is.Empty);

            service.UnlockAfterUserGesture();
            service.UnlockAfterUserGesture();

            Assert.That(service.IsUnlocked, Is.True);
            Assert.That(sfx.Plays, Is.Empty, "Unlock must not replay an earlier request.");
        }

        [Test]
        public void Play_RoutesMusicLoopToMusic_AndEveryOtherCueToSfx()
        {
            var music = new FakeChannel();
            var sfx = new FakeChannel();
            var bindings = Enum.GetValues(typeof(AudioCue)).Cast<AudioCue>().Select(Binding).ToArray();
            var service = CreateService(music, sfx, bindings);
            service.UnlockAfterUserGesture();

            foreach (var cue in Enum.GetValues(typeof(AudioCue)).Cast<AudioCue>())
            {
                Assert.That(service.Play(cue), Is.True, cue.ToString());
            }

            Assert.That(music.Plays, Has.Count.EqualTo(1));
            Assert.That(music.Plays[0].Loop, Is.True);
            Assert.That(sfx.Plays, Has.Count.EqualTo(8));
            Assert.That(sfx.Plays.All(play => !play.Loop), Is.True);
        }

        [Test]
        public void Mute_IsMasterEffectiveMute_WithoutLosingIndependentStoredVolumes()
        {
            var music = new FakeChannel();
            var sfx = new FakeChannel();
            var service = CreateService(music, sfx);
            service.SetMusicVolume(0.25f);
            service.SetSfxVolume(0.75f);

            service.SetMuted(true);

            Assert.That(service.IsMuted, Is.True);
            Assert.That(music.Muted, Is.True);
            Assert.That(sfx.Muted, Is.True);
            Assert.That(service.MusicVolume, Is.EqualTo(0.25f));
            Assert.That(service.SfxVolume, Is.EqualTo(0.75f));

            service.SetMuted(false);
            Assert.That(music.Muted, Is.False);
            Assert.That(sfx.Muted, Is.False);
            Assert.That(music.Volume, Is.EqualTo(0.25f));
            Assert.That(sfx.Volume, Is.EqualTo(0.75f));
        }

        [Test]
        public void Volumes_ClampFiniteValuesAndNormalizeNonFiniteValuesDeterministically()
        {
            var music = new FakeChannel();
            var sfx = new FakeChannel();
            var service = CreateService(music, sfx);

            service.SetMusicVolume(-10f);
            service.SetSfxVolume(10f);
            Assert.That(service.MusicVolume, Is.Zero);
            Assert.That(service.SfxVolume, Is.EqualTo(1f));

            service.SetMusicVolume(float.NaN);
            service.SetSfxVolume(float.PositiveInfinity);
            Assert.That(service.MusicVolume, Is.Zero);
            Assert.That(service.SfxVolume, Is.EqualTo(1f));

            service.SetMusicVolume(float.NegativeInfinity);
            Assert.That(service.MusicVolume, Is.Zero);
        }

        [Test]
        public void MissingNullAndUnknownCues_WarnOnceAndNeverReachPlayback()
        {
            var music = new FakeChannel();
            var sfx = new FakeChannel();
            var diagnostics = new FakeDiagnostics();
            var service = new AudioService(music, sfx,
                new[] { new AudioCueBinding(AudioCue.ButtonPress, null) }, diagnostics, true);
            service.UnlockAfterUserGesture();

            Assert.That(service.Play(AudioCue.ButtonPress), Is.False);
            Assert.That(service.Play(AudioCue.ButtonPress), Is.False);
            Assert.That(service.Play(AudioCue.TimerWarning), Is.False);
            Assert.That(service.Play(AudioCue.TimerWarning), Is.False);
            Assert.That(service.Play((AudioCue)999), Is.False);
            Assert.That(service.Play((AudioCue)999), Is.False);

            Assert.That(diagnostics.Warnings, Has.Count.EqualTo(3));
            Assert.That(music.Plays, Is.Empty);
            Assert.That(sfx.Plays, Is.Empty);
        }

        [Test]
        public void MissingCueWarnings_AreSuppressedWhenDevelopmentDiagnosticsAreDisabled()
        {
            var diagnostics = new FakeDiagnostics();
            var service = new AudioService(new FakeChannel(), new FakeChannel(),
                Array.Empty<AudioCueBinding>(), diagnostics, false);
            service.UnlockAfterUserGesture();

            Assert.That(service.Play(AudioCue.FeedbackOpen), Is.False);
            Assert.That(diagnostics.Warnings, Is.Empty);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var clip in clips)
            {
                UnityEngine.Object.DestroyImmediate(clip);
            }
            clips.Clear();
        }

        private AudioService CreateService(FakeChannel music, FakeChannel sfx,
            params AudioCueBinding[] bindings)
        {
            return new AudioService(music, sfx, bindings, new FakeDiagnostics(), true);
        }

        private AudioCueBinding Binding(AudioCue cue)
        {
            var clip = AudioClip.Create(cue.ToString(), 16, 1, 8000, false);
            clips.Add(clip);
            return new AudioCueBinding(cue, clip);
        }

        private sealed class FakeChannel : IAudioPlaybackChannel
        {
            public readonly List<PlayRecord> Plays = new List<PlayRecord>();
            public float Volume { get; set; }
            public bool Muted { get; set; }

            public void Play(AudioClip clip, bool loop)
            {
                Plays.Add(new PlayRecord(clip, loop));
            }
        }

        private sealed class FakeDiagnostics : IAudioDiagnostics
        {
            public readonly List<string> Warnings = new List<string>();
            public void Warn(string message) => Warnings.Add(message);
        }

        private sealed class PlayRecord
        {
            public PlayRecord(AudioClip clip, bool loop)
            {
                Clip = clip;
                Loop = loop;
            }

            public AudioClip Clip { get; }
            public bool Loop { get; }
        }
    }
}
