using System;
using UnityEngine;

namespace Agrovator.PitchSimulator.LMS
{
    [Serializable]
    public sealed class LmsCompletionPayload
    {
        public string PseudonymousLearnerId;
        public string SessionId;
        public string CourseId;
        public string ModuleId;
        public string LessonId;
        public string ScenarioId;
        public string GameVersion;
        public int ContentVersion;
        public string CompletionStatus;
        public string StartedAtUtc;
        public string CompletedAtUtc;
        public double DurationSeconds;
        public int OverallScore;
        public LmsCompetencyScore[] CompetencyScores = Array.Empty<LmsCompetencyScore>();
        public int FinalConfidence;
        public string[] SelectedResponseIds = Array.Empty<string>();
        public int TimeoutCount;
        public int AttemptNumber;
        public string RecommendedFollowUpLessonId;
    }

    [Serializable]
    public sealed class LmsCompetencyScore
    {
        public string CompetencyId;
        public int Score;
    }

    public static class LmsPayloadJson
    {
        public static string SerializeCompletion(LmsCompletionPayload payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            return JsonUtility.ToJson(payload);
        }

        public static LmsCompletionPayload DeserializeCompletion(string json)
        {
            return Deserialize<LmsCompletionPayload>(json);
        }

        public static string SerializeLaunchConfig(LmsLaunchConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return JsonUtility.ToJson(config);
        }

        public static LmsLaunchConfig DeserializeLaunchConfig(string json)
        {
            return Deserialize<LmsLaunchConfig>(json);
        }

        private static T Deserialize<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON is required.", nameof(json));
            }

            return JsonUtility.FromJson<T>(json);
        }
    }
}
