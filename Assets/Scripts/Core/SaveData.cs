using System.Runtime.Serialization;

namespace Agrovator.PitchSimulator.Core
{
    [DataContract]
    public sealed class SaveData
    {
        [DataMember(Name = "version", Order = 0)]
        public int Version;

        [DataMember(Name = "timerMode", Order = 1)]
        public int TimerMode;

        [DataMember(Name = "reducedMotion", Order = 2)]
        public bool ReducedMotion;

        [DataMember(Name = "musicVolume", Order = 3)]
        public float MusicVolume;

        [DataMember(Name = "sfxVolume", Order = 4)]
        public float SfxVolume;

        [DataMember(Name = "locale", Order = 5)]
        public string Locale;
    }
}
