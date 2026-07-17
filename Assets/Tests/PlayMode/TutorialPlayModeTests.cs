using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Agrovator.PitchSimulator.Accessibility;
using Agrovator.PitchSimulator.Core;
using Agrovator.PitchSimulator.Dialogue;
using Agrovator.PitchSimulator.LMS;
using Agrovator.PitchSimulator.Scoring;
using Agrovator.PitchSimulator.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.Tests.PlayMode
{
    public sealed class TutorialPlayModeTests
    {
        private static readonly IReadOnlyDictionary<string, string> LocalizedText =
            new Dictionary<string, string>
            {
                ["ui.back"] = "Back",
                ["ui.next"] = "Next",
                ["ui.skip_tutorial"] = "Skip tutorial",
                ["ui.start_practice"] = "Start practice",
                ["ui.tutorial.step.1"] = "Step 1 of 3",
                ["ui.tutorial.step.2"] = "Step 2 of 3",
                ["ui.tutorial.step.3"] = "Step 3 of 3",
                ["ui.tutorial.goal.title"] = "Build your pitch",
                ["ui.tutorial.goal.body"] = "Goal body",
                ["ui.tutorial.choices.title"] = "Choose your response",
                ["ui.tutorial.choices.body"] = "Choices body",
                ["ui.tutorial.feedback.title"] = "Learn from feedback",
                ["ui.tutorial.feedback.body"] = "Feedback body",
            };

        private readonly List<GameObject> ownedObjects = new List<GameObject>();
        private readonly List<PitchSessionController> controllers = new List<PitchSessionController>();

        [Test]
        public void Paging_RendersLocalizedThreePageTutorialAndSupportsBackAndNext()
        {
            var controller = CreateTutorialController();
            MoveToTutorial(controller);
            var rig = CreateRig(controller);

            rig.Presenter.Refresh(controller.Snapshot);

            Assert.That(rig.Presenter.PageCount, Is.EqualTo(3));
            Assert.That(rig.Presenter.CurrentPageIndex, Is.Zero);
            Assert.That(rig.Step.text, Is.EqualTo("Step 1 of 3"));
            Assert.That(rig.Heading.text, Is.EqualTo("Build your pitch"));
            Assert.That(rig.Back.interactable, Is.False);
            Assert.That(rig.Next.GetComponentInChildren<Text>().text, Is.EqualTo("Next"));

            rig.Next.onClick.Invoke();
            Assert.That(rig.Presenter.CurrentPageIndex, Is.EqualTo(1));
            Assert.That(rig.Heading.text, Is.EqualTo("Choose your response"));
            Assert.That(rig.Back.interactable, Is.True);

            rig.Next.onClick.Invoke();
            Assert.That(rig.Presenter.CurrentPageIndex, Is.EqualTo(2));
            Assert.That(rig.Next.GetComponentInChildren<Text>().text, Is.EqualTo("Start practice"));

            rig.Back.onClick.Invoke();
            Assert.That(rig.Presenter.CurrentPageIndex, Is.EqualTo(1));
        }

        [Test]
        public void Skip_AdvancesTutorialToJudgeIntroExactlyOnce()
        {
            var controller = CreateTutorialController();
            MoveToTutorial(controller);
            var rig = CreateRig(controller);
            rig.Presenter.Refresh(controller.Snapshot);

            rig.Skip.onClick.Invoke();

            Assert.That(controller.Snapshot.State, Is.EqualTo(GameState.JudgeIntro));
            Assert.That(rig.ChangeCount, Is.EqualTo(1));

            rig.Skip.onClick.Invoke();

            Assert.That(controller.Snapshot.State, Is.EqualTo(GameState.JudgeIntro));
            Assert.That(rig.ChangeCount, Is.EqualTo(1));
        }

        [Test]
        public void FreshTutorialSession_ResetsCurrentPageIndexToZero()
        {
            var firstController = CreateTutorialController();
            MoveToTutorial(firstController);
            var rig = CreateRig(firstController);
            rig.Presenter.Refresh(firstController.Snapshot);
            rig.Next.onClick.Invoke();
            Assert.That(rig.Presenter.CurrentPageIndex, Is.EqualTo(1));

            Assert.That(firstController.Continue(), Is.True);
            rig.Presenter.Refresh(firstController.Snapshot);

            var freshController = CreateTutorialController();
            MoveToTutorial(freshController);
            rig.Presenter.Initialize(
                freshController,
                () => rig.Presenter.Refresh(freshController.Snapshot),
                Localize);
            rig.Presenter.Refresh(freshController.Snapshot);

            Assert.That(rig.Presenter.CurrentPageIndex, Is.Zero);
            Assert.That(rig.Step.text, Is.EqualTo("Step 1 of 3"));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var controller in controllers)
            {
                controller.Dispose();
            }
            controllers.Clear();

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

        private TutorialRig CreateRig(PitchSessionController controller)
        {
            var root = Own(new GameObject("Tutorial Rig", typeof(RectTransform), typeof(TutorialPresenter)));
            var rig = new TutorialRig
            {
                Presenter = root.GetComponent<TutorialPresenter>(),
                Step = Text("Step", root.transform),
                Heading = Text("Heading", root.transform),
                Body = Text("Body", root.transform),
                Back = Button("Back", root.transform),
                Skip = Button("Skip", root.transform),
                Next = Button("Next", root.transform),
            };

            Text("Label", rig.Back.transform);
            Text("Label", rig.Skip.transform);
            var nextLabel = Text("Label", rig.Next.transform);
            Set(rig.Presenter, "stepText", rig.Step);
            Set(rig.Presenter, "headingText", rig.Heading);
            Set(rig.Presenter, "bodyText", rig.Body);
            Set(rig.Presenter, "backButton", rig.Back);
            Set(rig.Presenter, "skipButton", rig.Skip);
            Set(rig.Presenter, "nextButton", rig.Next);
            Set(rig.Presenter, "nextButtonText", nextLabel);
            rig.Presenter.Initialize(
                controller,
                () =>
                {
                    rig.ChangeCount++;
                    rig.Presenter.Refresh(controller.Snapshot);
                },
                Localize);
            return rig;
        }

        private PitchSessionController CreateTutorialController()
        {
            var scenario = RuntimeScenario.Compile(new ScenarioDefinitionDto
            {
                Id = "tutorial-presenter-test",
                Version = 2,
                InitialConfidence = 50,
                OpeningNodeId = "question",
                Nodes = new[]
                {
                    new DialogueNodeDto
                    {
                        Id = "question",
                        NodeType = "Question",
                        TextKey = "question.text",
                        TimerSeconds = 30,
                        Responses = new[]
                        {
                            new ResponseOptionDto
                            {
                                Id = "ready",
                                TextKey = "response.ready",
                                QualityTier = "Strong",
                                ScoreDelta = new ResponseScoreDeltaDto(),
                                ReactionCue = "Encouraging",
                                FeedbackKey = "feedback.ready",
                                ExplanationKey = "explanation.ready",
                                NextNodeId = "complete",
                            },
                        },
                    },
                    new DialogueNodeDto
                    {
                        Id = "complete",
                        NodeType = "Terminal",
                        TextKey = "node.complete",
                        Responses = Array.Empty<ResponseOptionDto>(),
                    },
                },
            });
            var launch = new LmsLaunchConfig
            {
                PseudonymousLearnerId = "tutorial-learner",
                SessionId = "tutorial-session",
                CourseId = "tutorial-course",
                ModuleId = "tutorial-module",
                LessonId = "tutorial-lesson",
                ScenarioId = scenario.Id,
                Language = "en",
                AttemptNumber = 1,
                TimerMode = "Normal",
                MusicVolume = 1f,
                SfxVolume = 1f,
                ContentVersion = scenario.Version,
                LaunchReference = "lref_tutorialTest01",
            };
            var controller = new PitchSessionController(
                scenario,
                new ScoreAccumulator(),
                new AccessibilitySettings(TimerMode.Normal, false, 1f, 1f, "en"),
                new QuestionTimer(0d),
                new MockLmsBridge(MockLmsBridgeMode.Success, launch),
                () => new DateTimeOffset(2026, 7, 15, 3, 0, 0, TimeSpan.Zero),
                "0.1.0");
            controllers.Add(controller);
            return controller;
        }

        private static void MoveToTutorial(PitchSessionController controller)
        {
            Assert.That(controller.FinishLaunch(), Is.True);
            Assert.That(controller.StartScenario(), Is.True);
            Assert.That(controller.Continue(), Is.True);
            Assert.That(controller.Snapshot.State, Is.EqualTo(GameState.Tutorial));
        }

        private static string Localize(string key)
        {
            return LocalizedText.TryGetValue(key, out var value) ? value : key;
        }

        private static Text Text(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<Text>();
        }

        private static Button Button(string name, Transform parent)
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

        private sealed class TutorialRig
        {
            public TutorialPresenter Presenter;
            public Text Step;
            public Text Heading;
            public Text Body;
            public Button Back;
            public Button Skip;
            public Button Next;
            public int ChangeCount;
        }
    }
}
