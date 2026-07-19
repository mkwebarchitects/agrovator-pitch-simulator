using System;
using System.Collections.Generic;
using System.Linq;
using Agrovator.PitchSimulator.GuidedPitch;
using Agrovator.PitchSimulator.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.Editor
{
    /// <summary>
    /// Serialized references produced by <see cref="GuidedPitchSceneBuilder.Create"/> so the
    /// project builder can finish scene composition (EventSystem focus wiring) without
    /// rebuilding any guided hierarchy knowledge of its own.
    /// </summary>
    internal sealed class GuidedPitchSceneReferences
    {
        public GuidedPitchScreenRouter Router;
        public TitlePresenter TitlePresenter;
        public BriefingPresenter BriefingPresenter;
        public GuidedPitchPresenter GuidedPresenter;
        public GuidedPitchResultsPresenter ResultsPresenter;
        public SettingsPresenter SettingsPresenter;
        public SafeFallbackPresenter SafeFallbackPresenter;
        public Selectable TitleDefault;
        public Selectable BriefingDefault;
        public Selectable SettingsDefault;
    }

    /// <summary>
    /// Deterministic construction of the guided pitch screens: Title, Briefing, the
    /// persistent Guided Pitch panel (rail, Aya row, Pitch Board, Phase Scroll,
    /// primary action), Results, Settings, and Safe Fallback. Every serialized view
    /// reference and explicit navigation link is assigned here before the scene is
    /// saved, so regeneration is reproducible and hand-editing is never required.
    /// </summary>
    internal static class GuidedPitchSceneBuilder
    {
        private const float FrameWidth = 980f;
        private const float FrameHeight = 672f;
        private static readonly Color ScreenDim = new Color(0.055f, 0.09f, 0.12f, 1f);
        private static readonly Color DeepNavy = new Color32(0x0E, 0x17, 0x1F, 0xFF);
        private static readonly Color CardNavy = new Color32(0x16, 0x24, 0x2F, 0xFF);
        private static readonly Color LightText = new Color32(0xFF, 0xF8, 0xE8, 0xFF);
        private static readonly Color Ink = new Color32(0x0E, 0x17, 0x1F, 0xFF);
        private static readonly Color Cream = new Color32(0xF4, 0xEA, 0xD5, 0xFF);
        private static readonly Color FocusGold = new Color32(0xFF, 0xD1, 0x66, 0xFF);
        private static readonly Color ActionTeal = new Color(0.08f, 0.42f, 0.34f, 1f);
        private static readonly string[] BriefingLineKeys =
        {
            "guided.title",
            "guided.briefing",
            "guided.briefing.judge",
            "guided.briefing.practice",
            "guided.briefing.untimed",
        };

        internal static GuidedPitchSceneReferences Create(
            Transform canvas, Sprite garden, Sprite[] ayaSprites)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            if (garden == null) throw new ArgumentNullException(nameof(garden));
            if (ayaSprites == null || ayaSprites.Length == 0)
            {
                throw new ArgumentException("Judge Aya sprites are required.", nameof(ayaSprites));
            }

            var references = new GuidedPitchSceneReferences();
            var title = CreateTitleScreen(canvas, references);
            var briefing = CreateBriefingScreen(canvas, references);
            var guided = CreateGuidedScreen(canvas, garden, ayaSprites, references,
                out var modeSelectionPanel);
            var results = CreateResultsScreen(canvas, references);
            var settings = CreateSettingsScreen(canvas, references);
            var fallback = CreateSafeFallbackScreen(canvas, references);

            var router = canvas.gameObject.AddComponent<GuidedPitchScreenRouter>();
            router.Configure(
                title, briefing, modeSelectionPanel, guided, results, settings, fallback,
                references.TitlePresenter, references.BriefingPresenter,
                references.GuidedPresenter, references.ResultsPresenter,
                references.SettingsPresenter, references.SafeFallbackPresenter,
                references.TitleDefault, references.BriefingDefault, references.SettingsDefault);
            references.Router = router;

            title.SetActive(true);
            briefing.SetActive(false);
            guided.SetActive(false);
            results.SetActive(false);
            settings.SetActive(false);
            fallback.SetActive(false);
            return references;
        }

        private static GameObject CreateTitleScreen(Transform canvas, GuidedPitchSceneReferences references)
        {
            var panel = CreateScreen("Title", canvas);
            var presenter = panel.AddComponent<TitlePresenter>();
            var frame = CreateFrame(panel.transform, 760f, 500f, 24, 20, 16f);
            var heading = CreateText("Heading", frame.transform, 40, FontStyle.Bold, LightText);
            var subtitle = CreateText("Subtitle", frame.transform, 20, FontStyle.Normal, LightText);
            heading.text = "Pitch Simulator";
            subtitle.text = "Build a clear, confident Smart School Garden pitch.";
            var start = CreateActionButton("Start Button", frame.transform, "Start Pitch", 460f);
            var settingsButton = CreateActionButton("Settings Button", frame.transform, "Settings", 380f);
            SetNavigation(start, settingsButton, settingsButton, null, null);
            SetNavigation(settingsButton, start, start, null, null);
            SetReference(presenter, "startButton", start);
            SetReference(presenter, "settingsButton", settingsButton);
            references.TitlePresenter = presenter;
            references.TitleDefault = start;
            return panel;
        }

        private static GameObject CreateBriefingScreen(
            Transform canvas, GuidedPitchSceneReferences references)
        {
            var panel = CreateScreen("Briefing", canvas);
            var presenter = panel.AddComponent<BriefingPresenter>();
            var frame = CreateFrame(panel.transform, 900f, 560f, 24, 20, 12f);
            var lines = new Text[BriefingLineKeys.Length];
            for (var index = 0; index < BriefingLineKeys.Length; index++)
            {
                var size = index == 0 ? 26 : 16;
                var style = index == 0 ? FontStyle.Bold : FontStyle.Normal;
                lines[index] = CreateText($"Line {index + 1}", frame.transform, size, style, LightText);
            }
            var continueButton = CreateActionButton(
                "Continue Button", frame.transform, "Continue", 460f);
            SetNavigation(continueButton, null, null, null, null);
            SetReference(presenter, "continueButton", continueButton);
            SetReferenceArray(presenter, "lineTexts", lines);
            SetStringArray(presenter, "lineKeys", BriefingLineKeys);
            references.BriefingPresenter = presenter;
            references.BriefingDefault = continueButton;
            return panel;
        }

        private static GameObject CreateGuidedScreen(
            Transform canvas, Sprite garden, Sprite[] ayaSprites,
            GuidedPitchSceneReferences references, out GameObject modeSelectionPanel)
        {
            var panel = CreateScreen("Guided Pitch", canvas);
            panel.GetComponent<Image>().color = DeepNavy;
            var presenter = panel.AddComponent<GuidedPitchPresenter>();
            var responsive = panel.AddComponent<GuidedPitchResponsiveLayout>();

            var environment = new GameObject("Environment Frame", typeof(RectTransform),
                typeof(Image), typeof(AspectRatioFitter), typeof(LayoutElement));
            environment.transform.SetParent(panel.transform, false);
            Stretch(environment.GetComponent<RectTransform>());
            environment.GetComponent<LayoutElement>().ignoreLayout = true;
            var environmentImage = environment.GetComponent<Image>();
            environmentImage.sprite = garden;
            environmentImage.color = Color.white;
            environmentImage.raycastTarget = false;
            var environmentAspect = environment.GetComponent<AspectRatioFitter>();
            environmentAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            environmentAspect.aspectRatio = 16f / 9f;

            var frame = CreateFrame(panel.transform, FrameWidth, FrameHeight, 8, 8, 8f);

            var rail = CreateProgressRail(frame.transform);
            var ayaRow = CreateAyaRow(frame.transform, ayaSprites, out var questionText,
                out var hintText);
            var board = CreateBoard(frame.transform);
            var phaseScroll = CreatePhaseScroll(frame.transform, out var scrollContent);

            modeSelectionPanel = CreateModeSelection(scrollContent);
            var learn = CreateLearn(scrollContent);
            var cards = CreateSentenceCards(scrollContent);
            var feedback = CreateFeedback(scrollContent);
            var strengthenButtons = CreateImproveActions(scrollContent, out var strengthenLabels);
            var presentation = CreatePresentPitch(scrollContent);
            var followUp = new GameObject("Follow Up", typeof(RectTransform));
            followUp.transform.SetParent(scrollContent, false);

            var primaryAction = CreateResponsiveRow("Primary Action", frame.transform, 12f, 64f);
            var continueButton = CreateActionButton(
                "Continue Button", primaryAction.transform, "Continue", 420f);
            var presentButton = CreateActionButton(
                "Present Button", primaryAction.transform, "Present Pitch", 420f);
            SetNavigation(continueButton, null, null, null, null);
            SetNavigation(presentButton, null, null, null, null);

            presenter.Configure(
                modeSelectionPanel.GetComponent<ModeSelectionView>(),
                learn,
                rail,
                board,
                cards,
                feedback,
                questionText,
                hintText,
                presentation,
                continueButton,
                presentButton,
                strengthenButtons,
                strengthenLabels,
                phaseScroll);
            responsive.Configure(
                canvas.GetComponent<Canvas>(),
                board.GetComponent<GridLayoutGroup>(),
                cards.GetComponent<GridLayoutGroup>(),
                phaseScroll,
                modeSelectionPanel.GetComponent<GuidedPitchFlowLayout>(),
                strengthenButtons[0].transform.parent.GetComponent<GuidedPitchFlowLayout>(),
                primaryAction.GetComponent<GuidedPitchFlowLayout>());
            ApplyWideLayoutDefaults(board.GetComponent<GridLayoutGroup>(),
                cards.GetComponent<GridLayoutGroup>(), phaseScroll);
            references.GuidedPresenter = presenter;
            return panel;
        }

        private static PitchProgressRailView CreateProgressRail(Transform frame)
        {
            var root = new GameObject("Progress Rail", typeof(RectTransform),
                typeof(HorizontalLayoutGroup), typeof(LayoutElement), typeof(PitchProgressRailView));
            root.transform.SetParent(frame, false);
            ConfigureRow(root.GetComponent<HorizontalLayoutGroup>(), 8f, expandWidth: true,
                expandHeight: true);
            root.GetComponent<LayoutElement>().preferredHeight = 40f;

            var slots = new PitchProgressRailSlot[4];
            foreach (var part in PitchParts.Ordered)
            {
                var visual = PitchPartVisuals.Get(part);
                var slot = new GameObject(part + " Slot", typeof(RectTransform),
                    typeof(HorizontalLayoutGroup));
                slot.transform.SetParent(root.transform, false);
                var slotLayout = slot.GetComponent<HorizontalLayoutGroup>();
                ConfigureRow(slotLayout, 6f, expandWidth: false, expandHeight: false);
                slotLayout.padding = new RectOffset(8, 8, 4, 4);
                slotLayout.childAlignment = TextAnchor.MiddleLeft;

                var accent = CreateImage("Accent", slot.transform, visual.Colour);
                var accentLayout = accent.gameObject.AddComponent<LayoutElement>();
                accentLayout.preferredWidth = 8f;
                accentLayout.preferredHeight = 28f;
                var icon = CreateText("Icon", slot.transform, 16, FontStyle.Bold, LightText);
                icon.text = visual.IconGlyph;
                var iconLayout = icon.gameObject.AddComponent<LayoutElement>();
                iconLayout.preferredWidth = 18f;
                var label = CreateText("Label", slot.transform, 14, FontStyle.Bold, LightText,
                    TextAnchor.MiddleLeft);
                SetBestFit(label, 10, 14);
                var labelLayout = label.gameObject.AddComponent<LayoutElement>();
                labelLayout.flexibleWidth = 1f;

                var marker = CreateImage("Current Marker", slot.transform, FocusGold);
                var markerRect = marker.GetComponent<RectTransform>();
                marker.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                markerRect.anchorMin = new Vector2(0f, 0f);
                markerRect.anchorMax = new Vector2(1f, 0f);
                markerRect.pivot = new Vector2(0.5f, 0f);
                markerRect.sizeDelta = new Vector2(0f, 4f);
                markerRect.anchoredPosition = Vector2.zero;
                marker.gameObject.SetActive(false);

                slots[(int)part] = new PitchProgressRailSlot(
                    part, slot, label, icon, accent, marker.gameObject);
            }

            var view = root.GetComponent<PitchProgressRailView>();
            view.Configure(slots);
            return view;
        }

        private static GameObject CreateAyaRow(
            Transform frame, Sprite[] ayaSprites, out Text questionText, out Text hintText)
        {
            var row = new GameObject("Aya Row", typeof(RectTransform),
                typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(frame, false);
            ConfigureRow(row.GetComponent<HorizontalLayoutGroup>(), 12f, expandWidth: false,
                expandHeight: true);
            row.GetComponent<LayoutElement>().preferredHeight = 160f;

            var judge = new GameObject("Judge Aya", typeof(RectTransform), typeof(Image),
                typeof(LayoutElement));
            judge.transform.SetParent(row.transform, false);
            var judgeImage = judge.GetComponent<Image>();
            judgeImage.sprite = ayaSprites.FirstOrDefault(sprite => sprite.name == "Encouraging")
                ?? ayaSprites[0];
            judgeImage.preserveAspect = true;
            judgeImage.raycastTarget = false;
            var judgeLayout = judge.GetComponent<LayoutElement>();
            judgeLayout.preferredWidth = judgeImage.sprite.rect.width;
            judgeLayout.preferredHeight = judgeImage.sprite.rect.height;
            judgeLayout.flexibleWidth = 0f;

            var card = new GameObject("Dialogue Card", typeof(RectTransform), typeof(Image),
                typeof(VerticalLayoutGroup), typeof(LayoutElement));
            card.transform.SetParent(row.transform, false);
            var cardImage = card.GetComponent<Image>();
            cardImage.color = CardNavy;
            cardImage.raycastTarget = false;
            var cardLayout = card.GetComponent<VerticalLayoutGroup>();
            ConfigureColumn(cardLayout, 6f, expandWidth: true, expandHeight: false);
            cardLayout.padding = new RectOffset(24, 16, 14, 14);
            cardLayout.childAlignment = TextAnchor.MiddleLeft;
            var cardElement = card.GetComponent<LayoutElement>();
            cardElement.flexibleWidth = 1f;
            cardElement.preferredHeight = 160f;

            var accent = CreateImage("Speaker Accent", card.transform, FocusGold);
            accent.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.sizeDelta = new Vector2(6f, 0f);
            accentRect.anchoredPosition = Vector2.zero;

            questionText = CreateText("Question", card.transform, 16, FontStyle.Normal, LightText,
                TextAnchor.MiddleLeft);
            hintText = CreateText("Hint", card.transform, 13, FontStyle.Italic, LightText,
                TextAnchor.MiddleLeft);
            return row;
        }

        private static PitchBoardView CreateBoard(Transform frame)
        {
            var root = new GameObject("Pitch Board", typeof(RectTransform),
                typeof(GridLayoutGroup), typeof(PitchBoardView));
            root.transform.SetParent(frame, false);

            var slots = new PitchBoardSlot[4];
            foreach (var part in PitchParts.Ordered)
            {
                var visual = PitchPartVisuals.Get(part);
                var slot = new GameObject(part + " Slot", typeof(RectTransform),
                    typeof(VerticalLayoutGroup));
                slot.transform.SetParent(root.transform, false);
                var slotLayout = slot.GetComponent<VerticalLayoutGroup>();
                ConfigureColumn(slotLayout, 2f, expandWidth: true, expandHeight: false);
                slotLayout.padding = new RectOffset(18, 12, 8, 8);

                var outline = CreateImage("Revision Outline", slot.transform, FocusGold);
                outline.raycastTarget = false;
                outline.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                StretchBeyond(outline.GetComponent<RectTransform>(), 4f);
                outline.gameObject.SetActive(false);

                var backing = CreateImage("Backing", slot.transform, CardNavy);
                backing.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                Stretch(backing.GetComponent<RectTransform>());

                var accent = CreateImage("Accent", slot.transform, visual.Colour);
                accent.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                var accentRect = accent.GetComponent<RectTransform>();
                accentRect.anchorMin = new Vector2(0f, 0f);
                accentRect.anchorMax = new Vector2(0f, 1f);
                accentRect.pivot = new Vector2(0f, 0.5f);
                accentRect.sizeDelta = new Vector2(6f, 0f);
                accentRect.anchoredPosition = Vector2.zero;

                var header = CreateRow("Header", slot.transform, 6f, 22f);
                header.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
                var icon = CreateText("Icon", header.transform, 14, FontStyle.Bold, LightText);
                icon.text = visual.IconGlyph;
                var iconLayout = icon.gameObject.AddComponent<LayoutElement>();
                iconLayout.preferredWidth = 16f;
                var label = CreateText("Label", header.transform, 13, FontStyle.Bold, LightText,
                    TextAnchor.MiddleLeft);
                SetBestFit(label, 9, 13);
                label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

                var sentence = CreateText("Sentence", slot.transform, 12, FontStyle.Normal, LightText,
                    TextAnchor.UpperLeft);
                SetBestFit(sentence, 8, 12);
                sentence.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
                var emptyPrompt = CreateText("Empty Prompt", slot.transform, 12, FontStyle.Italic,
                    LightText, TextAnchor.MiddleLeft);
                SetBestFit(emptyPrompt, 9, 12);
                emptyPrompt.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

                slots[(int)part] = new PitchBoardSlot(
                    part, slot, label, icon, accent, sentence, emptyPrompt, outline);
            }

            var view = root.GetComponent<PitchBoardView>();
            view.Configure(slots);
            return view;
        }

        private static ScrollRect CreatePhaseScroll(Transform frame, out Transform content)
        {
            var root = new GameObject("Phase Scroll", typeof(RectTransform), typeof(ScrollRect),
                typeof(LayoutElement));
            root.transform.SetParent(frame, false);
            var rootLayout = root.GetComponent<LayoutElement>();
            rootLayout.preferredHeight = 220f;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(root.transform, false);
            Stretch(viewport.GetComponent<RectTransform>());

            var contentObject = new GameObject("Content", typeof(RectTransform),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewport.transform, false);
            var contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            var contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
            ConfigureColumn(contentLayout, 8f, expandWidth: true, expandHeight: false);
            contentObject.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            var scroll = root.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            content = contentObject.transform;
            return scroll;
        }

        private static GameObject CreateModeSelection(Transform content)
        {
            var root = new GameObject("Mode Selection", typeof(RectTransform),
                typeof(GuidedPitchFlowLayout), typeof(ModeSelectionView));
            root.transform.SetParent(content, false);
            root.GetComponent<GuidedPitchFlowLayout>().Configure(150f, 12f);

            var cards = new ModeSelectionCard[2];
            var modes = new[] { LearnerMode.Primary, LearnerMode.Secondary };
            for (var index = 0; index < modes.Length; index++)
            {
                var card = new GameObject(modes[index] + " Card", typeof(RectTransform),
                    typeof(Button), typeof(VerticalLayoutGroup));
                card.transform.SetParent(root.transform, false);
                var cardLayout = card.GetComponent<VerticalLayoutGroup>();
                ConfigureColumn(cardLayout, 6f, expandWidth: true, expandHeight: false);
                cardLayout.padding = new RectOffset(16, 16, 12, 12);
                cardLayout.childAlignment = TextAnchor.MiddleLeft;

                var backing = CreateImage("Backing", card.transform, Cream);
                backing.raycastTarget = true;
                backing.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                Stretch(backing.GetComponent<RectTransform>());

                var title = CreateText("Title", card.transform, 20, FontStyle.Bold, Ink,
                    TextAnchor.MiddleLeft);
                SetBestFit(title, 15, 20);
                title.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
                var description = CreateText("Description", card.transform, 16, FontStyle.Normal, Ink,
                    TextAnchor.MiddleLeft);
                SetBestFit(description, 12, 16);
                description.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

                var button = card.GetComponent<Button>();
                button.targetGraphic = backing;
                AddFocusIndicator(button);
                cards[index] = new ModeSelectionCard(modes[index], button, title, description, backing);
            }
            SetNavigation(cards[0].Button, null, null, null, cards[1].Button);
            SetNavigation(cards[1].Button, null, null, cards[0].Button, null);

            var view = root.GetComponent<ModeSelectionView>();
            view.Configure(cards);
            return root;
        }

        private static LearnPitchView CreateLearn(Transform content)
        {
            var root = new GameObject("Learn", typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(LearnPitchView));
            root.transform.SetParent(content, false);
            ConfigureColumn(root.GetComponent<VerticalLayoutGroup>(), 8f, expandWidth: true,
                expandHeight: false);
            var incomplete = CreateText("Incomplete Pitch", root.transform, 15, FontStyle.Italic,
                LightText, TextAnchor.MiddleLeft);
            var explanation = CreateText("Explanation", root.transform, 15, FontStyle.Normal,
                LightText, TextAnchor.MiddleLeft);
            var view = root.GetComponent<LearnPitchView>();
            view.Configure(incomplete, explanation);
            return view;
        }

        private static SentenceCardListView CreateSentenceCards(Transform content)
        {
            var root = new GameObject("Sentence Cards", typeof(RectTransform),
                typeof(GridLayoutGroup), typeof(SentenceCardListView));
            root.transform.SetParent(content, false);

            var cards = new SentenceCardView[3];
            for (var index = 0; index < cards.Length; index++)
            {
                var card = new GameObject($"Card {index + 1}", typeof(RectTransform),
                    typeof(Button), typeof(VerticalLayoutGroup), typeof(SentenceCardView));
                card.transform.SetParent(root.transform, false);
                var cardLayout = card.GetComponent<VerticalLayoutGroup>();
                ConfigureColumn(cardLayout, 0f, expandWidth: true, expandHeight: true);
                cardLayout.padding = new RectOffset(12, 12, 8, 8);

                var outline = CreateImage("Focus Outline", card.transform, FocusGold);
                outline.raycastTarget = false;
                outline.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                StretchBeyond(outline.GetComponent<RectTransform>(), 4f);
                outline.gameObject.SetActive(false);

                var backing = CreateImage("Backing", card.transform, Cream);
                backing.raycastTarget = true;
                backing.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                Stretch(backing.GetComponent<RectTransform>());

                var label = CreateText("Label", card.transform, 15, FontStyle.Normal, Ink,
                    TextAnchor.MiddleLeft);
                SetBestFit(label, 10, 15);

                var view = card.GetComponent<SentenceCardView>();
                view.Configure(card.GetComponent<Button>(), label, backing, outline);
                AddFocusIndicator(card.GetComponent<Button>());
                SetNavigation(card.GetComponent<Button>(), null, null, null, null);
                cards[index] = view;
            }

            var list = root.GetComponent<SentenceCardListView>();
            list.Configure(cards);
            return list;
        }

        private static PitchFeedbackView CreateFeedback(Transform content)
        {
            var root = new GameObject("Feedback", typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(PitchFeedbackView));
            root.transform.SetParent(content, false);
            ConfigureColumn(root.GetComponent<VerticalLayoutGroup>(), 6f, expandWidth: true,
                expandHeight: false);

            var rows = new PitchFeedbackRow[3];
            for (var index = 0; index < rows.Length; index++)
            {
                var row = new GameObject($"Row {index + 1}", typeof(RectTransform), typeof(Image),
                    typeof(HorizontalLayoutGroup));
                row.transform.SetParent(root.transform, false);
                var rowImage = row.GetComponent<Image>();
                rowImage.color = CardNavy;
                rowImage.raycastTarget = false;
                var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
                ConfigureRow(rowLayout, 10f, expandWidth: false, expandHeight: false);
                rowLayout.padding = new RectOffset(12, 12, 8, 8);
                rowLayout.childAlignment = TextAnchor.UpperLeft;

                var label = CreateText("Label", row.transform, 13, FontStyle.Bold, FocusGold,
                    TextAnchor.UpperLeft);
                var labelLayout = label.gameObject.AddComponent<LayoutElement>();
                labelLayout.minWidth = 140f;
                labelLayout.preferredWidth = 140f;
                var value = CreateText("Value", row.transform, 13, FontStyle.Normal, LightText,
                    TextAnchor.UpperLeft);
                value.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
                rows[index] = new PitchFeedbackRow(row, label, value);
            }

            var view = root.GetComponent<PitchFeedbackView>();
            view.Configure(rows);
            return view;
        }

        private static Button[] CreateImproveActions(Transform content, out Text[] strengthenLabels)
        {
            var root = new GameObject("Improve Actions", typeof(RectTransform),
                typeof(GuidedPitchFlowLayout));
            root.transform.SetParent(content, false);
            root.GetComponent<GuidedPitchFlowLayout>().Configure(64f, 8f);

            var buttons = new Button[4];
            strengthenLabels = new Text[4];
            foreach (var part in PitchParts.Ordered)
            {
                var visual = PitchPartVisuals.Get(part);
                var buttonObject = new GameObject("Strengthen " + part, typeof(RectTransform),
                    typeof(Image), typeof(Button), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                buttonObject.transform.SetParent(root.transform, false);
                var image = buttonObject.GetComponent<Image>();
                image.color = ActionTeal;
                var button = buttonObject.GetComponent<Button>();
                button.targetGraphic = image;
                AddFocusIndicator(button);
                var layout = buttonObject.GetComponent<HorizontalLayoutGroup>();
                ConfigureRow(layout, 8f, expandWidth: false, expandHeight: false);
                layout.padding = new RectOffset(10, 10, 8, 8);
                var element = buttonObject.GetComponent<LayoutElement>();
                element.minHeight = 64f;
                element.preferredHeight = 64f;

                var chip = CreateImage("Chip", buttonObject.transform, visual.Colour);
                var chipLayout = chip.gameObject.AddComponent<LayoutElement>();
                chipLayout.preferredWidth = 28f;
                chipLayout.preferredHeight = 28f;
                var icon = CreateText("Icon", chip.transform, 16, FontStyle.Bold, Ink);
                Stretch(icon.GetComponent<RectTransform>());
                icon.text = visual.IconGlyph;

                var label = CreateText("Label", buttonObject.transform, 15, FontStyle.Bold, LightText,
                    TextAnchor.MiddleLeft);
                SetBestFit(label, 11, 15);
                label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
                buttons[(int)part] = button;
                strengthenLabels[(int)part] = label;
            }
            for (var index = 0; index < buttons.Length; index++)
            {
                SetNavigation(buttons[index], null, null,
                    index > 0 ? buttons[index - 1] : null,
                    index < buttons.Length - 1 ? buttons[index + 1] : null);
            }

            return buttons;
        }

        private static Text CreatePresentPitch(Transform content)
        {
            var root = new GameObject("Present Pitch", typeof(RectTransform),
                typeof(VerticalLayoutGroup));
            root.transform.SetParent(content, false);
            ConfigureColumn(root.GetComponent<VerticalLayoutGroup>(), 0f, expandWidth: true,
                expandHeight: false);
            return CreateText("Presentation", root.transform, 14, FontStyle.Normal, LightText,
                TextAnchor.UpperLeft);
        }

        private static GameObject CreateResultsScreen(
            Transform canvas, GuidedPitchSceneReferences references)
        {
            var panel = CreateScreen("Results", canvas);
            var presenter = panel.AddComponent<GuidedPitchResultsPresenter>();
            var frame = CreateFrame(panel.transform, FrameWidth, FrameHeight, 16, 12, 8f);

            var heading = CreateText("Heading", frame.transform, 26, FontStyle.Bold, LightText);
            heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;

            var scrollObject = new GameObject("Results Scroll", typeof(RectTransform),
                typeof(ScrollRect), typeof(LayoutElement));
            scrollObject.transform.SetParent(frame.transform, false);
            var scrollLayout = scrollObject.GetComponent<LayoutElement>();
            scrollLayout.preferredHeight = 460f;
            scrollLayout.flexibleHeight = 1f;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollObject.transform, false);
            Stretch(viewport.GetComponent<RectTransform>());

            var contentObject = new GameObject("Content", typeof(RectTransform),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewport.transform, false);
            var contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            var contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
            ConfigureColumn(contentLayout, 8f, expandWidth: true, expandHeight: false);
            contentLayout.padding = new RectOffset(12, 12, 12, 12);
            contentObject.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            var partViews = new PitchResultPartView[4];
            foreach (var part in PitchParts.Ordered)
            {
                partViews[(int)part] = CreateResultPartCard(contentObject.transform, part);
            }

            var readiness = CreateText("Readiness", contentObject.transform, 15, FontStyle.Bold,
                LightText, TextAnchor.MiddleLeft);
            var improvement = CreateText("Improvement", contentObject.transform, 13,
                FontStyle.Normal, LightText, TextAnchor.MiddleLeft);
            var finalPitchHeading = CreateText("Final Pitch Heading", contentObject.transform, 15,
                FontStyle.Bold, LightText, TextAnchor.MiddleLeft);
            var finalPitch = CreateText("Final Pitch", contentObject.transform, 14,
                FontStyle.Normal, LightText, TextAnchor.UpperLeft);
            var transfer = CreateText("Transfer Prompt", contentObject.transform, 13,
                FontStyle.Italic, LightText, TextAnchor.MiddleLeft);
            improvement.gameObject.SetActive(false);

            var status = CreateText("Submission Status", frame.transform, 14, FontStyle.Bold,
                LightText);
            status.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;
            var footer = CreateRow("Footer", frame.transform, 12f, 64f);
            var submit = CreateActionButton("Submit Button", footer.transform, "Submit results", 300f);
            var retry = CreateActionButton("Retry Button", footer.transform, "Retry", 300f);
            SetNavigation(submit, retry, retry, retry, retry);
            SetNavigation(retry, submit, submit, submit, submit);

            SetReference(presenter, "headingText", heading);
            SetReferenceArray(presenter, "partViews", partViews);
            SetReference(presenter, "readinessText", readiness);
            SetReference(presenter, "improvementText", improvement);
            SetReference(presenter, "transferText", transfer);
            SetReference(presenter, "finalPitchHeadingText", finalPitchHeading);
            SetReference(presenter, "finalPitchText", finalPitch);
            SetReference(presenter, "submissionStatusText", status);
            SetReference(presenter, "submitButton", submit);
            SetReference(presenter, "submitButtonText", submit.transform.Find("Label").GetComponent<Text>());
            SetReference(presenter, "retryButton", retry);
            SetReference(presenter, "retryButtonText", retry.transform.Find("Label").GetComponent<Text>());
            SetReference(presenter, "resultsScroll", scroll);
            references.ResultsPresenter = presenter;
            return panel;
        }

        private static PitchResultPartView CreateResultPartCard(Transform content, PitchPart part)
        {
            var visual = PitchPartVisuals.Get(part);
            var card = new GameObject(part + " Card", typeof(RectTransform), typeof(Image),
                typeof(VerticalLayoutGroup), typeof(PitchResultPartView));
            card.transform.SetParent(content, false);
            var cardImage = card.GetComponent<Image>();
            cardImage.color = CardNavy;
            cardImage.raycastTarget = false;
            var cardLayout = card.GetComponent<VerticalLayoutGroup>();
            ConfigureColumn(cardLayout, 4f, expandWidth: true, expandHeight: false);
            cardLayout.padding = new RectOffset(18, 12, 10, 10);

            var accent = CreateImage("Accent", card.transform, visual.Colour);
            accent.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.sizeDelta = new Vector2(6f, 0f);
            accentRect.anchoredPosition = Vector2.zero;

            var header = CreateRow("Header", card.transform, 8f, 22f);
            header.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            var icon = CreateText("Icon", header.transform, 14, FontStyle.Bold, LightText);
            icon.text = visual.IconGlyph;
            icon.gameObject.AddComponent<LayoutElement>().preferredWidth = 18f;
            var label = CreateText("Label", header.transform, 14, FontStyle.Bold, LightText,
                TextAnchor.MiddleLeft);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var statusText = CreateText("Status", header.transform, 13, FontStyle.Bold, FocusGold,
                TextAnchor.MiddleRight);
            statusText.gameObject.AddComponent<LayoutElement>().preferredWidth = 130f;

            var sentence = CreateText("Sentence", card.transform, 13, FontStyle.Normal, LightText,
                TextAnchor.UpperLeft);
            var revisionNote = CreateText("Revision Note", card.transform, 12, FontStyle.Italic,
                LightText, TextAnchor.MiddleLeft);
            revisionNote.gameObject.SetActive(false);

            var view = card.GetComponent<PitchResultPartView>();
            view.Configure(part, label, icon, accent, sentence, statusText, revisionNote);
            return view;
        }

        private static GameObject CreateSettingsScreen(
            Transform canvas, GuidedPitchSceneReferences references)
        {
            var panel = CreateScreen("Settings", canvas);
            var presenter = panel.AddComponent<SettingsPresenter>();
            var frame = CreateFrame(panel.transform, 720f, 420f, 20, 16, 12f);
            var heading = CreateText("Heading", frame.transform, 30, FontStyle.Bold, LightText);
            heading.text = "Settings";
            var note = CreateText("Foundation Note", frame.transform, 16, FontStyle.Normal, LightText);
            note.text = "Timer, motion, audio and language controls arrive with the LMS launch settings.";
            var close = CreateActionButton("Close Button", frame.transform, "Back", 420f);
            SetNavigation(close, null, null, null, null);
            SetReference(presenter, "closeButton", close);
            references.SettingsPresenter = presenter;
            references.SettingsDefault = close;
            return panel;
        }

        private static GameObject CreateSafeFallbackScreen(
            Transform canvas, GuidedPitchSceneReferences references)
        {
            var panel = CreateScreen("Safe Fallback", canvas);
            var presenter = panel.AddComponent<SafeFallbackPresenter>();
            var frame = CreateFrame(panel.transform, 760f, 300f, 24, 20, 12f);
            var message = CreateText("Recovery Message", frame.transform, 18, FontStyle.Normal,
                LightText);
            message.text = SafeFallbackPresenter.EnglishRecoveryMessage;
            presenter.Configure(message);
            references.SafeFallbackPresenter = presenter;
            return panel;
        }

        private static void ApplyWideLayoutDefaults(
            GridLayoutGroup board, GridLayoutGroup cards, ScrollRect phaseScroll)
        {
            board.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            board.constraintCount = 1;
            board.cellSize = new Vector2(224f, 140f);
            board.spacing = new Vector2(8f, 8f);
            board.childAlignment = TextAnchor.MiddleCenter;
            cards.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            cards.constraintCount = 1;
            cards.cellSize = new Vector2(288f, 96f);
            cards.spacing = new Vector2(8f, 8f);
            cards.childAlignment = TextAnchor.MiddleCenter;
            phaseScroll.enabled = false;
        }

        private static GameObject CreateScreen(string name, Transform canvas)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image),
                typeof(VerticalLayoutGroup));
            panel.transform.SetParent(canvas, false);
            Stretch(panel.GetComponent<RectTransform>());
            panel.GetComponent<Image>().color = ScreenDim;
            var layout = panel.GetComponent<VerticalLayoutGroup>();
            ConfigureColumn(layout, 0f, expandWidth: false, expandHeight: false);
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.childAlignment = TextAnchor.MiddleCenter;
            return panel;
        }

        private static GameObject CreateFrame(
            Transform parent, float width, float height,
            int horizontalPadding, int verticalPadding, float spacing)
        {
            var frame = new GameObject("Content Frame", typeof(RectTransform), typeof(Image),
                typeof(VerticalLayoutGroup), typeof(LayoutElement));
            frame.transform.SetParent(parent, false);
            frame.GetComponent<Image>().color = DeepNavy;
            var layout = frame.GetComponent<VerticalLayoutGroup>();
            ConfigureColumn(layout, spacing, expandWidth: true, expandHeight: false);
            layout.padding = new RectOffset(
                horizontalPadding, horizontalPadding, verticalPadding, verticalPadding);
            layout.childAlignment = TextAnchor.MiddleCenter;
            var element = frame.GetComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
            // Without an explicit cap the frame would inherit flexible sizing from
            // its own layout-group children and stretch past the approved width.
            element.flexibleWidth = 0f;
            element.flexibleHeight = 0f;
            return frame;
        }

        private static GameObject CreateRow(string name, Transform parent, float spacing, float height)
        {
            var row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            ConfigureRow(layout, spacing, expandWidth: false, expandHeight: false);
            layout.childAlignment = TextAnchor.MiddleCenter;
            row.GetComponent<LayoutElement>().preferredHeight = height;
            return row;
        }

        private static GameObject CreateResponsiveRow(
            string name, Transform parent, float spacing, float height)
        {
            var row = new GameObject(name, typeof(RectTransform),
                typeof(GuidedPitchFlowLayout));
            row.transform.SetParent(parent, false);
            row.GetComponent<GuidedPitchFlowLayout>().Configure(height, spacing);
            return row;
        }

        private static void ConfigureRow(
            HorizontalLayoutGroup layout, float spacing, bool expandWidth, bool expandHeight)
        {
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = expandWidth;
            layout.childForceExpandHeight = expandHeight;
        }

        private static void ConfigureColumn(
            VerticalLayoutGroup layout, float spacing, bool expandWidth, bool expandHeight)
        {
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = expandWidth;
            layout.childForceExpandHeight = expandHeight;
        }

        private static Button CreateActionButton(
            string name, Transform parent, string label, float width)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image),
                typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.GetComponent<Image>();
            image.color = ActionTeal;
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            AddFocusIndicator(button);
            var layout = buttonObject.GetComponent<LayoutElement>();
            layout.minHeight = 64f;
            layout.preferredHeight = 64f;
            layout.preferredWidth = width;

            var labelText = CreateText("Label", buttonObject.transform, 18, FontStyle.Bold, LightText);
            Stretch(labelText.GetComponent<RectTransform>());
            labelText.text = label;
            return button;
        }

        private static void AddFocusIndicator(Selectable selectable)
        {
            var outline = selectable.targetGraphic.gameObject.AddComponent<Outline>();
            outline.effectColor = FocusGold;
            outline.effectDistance = new Vector2(4f, -4f);
            outline.useGraphicAlpha = false;
            outline.enabled = false;
            selectable.gameObject.AddComponent<SelectableFocusIndicator>()
                .Configure(selectable, outline);
        }

        private static Text CreateText(
            string name, Transform parent, int fontSize, FontStyle style, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            text.text = string.Empty;
            return text;
        }

        private static void SetBestFit(Text text, int minimum, int maximum)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minimum;
            text.resizeTextMaxSize = maximum;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void SetNavigation(
            Selectable selectable, Selectable up, Selectable down, Selectable left, Selectable right)
        {
            var navigation = selectable.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = up;
            navigation.selectOnDown = down;
            navigation.selectOnLeft = left;
            navigation.selectOnRight = right;
            selectable.navigation = navigation;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void StretchBeyond(RectTransform rect, float margin)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-margin, -margin);
            rect.offsetMax = new Vector2(margin, margin);
        }

        private static void SetReference(
            UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            if (value == null)
            {
                throw new InvalidOperationException($"Cannot assign missing reference '{propertyName}'.");
            }
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Missing serialized property '{propertyName}'.");
            }
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetReferenceArray<T>(
            UnityEngine.Object target, string propertyName, IReadOnlyList<T> values)
            where T : UnityEngine.Object
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Missing serialized property '{propertyName}'.");
            }
            property.arraySize = values.Count;
            for (var index = 0; index < values.Count; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetStringArray(
            UnityEngine.Object target, string propertyName, IReadOnlyList<string> values)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Missing serialized property '{propertyName}'.");
            }
            property.arraySize = values.Count;
            for (var index = 0; index < values.Count; index++)
            {
                property.GetArrayElementAtIndex(index).stringValue = values[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
