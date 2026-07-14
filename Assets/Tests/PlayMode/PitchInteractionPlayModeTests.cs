using System;
using System.Collections;
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
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.Tests.PlayMode
{
    public sealed class PitchInteractionPlayModeTests
    {
        private readonly System.Collections.Generic.List<GameObject> roots =
            new System.Collections.Generic.List<GameObject>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (EventSystem.current == null)
            {
                Track(new GameObject("Test EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
            }
            yield return null;
        }

        [Test]
        public void ResponseList_UsesFixedNumberedPool_VisibleNavigation_AndOneShotLock()
        {
            var responses = BuildScenario(timerSeconds: 1).OpeningNode.Responses;
            var slots = new ResponseButtonView[3];
            var root = Track(new GameObject("Response List"));
            var list = root.AddComponent<ResponseListView>();
            for (var index = 0; index < slots.Length; index++)
            {
                var slotObject = new GameObject($"Response {index + 1}", typeof(RectTransform),
                    typeof(Image), typeof(Button), typeof(ResponseButtonView));
                slotObject.transform.SetParent(root.transform, false);
                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(slotObject.transform, false);
                slots[index] = slotObject.GetComponent<ResponseButtonView>();
                slots[index].Configure(slotObject.GetComponent<Button>(), labelObject.GetComponent<Text>());
            }

            var selections = 0;
            string selectedId = null;
            list.Configure(slots);
            list.Initialize(id =>
            {
                selections++;
                selectedId = id;
            });
            var originalSlots = (ResponseButtonView[])slots.Clone();
            list.Render(responses, true);

            Assert.That(slots.All(slot => slot.gameObject.activeSelf), Is.True);
            for (var index = 0; index < slots.Length; index++)
            {
                Assert.That(slots[index].DisplayText,
                    Is.EqualTo($"{index + 1}. {responses[index].TextKey}"));
            }

            list.Render(responses.Take(2).ToArray(), true);

            Assert.That(list.SlotCount, Is.EqualTo(3));
            for (var index = 0; index < slots.Length; index++)
            {
                Assert.That(slots[index], Is.SameAs(originalSlots[index]));
            }
            Assert.That(slots[0].DisplayText, Is.EqualTo($"1. {responses[0].TextKey}"));
            Assert.That(slots[1].DisplayText, Is.EqualTo($"2. {responses[1].TextKey}"));
            Assert.That(slots[2].gameObject.activeSelf, Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(slots[0].Button.gameObject));
            Assert.That(slots[0].Button.navigation.selectOnDown, Is.EqualTo(slots[1].Button));
            Assert.That(slots[1].Button.navigation.selectOnDown, Is.EqualTo(slots[0].Button));

            ExecuteEvents.Execute(slots[0].Button.gameObject, new BaseEventData(EventSystem.current),
                ExecuteEvents.submitHandler);
            slots[0].Button.onClick.Invoke();

            Assert.That(selections, Is.EqualTo(1));
            Assert.That(selectedId, Is.EqualTo(responses[0].Id));
            Assert.That(list.IsSelectionLocked, Is.True);
            Assert.That(slots.All(slot => !slot.Button.interactable), Is.True);
        }

        [Test]
        public void TimerView_RendersCeilingFillAndGentleFinalFivePulse_WithoutReducedMotion()
        {
            var root = Track(new GameObject("Timer", typeof(RectTransform), typeof(TimerView)));
            var seconds = new GameObject("Seconds", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            seconds.transform.SetParent(root.transform, false);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            fill.transform.SetParent(root.transform, false);
            fill.type = Image.Type.Filled;
            var view = root.GetComponent<TimerView>();
            view.Configure(seconds, fill, root.GetComponent<RectTransform>());

            view.Render(4.2d, 20d, false);
            Assert.That(view.DisplayedSeconds, Is.EqualTo(5));
            Assert.That(fill.fillAmount, Is.EqualTo(0.21f).Within(0.001f));
            Assert.That(view.IsPulsing, Is.True);
            Assert.That(root.transform.localScale.x, Is.GreaterThanOrEqualTo(1f).And.LessThanOrEqualTo(1.05f));

            view.Render(4.2d, 20d, true);
            Assert.That(view.IsPulsing, Is.False);
            Assert.That(root.transform.localScale, Is.EqualTo(Vector3.one));
        }

        [TestCase(0, "Getting Started", "[ ]")]
        [TestCase(19, "Getting Started", "[ ]")]
        [TestCase(20, "Listening", "[.]")]
        [TestCase(39, "Listening", "[.]")]
        [TestCase(40, "Curious", "[:]")]
        [TestCase(59, "Curious", "[:]")]
        [TestCase(60, "Interested", "[*]")]
        [TestCase(79, "Interested", "[*]")]
        [TestCase(80, "Convinced", "[#]")]
        [TestCase(100, "Convinced", "[#]")]
        public void ConfidenceView_UsesExactFiveStateBoundariesWithoutRubricLeakage(
            int value,
            string expectedLabel,
            string expectedGlyph)
        {
            var root = Track(new GameObject("Confidence", typeof(ConfidenceView)));
            var label = new GameObject("Label", typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(root.transform, false);
            var icon = new GameObject("Icon", typeof(Text)).GetComponent<Text>();
            icon.transform.SetParent(root.transform, false);
            var fill = new GameObject("Fill", typeof(Image)).GetComponent<Image>();
            fill.transform.SetParent(root.transform, false);
            var view = root.GetComponent<ConfidenceView>();
            view.Configure(label, icon, fill);

            view.Render(value);

            Assert.That(label.text, Is.EqualTo(expectedLabel));
            Assert.That(icon.text, Is.EqualTo(expectedGlyph));
            Assert.That(fill.fillAmount, Is.EqualTo(value / 100f).Within(0.001f));
            Assert.That(label.text, Does.Not.Contain("Score").And.Not.Contain(value.ToString()));
        }

        [Test]
        public void ConfidenceView_ShowsArtworkWhenPresent_AndGlyphOnlyWhenSpriteIsMissing()
        {
            var root = Track(new GameObject("Confidence", typeof(ConfidenceView)));
            var label = CreateText(root.transform, "Label");
            var glyph = CreateText(root.transform, "Glyph");
            var fill = new GameObject("Fill", typeof(Image)).GetComponent<Image>();
            fill.transform.SetParent(root.transform, false);
            var artwork = new GameObject("Artwork", typeof(Image)).GetComponent<Image>();
            artwork.transform.SetParent(root.transform, false);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));
            var view = root.GetComponent<ConfidenceView>();
            view.Configure(label, glyph, fill);
            view.ConfigureArtwork(artwork, new[] { sprite });

            try
            {
                view.Render(0);
                Assert.That(artwork.gameObject.activeSelf, Is.True);
                Assert.That(artwork.enabled, Is.True);
                Assert.That(artwork.sprite, Is.SameAs(sprite));
                Assert.That(glyph.gameObject.activeSelf, Is.False);

                view.Render(40);
                Assert.That(artwork.gameObject.activeSelf, Is.False);
                Assert.That(glyph.gameObject.activeSelf, Is.True);
                Assert.That(glyph.text, Is.EqualTo("[:]"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [UnityTest]
        public IEnumerator ControllerOwnedExpiry_RendersNeutralOutcomeAndLocksPresentation()
        {
            var controller = CreateController(BuildScenario(timerSeconds: 1));
            Assert.That(controller.FinishLaunch(), Is.True);
            Assert.That(controller.StartScenario(), Is.True);
            Assert.That(controller.Continue(), Is.True);
            Assert.That(controller.Continue(), Is.True);
            Assert.That(controller.Continue(), Is.True);
            Assert.That(controller.Continue(), Is.True);
            Assert.That(controller.Snapshot.State, Is.EqualTo(GameState.AwaitingResponse));
            Assert.That(controller.Snapshot.TimerTotalSeconds, Is.EqualTo(1d));

            var root = Track(new GameObject("Pitch Presenter"));
            var presenter = root.AddComponent<PitchRoomPresenter>();
            var prompt = CreateText(root.transform, "Prompt");
            var status = CreateText(root.transform, "Status");
            var continueButton = CreateButton(root.transform, "Continue");
            var responseList = CreateResponseList(root.transform);
            var timer = CreateTimer(root.transform);
            var confidence = CreateConfidence(root.transform);
            SetField(presenter, "promptText", prompt);
            SetField(presenter, "statusText", status);
            SetField(presenter, "responseList", responseList);
            SetField(presenter, "timerView", timer);
            SetField(presenter, "confidenceView", confidence);
            SetField(presenter, "continueButton", continueButton);
            presenter.Initialize(controller, null);
            presenter.Refresh(controller.Snapshot);

            controller.Tick(1d);
            presenter.Refresh(controller.Snapshot);
            yield return null;

            Assert.That(controller.Snapshot.State, Is.EqualTo(GameState.ShowingReaction));
            Assert.That(controller.Snapshot.LastResponseId, Is.Null);
            Assert.That(controller.Snapshot.LastReactionCue, Is.EqualTo("Neutral"));
            Assert.That(controller.Snapshot.TimeoutCount, Is.EqualTo(1));
            Assert.That(timer.DisplayedSeconds, Is.Zero);
            Assert.That(responseList.IsSelectionLocked, Is.True);
            controller.Dispose();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
            foreach (var root in roots)
            {
                if (root != null) UnityEngine.Object.Destroy(root);
            }
            roots.Clear();
            yield return null;
        }

        private ResponseListView CreateResponseList(Transform parent)
        {
            var root = new GameObject("Responses");
            root.transform.SetParent(parent, false);
            var list = root.AddComponent<ResponseListView>();
            var slots = new ResponseButtonView[3];
            for (var index = 0; index < 3; index++)
            {
                var button = CreateButton(root.transform, $"Response {index + 1}");
                slots[index] = button.gameObject.AddComponent<ResponseButtonView>();
                slots[index].Configure(button, button.GetComponentInChildren<Text>());
            }
            list.Configure(slots);
            return list;
        }

        private TimerView CreateTimer(Transform parent)
        {
            var root = new GameObject("Timer", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var view = root.AddComponent<TimerView>();
            var seconds = CreateText(root.transform, "Seconds");
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            fill.transform.SetParent(root.transform, false);
            fill.type = Image.Type.Filled;
            view.Configure(seconds, fill, root.GetComponent<RectTransform>());
            return view;
        }

        private ConfidenceView CreateConfidence(Transform parent)
        {
            var root = new GameObject("Confidence");
            root.transform.SetParent(parent, false);
            var view = root.AddComponent<ConfidenceView>();
            var label = CreateText(root.transform, "Label");
            var icon = CreateText(root.transform, "Icon");
            var fill = new GameObject("Fill", typeof(Image)).GetComponent<Image>();
            fill.transform.SetParent(root.transform, false);
            view.Configure(label, icon, fill);
            return view;
        }

        private static Button CreateButton(Transform parent, string name)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            var label = CreateText(root.transform, "Label");
            label.text = name;
            return root.GetComponent<Button>();
        }

        private static Text CreateText(Transform parent, string name)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return text;
        }

        private static void SetField(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }

        private GameObject Track(GameObject root)
        {
            roots.Add(root);
            return root;
        }

        private static PitchSessionController CreateController(RuntimeScenario scenario)
        {
            var launch = new LmsLaunchConfig
            {
                PseudonymousLearnerId = "test_learner",
                SessionId = "test_session",
                CourseId = "test_course",
                ModuleId = "test_module",
                LessonId = "test_lesson",
                ScenarioId = scenario.Id,
                Language = "en",
                AttemptNumber = 1,
                TimerMode = "Normal",
                ReducedMotion = false,
                MusicVolume = 0.8f,
                SfxVolume = 0.8f,
                ContentVersion = scenario.Version,
                LaunchReference = "lref_pitchTest123",
            };
            return new PitchSessionController(
                scenario,
                new ScoreAccumulator(),
                new AccessibilitySettings(TimerMode.Normal, false, 0.8f, 0.8f, "en"),
                new QuestionTimer(0d),
                new MockLmsBridge(MockLmsBridgeMode.Success, launch),
                () => DateTimeOffset.UtcNow,
                "0.1.0");
        }

        private static RuntimeScenario BuildScenario(int timerSeconds)
        {
            return RuntimeScenario.Compile(new ScenarioDefinitionDto
            {
                Id = "interaction-test",
                Version = 1,
                InitialConfidence = 50,
                OpeningNodeId = "question",
                Nodes = new[]
                {
                    new DialogueNodeDto
                    {
                        Id = "question",
                        NodeType = "Question",
                        TextKey = "question.text",
                        TimerSeconds = timerSeconds,
                        Responses = new[]
                        {
                            Response("first", "First response"),
                            Response("second", "Second response"),
                            Response("third", "Third response"),
                        },
                    },
                    new DialogueNodeDto
                    {
                        Id = "terminal",
                        NodeType = "Terminal",
                        TextKey = "terminal.text",
                        Responses = Array.Empty<ResponseOptionDto>(),
                    },
                },
            });
        }

        private static ResponseOptionDto Response(string id, string text)
        {
            return new ResponseOptionDto
            {
                Id = id,
                TextKey = text,
                QualityTier = id == "second" ? "Developing" : "Strong",
                ScoreDelta = new ResponseScoreDeltaDto(),
                ReactionCue = "Encouraging",
                FeedbackKey = $"feedback.{id}",
                ExplanationKey = $"explanation.{id}",
                NextNodeId = "terminal",
            };
        }
    }
}
