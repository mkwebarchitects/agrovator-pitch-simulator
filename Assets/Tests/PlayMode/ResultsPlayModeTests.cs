using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Agrovator.PitchSimulator.Accessibility;
using Agrovator.PitchSimulator.Core;
using Agrovator.PitchSimulator.Dialogue;
using Agrovator.PitchSimulator.LMS;
using Agrovator.PitchSimulator.Scoring;
using Agrovator.PitchSimulator.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.Tests.PlayMode
{
    public sealed class ResultsPlayModeTests
    {
        private readonly List<GameObject> ownedObjects = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (var index = ownedObjects.Count - 1; index >= 0; index--)
            {
                if (ownedObjects[index] != null)
                {
                    UnityEngine.Object.Destroy(ownedObjects[index]);
                }
            }

            ownedObjects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator StrongPath_RendersScoresTwoStrengthsTwoImprovementsAndOrderedReview()
        {
            var controller = CreateController(MockLmsBridgeMode.Success);
            MoveToResults(controller, "opening-strong", "strong-finish");
            var rig = CreateResultsRig(controller);

            rig.Presenter.Refresh(controller.Snapshot);
            yield return null;

            Assert.That(controller.Snapshot.OverallScore, Is.EqualTo(27));
            Assert.That(controller.Snapshot.Confidence, Is.EqualTo(59));
            Assert.That(controller.Snapshot.Result.PitchingScore, Is.EqualTo(21));
            Assert.That(controller.Snapshot.Result.CommunicationsScore, Is.EqualTo(6));
            Assert.That(controller.Snapshot.Result.StrengthKeys, Has.Count.EqualTo(2));
            Assert.That(controller.Snapshot.Result.ImprovementKeys, Has.Count.EqualTo(2));
            Assert.That(rig.Overall.text, Is.EqualTo("Overall 27"));
            Assert.That(rig.Confidence.text, Is.EqualTo("Final confidence 59"));
            Assert.That(rig.Pitching.text, Is.EqualTo("Pitching 21"));
            Assert.That(rig.Communications.text, Is.EqualTo("Communication 6"));
            Assert.That(rig.Strengths[0].text, Is.EqualTo("Clear strength"));
            Assert.That(rig.Strengths[1].text, Is.EqualTo("Problem strength"));
            Assert.That(rig.Improvements[0].text, Is.EqualTo("Solution improvement"));
            Assert.That(rig.Improvements[1].text, Is.EqualTo("Audience improvement"));
            Assert.That(controller.Snapshot.ReviewHistory, Has.Count.EqualTo(2));
            Assert.That(rig.ReviewItems[0].ResponseText, Is.EqualTo("Strong opening"));
            Assert.That(rig.ReviewItems[0].FeedbackText, Is.EqualTo("Strong opening feedback"));
            Assert.That(rig.ReviewItems[0].ExplanationText, Is.EqualTo("Strong opening explanation"));
            Assert.That(rig.ReviewItems[1].ResponseText, Is.EqualTo("Strong finish"));
            Assert.That(rig.ReviewItems[1].FeedbackText, Is.EqualTo("Strong finish feedback"));
            Assert.That(rig.ReviewItems[1].ExplanationText, Is.EqualTo("Strong finish explanation"));
            Assert.That(rig.ReviewItems, Has.Length.EqualTo(6));
            for (var index = 2; index < rig.ReviewItems.Length; index++)
            {
                Assert.That(rig.ReviewItems[index].gameObject.activeSelf, Is.False);
            }
            Assert.That(rig.Status.text, Is.EqualTo("Ready to submit"));
            Assert.That(rig.Submit.interactable, Is.True);
            Assert.That(rig.Retry.interactable, Is.True);
        }

        [UnityTest]
        public IEnumerator RecoveryPath_PreservesRealHistoryThenRetryClearsAttemptAndView()
        {
            var controller = CreateController(MockLmsBridgeMode.Success);
            MoveToResults(controller, "opening-weak", "recovery-answer", "recovery-finish");
            var rig = CreateResultsRig(controller);
            rig.Presenter.Refresh(controller.Snapshot);

            Assert.That(controller.Snapshot.ReviewHistory, Has.Count.EqualTo(3));
            Assert.That(controller.Snapshot.OverallScore, Is.EqualTo(27));
            Assert.That(controller.Snapshot.Confidence, Is.EqualTo(54));
            Assert.That(controller.Snapshot.Result.PitchingScore, Is.EqualTo(21));
            Assert.That(controller.Snapshot.Result.CommunicationsScore, Is.EqualTo(6));
            Assert.That(controller.Snapshot.ReviewHistory[0].ResponseDisplayKey, Is.EqualTo("response.opening.weak"));
            Assert.That(controller.Snapshot.ReviewHistory[1].ResponseDisplayKey, Is.EqualTo("response.recovery"));
            Assert.That(controller.Snapshot.ReviewHistory[2].ResponseDisplayKey, Is.EqualTo("response.recovery.finish"));
            Assert.That(controller.Snapshot.Result.StrengthKeys, Is.EqualTo(new[]
            {
                "result.strength.recovery",
                "result.strength.clear_explanation",
            }));
            Assert.That(controller.Snapshot.Result.ImprovementKeys, Has.Count.EqualTo(2));
            Assert.That(rig.ReviewItems[0].ResponseText, Is.EqualTo("Weak opening"));
            Assert.That(rig.ReviewItems[0].FeedbackText, Is.EqualTo("Weak opening feedback"));
            Assert.That(rig.ReviewItems[0].ExplanationText, Is.EqualTo("Weak opening explanation"));
            Assert.That(rig.ReviewItems[1].ResponseText, Is.EqualTo("Recovery answer"));
            Assert.That(rig.ReviewItems[1].FeedbackText, Is.EqualTo("Recovery feedback"));
            Assert.That(rig.ReviewItems[1].ExplanationText, Is.EqualTo("Recovery explanation"));
            Assert.That(rig.ReviewItems[2].ResponseText, Is.EqualTo("Recovery finish"));
            Assert.That(rig.ReviewItems[2].FeedbackText, Is.EqualTo("Recovery finish feedback"));
            Assert.That(rig.ReviewItems[2].ExplanationText, Is.EqualTo("Recovery finish explanation"));

            rig.Retry.onClick.Invoke();
            yield return null;

            var snapshot = controller.Snapshot;
            Assert.That(snapshot.State, Is.EqualTo(GameState.Briefing));
            Assert.That(snapshot.AttemptNumber, Is.EqualTo(2));
            Assert.That(snapshot.OverallScore, Is.Zero);
            Assert.That(snapshot.Confidence, Is.EqualTo(50));
            Assert.That(snapshot.SelectedResponseIds, Is.Empty);
            Assert.That(snapshot.ReviewHistory, Is.Empty);
            Assert.That(snapshot.TimeoutCount, Is.Zero);
            Assert.That(snapshot.Result, Is.Null);
            Assert.That(snapshot.CompletionPayload, Is.Null);
            Assert.That(snapshot.SubmissionError, Is.Null);
            Assert.That(rig.ReviewItems, Has.All.Matches<QuestionReviewItemView>(item => !item.gameObject.activeSelf));
        }

        [UnityTest]
        public IEnumerator CompletionSuccessAndFailure_RenderLocalizedRetryableStatusWithoutLosingReview()
        {
            var success = CreateController(MockLmsBridgeMode.Success);
            MoveToResults(success, "opening-strong", "strong-finish");
            var successRig = CreateResultsRig(success);
            successRig.Presenter.Refresh(success.Snapshot);
            successRig.Submit.onClick.Invoke();

            Assert.That(success.Snapshot.State, Is.EqualTo(GameState.Complete));
            Assert.That(successRig.Status.text, Is.EqualTo("Submission complete"));
            Assert.That(successRig.Submit.interactable, Is.False);
            Assert.That(successRig.Retry.interactable, Is.True);
            Assert.That(successRig.ReviewItems[0].gameObject.activeSelf, Is.True);

            var failure = CreateController(MockLmsBridgeMode.Expired);
            MoveToResults(failure, "opening-strong", "strong-finish");
            var failureRig = CreateResultsRig(failure);
            failureRig.Presenter.Refresh(failure.Snapshot);
            failureRig.Submit.onClick.Invoke();
            yield return null;

            Assert.That(failure.Snapshot.State, Is.EqualTo(GameState.Results));
            Assert.That(failureRig.Status.text, Is.EqualTo("Session expired. Submit again."));
            Assert.That(failureRig.Submit.interactable, Is.True);
            Assert.That(failureRig.Retry.interactable, Is.True);
            Assert.That(failureRig.ReviewItems[1].ResponseText, Is.EqualTo("Strong finish"));
        }

        [UnityTest]
        public IEnumerator SubmissionStates_RenderSubmittingGenericFailureMissingConfigAndAllowResubmit()
        {
            var deferred = new DeferredBridge();
            var controller = CreateController(deferred);
            MoveToResults(controller, "opening-strong", "strong-finish");
            var rig = CreateResultsRig(controller);
            rig.Presenter.Refresh(controller.Snapshot);

            rig.Submit.onClick.Invoke();
            Assert.That(controller.Snapshot.State, Is.EqualTo(GameState.Submitting));
            Assert.That(rig.Status.text, Is.EqualTo("Submitting"));
            Assert.That(rig.Submit.interactable, Is.False);
            Assert.That(rig.Retry.interactable, Is.False);

            deferred.Fail(new LmsSubmissionError(
                LmsSubmissionErrorCode.SubmissionFailed,
                "lms.submission.failed",
                controller.Snapshot.AttemptNumber));
            rig.Presenter.Refresh(controller.Snapshot);
            Assert.That(controller.Snapshot.State, Is.EqualTo(GameState.Results));
            Assert.That(rig.Status.text, Is.EqualTo("Submission failed. Submit again."));
            Assert.That(rig.Submit.interactable, Is.True);
            Assert.That(rig.ReviewItems[0].ResponseText, Is.EqualTo("Strong opening"));

            rig.Submit.onClick.Invoke();
            Assert.That(deferred.SubmissionCount, Is.EqualTo(2));
            deferred.Fail(new LmsSubmissionError(
                LmsSubmissionErrorCode.MissingConfiguration,
                "lms.configuration.missing",
                controller.Snapshot.AttemptNumber));
            rig.Presenter.Refresh(controller.Snapshot);
            yield return null;

            Assert.That(controller.Snapshot.State, Is.EqualTo(GameState.Results));
            Assert.That(rig.Status.text, Is.EqualTo("Configuration missing. Submit again."));
            Assert.That(rig.Submit.interactable, Is.True);
            Assert.That(rig.Retry.interactable, Is.True);
            Assert.That(controller.Snapshot.ReviewHistory, Has.Count.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator ReviewScroll_ResetsForInitialAndNewAttemptButNotSubmissionRefreshes()
        {
            var bridge = new DeferredBridge();
            var controller = CreateController(bridge);
            MoveToResults(controller, "opening-strong", "strong-finish");
            var rig = CreateResultsRig(controller);

            rig.ReviewScroll.verticalNormalizedPosition = 0.2f;
            rig.Presenter.Refresh(controller.Snapshot);
            Assert.That(rig.ReviewScroll.verticalNormalizedPosition, Is.EqualTo(1f));

            rig.ReviewScroll.verticalNormalizedPosition = 0.35f;
            rig.Submit.onClick.Invoke();
            Assert.That(controller.Snapshot.State, Is.EqualTo(GameState.Submitting));
            Assert.That(rig.ReviewScroll.verticalNormalizedPosition, Is.EqualTo(0.35f).Within(0.001f));

            bridge.Fail(new LmsSubmissionError(
                LmsSubmissionErrorCode.SubmissionFailed,
                "lms.submission.failed",
                controller.Snapshot.AttemptNumber));
            rig.Presenter.Refresh(controller.Snapshot);
            Assert.That(rig.ReviewScroll.verticalNormalizedPosition, Is.EqualTo(0.35f).Within(0.001f));

            rig.Retry.onClick.Invoke();
            Assert.That(controller.Snapshot.State, Is.EqualTo(GameState.Briefing));
            Assert.That(rig.ReviewScroll.verticalNormalizedPosition, Is.EqualTo(1f));

            CompleteCurrentAttempt(controller, "opening-weak", "recovery-answer", "recovery-finish");
            rig.ReviewScroll.verticalNormalizedPosition = 0.15f;
            rig.Presenter.Refresh(controller.Snapshot);
            yield return null;

            Assert.That(controller.Snapshot.AttemptNumber, Is.EqualTo(2));
            Assert.That(rig.ReviewScroll.verticalNormalizedPosition, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator GeneratedResults_UsesStateAwareFocusAndKeyboardScrollableReview()
        {
            var load = SceneManager.LoadSceneAsync("Game", LoadSceneMode.Additive);
            Assert.That(load, Is.Not.Null);
            yield return load;

            var scene = SceneManager.GetSceneByName("Game");
            var roots = scene.GetRootGameObjects();
            var router = roots.SelectMany(root => root.GetComponentsInChildren<GameScreenRouter>(true)).Single();
            var eventSystem = roots.SelectMany(root => root.GetComponentsInChildren<EventSystem>(true)).Single();
            var results = roots.SelectMany(root => root.GetComponentsInChildren<ResultsPresenter>(true)).Single().transform;
            var submit = results.Find("Content Frame/Footer/Submit Button").GetComponent<Button>();
            var retry = results.Find("Content Frame/Footer/Retry Button").GetComponent<Button>();
            var scrollbar = results.Find("Content Frame/Results Scroll/Scrollbar").GetComponent<Scrollbar>();
            var scroll = results.Find("Content Frame/Results Scroll").GetComponent<ScrollRect>();
            var bridge = new DeferredBridge();
            var controller = CreateController(bridge);
            MoveToResults(controller, "opening-strong", "strong-finish");

            router.Initialize(controller, Localize);
            yield return null;
            Assert.That(EventSystem.current, Is.SameAs(eventSystem));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(submit.gameObject));
            Assert.That(scroll.verticalScrollbar, Is.SameAs(scrollbar));
            Assert.That(scrollbar, Is.TypeOf<KeyboardReviewScrollbar>());
            Assert.That(scrollbar.gameObject.activeInHierarchy, Is.True);
            Assert.That(scrollbar.navigation.mode, Is.EqualTo(Navigation.Mode.Explicit));
            Assert.That(scrollbar.navigation.selectOnDown, Is.SameAs(submit));
            Assert.That(submit.navigation.selectOnUp, Is.SameAs(scrollbar));
            Assert.That(submit.navigation.selectOnDown, Is.SameAs(retry));
            Assert.That(retry.navigation.selectOnUp, Is.SameAs(submit));
            Assert.That(retry.navigation.selectOnDown, Is.SameAs(scrollbar));

            EventSystem.current.SetSelectedGameObject(scrollbar.gameObject);
            scrollbar.value = 0f;
            yield return null;
            var boundaryDown = new AxisEventData(eventSystem)
            {
                moveDir = MoveDirection.Down,
                moveVector = Vector2.down,
            };
            ExecuteEvents.Execute(scrollbar.gameObject, boundaryDown, ExecuteEvents.moveHandler);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(submit.gameObject));

            Assert.That(controller.SubmitResults(), Is.True);
            router.Refresh();
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(scrollbar.gameObject));
            scrollbar.value = 0.5f;
            yield return null;
            var before = scrollbar.value;
            var scrollBefore = scroll.verticalNormalizedPosition;
            var move = new AxisEventData(eventSystem) { moveDir = MoveDirection.Down, moveVector = Vector2.down };
            ExecuteEvents.Execute(scrollbar.gameObject, move, ExecuteEvents.moveHandler);
            yield return null;
            Assert.That(Mathf.Abs(scrollbar.value - before), Is.GreaterThan(0.0001f));
            Assert.That(
                Mathf.Abs(scroll.verticalNormalizedPosition - scrollBefore),
                Is.GreaterThan(0.0001f),
                $"scrollbar={scrollbar.value}, scrollBefore={scrollBefore}, scrollAfter={scroll.verticalNormalizedPosition}, " +
                $"contentHeight={scroll.content.rect.height}, viewportHeight={scroll.viewport.rect.height}, " +
                $"contentActive={scroll.content.gameObject.activeInHierarchy}, scrollActive={scroll.isActiveAndEnabled}");

            scrollbar.value = 0f;
            yield return null;
            EventSystem.current.SetSelectedGameObject(scrollbar.gameObject);
            var submittingBoundaryDown = new AxisEventData(eventSystem)
            {
                moveDir = MoveDirection.Down,
                moveVector = Vector2.down,
            };
            ExecuteEvents.Execute(scrollbar.gameObject, submittingBoundaryDown, ExecuteEvents.moveHandler);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(scrollbar.gameObject),
                "Submitting must keep a usable keyboard focus when footer actions are disabled.");

            bridge.Succeed();
            yield return null;
            Assert.That(controller.Snapshot.State, Is.EqualTo(GameState.Complete));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(retry.gameObject));

            controller.Dispose();
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        private ResultsRig CreateResultsRig(PitchSessionController controller)
        {
            var root = Own(new GameObject("Results Test", typeof(RectTransform), typeof(ResultsPresenter)));
            var rig = new ResultsRig
            {
                Presenter = root.GetComponent<ResultsPresenter>(),
                Level = Text("Level", root.transform),
                Overall = Text("Overall", root.transform),
                Confidence = Text("Confidence", root.transform),
                Pitching = Text("Pitching", root.transform),
                Communications = Text("Communications", root.transform),
                Strengths = new[] { Text("Strength 1", root.transform), Text("Strength 2", root.transform) },
                Improvements = new[] { Text("Improvement 1", root.transform), Text("Improvement 2", root.transform) },
                Status = Text("Status", root.transform),
                Submit = Button("Submit", root.transform),
                Retry = Button("Retry", root.transform),
                ReviewItems = new QuestionReviewItemView[6],
            };

            var scrollObject = new GameObject("Review Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollObject.transform.SetParent(root.transform, false);
            var contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(scrollObject.transform, false);
            var scrollbarObject = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image),
                typeof(KeyboardReviewScrollbar));
            scrollbarObject.transform.SetParent(scrollObject.transform, false);
            rig.ReviewScroll = scrollObject.GetComponent<ScrollRect>();
            scrollObject.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 200f);
            contentObject.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 1000f);
            rig.ReviewScroll.content = contentObject.GetComponent<RectTransform>();
            rig.ReviewScroll.horizontal = false;
            rig.ReviewScroll.vertical = true;
            rig.ReviewScroll.verticalScrollbar = scrollbarObject.GetComponent<Scrollbar>();
            rig.ReviewScroll.verticalNormalizedPosition = 1f;

            for (var index = 0; index < rig.ReviewItems.Length; index++)
            {
                var itemObject = new GameObject($"Review {index + 1}", typeof(RectTransform),
                    typeof(QuestionReviewItemView));
                itemObject.transform.SetParent(root.transform, false);
                var item = itemObject.GetComponent<QuestionReviewItemView>();
                Set(item, "responseLabel", Text("Response Label", itemObject.transform));
                Set(item, "responseText", Text("Response", itemObject.transform));
                Set(item, "feedbackLabel", Text("Feedback Label", itemObject.transform));
                Set(item, "feedbackText", Text("Feedback", itemObject.transform));
                Set(item, "explanationLabel", Text("Explanation Label", itemObject.transform));
                Set(item, "explanationText", Text("Explanation", itemObject.transform));
                rig.ReviewItems[index] = item;
            }

            Set(rig.Presenter, "headingText", Text("Heading", root.transform));
            Set(rig.Presenter, "levelText", rig.Level);
            Set(rig.Presenter, "overallText", rig.Overall);
            Set(rig.Presenter, "confidenceText", rig.Confidence);
            Set(rig.Presenter, "pitchingText", rig.Pitching);
            Set(rig.Presenter, "communicationsText", rig.Communications);
            Set(rig.Presenter, "strengthsHeadingText", Text("Strengths Heading", root.transform));
            Set(rig.Presenter, "strengthTexts", rig.Strengths);
            Set(rig.Presenter, "improvementsHeadingText", Text("Improvements Heading", root.transform));
            Set(rig.Presenter, "improvementTexts", rig.Improvements);
            Set(rig.Presenter, "reviewHeadingText", Text("Review Heading", root.transform));
            Set(rig.Presenter, "reviewItems", rig.ReviewItems);
            Set(rig.Presenter, "reviewScroll", rig.ReviewScroll);
            Set(rig.Presenter, "submissionStatusText", rig.Status);
            Set(rig.Presenter, "submitButton", rig.Submit);
            Set(rig.Presenter, "submitButtonText", Text("Submit Label", rig.Submit.transform));
            Set(rig.Presenter, "retryButton", rig.Retry);
            Set(rig.Presenter, "retryButtonText", Text("Retry Label", rig.Retry.transform));
            rig.Presenter.Initialize(controller, () => rig.Presenter.Refresh(controller.Snapshot), Localize);
            return rig;
        }

        private static void MoveToResults(PitchSessionController controller, params string[] responseIds)
        {
            Assert.That(controller.FinishLaunch(), Is.True);
            Assert.That(controller.StartScenario(), Is.True);
            CompleteCurrentAttempt(controller, responseIds);
        }

        private static void CompleteCurrentAttempt(PitchSessionController controller, params string[] responseIds)
        {
            foreach (var responseId in responseIds)
            {
                while (controller.Snapshot.State != GameState.AwaitingResponse)
                {
                    Assert.That(controller.Continue(), Is.True);
                }

                Assert.That(controller.SelectResponse(responseId), Is.True);
                Assert.That(controller.Continue(), Is.True);
                Assert.That(controller.Continue(), Is.True);
            }

            Assert.That(controller.Snapshot.State, Is.EqualTo(GameState.Results));
        }

        private static PitchSessionController CreateController(MockLmsBridgeMode mode)
        {
            return CreateController(new MockLmsBridge(mode, ValidLaunch()));
        }

        private static PitchSessionController CreateController(ILmsBridge bridge)
        {
            return new PitchSessionController(
                BuildScenario(),
                new ScoreAccumulator(),
                new AccessibilitySettings(TimerMode.Normal, false, 1f, 1f, "en"),
                new QuestionTimer(0d),
                bridge,
                () => new DateTimeOffset(2026, 7, 14, 3, 0, 0, TimeSpan.Zero),
                "0.1.0");
        }

        private static RuntimeScenario BuildScenario()
        {
            return RuntimeScenario.Compile(new ScenarioDefinitionDto
            {
                Id = "results-test",
                Version = 1,
                TitleKey = "scenario.title",
                BriefingKey = "scenario.briefing",
                InitialConfidence = 50,
                OpeningNodeId = "opening",
                Nodes = new[]
                {
                    Node("opening", Response("opening-strong", "response.opening.strong", "feedback.opening.strong",
                        "explanation.opening.strong", "strong-final", 5, Delta(clear: 12, communication: 3)),
                        Response("opening-weak", "response.opening.weak", "feedback.opening.weak",
                            "explanation.opening.weak", "recovery", -5, Delta(evidence: -2), "weak_claim_made")),
                    Node("strong-final", Response("strong-finish", "response.strong.finish", "feedback.strong.finish",
                        "explanation.strong.finish", "complete", 4, Delta(problem: 9, communication: 3))),
                    Node("recovery", Response("recovery-answer", "response.recovery", "feedback.recovery",
                        "explanation.recovery", "recovery-final", 6, Delta(clear: 12),
                        "recovered_after_weak_answer")),
                    Node("recovery-final", Response("recovery-finish", "response.recovery.finish",
                        "feedback.recovery.finish", "explanation.recovery.finish", "complete", 3,
                        Delta(problem: 9, communication: 6))),
                    new DialogueNodeDto
                    {
                        Id = "complete",
                        NodeType = "Terminal",
                        TextKey = "node.complete",
                        Responses = Array.Empty<ResponseOptionDto>(),
                    },
                },
            });
        }

        private static DialogueNodeDto Node(string id, params ResponseOptionDto[] responses)
        {
            return new DialogueNodeDto
            {
                Id = id,
                NodeType = "Question",
                TextKey = $"node.{id}",
                TimerSeconds = 30,
                Responses = responses,
            };
        }

        private static ResponseOptionDto Response(string id, string textKey, string feedbackKey,
            string explanationKey, string nextNodeId, int confidenceDelta, ResponseScoreDeltaDto delta,
            params string[] flags)
        {
            return new ResponseOptionDto
            {
                Id = id,
                TextKey = textKey,
                QualityTier = "Strong",
                ScoreDelta = delta,
                ConfidenceDelta = confidenceDelta,
                ReactionCue = "Encouraging",
                FeedbackKey = feedbackKey,
                ExplanationKey = explanationKey,
                NextNodeId = nextNodeId,
                SetFlags = flags,
            };
        }

        private static ResponseScoreDeltaDto Delta(int clear = 0, int problem = 0, int solution = 0,
            int audience = 0, int evidence = 0, int communication = 0, int time = 0)
        {
            return new ResponseScoreDeltaDto
            {
                ClearExplanation = clear,
                Problem = problem,
                Solution = solution,
                Audience = audience,
                Evidence = evidence,
                Communication = communication,
                TimeManagement = time,
            };
        }

        private static LmsLaunchConfig ValidLaunch()
        {
            return new LmsLaunchConfig
            {
                PseudonymousLearnerId = "learner",
                SessionId = "session",
                CourseId = "course",
                ModuleId = "module",
                LessonId = "lesson",
                ScenarioId = "results-test",
                Language = "en",
                AttemptNumber = 1,
                TimerMode = "Normal",
                MusicVolume = 1f,
                SfxVolume = 1f,
                ContentVersion = 1,
                LaunchReference = "lref_resultsTest01",
            };
        }

        private static string Localize(string key)
        {
            var values = new Dictionary<string, string>
            {
                ["ui.overall_score"] = "Overall",
                ["ui.final_confidence"] = "Final confidence",
                ["ui.pitching"] = "Pitching",
                ["ui.communications"] = "Communication",
                ["ui.results"] = "Results",
                ["ui.strengths"] = "Strengths",
                ["ui.improvements"] = "Improvements",
                ["ui.review"] = "Review your choices",
                ["ui.selected_response"] = "Your response",
                ["ui.feedback"] = "Feedback",
                ["ui.stronger_answer"] = "Stronger answer",
                ["ui.submit_results"] = "Submit results",
                ["ui.retry"] = "Try again",
                ["result.level.seedling"] = "Seedling",
                ["result.strength.recovery"] = "Recovery strength",
                ["result.strength.clear_explanation"] = "Clear strength",
                ["result.strength.problem"] = "Problem strength",
                ["result.improvement.solution"] = "Solution improvement",
                ["result.improvement.audience"] = "Audience improvement",
                ["response.opening.strong"] = "Strong opening",
                ["feedback.opening.strong"] = "Strong opening feedback",
                ["explanation.opening.strong"] = "Strong opening explanation",
                ["response.strong.finish"] = "Strong finish",
                ["feedback.strong.finish"] = "Strong finish feedback",
                ["explanation.strong.finish"] = "Strong finish explanation",
                ["response.opening.weak"] = "Weak opening",
                ["feedback.opening.weak"] = "Weak opening feedback",
                ["explanation.opening.weak"] = "Weak opening explanation",
                ["response.recovery"] = "Recovery answer",
                ["feedback.recovery"] = "Recovery feedback",
                ["explanation.recovery"] = "Recovery explanation",
                ["response.recovery.finish"] = "Recovery finish",
                ["feedback.recovery.finish"] = "Recovery finish feedback",
                ["explanation.recovery.finish"] = "Recovery finish explanation",
                ["lms.submission.ready"] = "Ready to submit",
                ["lms.submission.submitting"] = "Submitting",
                ["lms.submission.success"] = "Submission complete",
                ["lms.submission.failed"] = "Submission failed. Submit again.",
                ["lms.session.expired"] = "Session expired. Submit again.",
                ["lms.configuration.missing"] = "Configuration missing. Submit again.",
                ["lms.payload.invalid"] = "Submission data is invalid.",
            };
            return values.TryGetValue(key, out var value) ? value : $"localized:{key}";
        }

        private Text Text(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<Text>();
        }

        private Button Button(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<Button>();
        }

        private GameObject Own(GameObject gameObject)
        {
            ownedObjects.Add(gameObject);
            return gameObject;
        }

        private static void Set(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing serialized field {fieldName} on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private sealed class ResultsRig
        {
            public ResultsPresenter Presenter;
            public Text Level;
            public Text Overall;
            public Text Confidence;
            public Text Pitching;
            public Text Communications;
            public Text[] Strengths;
            public Text[] Improvements;
            public Text Status;
            public Button Submit;
            public Button Retry;
            public ScrollRect ReviewScroll;
            public QuestionReviewItemView[] ReviewItems;
        }

        private sealed class DeferredBridge : ILmsBridge
        {
            private Action onSuccess;
            private Action<LmsSubmissionError> onFailure;

            public int SubmissionCount { get; private set; }

            public LmsLaunchConfig GetLaunchConfig()
            {
                return ValidLaunch();
            }

            public void SubmitCompletion(
                LmsCompletionPayload payload,
                Action success,
                Action<LmsSubmissionError> failure)
            {
                SubmissionCount++;
                onSuccess = success;
                onFailure = failure;
            }

            public void Fail(LmsSubmissionError error)
            {
                var callback = onFailure;
                onSuccess = null;
                onFailure = null;
                callback?.Invoke(error);
            }

            public void Succeed()
            {
                var callback = onSuccess;
                onSuccess = null;
                onFailure = null;
                callback?.Invoke();
            }
        }
    }
}
