using System;

namespace Agrovator.PitchSimulator.LMS
{
    [Serializable]
    public sealed class LmsLaunchConfig
    {
        public string PseudonymousLearnerId;
        public string SessionId;
        public string CourseId;
        public string ModuleId;
        public string LessonId;
        public string ScenarioId;
        public string Language;
        public int AttemptNumber;
        public string TimerMode;
        public bool ReducedMotion;
        public float MusicVolume;
        public float SfxVolume;
        public int ContentVersion;
        public string LaunchReference;
    }
}
