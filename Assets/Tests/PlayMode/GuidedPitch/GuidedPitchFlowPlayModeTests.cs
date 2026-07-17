using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Agrovator.PitchSimulator.Accessibility;
using Agrovator.PitchSimulator.Audio;
using Agrovator.PitchSimulator.Core;
using Agrovator.PitchSimulator.GuidedPitch;
using Agrovator.PitchSimulator.LMS;
using Agrovator.PitchSimulator.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.Tests.PlayMode
{
    internal sealed class GuidedRig
    {
        internal GameObject Root;
        internal GuidedPitchScreenRouter Router;
        internal GuidedPitchPresenter Presenter;
        internal ModeSelectionView ModeSelection;
        internal LearnPitchView Learn;
        internal PitchProgressRailView Rail;
        internal PitchBoardView Board;
        internal SentenceCardListView Cards;
        internal PitchFeedbackView Feedback;
        internal Text QuestionText;
        internal Text HintText;
        internal Text PresentationText;
        internal Button StartButton;
        internal Button SettingsButton;
        internal Button BriefingContinueButton;
        internal Button ContinueButton;
        internal Button PresentButton;
        internal Button[] StrengthenButtons;
        internal Text[] StrengthenLabels;
        internal ScrollRect CardsScroll;
        internal RectTransform CardsViewport;
        internal GameObject TitlePanel;
        internal GameObject BriefingPanel;
        internal GameObject ModeSelectionPanel;
        internal GameObject GuidedPanel;
        internal GameObject ResultsPanel;
        internal GameObject SettingsPanel;
        internal Button SettingsCloseButton;
        internal GameObject SafeFallbackPanel;
        internal Text[] BriefingLineTexts;
        internal SafeFallbackPresenter SafeFallback;
        internal Text SafeFallbackText;
    }

    internal sealed class GuidedContentFixture
    {
        internal GuidedPitchContent Content;
        internal LocalizationCatalog Catalog;
        internal Func<string, string> Localize;

        internal string Sentence(string responseId)
        {
            return Localize(responseId);
        }

        internal GuidedPitchOption Option(LearnerMode mode, PitchPart part, MasteryState mastery)
        {
            return Content.Modes[mode].Parts.Single(candidate => candidate.Part == part)
                .Options.Single(candidate => candidate.Mastery == mastery);
        }

        internal GuidedPitchOption FollowUpOption(LearnerMode mode, MasteryState mastery)
        {
            return Content.Modes[mode].FollowUp.Options.Single(candidate => candidate.Mastery == mastery);
        }
    }

    internal static class GuidedRigFactory
    {
        internal static readonly string[] BriefingLineKeys =
        {
            "guided.title",
            "guided.briefing",
            "guided.briefing.judge",
            "guided.briefing.practice",
            "guided.briefing.untimed",
        };

        internal static GuidedContentFixture LoadAuthoredContent()
        {
            var catalog = LocalizationCatalog.Load(
                ReadProjectFile("Content", "Localization", "en.json"),
                ReadProjectFile("Content", "Localization", "ms.json"));
            var result = GuidedPitchContentLoader.Load(
                ReadProjectFile("Content", "Scenarios", "guided-pitch-builder.en.json"),
                catalog.GetKeys("en"));
            Assert.That(result.IsSuccess, Is.True,
                string.Join(", ", result.Issues.Select(issue => issue.Code + "@" + issue.Path)));

            var textKeysByResponseId = result.Content.Modes.Values
                .SelectMany(mode => mode.Parts.SelectMany(part => part.Options).Concat(mode.FollowUp.Options))
                .GroupBy(option => option.Id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().TextKey, StringComparer.Ordinal);
            Func<string, string> localize = key => textKeysByResponseId.TryGetValue(key, out var textKey)
                ? catalog.Resolve("en", textKey)
                : catalog.Resolve("en", key);
            return new GuidedContentFixture
            {
                Content = result.Content,
                Catalog = catalog,
                Localize = localize,
            };
        }

        internal static LmsLaunchConfig CreateLaunch(GuidedPitchContent content)
        {
            return new LmsLaunchConfig
            {
                PseudonymousLearnerId = "local_learner",
                SessionId = "local_session",
                CourseId = "local_course",
                ModuleId = "local_module",
                LessonId = "local_lesson",
                ScenarioId = content.Id,
                Language = "en",
                AttemptNumber = 1,
                TimerMode = "Normal",
                ReducedMotion = false,
                MusicVolume = 0.8f,
                SfxVolume = 0.8f,
                ContentVersion = content.Version,
                LaunchReference = "lref_guidedFlow01",
            };
        }

        internal static GuidedPitchSessionController CreateController(
            GuidedContentFixture fixture,
            ILmsBridge bridge)
        {
            return new GuidedPitchSessionController(
                fixture.Content,
                new AccessibilitySettings(TimerMode.Normal, false, 0.8f, 0.8f, "en"),
                bridge,
                () => new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero),
                "guided-test");
        }

        internal static void EnsureEventSystem(List<GameObject> roots)
        {
            if (EventSystem.current == null)
            {
                var root = new GameObject("Guided Event System", typeof(EventSystem));
                roots.Add(root);
            }
        }

        internal static GuidedRig CreateRig(List<GameObject> roots)
        {
            EnsureEventSystem(roots);
            var rig = new GuidedRig();
            rig.Root = new GameObject("Guided Canvas", typeof(RectTransform), typeof(Canvas));
            roots.Add(rig.Root);
            var canvasTransform = rig.Root.transform;

            rig.TitlePanel = Panel("Title", canvasTransform);
            var titlePresenter = rig.TitlePanel.AddComponent<TitlePresenter>();
            rig.StartButton = CreateButton("Start Button", rig.TitlePanel.transform);
            rig.SettingsButton = CreateButton("Settings Button", rig.TitlePanel.transform);
            SetField(titlePresenter, "startButton", rig.StartButton);
            SetField(titlePresenter, "settingsButton", rig.SettingsButton);

            rig.BriefingPanel = Panel("Briefing", canvasTransform);
            var briefingPresenter = rig.BriefingPanel.AddComponent<BriefingPresenter>();
            rig.BriefingContinueButton = CreateButton("Briefing Continue", rig.BriefingPanel.transform);
            rig.BriefingLineTexts = BriefingLineKeys
                .Select((key, index) => CreateText("Briefing Line " + index, rig.BriefingPanel.transform))
                .ToArray();
            SetField(briefingPresenter, "continueButton", rig.BriefingContinueButton);
            SetField(briefingPresenter, "lineTexts", rig.BriefingLineTexts);
            SetField(briefingPresenter, "lineKeys", (string[])BriefingLineKeys.Clone());

            rig.ModeSelectionPanel = Panel("Mode Selection", canvasTransform);
            rig.ModeSelection = rig.ModeSelectionPanel.AddComponent<ModeSelectionView>();
            rig.ModeSelection.Configure(new[]
            {
                CreateModeCard(LearnerMode.Primary, rig.ModeSelectionPanel.transform),
                CreateModeCard(LearnerMode.Secondary, rig.ModeSelectionPanel.transform),
            });

            rig.GuidedPanel = Panel("Guided Pitch", canvasTransform);
            var guidedTransform = rig.GuidedPanel.transform;
            rig.Presenter = rig.GuidedPanel.AddComponent<GuidedPitchPresenter>();

            var learnRoot = Panel("Learn", guidedTransform);
            rig.Learn = learnRoot.AddComponent<LearnPitchView>();
            rig.Learn.Configure(
                CreateText("Incomplete Pitch", learnRoot.transform),
                CreateText("Explanation", learnRoot.transform));

            rig.Rail = CreateRail(guidedTransform);
            rig.Board = CreateBoard(guidedTransform);

            rig.StrengthenButtons = new Button[4];
            rig.StrengthenLabels = new Text[4];
            for (var index = 0; index < 4; index++)
            {
                rig.StrengthenButtons[index] = CreateButton("Strengthen " + (PitchPart)index, guidedTransform);
                rig.StrengthenLabels[index] =
                    rig.StrengthenButtons[index].GetComponentInChildren<Text>(true);
            }

            rig.QuestionText = CreateText("Question", guidedTransform);
            rig.HintText = CreateText("Hint", guidedTransform);
            rig.PresentationText = CreateText("Presentation", guidedTransform);

            var scrollRoot = Panel("Cards Scroll", guidedTransform);
            rig.CardsScroll = scrollRoot.AddComponent<ScrollRect>();
            var viewport = Panel("Viewport", scrollRoot.transform);
            rig.CardsViewport = viewport.GetComponent<RectTransform>();
            var content = Panel("Cards Content", viewport.transform);
            var contentRect = content.GetComponent<RectTransform>();
            rig.CardsScroll.viewport = rig.CardsViewport;
            rig.CardsScroll.content = contentRect;
            rig.CardsScroll.horizontal = false;
            rig.Cards = content.AddComponent<SentenceCardListView>();
            rig.Cards.Configure(Enumerable.Range(0, 3)
                .Select(index => CreateSentenceCard("Card " + index, content.transform, index))
                .ToArray());
            SizeScrollRig(rig.CardsViewport, contentRect);

            rig.PresentButton = CreateButton("Present Button", guidedTransform);
            rig.ContinueButton = CreateButton("Continue Button", guidedTransform);

            var feedbackRoot = Panel("Feedback", guidedTransform);
            rig.Feedback = feedbackRoot.AddComponent<PitchFeedbackView>();
            rig.Feedback.Configure(Enumerable.Range(0, 3).Select(index =>
            {
                var row = Panel("Row " + index, feedbackRoot.transform);
                return new PitchFeedbackRow(row,
                    CreateText("Label", row.transform),
                    CreateText("Value", row.transform));
            }).ToArray());

            rig.Presenter.Configure(
                rig.ModeSelection,
                rig.Learn,
                rig.Rail,
                rig.Board,
                rig.Cards,
                rig.Feedback,
                rig.QuestionText,
                rig.HintText,
                rig.PresentationText,
                rig.ContinueButton,
                rig.PresentButton,
                rig.StrengthenButtons,
                rig.StrengthenLabels,
                rig.CardsScroll);

            rig.ResultsPanel = Panel("Results", canvasTransform);
            var resultsDefault = CreateButton("Results Placeholder", rig.ResultsPanel.transform);

            rig.SettingsPanel = Panel("Settings", canvasTransform);
            var settingsPresenter = rig.SettingsPanel.AddComponent<SettingsPresenter>();
            var closeButton = CreateButton("Close Button", rig.SettingsPanel.transform);
            SetField(settingsPresenter, "closeButton", closeButton);
            rig.SettingsCloseButton = closeButton;

            rig.SafeFallbackPanel = Panel("Safe Fallback", canvasTransform);
            rig.SafeFallback = rig.SafeFallbackPanel.AddComponent<SafeFallbackPresenter>();
            rig.SafeFallbackText = CreateText("Recovery Message", rig.SafeFallbackPanel.transform);
            rig.SafeFallback.Configure(rig.SafeFallbackText);

            rig.Router = rig.Root.AddComponent<GuidedPitchScreenRouter>();
            rig.Router.Configure(
                rig.TitlePanel,
                rig.BriefingPanel,
                rig.ModeSelectionPanel,
                rig.GuidedPanel,
                rig.ResultsPanel,
                rig.SettingsPanel,
                rig.SafeFallbackPanel,
                titlePresenter,
                briefingPresenter,
                rig.Presenter,
                settingsPresenter,
                rig.SafeFallback,
                rig.StartButton,
                rig.BriefingContinueButton,
                resultsDefault,
                closeButton);
            return rig;
        }

        internal static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing serialized field '{name}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        internal static string ReadProjectFile(params string[] segments)
        {
            return File.ReadAllText(segments.Aggregate(Application.dataPath, Path.Combine));
        }

        private static ModeSelectionCard CreateModeCard(LearnerMode mode, Transform parent)
        {
            var root = Panel(mode + " Card", parent);
            root.AddComponent<CanvasRenderer>();
            var background = root.AddComponent<Image>();
            var button = root.AddComponent<Button>();
            var title = CreateText("Title", root.transform);
            var description = CreateText("Description", root.transform);
            return new ModeSelectionCard(mode, button, title, description, background);
        }

        private static PitchProgressRailView CreateRail(Transform parent)
        {
            var root = Panel("Rail", parent);
            var view = root.AddComponent<PitchProgressRailView>();
            view.Configure(PitchParts.Ordered.Select(part =>
            {
                var slotRoot = Panel(part + " Rail Slot", root.transform);
                return new PitchProgressRailSlot(part, slotRoot,
                    CreateText("Label", slotRoot.transform),
                    CreateText("Icon", slotRoot.transform),
                    CreateImage("Accent", slotRoot.transform),
                    Panel("Current", slotRoot.transform));
            }).ToArray());
            return view;
        }

        private static PitchBoardView CreateBoard(Transform parent)
        {
            var root = Panel("Board", parent);
            var view = root.AddComponent<PitchBoardView>();
            view.Configure(PitchParts.Ordered.Select(part =>
            {
                var slotRoot = Panel(part + " Board Slot", root.transform);
                return new PitchBoardSlot(part, slotRoot,
                    CreateText("Label", slotRoot.transform),
                    CreateText("Icon", slotRoot.transform),
                    CreateImage("Accent", slotRoot.transform),
                    CreateText("Sentence", slotRoot.transform),
                    CreateText("Empty Prompt", slotRoot.transform),
                    CreateImage("Revision", slotRoot.transform));
            }).ToArray());
            return view;
        }

        private static SentenceCardView CreateSentenceCard(string name, Transform parent, int index)
        {
            var root = Panel(name, parent);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 96f);
            rect.anchoredPosition = new Vector2(0f, -96f * index);
            root.AddComponent<CanvasRenderer>();
            var background = root.AddComponent<Image>();
            var button = root.AddComponent<Button>();
            var label = CreateText("Label", root.transform);
            var focus = CreateImage("Focus", root.transform);
            var card = root.AddComponent<SentenceCardView>();
            card.Configure(button, label, background, focus);
            return card;
        }

        private static void SizeScrollRig(RectTransform viewport, RectTransform content)
        {
            viewport.anchorMin = new Vector2(0f, 1f);
            viewport.anchorMax = new Vector2(0f, 1f);
            viewport.pivot = new Vector2(0.5f, 1f);
            viewport.sizeDelta = new Vector2(320f, 160f);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, 288f);
            content.anchoredPosition = Vector2.zero;
        }

        private static GameObject Panel(string name, Transform parent)
        {
            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            return panel;
        }

        internal static Button CreateButton(string name, Transform parent)
        {
            var root = Panel(name, parent);
            root.AddComponent<CanvasRenderer>();
            var image = root.AddComponent<Image>();
            var button = root.AddComponent<Button>();
            button.targetGraphic = image;
            CreateText("Label", root.transform);
            return button;
        }

        internal static Text CreateText(string name, Transform parent)
        {
            var root = Panel(name, parent);
            root.AddComponent<CanvasRenderer>();
            return root.AddComponent<Text>();
        }

        internal static Image CreateImage(string name, Transform parent)
        {
            var root = Panel(name, parent);
            root.AddComponent<CanvasRenderer>();
            return root.AddComponent<Image>();
        }
    }

    public sealed class GuidedPitchFlowPlayModeTests
    {
        private readonly List<GameObject> roots = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var root in roots.Where(root => root != null))
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            roots.Clear();
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        [Test]
        public void GuidedFlow_PrimaryPath_CoversEveryVisibleCheckpointFromBriefingToResults()
        {
            var fixture = GuidedRigFactory.LoadAuthoredContent();
            var bridge = new MockLmsBridge(
                MockLmsBridgeMode.Success, GuidedRigFactory.CreateLaunch(fixture.Content));
            var controller = GuidedRigFactory.CreateController(fixture, bridge);
            var playedCues = new List<AudioCue>();
            var audioDirector = new AudioCueDirector(playedCues.Add);
            controller.EventPublished += audioDirector.HandleGuidedSessionEvent;
            Assert.That(controller.FinishLaunch(), Is.True);
            var rig = GuidedRigFactory.CreateRig(roots);
            rig.Router.Initialize(controller, fixture.Localize);

            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Title));
            Assert.That(rig.TitlePanel.activeSelf, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.SameAs(rig.StartButton.gameObject));

            rig.StartButton.onClick.Invoke();

            // Checkpoint 1: briefing identifies Judge Aya, the Smart School Garden,
            // individual practice for the team pitch, and no timer.
            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Briefing));
            var briefingCopy = string.Join(" ", rig.BriefingLineTexts.Select(line => line.text));
            Assert.That(briefingCopy, Does.Contain("Judge Aya"));
            Assert.That(briefingCopy, Does.Contain("Smart School Garden"));
            Assert.That(briefingCopy, Does.Contain("individual practice"));
            Assert.That(briefingCopy, Does.Contain("team"));
            Assert.That(briefingCopy, Does.Contain("no timer"));
            Assert.That(briefingCopy, Does.Not.Contain("[[missing:"));
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.SameAs(rig.BriefingContinueButton.gameObject));

            rig.BriefingContinueButton.onClick.Invoke();

            // Checkpoint 2: exactly two equally styled Primary and Secondary cards.
            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.ModeSelection));
            Assert.That(rig.ModeSelectionPanel.activeSelf, Is.True);
            Assert.That(rig.ModeSelection.Cards.Count, Is.EqualTo(2));
            Assert.That(rig.ModeSelection.Cards.Select(card => card.TitleText.text),
                Is.EqualTo(new[] { "Primary", "Secondary" }));
            Assert.That(rig.ModeSelection.Cards.All(card => card.Button.interactable), Is.True);
            Assert.That(rig.ModeSelection.Cards.Select(card => card.Background.color).Distinct().Count(),
                Is.EqualTo(1));
            Assert.That(rig.ModeSelection.Cards[0].Background.color,
                Is.EqualTo(PitchPartVisuals.CardCream));
            var launchFieldNames = typeof(LmsLaunchConfig).GetFields()
                .Select(field => field.Name.ToLowerInvariant()).ToArray();
            Assert.That(launchFieldNames, Has.None.Contains("category"));
            Assert.That(launchFieldNames, Has.None.Contains("learnermode"));
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.SameAs(rig.ModeSelection.Cards[0].Button.gameObject));

            rig.ModeSelection.Cards[0].Button.onClick.Invoke();

            // Checkpoint 4: Learn shows an incomplete pitch example, four persistent
            // part visuals, and a single Continue action.
            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Learn));
            Assert.That(rig.GuidedPanel.activeSelf, Is.True);
            Assert.That(rig.Learn.gameObject.activeInHierarchy, Is.True);
            Assert.That(rig.Learn.IncompletePitchText.text,
                Is.EqualTo(fixture.Catalog.Resolve("en", "guided.learn.incomplete_pitch")));
            Assert.That(rig.Learn.ExplanationText.text,
                Is.EqualTo(fixture.Catalog.Resolve("en", "guided.learn.explanation")));
            Assert.That(rig.Rail.Slots.All(slot => slot.Root.activeInHierarchy), Is.True);
            Assert.That(rig.Rail.Slots.Select(slot => slot.Label), Is.EqualTo(new[]
            {
                "Problem / Spot it",
                "Evidence / Prove it",
                "Solution / Solve it",
                "Value / Show why it matters",
            }));
            var learnSelectables = rig.GuidedPanel.GetComponentsInChildren<Selectable>(false)
                .Where(selectable => selectable.IsInteractable()).ToArray();
            Assert.That(learnSelectables, Has.Length.EqualTo(1));
            Assert.That(learnSelectables[0], Is.SameAs((Selectable)rig.ContinueButton));
            Assert.That(rig.GuidedPanel.GetComponentInChildren<TimerView>(true), Is.Null);
            Assert.That(rig.GuidedPanel.GetComponentInChildren<ConfidenceView>(true), Is.Null);

            rig.ContinueButton.onClick.Invoke();

            // Checkpoint 5: Build asks the active part question and shows exactly
            // three localized cards; a selection fills the matching board slot.
            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Build));
            Assert.That(rig.QuestionText.text,
                Is.EqualTo("What happens in the garden, and why does it matter?"));
            Assert.That(rig.Cards.Cards.Count(card => card.gameObject.activeSelf), Is.EqualTo(3));
            var problemOptions = controller.Snapshot.AvailableOptions;
            Assert.That(rig.Cards.Cards.Select(card => card.Label.text),
                Is.EquivalentTo(problemOptions.Select(option =>
                    fixture.Catalog.Resolve("en", option.TextKey))));
            Assert.That(rig.Cards.Cards.Select(card => card.Label.text),
                Has.Some.EqualTo("Our garden beds get too dry because we water them at the wrong times."));
            Assert.That(rig.Board.Slots[0].EmptyPromptText.text, Does.Not.Contain("[[missing:"));
            Assert.That(rig.PresentButton.gameObject.activeInHierarchy, Is.False);

            var developingProblem = fixture.Option(
                LearnerMode.Primary, PitchPart.Problem, MasteryState.Developing);
            ClickCard(rig, developingProblem.Id);

            // Checkpoint 6: Build feedback names worked/missing/improve rows.
            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.BuildFeedback));
            Assert.That(rig.Board.Slots[0].SentenceText.text,
                Is.EqualTo(fixture.Sentence(developingProblem.Id)));
            Assert.That(rig.Feedback.Rows.Select(row => row.LabelText.text),
                Is.EqualTo(new[] { "What worked", "What is missing", "How to improve" }));
            Assert.That(rig.Feedback.Rows.Select(row => row.ValueText.text), Is.EqualTo(new[]
            {
                fixture.Catalog.Resolve("en", developingProblem.Feedback.WorkedKey),
                fixture.Catalog.Resolve("en", developingProblem.Feedback.MissingKey),
                fixture.Catalog.Resolve("en", developingProblem.Feedback.ImproveKey),
            }));
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.SameAs(rig.ContinueButton.gameObject));

            rig.ContinueButton.onClick.Invoke();
            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Build));
            Assert.That(rig.QuestionText.text,
                Is.EqualTo(fixture.Catalog.Resolve("en", "guided.part.evidence.question")));
            Assert.That(rig.PresentButton.gameObject.activeInHierarchy, Is.False,
                "Present must stay unavailable until all four parts exist.");

            foreach (var part in new[] { PitchPart.Evidence, PitchPart.Solution, PitchPart.Value })
            {
                var clearOption = fixture.Option(LearnerMode.Primary, part, MasteryState.Clear);
                ClickCard(rig, clearOption.Id);
                Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.BuildFeedback));
                Assert.That(rig.Board.Slots[(int)part].SentenceText.text,
                    Is.EqualTo(fixture.Sentence(clearOption.Id)));
                rig.ContinueButton.onClick.Invoke();
            }

            // Checkpoint 7: Improve labels the weak part as an opportunity to
            // strengthen and revises the same slot without changing its initial ID.
            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Improve));
            Assert.That(rig.QuestionText.text, Does.Contain("opportunities to strengthen"));
            Assert.That(rig.StrengthenButtons[0].gameObject.activeInHierarchy, Is.True);
            Assert.That(rig.StrengthenLabels[0].text, Is.EqualTo("Developing"));
            Assert.That(rig.StrengthenButtons.Skip(1).All(button => !button.gameObject.activeInHierarchy),
                Is.True);
            Assert.That(rig.PresentButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(rig.PresentButton.interactable, Is.True);

            rig.StrengthenButtons[0].onClick.Invoke();
            Assert.That(rig.HintText.text,
                Is.EqualTo(fixture.Catalog.Resolve("en", "guided.part.problem.hint")));
            Assert.That(rig.Cards.Cards.Count(card => card.gameObject.activeSelf), Is.EqualTo(3));
            Assert.That(rig.Board.Slots[0].IsRevisionSelected, Is.True);

            var clearProblem = fixture.Option(LearnerMode.Primary, PitchPart.Problem, MasteryState.Clear);
            ClickCard(rig, clearProblem.Id);
            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Improve));
            Assert.That(rig.Board.Slots[0].SentenceText.text,
                Is.EqualTo(fixture.Sentence(clearProblem.Id)));
            Assert.That(controller.Snapshot.Draft[PitchPart.Problem].InitialResponseId,
                Is.EqualTo(developingProblem.Id));
            Assert.That(controller.Snapshot.Draft[PitchPart.Problem].CurrentResponseId,
                Is.EqualTo(clearProblem.Id));
            Assert.That(controller.Snapshot.Draft[PitchPart.Problem].WasRevised, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.SameAs(rig.PresentButton.gameObject),
                "Focus must recover to an active selectable after the revised card list hides.");

            // Checkpoint 8: Present renders four readable sentences in order.
            rig.PresentButton.onClick.Invoke();
            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Present));
            var presentation = rig.PresentationText.text;
            var orderedSentences = PitchParts.Ordered
                .Select(part => fixture.Sentence(controller.Snapshot.Draft[part].CurrentResponseId))
                .ToArray();
            var lastIndex = -1;
            foreach (var sentence in orderedSentences)
            {
                var position = presentation.IndexOf(sentence, StringComparison.Ordinal);
                Assert.That(position, Is.GreaterThan(lastIndex), sentence);
                lastIndex = position;
            }
            Assert.That(presentation, Does.Not.Contain("[[missing:"));

            rig.ContinueButton.onClick.Invoke();

            // Checkpoint 9: Follow-up asks the localized cost question, accepts one
            // card, shows coaching, and exposes Results only after review.
            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.FollowUp));
            Assert.That(rig.QuestionText.text, Is.EqualTo(
                "What will it cost, and what will you do if you do not know the final amount yet?"));
            Assert.That(rig.Cards.Cards.Count(card => card.gameObject.activeSelf), Is.EqualTo(3));

            var honestCost = fixture.FollowUpOption(LearnerMode.Primary, MasteryState.Clear);
            ClickCard(rig, honestCost.Id);
            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.FollowUpFeedback));
            Assert.That(rig.Feedback.Rows.Select(row => row.ValueText.text), Is.EqualTo(new[]
            {
                fixture.Catalog.Resolve("en", honestCost.Feedback.WorkedKey),
                fixture.Catalog.Resolve("en", honestCost.Feedback.MissingKey),
                fixture.Catalog.Resolve("en", honestCost.Feedback.ImproveKey),
            }));
            Assert.That(rig.ResultsPanel.activeSelf, Is.False,
                "Results must stay hidden until the follow-up coaching is reviewed.");

            rig.ContinueButton.onClick.Invoke();
            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Results));
            Assert.That(rig.ResultsPanel.activeSelf, Is.True);
            Assert.That(rig.GuidedPanel.activeSelf, Is.False);

            Assert.That(playedCues, Has.Some.EqualTo(AudioCue.ResponseSelected));
            Assert.That(playedCues, Has.Some.EqualTo(AudioCue.JudgeReaction));
            Assert.That(playedCues, Has.Some.EqualTo(AudioCue.FeedbackOpen));
            Assert.That(playedCues, Has.Some.EqualTo(AudioCue.ResultsReveal));
            Assert.That(playedCues, Has.None.EqualTo(AudioCue.TimerWarning),
                "The guided path must never play the timer warning cue.");
            controller.Dispose();
        }

        [Test]
        public void GuidedFlow_SecondaryMode_UsesSameMechanicsAndVisualTheme()
        {
            var fixture = GuidedRigFactory.LoadAuthoredContent();
            var bridge = new MockLmsBridge(
                MockLmsBridgeMode.Success, GuidedRigFactory.CreateLaunch(fixture.Content));
            var controller = GuidedRigFactory.CreateController(fixture, bridge);
            Assert.That(controller.FinishLaunch(), Is.True);
            var rig = GuidedRigFactory.CreateRig(roots);
            rig.Router.Initialize(controller, fixture.Localize);
            rig.StartButton.onClick.Invoke();
            rig.BriefingContinueButton.onClick.Invoke();

            // Checkpoint 3: the two cards name the mode-specific part framings.
            Assert.That(rig.ModeSelection.Cards[0].DescriptionText.text,
                Is.EqualTo("Spot it, Prove it, Solve it, Show why it matters"));
            Assert.That(rig.ModeSelection.Cards[1].DescriptionText.text,
                Is.EqualTo("Problem, Evidence, Solution, Value"));

            rig.ModeSelection.Cards[1].Button.onClick.Invoke();
            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Learn));
            rig.ContinueButton.onClick.Invoke();

            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Build));
            Assert.That(rig.QuestionText.text,
                Is.EqualTo(fixture.Catalog.Resolve("en", "guided.part.problem.question")));
            var secondaryOptions = controller.Snapshot.AvailableOptions;
            Assert.That(rig.Cards.Cards.Select(card => card.Label.text),
                Is.EquivalentTo(secondaryOptions.Select(option =>
                    fixture.Catalog.Resolve("en", option.TextKey))));
            Assert.That(rig.Cards.Cards.All(card =>
                card.Background.color == PitchPartVisuals.CardCream), Is.True);
            Assert.That(rig.Rail.Slots.Select(slot => slot.Label), Is.EqualTo(new[]
            {
                "Problem / Spot it",
                "Evidence / Prove it",
                "Solution / Solve it",
                "Value / Show why it matters",
            }));

            var clearSecondary = fixture.Option(
                LearnerMode.Secondary, PitchPart.Problem, MasteryState.Clear);
            ClickCard(rig, clearSecondary.Id);
            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.BuildFeedback));
            Assert.That(rig.Board.Slots[0].SentenceText.text,
                Is.EqualTo(fixture.Sentence(clearSecondary.Id)));
            controller.Dispose();
        }

        [Test]
        public void Keyboard_ArrowsEnterAndFocusRestoration_DriveTheBuildLoop()
        {
            var fixture = GuidedRigFactory.LoadAuthoredContent();
            var bridge = new MockLmsBridge(
                MockLmsBridgeMode.Success, GuidedRigFactory.CreateLaunch(fixture.Content));
            var controller = GuidedRigFactory.CreateController(fixture, bridge);
            Assert.That(controller.FinishLaunch(), Is.True);
            var rig = GuidedRigFactory.CreateRig(roots);
            rig.Router.Initialize(controller, fixture.Localize);
            rig.StartButton.onClick.Invoke();
            rig.BriefingContinueButton.onClick.Invoke();
            rig.ModeSelection.Cards[0].Button.onClick.Invoke();
            rig.ContinueButton.onClick.Invoke();
            var eventSystem = EventSystem.current;

            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Build));
            Assert.That(eventSystem.currentSelectedGameObject,
                Is.SameAs(rig.Cards.Cards[0].Button.gameObject));
            Assert.That(rig.Cards.Cards[0].State, Is.EqualTo(SentenceCardVisualState.KeyboardFocus),
                "The focused card must show a visible keyboard-focus state.");

            Move(eventSystem, MoveDirection.Down);
            Assert.That(eventSystem.currentSelectedGameObject,
                Is.SameAs(rig.Cards.Cards[1].Button.gameObject));
            Assert.That(rig.Cards.Cards[1].State, Is.EqualTo(SentenceCardVisualState.KeyboardFocus));
            Move(eventSystem, MoveDirection.Down);
            Assert.That(eventSystem.currentSelectedGameObject,
                Is.SameAs(rig.Cards.Cards[2].Button.gameObject));
            Move(eventSystem, MoveDirection.Down);
            Assert.That(eventSystem.currentSelectedGameObject,
                Is.SameAs(rig.Cards.Cards[2].Button.gameObject),
                "Arrow navigation must stop at the final card.");
            Move(eventSystem, MoveDirection.Up);
            Move(eventSystem, MoveDirection.Up);
            Assert.That(eventSystem.currentSelectedGameObject,
                Is.SameAs(rig.Cards.Cards[0].Button.gameObject));

            Submit(eventSystem, eventSystem.currentSelectedGameObject);
            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.BuildFeedback));
            Assert.That(eventSystem.currentSelectedGameObject,
                Is.SameAs(rig.ContinueButton.gameObject),
                "Focus must be restored to Continue after the phase change.");

            Submit(eventSystem, eventSystem.currentSelectedGameObject);
            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Build));
            Assert.That(eventSystem.currentSelectedGameObject,
                Is.SameAs(rig.Cards.Cards[0].Button.gameObject),
                "Focus must return to the first card for the next Build round.");
            controller.Dispose();
        }

        [Test]
        public void Keyboard_TabAndShiftTab_CycleTheActiveGuidedSelectables()
        {
            var fixture = GuidedRigFactory.LoadAuthoredContent();
            var bridge = new MockLmsBridge(
                MockLmsBridgeMode.Success, GuidedRigFactory.CreateLaunch(fixture.Content));
            var controller = GuidedRigFactory.CreateController(fixture, bridge);
            Assert.That(controller.FinishLaunch(), Is.True);
            var rig = GuidedRigFactory.CreateRig(roots);
            rig.Router.Initialize(controller, fixture.Localize);
            rig.StartButton.onClick.Invoke();
            rig.BriefingContinueButton.onClick.Invoke();
            rig.ModeSelection.Cards[0].Button.onClick.Invoke();
            rig.ContinueButton.onClick.Invoke();
            ClickCard(rig, fixture.Option(
                LearnerMode.Primary, PitchPart.Problem, MasteryState.Developing).Id);
            rig.ContinueButton.onClick.Invoke();
            foreach (var part in new[] { PitchPart.Evidence, PitchPart.Solution, PitchPart.Value })
            {
                ClickCard(rig, fixture.Option(LearnerMode.Primary, part, MasteryState.Clear).Id);
                rig.ContinueButton.onClick.Invoke();
            }

            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Improve));
            var eventSystem = EventSystem.current;
            Assert.That(eventSystem.currentSelectedGameObject,
                Is.SameAs(rig.StrengthenButtons[0].gameObject));

            Assert.That(rig.Presenter.MoveFocus(false), Is.True);
            Assert.That(eventSystem.currentSelectedGameObject,
                Is.SameAs(rig.PresentButton.gameObject));
            Assert.That(rig.Presenter.MoveFocus(false), Is.True);
            Assert.That(eventSystem.currentSelectedGameObject,
                Is.SameAs(rig.StrengthenButtons[0].gameObject),
                "Tab must wrap back to the first active selectable.");
            Assert.That(rig.Presenter.MoveFocus(true), Is.True);
            Assert.That(eventSystem.currentSelectedGameObject,
                Is.SameAs(rig.PresentButton.gameObject),
                "Shift+Tab must move backward through the cycle.");
            controller.Dispose();
        }

        [Test]
        public void Keyboard_RouterTab_CyclesModeCardsWhileTheGuidedPresenterPanelIsInactive()
        {
            var fixture = GuidedRigFactory.LoadAuthoredContent();
            var bridge = new MockLmsBridge(
                MockLmsBridgeMode.Success, GuidedRigFactory.CreateLaunch(fixture.Content));
            var controller = GuidedRigFactory.CreateController(fixture, bridge);
            Assert.That(controller.FinishLaunch(), Is.True);
            var rig = GuidedRigFactory.CreateRig(roots);
            rig.Router.Initialize(controller, fixture.Localize);
            rig.StartButton.onClick.Invoke();
            rig.BriefingContinueButton.onClick.Invoke();

            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.ModeSelection));
            Assert.That(rig.ModeSelectionPanel.activeSelf, Is.True);
            Assert.That(rig.GuidedPanel.activeSelf, Is.False);
            Assert.That(rig.Presenter.gameObject.activeInHierarchy, Is.False,
                "The guided presenter must be inactive here, so the always-active router owns Tab.");
            var eventSystem = EventSystem.current;
            Assert.That(eventSystem.currentSelectedGameObject,
                Is.SameAs(rig.ModeSelection.Cards[0].Button.gameObject));

            Assert.That(rig.Router.MoveFocus(false), Is.True);
            Assert.That(eventSystem.currentSelectedGameObject,
                Is.SameAs(rig.ModeSelection.Cards[1].Button.gameObject));
            Assert.That(rig.Router.MoveFocus(false), Is.True);
            Assert.That(eventSystem.currentSelectedGameObject,
                Is.SameAs(rig.ModeSelection.Cards[0].Button.gameObject),
                "Tab must wrap back to the first mode card.");
            Assert.That(rig.Router.MoveFocus(true), Is.True);
            Assert.That(eventSystem.currentSelectedGameObject,
                Is.SameAs(rig.ModeSelection.Cards[1].Button.gameObject),
                "Shift+Tab must move backward through the mode cards.");
            controller.Dispose();
        }

        [Test]
        public void Settings_StaysOpenWhenAnAsyncRefreshChangesPhase_ThenClosesOntoTheNewPanel()
        {
            var fixture = GuidedRigFactory.LoadAuthoredContent();
            var bridge = new MockLmsBridge(
                MockLmsBridgeMode.Success, GuidedRigFactory.CreateLaunch(fixture.Content));
            var controller = GuidedRigFactory.CreateController(fixture, bridge);
            Assert.That(controller.FinishLaunch(), Is.True);
            var rig = GuidedRigFactory.CreateRig(roots);
            rig.Router.Initialize(controller, fixture.Localize);
            Assert.That(rig.TitlePanel.activeSelf, Is.True);

            rig.SettingsButton.onClick.Invoke();
            Assert.That(rig.SettingsPanel.activeSelf, Is.True);
            Assert.That(rig.TitlePanel.activeSelf, Is.False);

            Assert.That(controller.StartScenario(), Is.True);
            rig.Router.Refresh();

            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Briefing));
            Assert.That(rig.SettingsPanel.activeSelf, Is.True,
                "An asynchronous session refresh must not close the Settings overlay.");
            Assert.That(rig.BriefingPanel.activeSelf, Is.False,
                "The new phase panel must wait behind the open Settings overlay.");

            rig.SettingsCloseButton.onClick.Invoke();
            Assert.That(rig.SettingsPanel.activeSelf, Is.False);
            Assert.That(rig.BriefingPanel.activeSelf, Is.True,
                "Closing Settings must land on the phase reached while the overlay was open.");
            Assert.That(rig.TitlePanel.activeSelf, Is.False);
            controller.Dispose();
        }

        [UnityTest]
        public IEnumerator ConstrainedLayout_KeepsTheFocusedCardVisibleInsideTheGuidedScrollRect()
        {
            var fixture = GuidedRigFactory.LoadAuthoredContent();
            var bridge = new MockLmsBridge(
                MockLmsBridgeMode.Success, GuidedRigFactory.CreateLaunch(fixture.Content));
            var controller = GuidedRigFactory.CreateController(fixture, bridge);
            Assert.That(controller.FinishLaunch(), Is.True);
            var rig = GuidedRigFactory.CreateRig(roots);
            rig.Router.Initialize(controller, fixture.Localize);
            rig.StartButton.onClick.Invoke();
            rig.BriefingContinueButton.onClick.Invoke();
            rig.ModeSelection.Cards[0].Button.onClick.Invoke();
            rig.ContinueButton.onClick.Invoke();
            Assert.That(rig.Router.ActivePhase, Is.EqualTo(GuidedPitchPhase.Build));

            var lastCard = (RectTransform)rig.Cards.Cards[2].transform;
            Assert.That(CardIsFullyVisible(rig.CardsViewport, lastCard), Is.False,
                "The rig must start with the last card outside the constrained viewport.");

            EventSystem.current.SetSelectedGameObject(lastCard.gameObject);
            yield return null;
            Assert.That(CardIsFullyVisible(rig.CardsViewport, lastCard), Is.True,
                "Focusing the last card must scroll it into the guided ScrollRect viewport.");

            var firstCard = (RectTransform)rig.Cards.Cards[0].transform;
            EventSystem.current.SetSelectedGameObject(firstCard.gameObject);
            yield return null;
            Assert.That(CardIsFullyVisible(rig.CardsViewport, firstCard), Is.True,
                "Focusing the first card must scroll back up.");
            controller.Dispose();
        }

        private static bool CardIsFullyVisible(RectTransform viewport, RectTransform card)
        {
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, card);
            var rect = viewport.rect;
            return bounds.min.y >= rect.yMin - 0.5f && bounds.max.y <= rect.yMax + 0.5f;
        }

        private static void ClickCard(GuidedRig rig, string responseId)
        {
            var card = rig.Cards.Cards.Single(candidate =>
                candidate.gameObject.activeSelf && candidate.ResponseId == responseId);
            card.Button.onClick.Invoke();
        }

        private static void Move(EventSystem eventSystem, MoveDirection direction)
        {
            var data = new AxisEventData(eventSystem)
            {
                moveDir = direction,
                moveVector = direction == MoveDirection.Down ? Vector2.down : Vector2.up,
            };
            ExecuteEvents.Execute(
                eventSystem.currentSelectedGameObject, data, ExecuteEvents.moveHandler);
        }

        private static void Submit(EventSystem eventSystem, GameObject target)
        {
            ExecuteEvents.Execute(
                target, new BaseEventData(eventSystem), ExecuteEvents.submitHandler);
        }
    }
}
