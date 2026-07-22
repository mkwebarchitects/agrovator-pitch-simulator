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
        public RotateToPlayOverlay RotatePrompt;
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
        internal const string PartIconSheetPath = "Assets/Art/UI/part-icons.png";
        private const float FrameWidth = 980f;
        private const float FrameHeight = 672f;
        // Internal so the project builder's idempotence guard can compare a saved
        // scene against the tuning this builder would produce today.
        internal const float JudgeBlinkIntervalSeconds = 5f;
        internal const float JudgeBlinkDurationSeconds = 0.12f;
        internal const float JudgeTalkFrameSeconds = 0.18f;
        // Long enough for a learner to register the reaction to their statement
        // before Aya relaxes, short enough that she is never holding a Concerned
        // face at them while they read the feedback.
        internal const float JudgeSemanticHoldSeconds = 2.5f;
        private static readonly Color ScreenDim = new Color(0.055f, 0.09f, 0.12f, 1f);
        private static readonly Color DeepNavy = new Color32(0x0E, 0x17, 0x1F, 0xFF);
        private static readonly Color CardNavy = new Color32(0x16, 0x24, 0x2F, 0xFF);
        // The guided frame is see-through so the pitch room reads as a place behind
        // it rather than a sliver at the edges. 0.82 is the most transparent value
        // that still clears every contrast assertion when composited over the room's
        // brightest pixel, which is the whiteboard. The inner cards stay opaque:
        // they carry the sentence text and must not sit on a moving backdrop.
        internal static readonly Color TranslucentPanel = new Color(
            DeepNavy.r, DeepNavy.g, DeepNavy.b, 0.82f);
        private static readonly Color LightText = new Color32(0xFF, 0xF8, 0xE8, 0xFF);
        private static readonly Color Ink = new Color32(0x0E, 0x17, 0x1F, 0xFF);
        private static readonly Color Cream = new Color32(0xF4, 0xEA, 0xD5, 0xFF);
        private static readonly Color FocusGold = new Color32(0xFF, 0xD1, 0x66, 0xFF);
        private static readonly Color ActionTeal = new Color(0.08f, 0.42f, 0.34f, 1f);
        // Internal so the results contract can assert the meter stays legible in
        // both states rather than trusting two colours chosen by eye.
        internal static readonly Color MeterFilled = new Color32(0xFF, 0xD1, 0x66, 0xFF);
        internal static readonly Color MeterEmpty = new Color32(0x67, 0x6F, 0x77, 0xFF);
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
            // Built last so it is the canvas's last child and draws over whichever
            // screen is current. It is not a screen and the router never owns it.
            CreateRotatePrompt(canvas, ayaSprites, references);

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
            var heading = CreateText("Heading", frame.transform, 40, FontStyle.Bold, LightText, display: true);
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
            frame.GetComponent<Image>().color = TranslucentPanel;

            var rail = CreateProgressRail(frame.transform);
            var ayaRow = CreateAyaRow(frame.transform, ayaSprites, out var questionText,
                out var hintText, out var judge);
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
                phaseScroll,
                judge);
            responsive.Configure(
                canvas.GetComponent<Canvas>(),
                board.GetComponent<GridLayoutGroup>(),
                cards.GetComponent<GridLayoutGroup>(),
                phaseScroll,
                modeSelectionPanel.GetComponent<GuidedPitchFlowLayout>(),
                strengthenButtons[0].transform.parent.GetComponent<GuidedPitchFlowLayout>(),
                primaryAction.GetComponent<GuidedPitchFlowLayout>(),
                environmentAspect);
            ApplyWideLayoutDefaults(board.GetComponent<GridLayoutGroup>(),
                cards.GetComponent<GridLayoutGroup>(), phaseScroll, environmentAspect);
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
            // minHeight, not just preferred: on a short landscape-phone viewport the
            // frame overflows and the layout squeezes children from preferred toward
            // their minimum. Without a floor the rail collapsed to zero height and
            // its part labels vanished, taking the four-part concept with them. The
            // floor keeps the rail readable; the phase scroll gives up its height
            // instead.
            var railLayout = root.GetComponent<LayoutElement>();
            railLayout.preferredHeight = 40f;
            // Below where the rail settles on the tall compact phone, so it does not
            // steal height from the card scroll there, but enough to keep a readable
            // line box (32 minus 8px padding) when a short landscape phone compresses
            // the frame hard and would otherwise collapse the rail labels to nothing.
            railLayout.minHeight = 32f;

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
                var icon = CreatePartIcon(slot.transform, visual.Part, 22f);
                var iconLayout = icon.gameObject.AddComponent<LayoutElement>();
                iconLayout.preferredWidth = 18f;
                var label = CreateText("Label", slot.transform, 14, FontStyle.Bold, LightText,
                    TextAnchor.MiddleLeft, display: true);
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
            Transform frame, Sprite[] ayaSprites, out Text questionText, out Text hintText,
            out JudgeReactionView judgeView)
        {
            var row = new GameObject("Aya Row", typeof(RectTransform),
                typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(frame, false);
            ConfigureRow(row.GetComponent<HorizontalLayoutGroup>(), 12f, expandWidth: false,
                expandHeight: true);
            row.GetComponent<LayoutElement>().preferredHeight = 160f;

            var judge = new GameObject("Judge Aya", typeof(RectTransform), typeof(Image),
                typeof(LayoutElement), typeof(JudgeReactionView));
            judge.transform.SetParent(row.transform, false);
            var judgeImage = judge.GetComponent<Image>();
            judgeImage.sprite = ResolveAyaSprite(ayaSprites, JudgeReaction.Encouraging);
            judgeImage.preserveAspect = true;
            judgeImage.raycastTarget = false;
            var judgeLayout = judge.GetComponent<LayoutElement>();
            // Every sheet slice shares one cell size, so the reserved row size holds
            // as the portrait swaps and the art is never scaled to fit.
            judgeLayout.preferredWidth = judgeImage.sprite.rect.width;
            judgeLayout.preferredHeight = judgeImage.sprite.rect.height;
            judgeLayout.flexibleWidth = 0f;

            judgeView = judge.GetComponent<JudgeReactionView>();
            var spriteSet = new JudgeReactionSpriteSet();
            foreach (JudgeReaction reaction in Enum.GetValues(typeof(JudgeReaction)))
            {
                spriteSet.Set(reaction, ResolveAyaSprite(ayaSprites, reaction));
            }
            // Configure applies the resting portrait itself, which is also what
            // the saved scene must show before any presenter runs.
            judgeView.Configure(judgeImage, spriteSet, JudgeBlinkIntervalSeconds,
                JudgeBlinkDurationSeconds, JudgeTalkFrameSeconds, JudgeSemanticHoldSeconds);

            // Aya's line sits in a rounded speech bubble rather than a flat panel.
            // The bubble is a purpose-drawn nine-slice: navy fill and a thin gold
            // border baked in, tinted white so it shows as drawn. Its fill is
            // CardNavy, so the light text keeps the same contrast it had on the flat
            // card; the border carries the speech-bubble read, replacing the old
            // gold accent bar.
            var card = new GameObject("Dialogue Card", typeof(RectTransform), typeof(Image),
                typeof(VerticalLayoutGroup), typeof(LayoutElement));
            card.transform.SetParent(row.transform, false);
            var cardImage = card.GetComponent<Image>();
            cardImage.sprite = LoadSpeechBubble();
            cardImage.type = Image.Type.Sliced;
            cardImage.color = Color.white;
            cardImage.raycastTarget = false;
            var cardLayout = card.GetComponent<VerticalLayoutGroup>();
            ConfigureColumn(cardLayout, 6f, expandWidth: true, expandHeight: false);
            // The 22px nine-slice border is a slicing boundary, not dead space - the
            // straight top edge is only the thin gold line - so the same text padding
            // the flat card used still clears it and keeps the original line room.
            cardLayout.padding = new RectOffset(24, 16, 14, 14);
            cardLayout.childAlignment = TextAnchor.MiddleLeft;
            var cardElement = card.GetComponent<LayoutElement>();
            cardElement.flexibleWidth = 1f;
            cardElement.preferredHeight = 160f;

            questionText = CreateText("Question", card.transform, 16, FontStyle.Normal, LightText,
                TextAnchor.MiddleLeft);
            hintText = CreateText("Hint", card.transform, 13, FontStyle.Italic, LightText,
                TextAnchor.MiddleLeft);
            return row;
        }

        /// <summary>
        /// Resolves one reaction to the identically named slice of the Judge Aya
        /// sheet. A missing slice is a build-stopping content error rather than a
        /// silent fallback, so a renamed slice can never quietly freeze her face.
        /// </summary>
        private static Sprite ResolveAyaSprite(Sprite[] ayaSprites, JudgeReaction reaction)
        {
            var name = reaction.ToString();
            var sprite = ayaSprites.FirstOrDefault(candidate =>
                string.Equals(candidate.name, name, StringComparison.Ordinal));
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"Judge Aya sheet is missing the '{name}' slice.");
            }

            return sprite;
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
                var icon = CreatePartIcon(header.transform, visual.Part, 20f);
                var iconLayout = icon.gameObject.AddComponent<LayoutElement>();
                iconLayout.preferredWidth = 16f;
                var label = CreateText("Label", header.transform, 13, FontStyle.Bold, LightText,
                    TextAnchor.MiddleLeft, display: true);
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
            // The focus/hover outline (AddFocusIndicator) draws a few pixels
            // beyond its control's own rect. The last row in this content
            // (typically the strengthen buttons) sits with zero padding against
            // this mask, so that outline edge was clipped exactly at the
            // boundary. A small negative padding lets the mask itself render a
            // few pixels wider without changing the content's own layout,
            // spacing or ContentSizeFitter-driven height.
            viewport.GetComponent<RectMask2D>().padding = new Vector4(-OutlineOverflow, -OutlineOverflow,
                -OutlineOverflow, -OutlineOverflow);

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
            root.GetComponent<GuidedPitchFlowLayout>().Configure(150f, 12f, 96f);

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
                    TextAnchor.MiddleLeft, display: true);
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

                // The composed pitch sentence is body text, so it keeps the legible
                // built-in font rather than the decorative display face.
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
                    TextAnchor.UpperLeft, display: true);
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

                // The icon goes straight into the button's layout row. Nesting it in
                // a plain Image chip left CreatePartIcon's LayoutElement with no
                // layout group to honour, so the icon rendered at its default rect
                // and ballooned to the full button height.
                var icon = CreatePartIcon(buttonObject.transform, visual.Part, 26f);

                var label = CreateText("Label", buttonObject.transform, 15, FontStyle.Bold, LightText,
                    TextAnchor.MiddleLeft, display: true);
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

        // The Phase Scroll's own fixed budget (see CreatePhaseScroll) minus this
        // row's 14px top/bottom padding and a margin for the still-active but
        // visually collapsed sibling rows (Sentence Cards, Feedback, Improve
        // Actions, Follow Up), which each still contribute one 8px spacing gap
        // even at zero height. Kept comfortably under that ceiling so the
        // composed pitch's own row can never exceed the viewport's mask.
        private const float PresentationMaxHeight = 160f;

        private static Text CreatePresentPitch(Transform content)
        {
            var root = new GameObject("Present Pitch", typeof(RectTransform),
                typeof(VerticalLayoutGroup));
            root.transform.SetParent(content, false);
            var layout = root.GetComponent<VerticalLayoutGroup>();
            ConfigureColumn(layout, 0f, expandWidth: true, expandHeight: false);
            layout.padding = new RectOffset(0, 0, 14, 14);
            var presentation = CreateText("Presentation", root.transform, 14, FontStyle.Normal, LightText,
                TextAnchor.UpperLeft);
            // The composed pitch is four learner-chosen sentences, so its length
            // varies with which options were picked and can run well past any
            // single authored line. Capping the row's height and letting the
            // font shrink to fit removes the ceiling on how long a real
            // combination can safely be, instead of chasing a fixed pixel
            // budget for whichever combination happens to render next - the
            // failure this replaces was exactly that: a real combination longer
            // than the two representative strings the previous layout was
            // sized against overflowed the Phase Scroll mask and was clipped.
            presentation.gameObject.AddComponent<LayoutElement>().preferredHeight = PresentationMaxHeight;
            SetBestFit(presentation, 10, 14);
            return presentation;
        }

        private static GameObject CreateResultsScreen(
            Transform canvas, GuidedPitchSceneReferences references)
        {
            var panel = CreateScreen("Results", canvas);
            var presenter = panel.AddComponent<GuidedPitchResultsPresenter>();
            // Vertical padding trimmed 12 to 4 to pay for the readiness row. The
            // scroll viewport height does not move the fixed submission status, so
            // the height the row needs has to come out of the frame itself.
            var frame = CreateFrame(panel.transform, FrameWidth, FrameHeight, 16, 4, 8f);

            var heading = CreateText("Heading", frame.transform, 26, FontStyle.Bold, LightText, display: true);
            heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;

            var scrollObject = new GameObject("Results Scroll", typeof(RectTransform),
                typeof(ScrollRect), typeof(LayoutElement));
            scrollObject.transform.SetParent(frame.transform, false);
            var scrollLayout = scrollObject.GetComponent<LayoutElement>();
            // Trimmed slightly when the readiness row grew; the row shares its
            // height with the meter so the cost is one line, not two.
            scrollLayout.preferredHeight = 460f;
            scrollLayout.flexibleHeight = 1f;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollObject.transform, false);
            Stretch(viewport.GetComponent<RectTransform>());
            // See the matching comment in CreatePhaseScroll: keeps a focus/hover
            // outline on the first or last card from being clipped at the mask
            // boundary. This content already carries its own 12px padding, so
            // this is defense in depth rather than a fix for an observed clip.
            viewport.GetComponent<RectMask2D>().padding = new Vector4(-OutlineOverflow, -OutlineOverflow,
                -OutlineOverflow, -OutlineOverflow);

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
            ConfigureColumn(contentLayout, 5f, expandWidth: true, expandHeight: false);
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

            // The score of the whole session, sized like a result rather than a
            // caption, with a meter that gives it a shape readable at a glance.
            // Score and meter share one row. Stacking them cost a full row of
            // height and pushed the final pitch under the fixed submission status,
            // which GeneratedResults_SecondaryPitchClearsMaskAndFixedActions caught.
            var readinessRow = CreateRow("Readiness Row", contentObject.transform, 12f, 34f);
            var readiness = CreateText("Readiness", readinessRow.transform, 24, FontStyle.Bold,
                FocusGold, TextAnchor.MiddleLeft, display: true);
            var readinessLayout = readiness.gameObject.AddComponent<LayoutElement>();
            readinessLayout.preferredWidth = 330f;
            readinessLayout.flexibleWidth = 0f;
            // The display face is wider than the built-in font, so best-fit keeps the
            // readiness line inside its box rather than clipping "Pitch Readiness: 100%".
            SetBestFit(readiness, 16, 24);
            var meterSegments = CreateReadinessMeter(readinessRow.transform);
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
            SetReferenceArray(presenter, "readinessSegments", meterSegments);
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
                typeof(VerticalLayoutGroup), typeof(CanvasGroup), typeof(PitchResultPartView));
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
            var icon = CreatePartIcon(header.transform, visual.Part, 20f);
            icon.gameObject.AddComponent<LayoutElement>().preferredWidth = 18f;
            var label = CreateText("Label", header.transform, 14, FontStyle.Bold, LightText,
                TextAnchor.MiddleLeft, display: true);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var statusText = CreateText("Status", header.transform, 13, FontStyle.Bold, FocusGold,
                TextAnchor.MiddleRight, display: true);
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
            var heading = CreateText("Heading", frame.transform, 30, FontStyle.Bold, LightText, display: true);
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

        private static void CreateRotatePrompt(
            Transform canvas, Sprite[] ayaSprites, GuidedPitchSceneReferences references)
        {
            // The host stays enabled so it can poll the viewport every frame; only
            // the panel beneath it is toggled, which is what the learner sees.
            var host = new GameObject("Rotate To Play", typeof(RectTransform),
                typeof(RotateToPlayOverlay));
            host.transform.SetParent(canvas, false);
            Stretch(host.GetComponent<RectTransform>());

            var panel = CreateScreen("Panel", host.transform);
            // Opaque, and a raycast target, so the prompt both covers the game and
            // swallows taps aimed at the controls underneath it.
            var backing = panel.GetComponent<Image>();
            backing.color = DeepNavy;
            backing.raycastTarget = true;

            var frame = CreateFrame(panel.transform, 660f, 460f, 24, 24, 20f);

            // Aya carries the instruction so a blocked screen still reads as part of
            // the game rather than an error page. She already exists, so this costs
            // no new art and no licensing question.
            var aya = new GameObject("Judge Aya", typeof(RectTransform), typeof(Image),
                typeof(LayoutElement));
            aya.transform.SetParent(frame.transform, false);
            var ayaImage = aya.GetComponent<Image>();
            ayaImage.sprite = ResolveAyaSprite(ayaSprites, JudgeReaction.Encouraging);
            ayaImage.preserveAspect = true;
            ayaImage.raycastTarget = false;
            var ayaLayout = aya.GetComponent<LayoutElement>();
            ayaLayout.preferredWidth = 176f;
            ayaLayout.preferredHeight = 220f;
            ayaLayout.flexibleWidth = 0f;
            ayaLayout.flexibleHeight = 0f;

            // This is the only thing on screen and is read at arm's length while the
            // learner turns the device, so it is sized as a headline, not a caption.
            var title = CreateText("Title", frame.transform, 44, FontStyle.Bold, FocusGold, display: true);
            title.alignment = TextAnchor.MiddleCenter;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;

            var body = CreateText("Body", frame.transform, 24, FontStyle.Normal, LightText);
            body.alignment = TextAnchor.MiddleCenter;
            body.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;

            title.text = RotateToPlayOverlay.EnglishTitle;
            body.text = RotateToPlayOverlay.EnglishBody;

            var overlay = host.GetComponent<RotateToPlayOverlay>();
            overlay.Configure(panel, title, body);
            references.RotatePrompt = overlay;
        }

        // One segment per pitch part, so the meter reads as the four parts the
        // learner just built rather than an arbitrary bar. Filled state is applied
        // by the presenter from the assessment; nothing here animates, so the
        // display is identical with and without reduced motion.
        private static Image[] CreateReadinessMeter(Transform parent)
        {
            var meter = CreateRow("Readiness Meter", parent, 6f, 16f);
            var segments = new Image[4];
            for (var index = 0; index < segments.Length; index++)
            {
                var segment = CreateImage("Segment " + (index + 1), meter.transform, MeterEmpty);
                var layout = segment.gameObject.AddComponent<LayoutElement>();
                layout.preferredHeight = 16f;
                layout.flexibleWidth = 1f;
                segments[index] = segment;
            }
            return segments;
        }

        private static void ApplyWideLayoutDefaults(
            GridLayoutGroup board, GridLayoutGroup cards, ScrollRect phaseScroll,
            AspectRatioFitter environment)
        {
            // The committed scene is the wide layout, so bake the wide fill too.
            // Leaving it letterboxed made the room snap from bands to full bleed on
            // the first frame the responsive layout ran.
            environment.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
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

        // The four parts carry real icons rather than the ASCII placeholders they
        // shipped with. A part's icon never changes at runtime, so the sprite is
        // serialized here once and no view assigns it.
        private static Image CreatePartIcon(Transform parent, PitchPart part, float size)
        {
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image),
                typeof(LayoutElement));
            icon.transform.SetParent(parent, false);
            var image = icon.GetComponent<Image>();
            image.sprite = ResolvePartIcon(part);
            image.preserveAspect = true;
            image.raycastTarget = false;
            var layout = icon.GetComponent<LayoutElement>();
            layout.preferredWidth = size;
            layout.preferredHeight = size;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
            return image;
        }

        private const string SpeechBubblePath = "Assets/Art/UI/speech-bubble.png";

        private static Sprite LoadSpeechBubble()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpeechBubblePath);
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"The speech bubble sprite is missing at {SpeechBubblePath}.");
            }
            return sprite;
        }

        private static Sprite ResolvePartIcon(PitchPart part)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(PartIconSheetPath)
                .OfType<Sprite>()
                .ToArray();
            var name = part.ToString();
            var sprite = sprites.FirstOrDefault(candidate => candidate.name == name);
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"The part icon sheet has no '{name}' cell.");
            }
            return sprite;
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

            var labelText = CreateText("Label", buttonObject.transform, 18, FontStyle.Bold, LightText, display: true);
            Stretch(labelText.GetComponent<RectTransform>());
            // Best-fit keeps the wider display face inside the button on longer
            // labels like "Present my pitch".
            SetBestFit(labelText, 12, 18);
            labelText.text = label;
            return button;
        }

        // How far AddFocusIndicator's Outline draws beyond its control's own
        // rect. Scrollable viewports pad their RectMask2D by this much so a
        // control sitting flush against the mask boundary (commonly the last
        // row in the list) does not have its outline clipped.
        private const float OutlineOverflow = 6f;

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

        // A warm, playful display face for the game's headings and short labels. Body
        // text keeps the legible built-in font: this face is decorative and would
        // slow reading of the pitch sentences and feedback. OFL-licensed, recorded in
        // docs/16-ASSET-MANIFEST.md.
        internal const string DisplayFontPath = "Assets/Art/Fonts/MysteryQuest-Regular.ttf";

        private static Font cachedDisplayFont;

        private static Font DisplayFont()
        {
            if (cachedDisplayFont == null)
            {
                cachedDisplayFont = AssetDatabase.LoadAssetAtPath<Font>(DisplayFontPath);
                if (cachedDisplayFont == null)
                {
                    throw new InvalidOperationException(
                        $"The display font is missing at {DisplayFontPath}.");
                }
            }
            return cachedDisplayFont;
        }

        private static Text CreateText(
            string name, Transform parent, int fontSize, FontStyle style, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter, bool display = false)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.font = display
                ? DisplayFont()
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            // Mystery Quest ships one regular weight. Asking for Bold makes Unity
            // synthesise a faux-bold that renders thick and fuzzy - the "too bold"
            // and "blurry" the display headings showed. The face already reads as
            // characterful, so display text always renders at its true weight;
            // italics still apply since callers use them for genuine emphasis.
            text.fontStyle = display && style == FontStyle.Bold ? FontStyle.Normal : style;
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
