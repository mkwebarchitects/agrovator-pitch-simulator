using System;
using System.Linq;
using Agrovator.PitchSimulator.LMS;
using NUnit.Framework;

namespace Agrovator.PitchSimulator.Tests.EditMode.LMS
{
    public sealed class LmsPayloadTests
    {
        [Test]
        public void CompletionJson_RoundTripsApprovedFields()
        {
            var source = ValidPayload();

            var json = LmsPayloadJson.SerializeCompletion(source);
            var copy = LmsPayloadJson.DeserializeCompletion(json);

            Assert.That(copy.PseudonymousLearnerId, Is.EqualTo(source.PseudonymousLearnerId));
            Assert.That(copy.SessionId, Is.EqualTo(source.SessionId));
            Assert.That(copy.CourseId, Is.EqualTo(source.CourseId));
            Assert.That(copy.ModuleId, Is.EqualTo(source.ModuleId));
            Assert.That(copy.LessonId, Is.EqualTo(source.LessonId));
            Assert.That(copy.ScenarioId, Is.EqualTo(source.ScenarioId));
            Assert.That(copy.GameVersion, Is.EqualTo(source.GameVersion));
            Assert.That(copy.ContentVersion, Is.EqualTo(source.ContentVersion));
            Assert.That(copy.CompletionStatus, Is.EqualTo(source.CompletionStatus));
            Assert.That(copy.StartedAtUtc, Is.EqualTo(source.StartedAtUtc));
            Assert.That(copy.CompletedAtUtc, Is.EqualTo(source.CompletedAtUtc));
            Assert.That(copy.DurationSeconds, Is.EqualTo(source.DurationSeconds));
            Assert.That(copy.OverallScore, Is.EqualTo(source.OverallScore));
            Assert.That(copy.FinalConfidence, Is.EqualTo(source.FinalConfidence));
            Assert.That(copy.CompetencyScores.Select(value => value.CompetencyId),
                Is.EqualTo(source.CompetencyScores.Select(value => value.CompetencyId)));
            Assert.That(copy.CompetencyScores.Select(value => value.Score),
                Is.EqualTo(source.CompetencyScores.Select(value => value.Score)));
            Assert.That(copy.SelectedResponseIds, Is.EqualTo(source.SelectedResponseIds));
            Assert.That(copy.TimeoutCount, Is.EqualTo(source.TimeoutCount));
            Assert.That(copy.AttemptNumber, Is.EqualTo(source.AttemptNumber));
            Assert.That(copy.RecommendedFollowUpLessonId, Is.EqualTo(source.RecommendedFollowUpLessonId));
        }

        [Test]
        public void LaunchJson_RoundTripsOpaqueReferenceWithoutCredentialFields()
        {
            var source = ValidLaunchConfig();

            var copy = LmsPayloadJson.DeserializeLaunchConfig(
                LmsPayloadJson.SerializeLaunchConfig(source));

            Assert.That(copy.PseudonymousLearnerId, Is.EqualTo(source.PseudonymousLearnerId));
            Assert.That(copy.SessionId, Is.EqualTo(source.SessionId));
            Assert.That(copy.CourseId, Is.EqualTo(source.CourseId));
            Assert.That(copy.ModuleId, Is.EqualTo(source.ModuleId));
            Assert.That(copy.LessonId, Is.EqualTo(source.LessonId));
            Assert.That(copy.ScenarioId, Is.EqualTo(source.ScenarioId));
            Assert.That(copy.Language, Is.EqualTo(source.Language));
            Assert.That(copy.AttemptNumber, Is.EqualTo(source.AttemptNumber));
            Assert.That(copy.TimerMode, Is.EqualTo(source.TimerMode));
            Assert.That(copy.ReducedMotion, Is.EqualTo(source.ReducedMotion));
            Assert.That(copy.MusicVolume, Is.EqualTo(source.MusicVolume));
            Assert.That(copy.SfxVolume, Is.EqualTo(source.SfxVolume));
            Assert.That(copy.ContentVersion, Is.EqualTo(source.ContentVersion));
            Assert.That(copy.LaunchReference, Is.EqualTo(source.LaunchReference));
        }

        [TestCase(double.NaN)]
        [TestCase(double.NegativeInfinity)]
        [TestCase(double.PositiveInfinity)]
        public void SerializeCompletion_RejectsNonFiniteDurationBeforeProducingJson(double durationSeconds)
        {
            var payload = ValidPayload();
            payload.DurationSeconds = durationSeconds;

            Assert.Throws<ArgumentException>(() => LmsPayloadJson.SerializeCompletion(payload));
        }

        [Test]
        public void SerializeCompletion_RejectsAnyInvalidPayloadBeforeProducingJson()
        {
            var payload = ValidPayload();
            payload.ScenarioId = "";

            Assert.Throws<ArgumentException>(() => LmsPayloadJson.SerializeCompletion(payload));
        }

        [TestCase(float.NaN)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(-0.01f)]
        [TestCase(1.01f)]
        public void SerializeLaunchConfig_RejectsInvalidMusicVolumeBeforeProducingJson(float volume)
        {
            var config = ValidLaunchConfig();
            config.MusicVolume = volume;

            Assert.Throws<ArgumentException>(() => LmsPayloadJson.SerializeLaunchConfig(config));
        }

        [TestCase(float.NaN)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(-0.01f)]
        [TestCase(1.01f)]
        public void SerializeLaunchConfig_RejectsInvalidSfxVolumeBeforeProducingJson(float volume)
        {
            var config = ValidLaunchConfig();
            config.SfxVolume = volume;

            Assert.Throws<ArgumentException>(() => LmsPayloadJson.SerializeLaunchConfig(config));
        }

        [Test]
        public void SerializeLaunchConfig_RejectsAnyInvalidConfigBeforeProducingJson()
        {
            var config = ValidLaunchConfig();
            config.SessionId = "";

            Assert.Throws<ArgumentException>(() => LmsPayloadJson.SerializeLaunchConfig(config));
        }

        [Test]
        public void ValidateCompletion_AcceptsValidPayload()
        {
            Assert.That(LmsPayloadValidator.ValidateCompletion(ValidPayload()), Is.Empty);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void ValidateCompletion_RejectsMissingSessionId(string sessionId)
        {
            var payload = ValidPayload();
            payload.SessionId = sessionId;

            AssertIssue(payload, "lms.session.required", "SessionId");
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void ValidateCompletion_RejectsMissingScenarioId(string scenarioId)
        {
            var payload = ValidPayload();
            payload.ScenarioId = scenarioId;

            AssertIssue(payload, "lms.scenario.required", "ScenarioId");
        }

        [Test]
        public void ValidateCompletion_RejectsUnsupportedContentVersion()
        {
            var payload = ValidPayload();
            payload.ContentVersion = LmsPayloadValidator.SupportedContentVersion + 1;

            AssertIssue(payload, "lms.content_version.unsupported", "ContentVersion");
        }

        [TestCase(-1)]
        [TestCase(101)]
        public void ValidateCompletion_RejectsOverallScoreOutsideInclusiveRange(int score)
        {
            var payload = ValidPayload();
            payload.OverallScore = score;

            AssertIssue(payload, "lms.overall_score.range", "OverallScore");
        }

        [TestCase(-1)]
        [TestCase(101)]
        public void ValidateCompletion_RejectsConfidenceOutsideInclusiveRange(int confidence)
        {
            var payload = ValidPayload();
            payload.FinalConfidence = confidence;

            AssertIssue(payload, "lms.final_confidence.range", "FinalConfidence");
        }

        [TestCase(-1)]
        [TestCase(101)]
        public void ValidateCompletion_RejectsCompetencyScoreOutsideInclusiveRange(int score)
        {
            var payload = ValidPayload();
            payload.CompetencyScores[0].Score = score;

            AssertIssue(payload, "lms.competency_score.range", "CompetencyScores[0].Score");
        }

        [Test]
        public void ValidateCompletion_RejectsCompletionBeforeStart()
        {
            var payload = ValidPayload();
            payload.CompletedAtUtc = "2026-07-14T01:59:59Z";

            AssertIssue(payload, "lms.timestamp.order", "CompletedAtUtc");
        }

        [TestCase("2026-07-14T10:00:00+08:00", "StartedAtUtc")]
        [TestCase("not-a-timestamp", "StartedAtUtc")]
        public void ValidateCompletion_RequiresUtcStartTimestamp(string timestamp, string path)
        {
            var payload = ValidPayload();
            payload.StartedAtUtc = timestamp;

            AssertIssue(payload, "lms.timestamp.utc", path);
        }

        [TestCase("2026-07-14T10:05:00+08:00", "CompletedAtUtc")]
        [TestCase("not-a-timestamp", "CompletedAtUtc")]
        public void ValidateCompletion_RequiresUtcCompletionTimestamp(string timestamp, string path)
        {
            var payload = ValidPayload();
            payload.CompletedAtUtc = timestamp;

            AssertIssue(payload, "lms.timestamp.utc", path);
        }

        [TestCase("2024-02-29T23:59:59Z")]
        [TestCase("2026-07-14T02:00:00.1Z")]
        [TestCase("2026-07-14T02:00:00.1234567Z")]
        public void ValidateCompletion_AcceptsCanonicalRfc3339UtcTimestamp(string timestamp)
        {
            var payload = ValidPayload();
            payload.StartedAtUtc = timestamp;
            payload.CompletedAtUtc = timestamp;

            Assert.That(LmsPayloadValidator.ValidateCompletion(payload), Is.Empty);
        }

        [TestCase("2026-07-14")]
        [TestCase("2026-07-14 02:00:00Z")]
        [TestCase("2026-07-14T02:00:00z")]
        [TestCase("14/07/2026T02:00:00Z")]
        [TestCase("2026-07-14T02:00Z")]
        [TestCase("2026-07-14T02:00:00.Z")]
        [TestCase("2026-07-14T02:00:00.12345678Z")]
        [TestCase("2026-07-14T02:00:00+00:00")]
        public void ValidateCompletion_RejectsNonCanonicalUtcTimestamp(string timestamp)
        {
            var payload = ValidPayload();
            payload.StartedAtUtc = timestamp;

            AssertIssue(payload, "lms.timestamp.utc", "StartedAtUtc");
        }

        [TestCase(-1d)]
        [TestCase(double.NaN)]
        [TestCase(double.NegativeInfinity)]
        [TestCase(double.PositiveInfinity)]
        public void ValidateCompletion_RejectsInvalidDuration(double durationSeconds)
        {
            var payload = ValidPayload();
            payload.DurationSeconds = durationSeconds;

            AssertIssue(payload, "lms.duration.invalid", "DurationSeconds");
        }

        [Test]
        public void ValidateCompletion_RejectsNegativeAttemptAndTimeoutCounts()
        {
            var payload = ValidPayload();
            payload.AttemptNumber = -1;
            payload.TimeoutCount = -1;

            var issues = LmsPayloadValidator.ValidateCompletion(payload);

            Assert.That(issues.Any(issue => issue.Code == "lms.attempt.invalid"), Is.True);
            Assert.That(issues.Any(issue => issue.Code == "lms.timeout_count.invalid"), Is.True);
        }

        [Test]
        public void CompletionPayload_ContainsOnlyApprovedPrivacyMinimizedFields()
        {
            var approvedFields = new[]
            {
                "PseudonymousLearnerId", "SessionId", "CourseId", "ModuleId", "LessonId",
                "ScenarioId", "GameVersion", "ContentVersion", "CompletionStatus", "StartedAtUtc",
                "CompletedAtUtc", "DurationSeconds", "OverallScore", "CompetencyScores",
                "FinalConfidence", "SelectedResponseIds", "TimeoutCount", "AttemptNumber",
                "RecommendedFollowUpLessonId",
            };
            var actualFields = typeof(LmsCompletionPayload).GetFields().Select(field => field.Name);

            Assert.That(actualFields, Is.EquivalentTo(approvedFields));
            Assert.That(LmsPayloadValidator.ValidateCompletionPrivacyShape(), Is.Empty);
        }

        [Test]
        public void LaunchConfig_DoesNotExposeRawCredentialFields()
        {
            var fields = typeof(LmsLaunchConfig).GetFields().Select(field => field.Name).ToArray();

            Assert.That(fields, Does.Contain("LaunchReference"));
            Assert.That(fields, Has.None.EqualTo("Token"));
            Assert.That(fields, Has.None.EqualTo("Nonce"));
            Assert.That(fields, Has.None.EqualTo("RawToken"));
            Assert.That(fields, Has.None.EqualTo("RawNonce"));
        }

        [Test]
        public void LaunchConfig_ContainsOnlyApprovedFields()
        {
            var approvedFields = new[]
            {
                "PseudonymousLearnerId", "SessionId", "CourseId", "ModuleId", "LessonId",
                "ScenarioId", "Language", "AttemptNumber", "TimerMode", "ReducedMotion",
                "MusicVolume", "SfxVolume", "ContentVersion", "LaunchReference",
            };
            var actualFields = typeof(LmsLaunchConfig).GetFields().Select(field => field.Name);

            Assert.That(actualFields, Is.EquivalentTo(approvedFields));
        }

        [TestCase(float.NaN, "lms.music_volume.range", "MusicVolume")]
        [TestCase(float.NegativeInfinity, "lms.music_volume.range", "MusicVolume")]
        [TestCase(float.PositiveInfinity, "lms.music_volume.range", "MusicVolume")]
        [TestCase(-0.01f, "lms.music_volume.range", "MusicVolume")]
        [TestCase(1.01f, "lms.music_volume.range", "MusicVolume")]
        public void ValidateLaunch_RejectsInvalidMusicVolume(float volume, string code, string path)
        {
            var config = ValidLaunchConfig();
            config.MusicVolume = volume;

            AssertLaunchIssue(config, code, path);
        }

        [TestCase(float.NaN, "lms.sfx_volume.range", "SfxVolume")]
        [TestCase(float.NegativeInfinity, "lms.sfx_volume.range", "SfxVolume")]
        [TestCase(float.PositiveInfinity, "lms.sfx_volume.range", "SfxVolume")]
        [TestCase(-0.01f, "lms.sfx_volume.range", "SfxVolume")]
        [TestCase(1.01f, "lms.sfx_volume.range", "SfxVolume")]
        public void ValidateLaunch_RejectsInvalidSfxVolume(float volume, string code, string path)
        {
            var config = ValidLaunchConfig();
            config.SfxVolume = volume;

            AssertLaunchIssue(config, code, path);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("lref_short")]
        [TestCase("lref_abcdefghijkl.")]
        [TestCase("lref_abcdef ghijkl")]
        [TestCase("Bearer abcdefghijklmnop")]
        [TestCase("eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.signature")]
        [TestCase("raw-token-abcdefghijklmnop")]
        [TestCase("lref_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ123456789")]
        public void ValidateLaunch_RejectsMissingOrMalformedLaunchReference(string launchReference)
        {
            var config = ValidLaunchConfig();
            config.LaunchReference = launchReference;

            AssertLaunchIssue(config, "lms.launch_reference.invalid", "LaunchReference");
        }

        [TestCase("lref_abcdefghijkl")]
        [TestCase("lref_ABCDEFGHIJKL")]
        [TestCase("lref_abcDEF123_-x")]
        [TestCase("lref_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ12345678")]
        public void ValidateLaunch_AcceptsConstrainedOpaqueLaunchReference(string launchReference)
        {
            var config = ValidLaunchConfig();
            config.LaunchReference = launchReference;

            Assert.That(LmsPayloadValidator.ValidateLaunch(config), Is.Empty);
        }

        [Test]
        public void ValidateLaunch_RejectsMissingIdentifiersUnsupportedVersionAndNegativeAttempt()
        {
            var config = ValidLaunchConfig();
            config.SessionId = "";
            config.ScenarioId = null;
            config.ContentVersion = LmsPayloadValidator.SupportedContentVersion + 1;
            config.AttemptNumber = -1;

            var issues = LmsPayloadValidator.ValidateLaunch(config);

            Assert.That(issues.Select(issue => issue.Code), Is.EquivalentTo(new[]
            {
                "lms.session.required",
                "lms.scenario.required",
                "lms.content_version.unsupported",
                "lms.attempt.invalid",
            }));
        }

        [TestCase("{}")]
        [TestCase("{\"CompetencyScores\":null,\"SelectedResponseIds\":null}")]
        [TestCase("{\"CompetencyScores\":[],\"SelectedResponseIds\":[]}")]
        public void DeserializeCompletion_NormalizesMissingNullAndEmptyArrays(string json)
        {
            var payload = LmsPayloadJson.DeserializeCompletion(json);

            Assert.That(payload.CompetencyScores, Is.Not.Null.And.Empty);
            Assert.That(payload.SelectedResponseIds, Is.Not.Null.And.Empty);
        }

        [Test]
        public void MockBridge_SuccessInvokesOnlySuccessAndPreservesAttempt()
        {
            var bridge = new MockLmsBridge(MockLmsBridgeMode.Success, ValidLaunchConfig());
            var successes = 0;
            LmsSubmissionError failure = null;

            bridge.SubmitCompletion(ValidPayload(), () => successes++, error => failure = error);

            Assert.That(successes, Is.EqualTo(1));
            Assert.That(failure, Is.Null);
            Assert.That(bridge.LastSubmittedAttemptNumber, Is.EqualTo(2));
        }

        [TestCase(MockLmsBridgeMode.Failure, LmsSubmissionErrorCode.SubmissionFailed, "lms.submission.failed")]
        [TestCase(MockLmsBridgeMode.Expired, LmsSubmissionErrorCode.SessionExpired, "lms.session.expired")]
        [TestCase(MockLmsBridgeMode.MissingConfiguration, LmsSubmissionErrorCode.MissingConfiguration, "lms.configuration.missing")]
        public void MockBridge_ErrorModesReturnSanitizedTypedErrorAndPreserveAttempt(
            MockLmsBridgeMode mode,
            LmsSubmissionErrorCode expectedCode,
            string expectedMessageKey)
        {
            var bridge = new MockLmsBridge(mode, ValidLaunchConfig());
            var successes = 0;
            LmsSubmissionError failure = null;

            bridge.SubmitCompletion(ValidPayload(), () => successes++, error => failure = error);

            Assert.That(successes, Is.Zero);
            Assert.That(failure, Is.Not.Null);
            Assert.That(failure.Code, Is.EqualTo(expectedCode));
            Assert.That(failure.MessageKey, Is.EqualTo(expectedMessageKey));
            Assert.That(failure.AttemptNumber, Is.EqualTo(2));
            Assert.That(bridge.LastSubmittedAttemptNumber, Is.EqualTo(2));
        }

        [Test]
        public void MockBridge_MissingConfigurationReturnsNoLaunchConfig()
        {
            var bridge = new MockLmsBridge(MockLmsBridgeMode.MissingConfiguration, ValidLaunchConfig());

            Assert.That(bridge.GetLaunchConfig(), Is.Null);
        }

        [Test]
        public void MockBridge_NullCallbacksAreSafeInEveryMode()
        {
            foreach (MockLmsBridgeMode mode in Enum.GetValues(typeof(MockLmsBridgeMode)))
            {
                var bridge = new MockLmsBridge(mode, ValidLaunchConfig());

                Assert.DoesNotThrow(() => bridge.SubmitCompletion(ValidPayload(), null, null));
            }
        }

        [Test]
        public void MockBridge_InvalidPayloadReturnsTypedErrorWithoutPayloadValues()
        {
            var payload = ValidPayload();
            payload.SessionId = "private-session-value";
            payload.OverallScore = 101;
            var bridge = new MockLmsBridge(MockLmsBridgeMode.Success, ValidLaunchConfig());
            LmsSubmissionError failure = null;

            bridge.SubmitCompletion(payload, null, error => failure = error);

            Assert.That(failure.Code, Is.EqualTo(LmsSubmissionErrorCode.InvalidPayload));
            Assert.That(failure.MessageKey, Is.EqualTo("lms.payload.invalid"));
            Assert.That(failure.MessageKey, Does.Not.Contain(payload.SessionId));
            Assert.That(failure.AttemptNumber, Is.EqualTo(payload.AttemptNumber));
        }

        private static void AssertIssue(LmsCompletionPayload payload, string code, string path)
        {
            var issues = LmsPayloadValidator.ValidateCompletion(payload);

            Assert.That(issues.Any(issue => issue.Code == code && issue.Path == path), Is.True,
                $"Expected issue {code} at {path}.");
        }

        private static void AssertLaunchIssue(LmsLaunchConfig config, string code, string path)
        {
            var issues = LmsPayloadValidator.ValidateLaunch(config);

            Assert.That(issues.Any(issue => issue.Code == code && issue.Path == path), Is.True,
                $"Expected issue {code} at {path}.");
        }

        private static LmsLaunchConfig ValidLaunchConfig()
        {
            return new LmsLaunchConfig
            {
                PseudonymousLearnerId = "learner-7b9",
                SessionId = "session-42",
                CourseId = "course-garden",
                ModuleId = "module-pitching",
                LessonId = "lesson-smart-school-garden",
                ScenarioId = "smart-school-garden",
                Language = "en",
                AttemptNumber = 2,
                TimerMode = "normal",
                ReducedMotion = false,
                MusicVolume = 0.75f,
                SfxVolume = 0.8f,
                ContentVersion = LmsPayloadValidator.SupportedContentVersion,
                LaunchReference = "lref_opaque_ref_17",
            };
        }

        private static LmsCompletionPayload ValidPayload()
        {
            return new LmsCompletionPayload
            {
                PseudonymousLearnerId = "learner-7b9",
                SessionId = "session-42",
                CourseId = "course-garden",
                ModuleId = "module-pitching",
                LessonId = "lesson-smart-school-garden",
                ScenarioId = "smart-school-garden",
                GameVersion = "0.1.0",
                ContentVersion = LmsPayloadValidator.SupportedContentVersion,
                CompletionStatus = "completed",
                StartedAtUtc = "2026-07-14T02:00:00Z",
                CompletedAtUtc = "2026-07-14T02:05:00Z",
                DurationSeconds = 300d,
                OverallScore = 82,
                CompetencyScores = new[]
                {
                    new LmsCompetencyScore { CompetencyId = "problem", Score = 80 },
                    new LmsCompetencyScore { CompetencyId = "evidence", Score = 75 },
                },
                FinalConfidence = 73,
                SelectedResponseIds = new[] { "opening-clear", "evidence-recovery" },
                TimeoutCount = 1,
                AttemptNumber = 2,
                RecommendedFollowUpLessonId = "lesson-evidence-practice",
            };
        }
    }
}
