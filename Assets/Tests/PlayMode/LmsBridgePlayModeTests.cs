using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Agrovator.PitchSimulator.LMS;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.Tests.PlayMode
{
    public sealed class LmsBridgePlayModeTests
    {
        [Test]
        public void GetLaunchConfig_DeserializesValidTransportJson()
        {
            var expected = ValidLaunchConfig();
            var bridge = new WebGlLmsBridge(
                new FakeWebGlLmsTransport { LaunchConfigJson = LmsPayloadJson.SerializeLaunchConfig(expected) });

            var actual = bridge.GetLaunchConfig();

            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.SessionId, Is.EqualTo(expected.SessionId));
            Assert.That(actual.ScenarioId, Is.EqualTo(expected.ScenarioId));
            Assert.That(actual.AttemptNumber, Is.EqualTo(expected.AttemptNumber));
            Assert.That(actual.LaunchReference, Is.EqualTo(expected.LaunchReference));
        }

        [Test]
        public void SubmitCompletion_Success_UsesJsonAndInvokesSuccessCallback()
        {
            var transport = new FakeWebGlLmsTransport();
            var bridge = new WebGlLmsBridge(transport);
            var succeeded = false;
            LmsSubmissionError error = null;

            bridge.SubmitCompletion(ValidCompletion(), () => succeeded = true, value => error = value);
            Assert.That(transport.SubmittedJson, Does.Contain("\"SessionId\":\"session-web-1\""));

            transport.CompleteSuccessfully();

            Assert.That(succeeded, Is.True);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void SubmitCompletion_Failure_MapsSanitizedFailureForAttempt()
        {
            var transport = new FakeWebGlLmsTransport();
            var bridge = new WebGlLmsBridge(transport);
            LmsSubmissionError error = null;

            bridge.SubmitCompletion(ValidCompletion(), null, value => error = value);
            transport.Fail(WebGlLmsTransportFailure.SubmissionFailed);

            Assert.That(error, Is.Not.Null);
            Assert.That(error.Code, Is.EqualTo(LmsSubmissionErrorCode.SubmissionFailed));
            Assert.That(error.MessageKey, Is.EqualTo("lms.submission.failed"));
            Assert.That(error.AttemptNumber, Is.EqualTo(2));
        }

        [Test]
        public void SubmitCompletion_Expired_MapsSessionExpired()
        {
            var transport = new FakeWebGlLmsTransport();
            var bridge = new WebGlLmsBridge(transport);
            LmsSubmissionError error = null;

            bridge.SubmitCompletion(ValidCompletion(), null, value => error = value);
            transport.Fail(WebGlLmsTransportFailure.SessionExpired);

            Assert.That(error.Code, Is.EqualTo(LmsSubmissionErrorCode.SessionExpired));
            Assert.That(error.MessageKey, Is.EqualTo("lms.session.expired"));
        }

        [Test]
        public void SubmitCompletion_MissingConfiguration_MapsMissingConfigurationForAttempt()
        {
            var transport = new FakeWebGlLmsTransport();
            var bridge = new WebGlLmsBridge(transport);
            LmsSubmissionError error = null;

            bridge.SubmitCompletion(ValidCompletion(), null, value => error = value);
            transport.Fail(WebGlLmsTransportFailure.MissingConfiguration);

            Assert.That(error, Is.Not.Null);
            Assert.That(error.Code, Is.EqualTo(LmsSubmissionErrorCode.MissingConfiguration));
            Assert.That(error.MessageKey, Is.EqualTo("lms.configuration.missing"));
            Assert.That(error.AttemptNumber, Is.EqualTo(2));
        }

        [Test]
        public void GetLaunchConfig_MissingConfig_ReturnsNullWithoutSubmissionOrException()
        {
            var transport = new FakeWebGlLmsTransport { LaunchConfigJson = string.Empty };
            var bridge = new WebGlLmsBridge(transport);

            Assert.That(bridge.GetLaunchConfig(), Is.Null);
            Assert.That(transport.SubmittedJson, Is.Null);
        }

        [Test]
        public void SubmitCompletion_DuplicateTransportCallbacks_CompleteOnlyOnce()
        {
            var transport = new FakeWebGlLmsTransport();
            var bridge = new WebGlLmsBridge(transport);
            var successes = 0;
            var failures = 0;

            bridge.SubmitCompletion(ValidCompletion(), () => successes++, _ => failures++);
            transport.CompleteSuccessfully();
            transport.Fail(WebGlLmsTransportFailure.SubmissionFailed);

            Assert.That(successes, Is.EqualTo(1));
            Assert.That(failures, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Host_ObservesLaunchConfigThatArrivesAfterInitialReadyRequest()
        {
            var transport = new FakeWebGlLmsTransport { LaunchConfigJson = string.Empty };
            var hostObject = new GameObject("Bridge Host Test");
            var labelObject = new GameObject("Diagnostics", typeof(RectTransform), typeof(Text));
            var host = hostObject.AddComponent<WebGlLmsBridgeHost>();
            var label = labelObject.GetComponent<Text>();
            typeof(WebGlLmsBridgeHost)
                .GetField("diagnosticsLabel", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(host, label);
            host.Initialize(transport);
            Assert.That(host.IsConfigured, Is.False);

            transport.LaunchConfigJson = LmsPayloadJson.SerializeLaunchConfig(ValidLaunchConfig());
            yield return new WaitForSecondsRealtime(0.3f);

            Assert.That(host.IsConfigured, Is.True);
            Assert.That(label.text, Is.EqualTo("LMS bridge ready (attempt 2)."));
            UnityEngine.Object.Destroy(hostObject);
            UnityEngine.Object.Destroy(labelObject);
        }

#if UNITY_EDITOR
        [Test]
        public void LaunchPoller_MissingThenResentConfig_ThrottlesAndRecovers()
        {
            var transport = new FakeWebGlLmsTransport { LaunchConfigJson = string.Empty };
            var poller = new LmsLaunchPoller(new WebGlLmsBridge(transport), 0.2f);

            Assert.That(poller.TryPoll(10f, out var first), Is.False);
            Assert.That(first, Is.Null);
            Assert.That(transport.LaunchReadCount, Is.EqualTo(1));

            transport.LaunchConfigJson = LmsPayloadJson.SerializeLaunchConfig(ValidLaunchConfig());
            Assert.That(poller.TryPoll(10.1f, out var throttled), Is.False);
            Assert.That(throttled, Is.Null);
            Assert.That(transport.LaunchReadCount, Is.EqualTo(1));

            Assert.That(poller.TryPoll(10.2f, out var recovered), Is.True);
            Assert.That(recovered.SessionId, Is.EqualTo("session-web-1"));
            Assert.That(transport.LaunchReadCount, Is.EqualTo(2));
        }

        [Test]
        public void BrowserSources_CancelStaleRequests_AndAllowlistCompletionStatus()
        {
            var jslib = File.ReadAllText(Path.Combine(
                Application.dataPath, "Plugins", "WebGL", "PitchSimulatorBridge.jslib"));
            var bridgeSource = File.ReadAllText(Path.Combine(
                Application.dataPath, "Scripts", "LMS", "WebGlLmsBridge.cs"));
            var harness = File.ReadAllText(Path.Combine(
                Application.dataPath, "..", "WebHarness", "harness.js"));

            Assert.That(jslib, Does.Contain("PitchSimulatorBridge_CancelSubmission"));
            Assert.That(jslib, Does.Contain("delete PitchSimulatorLmsBridge.pending[requestId]"));
            Assert.That(bridgeSource, Does.Contain("Pending.Remove(requestId);"));
            Assert.That(bridgeSource, Does.Contain("TryCancelJavaScriptRequest(requestId);"));
            Assert.That(bridgeSource, Does.Contain("PitchSimulatorBridge_CancelSubmission(requestId);"));
            Assert.That(harness, Does.Contain("allowedCompletionStatuses"));
            Assert.That(harness, Does.Contain(
                "Object.prototype.hasOwnProperty.call(allowedCompletionStatuses, normalized)"));
            Assert.That(harness, Does.Contain("return \"Unknown\""));
            Assert.That(harness, Does.Not.Contain(
                "completionStatus.textContent = String(payload.CompletionStatus"));
        }
#endif

        private static LmsLaunchConfig ValidLaunchConfig()
        {
            return new LmsLaunchConfig
            {
                PseudonymousLearnerId = "learner-web-1",
                SessionId = "session-web-1",
                CourseId = "course-1",
                ModuleId = "module-1",
                LessonId = "lesson-1",
                ScenarioId = "smart-school-garden",
                Language = "en",
                AttemptNumber = 2,
                TimerMode = "Normal",
                ReducedMotion = false,
                MusicVolume = 0.6f,
                SfxVolume = 0.7f,
                ContentVersion = LmsPayloadValidator.SupportedContentVersion,
                LaunchReference = "lref_123456789012",
            };
        }

        private static LmsCompletionPayload ValidCompletion()
        {
            return new LmsCompletionPayload
            {
                PseudonymousLearnerId = "learner-web-1",
                SessionId = "session-web-1",
                CourseId = "course-1",
                ModuleId = "module-1",
                LessonId = "lesson-1",
                ScenarioId = "smart-school-garden",
                GameVersion = "0.1.0",
                ContentVersion = LmsPayloadValidator.SupportedContentVersion,
                CompletionStatus = "completed",
                StartedAtUtc = "2026-07-14T01:00:00Z",
                CompletedAtUtc = "2026-07-14T01:05:00Z",
                DurationSeconds = 300d,
                OverallScore = 84,
                CompetencyScores = new[]
                {
                    new LmsCompetencyScore { CompetencyId = "evidence", Score = 85 },
                },
                FinalConfidence = 76,
                SelectedResponseIds = new[] { "response-1" },
                TimeoutCount = 0,
                AttemptNumber = 2,
                RecommendedFollowUpLessonId = "lesson-follow-up",
            };
        }

        private sealed class FakeWebGlLmsTransport : IWebGlLmsTransport
        {
            private Action success;
            private Action<WebGlLmsTransportFailure> failure;

            public string LaunchConfigJson { get; set; }

            public string SubmittedJson { get; private set; }

            public int LaunchReadCount { get; private set; }

            public string GetLaunchConfigJson()
            {
                LaunchReadCount++;
                return LaunchConfigJson;
            }

            public void SubmitCompletion(
                string completionJson,
                Action onSuccess,
                Action<WebGlLmsTransportFailure> onFailure)
            {
                SubmittedJson = completionJson;
                success = onSuccess;
                failure = onFailure;
            }

            public void CompleteSuccessfully()
            {
                success?.Invoke();
            }

            public void Fail(WebGlLmsTransportFailure reason)
            {
                failure?.Invoke(reason);
            }
        }
    }
}
