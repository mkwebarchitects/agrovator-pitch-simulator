using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Agrovator.PitchSimulator.Accessibility;
using Agrovator.PitchSimulator.GuidedPitch;
using Agrovator.PitchSimulator.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.Tests.PlayMode
{
    public sealed class GuidedPitchViewTests
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
        }

        [Test]
        public void PartVisuals_KeepTheApprovedGlyphAndColourPairing()
        {
            AssertVisual(PitchPart.Problem, "!", "#F28C6F");
            AssertVisual(PitchPart.Evidence, "?", "#67B7D1");
            AssertVisual(PitchPart.Solution, ">", "#7BC47F");
            AssertVisual(PitchPart.Value, "*", "#E5B95C");
            Assert.Throws<ArgumentOutOfRangeException>(() => PitchPartVisuals.Get((PitchPart)99));
        }

        [Test]
        public void RailAndBoard_RenderFourFixedSlotsWithTheSameVisuals()
        {
            var draft = DraftWithProblem();
            var rail = CreateRail();
            var board = CreateBoard();

            rail.Render(PitchPart.Evidence, draft, Localize);
            board.Render(draft, Localize);

            Assert.That(rail.Slots.Select(slot => slot.Label), Is.EqualTo(new[]
            {
                "Problem", "Evidence", "Solution", "Value",
            }));
            Assert.That(rail.Slots.Single(slot => slot.Part == PitchPart.Evidence).IsCurrent, Is.True);
            Assert.That(board.Slots.Count, Is.EqualTo(4));
            Assert.That(board.Slots[0].SentenceText.text,
                Is.EqualTo("Our garden beds get too dry because we water them at the wrong times."));
            Assert.That(board.Slots[1].EmptyPromptText.text, Is.EqualTo("Add evidence"));

            foreach (var part in PitchParts.Ordered)
            {
                var expected = PitchPartVisuals.Get(part);
                Assert.That(rail.Slots[(int)part].IconText.text, Is.EqualTo(expected.IconGlyph));
                Assert.That(board.Slots[(int)part].IconText.text, Is.EqualTo(expected.IconGlyph));
                Assert.That(rail.Slots[(int)part].AccentImage.color, Is.EqualTo(expected.Colour));
                Assert.That(board.Slots[(int)part].AccentImage.color, Is.EqualTo(expected.Colour));
            }
        }

        [Test]
        public void Board_RevisionSelectionIsExplicitAndRenderDoesNotReplacePool()
        {
            var board = CreateBoard();
            var identities = board.Slots.Select(slot => slot.Root).ToArray();

            board.Render(DraftWithProblem(), Localize);
            board.SetRevisionSelection(PitchPart.Problem);
            board.Render(DraftWithProblem(), Localize);

            Assert.That(board.Slots.Select(slot => slot.Root), Is.EqualTo(identities));
            Assert.That(board.Slots[0].IsRevisionSelected, Is.True);
            Assert.That(board.Slots.Skip(1).All(slot => !slot.IsRevisionSelected), Is.True);
        }

        [Test]
        public void Cards_RenderThreeCreamTargetsAndExposePointerSelectionAndFocusStates()
        {
            EnsureEventSystem();
            var rig = CreateCards();
            rig.List.Initialize(_ => { });
            rig.List.Render(Options(3), null, true, Localize);

            Assert.That(rig.List.Cards.Count, Is.EqualTo(3));
            foreach (var card in rig.List.Cards)
            {
                Assert.That(card.Background.color, Is.EqualTo(PitchPartVisuals.CardCream));
                Assert.That(card.TargetHeight, Is.GreaterThanOrEqualTo(64f));
                Assert.That(card.State, Is.EqualTo(SentenceCardVisualState.Normal));
            }

            rig.List.Cards[0].OnPointerEnter(null);
            Assert.That(rig.List.Cards[0].State, Is.EqualTo(SentenceCardVisualState.Hover));
            rig.List.Cards[0].OnPointerExit(null);
            rig.List.Cards[0].OnSelect(null);
            Assert.That(rig.List.Cards[0].State, Is.EqualTo(SentenceCardVisualState.KeyboardFocus));

            rig.List.Render(Options(3), "option-2", true, Localize);
            Assert.That(rig.List.Cards[1].State, Is.EqualTo(SentenceCardVisualState.Selected));
            rig.List.Render(Options(3), null, false, Localize);
            Assert.That(rig.List.Cards.All(card => card.State == SentenceCardVisualState.Disabled), Is.True);
        }

        [Test]
        public void Cards_SelectStableIdOnceThenLockUntilNextRender()
        {
            var selected = new List<string>();
            var rig = CreateCards();
            rig.List.Initialize(selected.Add);
            rig.List.Render(Options(3), null, true, Localize);

            rig.List.Cards[1].Button.onClick.Invoke();
            rig.List.Cards[1].Button.onClick.Invoke();

            Assert.That(selected, Is.EqualTo(new[] { "option-2" }));
            Assert.That(rig.List.IsSelectionLocked, Is.True);
            Assert.That(rig.List.Cards.All(card => !card.Button.interactable), Is.True);

            rig.List.Render(Options(3), null, true, Localize);
            rig.List.Cards[0].Button.onClick.Invoke();
            Assert.That(selected, Is.EqualTo(new[] { "option-2", "option-1" }));
        }

        [Test]
        public void Cards_UseBoundedExplicitVerticalNavigation()
        {
            var rig = CreateCards();
            rig.List.Initialize(_ => { });
            rig.List.Render(Options(3), null, true, Localize);

            var first = rig.List.Cards[0].Button.navigation;
            var middle = rig.List.Cards[1].Button.navigation;
            var last = rig.List.Cards[2].Button.navigation;
            Assert.That(first.mode, Is.EqualTo(Navigation.Mode.Explicit));
            Assert.That(first.selectOnUp, Is.Null);
            Assert.That(first.selectOnDown, Is.SameAs(rig.List.Cards[1].Button));
            Assert.That(middle.selectOnUp, Is.SameAs(rig.List.Cards[0].Button));
            Assert.That(middle.selectOnDown, Is.SameAs(rig.List.Cards[2].Button));
            Assert.That(last.selectOnUp, Is.SameAs(rig.List.Cards[1].Button));
            Assert.That(last.selectOnDown, Is.Null);
        }

        [Test]
        public void Cards_HideUnusedPoolEntriesAndClearTheirSelectionTarget()
        {
            var selections = new List<string>();
            var rig = CreateCards();
            rig.List.Initialize(selections.Add);
            rig.List.Render(Options(1), null, true, Localize);

            var hidden = rig.List.Cards[1];
            Assert.That(hidden.gameObject.activeSelf, Is.False);
            Assert.That(hidden.Label.text, Is.Empty);
            Assert.That(hidden.Button.interactable, Is.False);
            Assert.That(hidden.Button.navigation.selectOnUp, Is.Null);
            Assert.That(hidden.Button.navigation.selectOnDown, Is.Null);
            hidden.Button.onClick.Invoke();
            Assert.That(selections, Is.Empty);

            rig.List.Clear();
            Assert.That(rig.List.Cards.All(card => !card.gameObject.activeSelf), Is.True);
            Assert.That(rig.List.Cards.All(card => card.Label.text == string.Empty), Is.True);
        }

        [Test]
        public void Feedback_RendersThreeNamedCoachingRowsWithoutNumericRubric()
        {
            var view = CreateFeedback();
            view.Render(Feedback(), Localize);

            Assert.That(view.Rows.Select(row => row.LabelText.text),
                Is.EqualTo(new[] { "What worked", "What is missing", "How to improve" }));
            Assert.That(view.Rows.Select(row => row.ValueText.text),
                Is.EqualTo(new[] { "Specific problem", "One observation", "Add measured evidence" }));
            Assert.That(view.Rows.SelectMany(row => row.LabelText.text.Concat(row.ValueText.text))
                .Any(char.IsDigit), Is.False);
        }

        [Test]
        public void ResponsiveLayout_AppliesWideAndCompactRulesWithoutAllocatingObjects()
        {
            var rig = CreateResponsive();
            var objectCount = rig.Root.GetComponentsInChildren<Transform>(true).Length;

            Assert.That(rig.Layout.Apply(new Vector2(1280, 720)), Is.True);
            Assert.That(rig.Layout.IsCompact, Is.False);
            Assert.That(rig.Board.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedRowCount));
            Assert.That(rig.Board.constraintCount, Is.EqualTo(1));
            Assert.That(rig.Cards.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedRowCount));
            Assert.That(rig.Cards.constraintCount, Is.EqualTo(1));
            Assert.That(rig.Scroll.enabled, Is.False);

            Assert.That(rig.Layout.Apply(new Vector2(720, 960)), Is.True);
            Assert.That(rig.Layout.IsCompact, Is.True);
            Assert.That(rig.Board.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
            Assert.That(rig.Board.constraintCount, Is.EqualTo(2));
            Assert.That(rig.Cards.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
            Assert.That(rig.Cards.constraintCount, Is.EqualTo(1));
            Assert.That(rig.Scroll.enabled, Is.True);
            Assert.That(rig.Cards.cellSize.y, Is.GreaterThanOrEqualTo(64f));
            Assert.That(rig.Root.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(objectCount));

            Assert.That(rig.Layout.Apply(new Vector2(700, 900)), Is.False);
            Assert.That(rig.Root.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(objectCount));
        }

        [Test]
        public void Board_LocalizesStoredResponseIdsToAuthoredSentencesThroughCompositeLocalizer()
        {
            var catalog = LocalizationCatalog.Load(ReadProjectFile("Content", "Localization", "en.json"));
            var result = GuidedPitchContentLoader.Load(
                ReadProjectFile("Content", "Scenarios", "guided-pitch-builder.en.json"),
                catalog.GetKeys("en"));
            Assert.That(result.IsSuccess, Is.True,
                string.Join(", ", result.Issues.Select(issue => issue.Code + "@" + issue.Path)));

            var optionsById = result.Content.Modes.Values
                .SelectMany(mode => mode.Parts.SelectMany(part => part.Options).Concat(mode.FollowUp.Options))
                .ToDictionary(option => option.Id, StringComparer.Ordinal);
            Func<string, string> localize = key => optionsById.TryGetValue(key, out var option)
                ? catalog.Resolve("en", option.TextKey)
                : catalog.Resolve("en", key);

            var draft = new PitchDraft();
            var primary = result.Content.Modes[LearnerMode.Primary];
            foreach (var part in primary.Parts)
            {
                var clear = part.Options.Single(option => option.Mastery == MasteryState.Clear);
                Assert.That(draft.TrySelectInitial(part.Part, clear.Id, clear.Mastery), Is.True, clear.Id);
            }

            var board = CreateBoard();
            board.Render(draft.Snapshot, localize);

            Assert.That(board.Slots[(int)PitchPart.Problem].SentenceText.text,
                Is.EqualTo("Our garden beds get too dry because we water them at the wrong times."));
            foreach (var part in primary.Parts)
            {
                var slot = board.Slots[(int)part.Part];
                var clear = part.Options.Single(option => option.Mastery == MasteryState.Clear);
                Assert.That(slot.SentenceText.text, Is.EqualTo(catalog.Resolve("en", clear.TextKey)), clear.Id);
                Assert.That(slot.SentenceText.text, Is.Not.EqualTo(clear.Id), clear.Id);
                Assert.That(slot.SentenceText.text, Does.Not.Contain("[[missing:"), clear.Id);
                Assert.That(slot.LabelText.text,
                    Is.EqualTo(catalog.Resolve("en", PitchPartVisuals.Get(part.Part).LabelKey)), clear.Id);
                Assert.That(slot.LabelText.text, Does.Not.Contain("[[missing:"), clear.Id);
            }
        }

        [Test]
        public void ResponsiveLayout_RecomputesCompactCellSizesWhenViewportShrinksInSameMode()
        {
            var rig = CreateResponsive();
            var objectCount = rig.Root.GetComponentsInChildren<Transform>(true).Length;

            rig.Layout.Apply(new Vector2(720, 960));
            rig.Layout.Apply(new Vector2(600, 900));

            Assert.That(rig.Layout.IsCompact, Is.True);
            Assert.That(rig.Board.cellSize.x, Is.LessThanOrEqualTo((600f - 72f) / 2f));
            Assert.That(rig.Cards.cellSize.x, Is.LessThanOrEqualTo(600f - 64f));
            Assert.That(rig.Cards.cellSize.y, Is.GreaterThanOrEqualTo(64f));
            Assert.That(rig.Root.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(objectCount));
        }

        [Test]
        public void Cards_ConfigureAssignsBackgroundAsButtonTargetGraphic()
        {
            var rig = CreateCards();
            foreach (var card in rig.List.Cards)
            {
                Assert.That(card.Button.targetGraphic, Is.SameAs(card.Background));
            }

            var inactiveRoot = Root("Inactive Card");
            inactiveRoot.SetActive(false);
            var background = inactiveRoot.AddComponent<Image>();
            var button = inactiveRoot.AddComponent<Button>();
            var label = Text("Label", inactiveRoot.transform);
            var focus = Image("Focus", inactiveRoot.transform);
            var card2 = inactiveRoot.AddComponent<SentenceCardView>();

            card2.Configure(button, label, background, focus);

            Assert.That(card2.Button.targetGraphic, Is.SameAs(background));
        }

        [Test]
        public void Palette_MeetsTextAndFocusContrastRequirements()
        {
            Assert.That(PitchPartVisuals.ContrastRatio(PitchPartVisuals.CardText, PitchPartVisuals.CardCream),
                Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(PitchPartVisuals.ContrastRatio(PitchPartVisuals.LightText, PitchPartVisuals.DeepNavy),
                Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(PitchPartVisuals.ContrastRatio(PitchPartVisuals.FocusGold, PitchPartVisuals.DeepNavy),
                Is.GreaterThanOrEqualTo(3f));
        }

        private static void AssertVisual(PitchPart part, string glyph, string htmlColour)
        {
            var visual = PitchPartVisuals.Get(part);
            Assert.That(visual.Part, Is.EqualTo(part));
            Assert.That(visual.IconGlyph, Is.EqualTo(glyph));
            Assert.That(ColorUtility.ToHtmlStringRGB(visual.Colour), Is.EqualTo(htmlColour.Substring(1)));
        }

        private PitchProgressRailView CreateRail()
        {
            var root = Root("Rail");
            var view = root.AddComponent<PitchProgressRailView>();
            var slots = PitchParts.Ordered.Select(part =>
            {
                var slotRoot = Root(part + " Rail Slot", root.transform);
                return new PitchProgressRailSlot(part, slotRoot, Text("Label", slotRoot.transform),
                    Text("Icon", slotRoot.transform), Image("Accent", slotRoot.transform),
                    Root("Current", slotRoot.transform));
            }).ToArray();
            view.Configure(slots);
            return view;
        }

        private PitchBoardView CreateBoard()
        {
            var root = Root("Board");
            var view = root.AddComponent<PitchBoardView>();
            var slots = PitchParts.Ordered.Select(part =>
            {
                var slotRoot = Root(part + " Board Slot", root.transform);
                return new PitchBoardSlot(part, slotRoot, Text("Label", slotRoot.transform),
                    Text("Icon", slotRoot.transform), Image("Accent", slotRoot.transform),
                    Text("Sentence", slotRoot.transform), Text("Empty Prompt", slotRoot.transform),
                    Image("Revision", slotRoot.transform));
            }).ToArray();
            view.Configure(slots);
            return view;
        }

        private CardRig CreateCards()
        {
            var root = Root("Cards");
            var list = root.AddComponent<SentenceCardListView>();
            var cards = Enumerable.Range(0, 3).Select(index =>
            {
                var cardRoot = Root("Card " + index, root.transform);
                var rect = cardRoot.GetComponent<RectTransform>();
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 96f);
                var background = cardRoot.AddComponent<Image>();
                var button = cardRoot.AddComponent<Button>();
                var label = Text("Label", cardRoot.transform);
                var focus = Image("Focus", cardRoot.transform);
                var card = cardRoot.AddComponent<SentenceCardView>();
                card.Configure(button, label, background, focus);
                return card;
            }).ToArray();
            list.Configure(cards);
            return new CardRig(list);
        }

        private PitchFeedbackView CreateFeedback()
        {
            var root = Root("Feedback");
            var view = root.AddComponent<PitchFeedbackView>();
            var rows = Enumerable.Range(0, 3).Select(index =>
            {
                var row = Root("Row " + index, root.transform);
                return new PitchFeedbackRow(row, Text("Label", row.transform), Text("Value", row.transform));
            }).ToArray();
            view.Configure(rows);
            return view;
        }

        private ResponsiveRig CreateResponsive()
        {
            var root = Root("Responsive");
            var boardObject = Root("Board Grid", root.transform);
            var cardsObject = Root("Cards Grid", root.transform);
            var scrollObject = Root("Scroll", root.transform);
            var board = boardObject.AddComponent<GridLayoutGroup>();
            var cards = cardsObject.AddComponent<GridLayoutGroup>();
            var scroll = scrollObject.AddComponent<ScrollRect>();
            var layout = root.AddComponent<GuidedPitchResponsiveLayout>();
            layout.Configure(board, cards, scroll);
            return new ResponsiveRig(root, layout, board, cards, scroll);
        }

        private PitchDraftSnapshot DraftWithProblem()
        {
            var draft = new PitchDraft();
            draft.TrySelectInitial(PitchPart.Problem, "sentence.problem", MasteryState.Clear);
            return draft.Snapshot;
        }

        private static IReadOnlyList<GuidedPitchOption> Options(int count)
        {
            return Enumerable.Range(1, count).Select(index => Option(index)).ToArray();
        }

        private static GuidedPitchOption Option(int index)
        {
            return (GuidedPitchOption)Activator.CreateInstance(
                typeof(GuidedPitchOption),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new object[]
                {
                    "option-" + index,
                    "sentence.option-" + index,
                    index == 1 ? MasteryState.Clear : MasteryState.Developing,
                    0,
                    "Encouraging",
                    Feedback(),
                },
                null);
        }

        private static GuidedPitchFeedback Feedback()
        {
            return (GuidedPitchFeedback)Activator.CreateInstance(
                typeof(GuidedPitchFeedback),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new object[] { "feedback.worked", "feedback.missing", "feedback.improve" },
                null);
        }

        private static string ReadProjectFile(params string[] segments)
        {
            return File.ReadAllText(segments.Aggregate(Application.dataPath, Path.Combine));
        }

        private static string Localize(string key)
        {
            var values = new Dictionary<string, string>
            {
                ["guided.part.problem.label"] = "Problem",
                ["guided.part.evidence.label"] = "Evidence",
                ["guided.part.solution.label"] = "Solution",
                ["guided.part.value.label"] = "Value",
                ["guided.board.add.problem"] = "Add problem",
                ["guided.board.add.evidence"] = "Add evidence",
                ["guided.board.add.solution"] = "Add solution",
                ["guided.board.add.value"] = "Add value",
                ["sentence.problem"] = "Our garden beds get too dry because we water them at the wrong times.",
                ["sentence.option-1"] = "First sentence",
                ["sentence.option-2"] = "Second sentence",
                ["sentence.option-3"] = "Third sentence",
                ["guided.feedback.worked"] = "What worked",
                ["guided.feedback.missing"] = "What is missing",
                ["guided.feedback.improve"] = "How to improve",
                ["feedback.worked"] = "Specific problem",
                ["feedback.missing"] = "One observation",
                ["feedback.improve"] = "Add measured evidence",
            };
            return values.TryGetValue(key, out var value) ? value : key;
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current == null)
            {
                Root("Event System").AddComponent<EventSystem>();
            }
        }

        private GameObject Root(string name, Transform parent = null)
        {
            var root = new GameObject(name, typeof(RectTransform));
            if (parent == null)
            {
                roots.Add(root);
            }
            else
            {
                root.transform.SetParent(parent, false);
            }
            return root;
        }

        private Text Text(string name, Transform parent)
        {
            var text = Root(name, parent);
            text.AddComponent<CanvasRenderer>();
            return text.AddComponent<Text>();
        }

        private Image Image(string name, Transform parent)
        {
            var image = Root(name, parent);
            image.AddComponent<CanvasRenderer>();
            return image.AddComponent<Image>();
        }

        private sealed class CardRig
        {
            internal CardRig(SentenceCardListView list) { List = list; }
            internal SentenceCardListView List { get; }
        }

        private sealed class ResponsiveRig
        {
            internal ResponsiveRig(GameObject root, GuidedPitchResponsiveLayout layout,
                GridLayoutGroup board, GridLayoutGroup cards, ScrollRect scroll)
            {
                Root = root;
                Layout = layout;
                Board = board;
                Cards = cards;
                Scroll = scroll;
            }

            internal GameObject Root { get; }
            internal GuidedPitchResponsiveLayout Layout { get; }
            internal GridLayoutGroup Board { get; }
            internal GridLayoutGroup Cards { get; }
            internal ScrollRect Scroll { get; }
        }
    }
}
