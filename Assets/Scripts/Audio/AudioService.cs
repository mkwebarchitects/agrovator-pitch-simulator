using System;
using System.Collections.Generic;
using UnityEngine;

namespace Agrovator.PitchSimulator.Audio
{
    public interface IAudioPlaybackChannel
    {
        float Volume { get; set; }
        bool Muted { get; set; }
        void Play(AudioClip clip, bool loop);
    }

    public interface IAudioDiagnostics
    {
        void Warn(string message);
    }

    public sealed class AudioService
    {
        private readonly IAudioPlaybackChannel musicChannel;
        private readonly IAudioPlaybackChannel sfxChannel;
        private readonly Dictionary<AudioCue, AudioClip> clips = new Dictionary<AudioCue, AudioClip>();
        private readonly HashSet<int> warnedCues = new HashSet<int>();
        private readonly IAudioDiagnostics diagnostics;
        private readonly bool diagnosticsEnabled;
        private float musicVolume = 1f;
        private float sfxVolume = 1f;

        public AudioService(
            IAudioPlaybackChannel musicChannel,
            IAudioPlaybackChannel sfxChannel,
            IEnumerable<AudioCueBinding> bindings,
            IAudioDiagnostics diagnostics,
            bool diagnosticsEnabled)
        {
            this.musicChannel = musicChannel ?? throw new ArgumentNullException(nameof(musicChannel));
            this.sfxChannel = sfxChannel ?? throw new ArgumentNullException(nameof(sfxChannel));
            this.diagnostics = diagnostics;
            this.diagnosticsEnabled = diagnosticsEnabled;
            if (bindings != null)
            {
                foreach (var binding in bindings)
                {
                    if (binding != null && Enum.IsDefined(typeof(AudioCue), binding.Cue))
                    {
                        clips[binding.Cue] = binding.Clip;
                    }
                }
            }
            ApplyVolumes();
        }

        public bool IsUnlocked { get; private set; }
        public bool IsMuted { get; private set; }
        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;

        public void UnlockAfterUserGesture()
        {
            IsUnlocked = true;
        }

        public bool Play(AudioCue cue)
        {
            if (!IsUnlocked)
            {
                return false;
            }
            if (!Enum.IsDefined(typeof(AudioCue), cue))
            {
                WarnOnce((int)cue, $"Unknown audio cue value '{(int)cue}'.");
                return false;
            }
            if (!clips.TryGetValue(cue, out var clip) || clip == null)
            {
                WarnOnce((int)cue, $"Audio cue '{cue}' has no clip assigned.");
                return false;
            }

            var music = cue == AudioCue.MusicLoop;
            (music ? musicChannel : sfxChannel).Play(clip, music);
            return true;
        }

        public void SetMuted(bool muted)
        {
            IsMuted = muted;
            musicChannel.Muted = muted;
            sfxChannel.Muted = muted;
        }

        public void SetMusicVolume(float value)
        {
            musicVolume = NormalizeVolume(value);
            musicChannel.Volume = musicVolume;
        }

        public void SetSfxVolume(float value)
        {
            sfxVolume = NormalizeVolume(value);
            sfxChannel.Volume = sfxVolume;
        }

        private void ApplyVolumes()
        {
            musicChannel.Volume = musicVolume;
            sfxChannel.Volume = sfxVolume;
            musicChannel.Muted = IsMuted;
            sfxChannel.Muted = IsMuted;
        }

        private static float NormalizeVolume(float value)
        {
            if (float.IsNaN(value) || float.IsNegativeInfinity(value)) return 0f;
            if (float.IsPositiveInfinity(value)) return 1f;
            return Mathf.Clamp01(value);
        }

        private void WarnOnce(int cueKey, string message)
        {
            if (diagnosticsEnabled && diagnostics != null && warnedCues.Add(cueKey))
            {
                diagnostics.Warn(message);
            }
        }
    }

    public sealed class UnityAudioSourceChannel : IAudioPlaybackChannel
    {
        private readonly AudioSource source;

        public UnityAudioSourceChannel(AudioSource source)
        {
            this.source = source != null ? source : throw new ArgumentNullException(nameof(source));
            this.source.playOnAwake = false;
            this.source.spatialBlend = 0f;
        }

        public float Volume
        {
            get => source.volume;
            set => source.volume = value;
        }

        public bool Muted
        {
            get => source.mute;
            set => source.mute = value;
        }

        public void Play(AudioClip clip, bool loop)
        {
            if (loop)
            {
                source.loop = true;
                source.clip = clip;
                source.Play();
            }
            else
            {
                source.loop = false;
                source.PlayOneShot(clip);
            }
        }
    }

    public sealed class SilentAudioPlaybackChannel : IAudioPlaybackChannel
    {
        public float Volume { get; set; }
        public bool Muted { get; set; }
        public void Play(AudioClip clip, bool loop) { }
    }

    public sealed class UnityAudioDiagnostics : IAudioDiagnostics
    {
        private readonly UnityEngine.Object context;

        public UnityAudioDiagnostics(UnityEngine.Object context)
        {
            this.context = context;
        }

        public void Warn(string message)
        {
            Debug.LogWarning(message, context);
        }
    }
}
