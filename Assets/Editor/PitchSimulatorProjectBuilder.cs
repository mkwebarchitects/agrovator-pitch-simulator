using System;
using System.Collections.Generic;
using System.Linq;
using Agrovator.PitchSimulator.UI;
using Agrovator.PitchSimulator.Audio;
using Agrovator.PitchSimulator.LMS;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.Editor
{
    public static class PitchSimulatorProjectBuilder
    {
        private const string BootstrapPath = "Assets/Scenes/Bootstrap.unity";
        private const string GamePath = "Assets/Scenes/Game.unity";
        private const string WebTestPath = "Assets/Scenes/WebIntegrationTest.unity";
        private const string JudgeArtPath = "Assets/Art/Characters/judge-aya-sheet.png";
        private const string EnvironmentArtPath = "Assets/Art/Environment/pitch-room.png";
        private const string DialoguePanelArtPath = "Assets/Art/UI/dialogue-panel.png";
        private const string ConfidenceArtPath = "Assets/Art/UI/confidence-icons.png";
        private static readonly Color Ink = new Color32(0x0E, 0x17, 0x1F, 0xFF);
        private static readonly Color Cream = new Color32(0xF4, 0xEA, 0xD5, 0xFF);

        [MenuItem("Pitch Simulator/Build Project Foundation")]
        public static void BuildProjectFoundation()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("Pitch Simulator project foundation build cancelled.");
                return;
            }

            BuildProjectFoundationBatch();
        }

        public static void BuildProjectFoundationBatch()
        {
            EnsureFolder("Assets/Scenes");
            var originalActive = SceneManager.GetActiveScene();
            try
            {
                BuildBootstrapScene();
                BuildGameScene();
                BuildWebIntegrationTestScene();
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(BootstrapPath, true),
                    new EditorBuildSettingsScene(GamePath, true),
                };
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Pitch Simulator project foundation built successfully.");
            }
            finally
            {
                if (originalActive.IsValid() && originalActive.isLoaded)
                {
                    SceneManager.SetActiveScene(originalActive);
                }
            }
        }

        private static void BuildBootstrapScene()
        {
            var scene = OpenTarget(BootstrapPath, out var closeAfter);
            try
            {
                var root = ReplaceOwnedRoot(scene, "Generated Bootstrap");
                var bootstrapObject = new GameObject("Bootstrapper");
                bootstrapObject.transform.SetParent(root.transform, false);
                var bootstrapper = bootstrapObject.AddComponent<Bootstrapper>();
                var musicSource = bootstrapObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.spatialBlend = 0f;
                musicSource.loop = true;
                var sfxSource = bootstrapObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f;
                sfxSource.loop = false;
                SetReference(bootstrapper, "musicSource", musicSource);
                SetReference(bootstrapper, "sfxSource", sfxSource);
                SetAudioCueBindings(bootstrapper);
                SetReference(bootstrapper, "scenarioJson",
                    AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Content/Scenarios/smart-school-garden.en.json"));
                SetReference(bootstrapper, "englishCatalogJson",
                    AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Content/Localization/en.json"));
                SetReference(bootstrapper, "malayCatalogJson",
                    AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Content/Localization/ms.json"));
                EditorSceneManager.SaveScene(scene, BootstrapPath);
            }
            finally
            {
                CloseIfOwned(scene, closeAfter);
            }
        }

        private static void SetAudioCueBindings(Bootstrapper bootstrapper)
        {
            var serialized = new SerializedObject(bootstrapper);
            var bindings = serialized.FindProperty("audioCueBindings");
            var cues = (AudioCue[])Enum.GetValues(typeof(AudioCue));
            bindings.arraySize = cues.Length;
            for (var index = 0; index < cues.Length; index++)
            {
                var binding = bindings.GetArrayElementAtIndex(index);
                binding.FindPropertyRelative("cue").enumValueIndex = (int)cues[index];
                binding.FindPropertyRelative("clip").objectReferenceValue = null;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildGameScene()
        {
            var scene = OpenTarget(GamePath, out var closeAfter);
            try
            {
                var root = ReplaceOwnedRoot(scene, "Generated UI");

                var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                eventSystemObject.transform.SetParent(root.transform, false);
                var eventSystem = eventSystemObject.GetComponent<EventSystem>();

                var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
                    typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(root.transform, false);
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280f, 720f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                var router = canvasObject.AddComponent<GameScreenRouter>();
                var title = CreateTitleScreen(canvasObject.transform, out var titlePresenter);
                var briefing = CreateBriefingScreen(canvasObject.transform, out var briefingPresenter);
                var tutorial = CreateTutorialScreen(canvasObject.transform, out var tutorialPresenter);
                var pitch = CreatePitchRoomScreen(canvasObject.transform, out var pitchPresenter);
                var results = CreateResultsScreen(canvasObject.transform, out var resultsPresenter);
                var settings = CreateSettingsScreen(canvasObject.transform, out var settingsPresenter);

                var titleDefault = title.transform.Find("Content Frame/Start Button").GetComponent<Button>();
                var briefingDefault = briefing.transform.Find("Content Frame/Continue Button").GetComponent<Button>();
                var tutorialDefault = tutorial.transform.Find("Content Frame/Navigation/Next Button")
                    .GetComponent<Button>();
                var pitchDefault = pitch.transform.Find("Content Frame/Responses/Response 1").GetComponent<Button>();
                var pitchContinueDefault = pitch.transform.Find("Content Frame/Continue Button").GetComponent<Button>();
                var resultsDefault = results.transform.Find("Content Frame/Footer/Submit Button")
                    .GetComponent<Button>();
                var resultsSubmittingDefault = results.transform.Find("Content Frame/Results Scroll/Scrollbar")
                    .GetComponent<Scrollbar>();
                var resultsCompleteDefault = results.transform.Find("Content Frame/Footer/Retry Button")
                    .GetComponent<Button>();
                var settingsDefault = settings.transform.Find("Content Frame/Close Button").GetComponent<Button>();

                SetReference(router, "titlePanel", title);
                SetReference(router, "briefingPanel", briefing);
                SetReference(router, "tutorialPanel", tutorial);
                SetReference(router, "pitchRoomPanel", pitch);
                SetReference(router, "resultsPanel", results);
                SetReference(router, "settingsPanel", settings);
                SetReference(router, "titlePresenter", titlePresenter);
                SetReference(router, "briefingPresenter", briefingPresenter);
                SetReference(router, "tutorialPresenter", tutorialPresenter);
                SetReference(router, "pitchRoomPresenter", pitchPresenter);
                SetReference(router, "resultsPresenter", resultsPresenter);
                SetReference(router, "settingsPresenter", settingsPresenter);
                SetReference(router, "titleDefault", titleDefault);
                SetReference(router, "briefingDefault", briefingDefault);
                SetReference(router, "tutorialDefault", tutorialDefault);
                SetReference(router, "pitchRoomDefault", pitchDefault);
                SetReference(router, "pitchRoomContinueDefault", pitchContinueDefault);
                SetReference(router, "resultsDefault", resultsDefault);
                SetReference(router, "resultsSubmittingDefault", resultsSubmittingDefault);
                SetReference(router, "resultsCompleteDefault", resultsCompleteDefault);
                SetReference(router, "settingsDefault", settingsDefault);
                eventSystem.firstSelectedGameObject = titleDefault.gameObject;

                title.SetActive(true);
                briefing.SetActive(false);
                tutorial.SetActive(false);
                pitch.SetActive(false);
                results.SetActive(false);
                settings.SetActive(false);
                EditorSceneManager.SaveScene(scene, GamePath);
            }
            finally
            {
                CloseIfOwned(scene, closeAfter);
            }
        }

        private static void BuildWebIntegrationTestScene()
        {
            var scene = OpenTarget(WebTestPath, out var closeAfter);
            try
            {
                var root = ReplaceOwnedRoot(scene, "Generated Web Integration Test");
                var bridgeObject = new GameObject("Bridge Host");
                bridgeObject.transform.SetParent(root.transform, false);
                var bridgeHost = bridgeObject.AddComponent<WebGlLmsBridgeHost>();

                var canvasObject = new GameObject("Diagnostics Canvas", typeof(RectTransform),
                    typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(root.transform, false);
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280f, 720f);

                var panel = CreateLegacyLayoutScreen("Diagnostics", canvasObject.transform);
                CreateLabel("Heading", panel.transform, "Web LMS Integration Test", 40, FontStyle.Bold);
                var label = CreateLabel("Status", panel.transform,
                    "LMS bridge waiting for launch configuration.", 28);
                SetReference(bridgeHost, "diagnosticsLabel", label);
                EditorSceneManager.SaveScene(scene, WebTestPath);
            }
            finally
            {
                CloseIfOwned(scene, closeAfter);
            }
        }

        private static Scene OpenTarget(string path, out bool closeAfter)
        {
            var loaded = SceneManager.GetSceneByPath(path);
            if (loaded.IsValid() && loaded.isLoaded)
            {
                closeAfter = false;
                return loaded;
            }

            closeAfter = true;
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null
                ? EditorSceneManager.OpenScene(path, OpenSceneMode.Additive)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        }

        private static GameObject ReplaceOwnedRoot(Scene scene, string rootName)
        {
            foreach (var existing in scene.GetRootGameObjects())
            {
                if (string.Equals(existing.name, rootName, StringComparison.Ordinal))
                {
                    UnityEngine.Object.DestroyImmediate(existing);
                }
            }

            var root = new GameObject(rootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            return root;
        }

        private static void CloseIfOwned(Scene scene, bool closeAfter)
        {
            if (closeAfter && scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject CreateTitleScreen(Transform parent, out TitlePresenter presenter)
        {
            var panel = CreateScreen("Title", parent);
            presenter = panel.AddComponent<TitlePresenter>();
            var frame = CreateContentFrame(panel.transform, 760f, 500f);
            var heading = CreateLabel("Heading", frame.transform, "Pitch Simulator", 44, FontStyle.Bold);
            var subtitle = CreateLabel("Subtitle", frame.transform,
                "Practise a clear, confident innovation pitch.", 24);
            var start = CreateButton("Start Button", frame.transform, "Start Pitch");
            var settings = CreateButton("Settings Button", frame.transform, "Settings");
            SetPreferredWidth(heading, 680f);
            SetPreferredWidth(subtitle, 680f);
            SetPreferredWidth(start, 520f);
            SetPreferredWidth(settings, 420f);
            ConfigureVerticalNavigation(start, settings);
            SetReference(presenter, "startButton", start);
            SetReference(presenter, "settingsButton", settings);
            return panel;
        }

        private static GameObject CreateBriefingScreen(Transform parent, out BriefingPresenter presenter)
        {
            var panel = CreateScreen("Briefing", parent);
            presenter = panel.AddComponent<BriefingPresenter>();
            var frame = CreateContentFrame(panel.transform, 880f, 520f);
            var heading = CreateLabel("Heading", frame.transform, "Your Brief", 40, FontStyle.Bold);
            var brief = CreateLabel("Brief", frame.transform,
                "Pitch the Smart School Garden to a friendly youth innovation mentor.", 25);
            var continueButton = CreateButton("Continue Button", frame.transform, "Enter Pitch Room");
            SetPreferredWidth(heading, 680f);
            SetPreferredWidth(brief, 820f);
            SetPreferredWidth(continueButton, 520f);
            SetReference(presenter, "continueButton", continueButton);
            return panel;
        }

        private static GameObject CreateTutorialScreen(Transform parent, out TutorialPresenter presenter)
        {
            var panel = CreateScreen("Tutorial", parent);
            presenter = panel.AddComponent<TutorialPresenter>();

            var contentFrame = CreateContentFrame(panel.transform, 920f, 560f);

            var step = CreateLabel("Step", contentFrame.transform, string.Empty, 22, FontStyle.Bold);
            var heading = CreateLabel("Heading", contentFrame.transform, string.Empty, 40, FontStyle.Bold);
            var body = CreateLabel("Body", contentFrame.transform, string.Empty, 26);
            body.GetComponent<LayoutElement>().preferredHeight = 190f;
            SetPreferredWidth(step, 680f);
            SetPreferredWidth(heading, 680f);
            SetPreferredWidth(body, 820f);
            foreach (var text in new[] { step, heading, body })
            {
                text.color = Cream;
            }

            var navigation = new GameObject("Navigation", typeof(RectTransform),
                typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            navigation.transform.SetParent(contentFrame.transform, false);
            var navigationLayout = navigation.GetComponent<HorizontalLayoutGroup>();
            navigationLayout.spacing = 16f;
            navigationLayout.childAlignment = TextAnchor.MiddleCenter;
            navigationLayout.childControlWidth = true;
            navigationLayout.childControlHeight = true;
            navigationLayout.childForceExpandWidth = false;
            navigationLayout.childForceExpandHeight = false;
            navigation.GetComponent<LayoutElement>().preferredHeight = 72f;
            SetPreferredWidth(navigation.GetComponent<RectTransform>(), 820f);

            var back = CreateButton("Back Button", navigation.transform, "Back");
            var skip = CreateButton("Skip Button", navigation.transform, "Skip");
            var next = CreateButton("Next Button", navigation.transform, "Next");
            SetPreferredWidth(back, 180f);
            SetPreferredWidth(skip, 180f);
            SetPreferredWidth(next, 420f);
            ConfigureHorizontalNavigation(back, skip, next);

            SetReference(presenter, "stepText", step);
            SetReference(presenter, "headingText", heading);
            SetReference(presenter, "bodyText", body);
            SetReference(presenter, "backButton", back);
            SetReference(presenter, "skipButton", skip);
            SetReference(presenter, "nextButton", next);
            SetReference(presenter, "nextButtonText", next.transform.Find("Label").GetComponent<Text>());
            return panel;
        }

        private static GameObject CreatePitchRoomScreen(Transform parent, out PitchRoomPresenter presenter)
        {
            var panel = CreateScreen("PitchRoom", parent);
            presenter = panel.AddComponent<PitchRoomPresenter>();
            var environmentObject = new GameObject("Environment", typeof(RectTransform),
                typeof(Image), typeof(LayoutElement));
            environmentObject.transform.SetParent(panel.transform, false);
            environmentObject.transform.SetAsFirstSibling();
            Stretch(environmentObject.GetComponent<RectTransform>());
            environmentObject.GetComponent<LayoutElement>().ignoreLayout = true;
            var environmentImage = environmentObject.GetComponent<Image>();
            environmentImage.sprite = LoadSprite(EnvironmentArtPath);
            environmentImage.color = Color.white;
            environmentImage.raycastTarget = false;

            var contentFrame = CreateContentFrame(
                panel.transform, 960f, 680f, horizontalPadding: 24, verticalPadding: 24, spacing: 8f);

            var statusBacking = new GameObject("Status Backing", typeof(RectTransform),
                typeof(Image), typeof(LayoutElement));
            statusBacking.transform.SetParent(contentFrame.transform, false);
            statusBacking.GetComponent<Image>().color = Ink;
            statusBacking.GetComponent<Image>().raycastTarget = false;
            statusBacking.GetComponent<LayoutElement>().preferredHeight = 40f;
            SetPreferredWidth(statusBacking.transform, 860f);
            var status = CreateLabel("Status", statusBacking.transform, "Score 0", 22);
            status.color = Cream;
            Stretch(status.GetComponent<RectTransform>());
            var judgeObject = new GameObject("Judge Aya", typeof(RectTransform), typeof(Image),
                typeof(LayoutElement), typeof(JudgeReactionView));
            judgeObject.transform.SetParent(contentFrame.transform, false);
            var judgeLayout = judgeObject.GetComponent<LayoutElement>();
            judgeLayout.preferredWidth = 240f;
            judgeLayout.preferredHeight = 112f;
            var judgeImage = judgeObject.GetComponent<Image>();
            judgeImage.preserveAspect = true;
            judgeImage.raycastTarget = false;
            var judgeView = judgeObject.GetComponent<JudgeReactionView>();
            SetReference(judgeView, "portraitImage", judgeImage);
            SetJudgeSprites(judgeView);

            var dialoguePanel = new GameObject("Dialogue Panel", typeof(RectTransform),
                typeof(Image), typeof(LayoutElement));
            dialoguePanel.transform.SetParent(contentFrame.transform, false);
            var dialogueImage = dialoguePanel.GetComponent<Image>();
            dialogueImage.sprite = LoadSprite(DialoguePanelArtPath);
            dialogueImage.type = Image.Type.Sliced;
            dialogueImage.raycastTarget = false;
            dialoguePanel.GetComponent<LayoutElement>().preferredHeight = 96f;
            SetPreferredWidth(dialoguePanel.transform, 860f);
            var promptBacking = new GameObject("Prompt Backing", typeof(RectTransform), typeof(Image));
            promptBacking.transform.SetParent(dialoguePanel.transform, false);
            var promptBackingRect = promptBacking.GetComponent<RectTransform>();
            Stretch(promptBackingRect);
            promptBackingRect.offsetMin = new Vector2(28f, 18f);
            promptBackingRect.offsetMax = new Vector2(-28f, -18f);
            var promptBackingImage = promptBacking.GetComponent<Image>();
            promptBackingImage.color = Cream;
            promptBackingImage.raycastTarget = false;
            var prompt = CreateLabel("Prompt", promptBacking.transform,
                "Preparing your pitch…", 30, FontStyle.Bold);
            prompt.color = Ink;
            prompt.resizeTextForBestFit = true;
            prompt.resizeTextMinSize = 22;
            prompt.resizeTextMaxSize = 24;
            Stretch(prompt.GetComponent<RectTransform>());

            var metrics = new GameObject("Metrics", typeof(RectTransform),
                typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            metrics.transform.SetParent(contentFrame.transform, false);
            var metricsLayout = metrics.GetComponent<HorizontalLayoutGroup>();
            metricsLayout.spacing = 20f;
            metricsLayout.childAlignment = TextAnchor.MiddleCenter;
            metricsLayout.childControlWidth = true;
            metricsLayout.childControlHeight = true;
            metricsLayout.childForceExpandWidth = false;
            metricsLayout.childForceExpandHeight = false;
            metrics.GetComponent<LayoutElement>().preferredHeight = 48f;
            SetPreferredWidth(metrics.transform, 680f);

            var confidenceRoot = CreateIndicatorRoot("Confidence", metrics.transform);
            SetPreferredWidth(confidenceRoot.transform, 330f);
            var confidenceIcon = CreateLabel("Icon", confidenceRoot.transform, "[:]", 22, FontStyle.Bold);
            confidenceIcon.color = Cream;
            var confidenceArtworkObject = new GameObject("Artwork Icon", typeof(RectTransform),
                typeof(Image), typeof(LayoutElement));
            confidenceArtworkObject.transform.SetParent(confidenceRoot.transform, false);
            var confidenceArtwork = confidenceArtworkObject.GetComponent<Image>();
            confidenceArtwork.preserveAspect = true;
            confidenceArtwork.raycastTarget = false;
            var confidenceArtworkLayout = confidenceArtworkObject.GetComponent<LayoutElement>();
            confidenceArtworkLayout.preferredWidth = 48f;
            confidenceArtworkLayout.preferredHeight = 48f;
            var confidenceLabel = CreateLabel("Label", confidenceRoot.transform, "Curious", 22, FontStyle.Bold);
            confidenceLabel.color = Cream;
            var confidenceLabelLayout = confidenceLabel.GetComponent<LayoutElement>();
            confidenceLabelLayout.minWidth = 160f;
            confidenceLabelLayout.preferredWidth = 160f;
            var confidenceFill = CreateFilledBar("Fill", confidenceRoot.transform);
            confidenceFill.GetComponent<LayoutElement>().preferredWidth = 72f;
            var confidenceView = confidenceRoot.AddComponent<ConfidenceView>();
            confidenceRoot.GetComponent<LayoutElement>().preferredHeight = 48f;
            SetReference(confidenceView, "stateLabel", confidenceLabel);
            SetReference(confidenceView, "iconLabel", confidenceIcon);
            SetReference(confidenceView, "iconImage", confidenceArtwork);
            SetReferenceArray(confidenceView, "iconSprites", LoadSprites(ConfidenceArtPath));
            SetReference(confidenceView, "fillImage", confidenceFill);

            var timerRoot = CreateIndicatorRoot("Timer", metrics.transform);
            SetPreferredWidth(timerRoot.transform, 330f);
            timerRoot.GetComponent<LayoutElement>().preferredHeight = 48f;
            var timerSeconds = CreateLabel("Seconds", timerRoot.transform, "0", 22, FontStyle.Bold);
            timerSeconds.color = Cream;
            var timerFill = CreateFilledBar("Fill", timerRoot.transform);
            var timerView = timerRoot.AddComponent<TimerView>();
            SetReference(timerView, "secondsLabel", timerSeconds);
            SetReference(timerView, "fillImage", timerFill);
            SetReference(timerView, "pulseTarget", timerRoot.GetComponent<RectTransform>());

            var responseRoot = new GameObject("Responses", typeof(RectTransform),
                typeof(VerticalLayoutGroup), typeof(LayoutElement), typeof(ResponseListView));
            responseRoot.transform.SetParent(contentFrame.transform, false);
            var responseLayout = responseRoot.GetComponent<VerticalLayoutGroup>();
            responseLayout.spacing = 12f;
            responseLayout.childAlignment = TextAnchor.MiddleCenter;
            responseLayout.childControlWidth = true;
            responseLayout.childControlHeight = true;
            responseLayout.childForceExpandWidth = false;
            responseLayout.childForceExpandHeight = false;
            responseRoot.GetComponent<LayoutElement>().preferredHeight = 240f;
            SetPreferredWidth(responseRoot.transform, 680f);
            var slots = new ResponseButtonView[3];
            for (var index = 0; index < slots.Length; index++)
            {
                var button = CreateButton($"Response {index + 1}", responseRoot.transform,
                    $"Response {index + 1}");
                var buttonLayout = button.GetComponent<LayoutElement>();
                buttonLayout.minHeight = 72f;
                buttonLayout.preferredHeight = 72f;
                var responseLabel = button.GetComponentInChildren<Text>();
                responseLabel.fontSize = 22;
                responseLabel.lineSpacing = 0.9f;
                slots[index] = button.gameObject.AddComponent<ResponseButtonView>();
                SetReference(slots[index], "button", button);
                SetReference(slots[index], "label", responseLabel);
            }
            var responseList = responseRoot.GetComponent<ResponseListView>();
            SetReferenceArray(responseList, "slots", slots);

            var continueButton = CreateButton("Continue Button", contentFrame.transform, "Continue");
            continueButton.GetComponent<LayoutElement>().preferredHeight = 58f;
            SetPreferredWidth(continueButton.transform, 520f);
            SetReference(presenter, "promptText", prompt);
            SetReference(presenter, "statusText", status);
            SetReference(presenter, "responseList", responseList);
            SetReference(presenter, "timerView", timerView);
            SetReference(presenter, "confidenceView", confidenceView);
            SetReference(presenter, "judgeReactionView", judgeView);
            SetReference(presenter, "continueButton", continueButton);
            return panel;
        }

        private static GameObject CreateIndicatorRoot(string name, Transform parent)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup),
                typeof(LayoutElement), typeof(Image));
            root.transform.SetParent(parent, false);
            var backing = root.GetComponent<Image>();
            backing.color = Ink;
            backing.raycastTarget = false;
            var layout = root.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 4, 4);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            root.GetComponent<LayoutElement>().preferredHeight = 54f;
            return root;
        }

        private static Image CreateFilledBar(string name, Transform parent)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            root.transform.SetParent(parent, false);
            var image = root.GetComponent<Image>();
            image.color = new Color(0.93f, 0.76f, 0.2f, 1f);
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
            image.fillAmount = 0.5f;
            var layout = root.GetComponent<LayoutElement>();
            layout.preferredWidth = 240f;
            layout.preferredHeight = 22f;
            return image;
        }

        private static GameObject CreateResultsScreen(Transform parent, out ResultsPresenter presenter)
        {
            var panel = CreateScreen("Results", parent);
            presenter = panel.AddComponent<ResultsPresenter>();
            var contentFrame = CreateContentFrame(
                panel.transform, 960f, 680f, horizontalPadding: 32, verticalPadding: 20, spacing: 12f);

            var heading = CreateLabel("Heading", contentFrame.transform, "Results", 36, FontStyle.Bold);
            heading.GetComponent<LayoutElement>().preferredHeight = 50f;

            var scrollObject = new GameObject("Results Scroll", typeof(RectTransform), typeof(Image),
                typeof(ScrollRect), typeof(LayoutElement));
            scrollObject.transform.SetParent(contentFrame.transform, false);
            var scrollBackground = new Color(0.075f, 0.12f, 0.15f, 1f);
            scrollObject.GetComponent<Image>().color = scrollBackground;
            var scrollLayout = scrollObject.GetComponent<LayoutElement>();
            scrollLayout.minHeight = 280f;
            scrollLayout.preferredHeight = 450f;
            scrollLayout.flexibleHeight = 1f;
            scrollLayout.preferredWidth = 860f;

            var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportObject.transform.SetParent(scrollObject.transform, false);
            Stretch(viewportObject.GetComponent<RectTransform>());
            viewportObject.GetComponent<RectTransform>().offsetMax = new Vector2(-64f, 0f);
            viewportObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);

            var contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewportObject.transform, false);
            var contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            var contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(28, 28, 24, 24);
            contentLayout.spacing = 12f;
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewportObject.GetComponent<RectTransform>();
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            var scrollbarObject = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image),
                typeof(KeyboardReviewScrollbar));
            scrollbarObject.transform.SetParent(scrollObject.transform, false);
            var scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.anchoredPosition = Vector2.zero;
            scrollbarRect.sizeDelta = new Vector2(64f, 0f);
            var scrollbarHitTarget = scrollbarObject.GetComponent<Image>();
            scrollbarHitTarget.color = Color.clear;
            scrollbarHitTarget.raycastTarget = true;

            var focusObject = new GameObject("Focus Indicator", typeof(RectTransform), typeof(Image));
            focusObject.transform.SetParent(scrollbarObject.transform, false);
            var focusRect = focusObject.GetComponent<RectTransform>();
            focusRect.anchorMin = new Vector2(0.5f, 0f);
            focusRect.anchorMax = new Vector2(0.5f, 1f);
            focusRect.pivot = new Vector2(0.5f, 0.5f);
            focusRect.anchoredPosition = Vector2.zero;
            focusRect.sizeDelta = new Vector2(48f, 0f);
            var focusImage = focusObject.GetComponent<Image>();
            focusImage.color = Color.white;
            focusImage.raycastTarget = false;

            var trackObject = new GameObject("Track", typeof(RectTransform), typeof(Image));
            trackObject.transform.SetParent(scrollbarObject.transform, false);
            var trackRect = trackObject.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0.5f, 0f);
            trackRect.anchorMax = new Vector2(0.5f, 1f);
            trackRect.pivot = new Vector2(0.5f, 0.5f);
            trackRect.anchoredPosition = Vector2.zero;
            trackRect.sizeDelta = new Vector2(32f, 0f);
            var trackImage = trackObject.GetComponent<Image>();
            trackImage.color = new Color(0.12f, 0.19f, 0.22f, 1f);
            trackImage.raycastTarget = false;

            var slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
            slidingArea.transform.SetParent(trackObject.transform, false);
            Stretch(slidingArea.GetComponent<RectTransform>());
            slidingArea.GetComponent<RectTransform>().offsetMin = new Vector2(4f, 4f);
            slidingArea.GetComponent<RectTransform>().offsetMax = new Vector2(-4f, -4f);
            var handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleObject.transform.SetParent(slidingArea.transform, false);
            Stretch(handleObject.GetComponent<RectTransform>());
            var handleImage = handleObject.GetComponent<Image>();
            handleImage.color = new Color(0.93f, 0.76f, 0.2f, 1f);
            var scrollbar = scrollbarObject.GetComponent<KeyboardReviewScrollbar>();
            scrollbar.handleRect = handleObject.GetComponent<RectTransform>();
            scrollbar.targetGraphic = focusImage;
            var scrollbarColors = scrollbar.colors;
            scrollbarColors.normalColor = scrollBackground;
            scrollbarColors.highlightedColor = Cream;
            scrollbarColors.pressedColor = new Color(0.93f, 0.76f, 0.2f, 1f);
            scrollbarColors.selectedColor = Cream;
            scrollbarColors.disabledColor = scrollBackground;
            scrollbarColors.colorMultiplier = 1f;
            scrollbarColors.fadeDuration = 0f;
            scrollbar.colors = scrollbarColors;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.numberOfSteps = 20;
            scrollbar.size = 0.2f;
            scrollbar.value = 1f;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            var level = CreateLabel("Level", contentObject.transform, "Level", 30, FontStyle.Bold);
            var overall = CreateLabel("Overall", contentObject.transform, "Overall 0", 24);
            var confidence = CreateLabel("Final Confidence", contentObject.transform, "Final confidence 0", 24);
            var pitching = CreateLabel("Pitching", contentObject.transform, "Pitching 0", 24);
            var communications = CreateLabel("Communications", contentObject.transform, "Communication 0", 24);
            var strengthsHeading = CreateLabel("Strengths Heading", contentObject.transform, "Strengths", 26,
                FontStyle.Bold);
            var strengths = new[]
            {
                CreateLabel("Strength 1", contentObject.transform, string.Empty, 22),
                CreateLabel("Strength 2", contentObject.transform, string.Empty, 22),
            };
            var improvementsHeading = CreateLabel("Improvements Heading", contentObject.transform, "Improvements",
                26, FontStyle.Bold);
            var improvements = new[]
            {
                CreateLabel("Improvement 1", contentObject.transform, string.Empty, 22),
                CreateLabel("Improvement 2", contentObject.transform, string.Empty, 22),
            };
            var reviewHeading = CreateLabel("Review Heading", contentObject.transform, "Review your choices", 28,
                FontStyle.Bold);
            var reviewItems = new QuestionReviewItemView[6];
            for (var index = 0; index < reviewItems.Length; index++)
            {
                reviewItems[index] = CreateQuestionReviewItem(index + 1, contentObject.transform);
            }

            foreach (var text in contentObject.GetComponentsInChildren<Text>(true))
            {
                text.alignment = TextAnchor.MiddleLeft;
            }

            var status = CreateLabel("Submission Status", contentFrame.transform, "", 22, FontStyle.Bold);
            status.GetComponent<LayoutElement>().preferredHeight = 40f;
            var footer = new GameObject("Footer", typeof(RectTransform), typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            footer.transform.SetParent(contentFrame.transform, false);
            var footerLayout = footer.GetComponent<HorizontalLayoutGroup>();
            footerLayout.spacing = 20f;
            footerLayout.childAlignment = TextAnchor.MiddleCenter;
            footerLayout.childControlWidth = true;
            footerLayout.childControlHeight = true;
            footerLayout.childForceExpandWidth = false;
            footerLayout.childForceExpandHeight = false;
            footer.GetComponent<LayoutElement>().preferredHeight = 64f;
            var submit = CreateButton("Submit Button", footer.transform, "Submit Results");
            var retry = CreateButton("Retry Button", footer.transform, "Try Again");
            foreach (var button in new[] { submit, retry })
            {
                var layout = button.GetComponent<LayoutElement>();
                layout.preferredWidth = 260f;
                layout.preferredHeight = 64f;
            }
            ConfigureVerticalNavigation(scrollbar, submit, retry);
            SetReference(presenter, "headingText", heading);
            SetReference(presenter, "levelText", level);
            SetReference(presenter, "overallText", overall);
            SetReference(presenter, "confidenceText", confidence);
            SetReference(presenter, "pitchingText", pitching);
            SetReference(presenter, "communicationsText", communications);
            SetReference(presenter, "strengthsHeadingText", strengthsHeading);
            SetReferenceArray(presenter, "strengthTexts", strengths);
            SetReference(presenter, "improvementsHeadingText", improvementsHeading);
            SetReferenceArray(presenter, "improvementTexts", improvements);
            SetReference(presenter, "reviewHeadingText", reviewHeading);
            SetReference(presenter, "reviewScroll", scroll);
            SetReferenceArray(presenter, "reviewItems", reviewItems);
            SetReference(presenter, "submissionStatusText", status);
            SetReference(presenter, "submitButton", submit);
            SetReference(presenter, "submitButtonText", submit.transform.Find("Label").GetComponent<Text>());
            SetReference(presenter, "retryButton", retry);
            SetReference(presenter, "retryButtonText", retry.transform.Find("Label").GetComponent<Text>());
            return panel;
        }

        private static QuestionReviewItemView CreateQuestionReviewItem(int number, Transform parent)
        {
            var root = new GameObject($"Review Item {number}", typeof(RectTransform), typeof(Image),
                typeof(VerticalLayoutGroup), typeof(LayoutElement), typeof(QuestionReviewItemView));
            root.transform.SetParent(parent, false);
            root.GetComponent<Image>().color = Cream;
            var layout = root.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 18, 18);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            root.GetComponent<LayoutElement>().preferredHeight = 430f;

            var responseLabel = CreateLabel("Response Label", root.transform, "Your response", 18, FontStyle.Bold);
            var response = CreateLabel("Response", root.transform, string.Empty, 21);
            var feedbackLabel = CreateLabel("Feedback Label", root.transform, "Feedback", 18, FontStyle.Bold);
            var feedback = CreateLabel("Feedback", root.transform, string.Empty, 21);
            var explanationLabel = CreateLabel("Explanation Label", root.transform, "Stronger answer", 18,
                FontStyle.Bold);
            var explanation = CreateLabel("Explanation", root.transform, string.Empty, 21);
            foreach (var text in root.GetComponentsInChildren<Text>())
            {
                text.color = Ink;
                text.alignment = TextAnchor.UpperLeft;
            }
            foreach (var label in new[] { responseLabel, feedbackLabel, explanationLabel })
            {
                label.GetComponent<LayoutElement>().preferredHeight = 42f;
            }
            foreach (var value in new[] { response, feedback, explanation })
            {
                value.GetComponent<LayoutElement>().preferredHeight = 76f;
            }

            var view = root.GetComponent<QuestionReviewItemView>();
            SetReference(view, "responseLabel", responseLabel);
            SetReference(view, "responseText", response);
            SetReference(view, "feedbackLabel", feedbackLabel);
            SetReference(view, "feedbackText", feedback);
            SetReference(view, "explanationLabel", explanationLabel);
            SetReference(view, "explanationText", explanation);
            return view;
        }

        private static GameObject CreateSettingsScreen(Transform parent, out SettingsPresenter presenter)
        {
            var panel = CreateScreen("Settings", parent);
            presenter = panel.AddComponent<SettingsPresenter>();
            var frame = CreateContentFrame(panel.transform, 720f, 420f, horizontalPadding: 20);
            var heading = CreateLabel("Heading", frame.transform, "Settings", 40, FontStyle.Bold);
            var note = CreateLabel("Foundation Note", frame.transform,
                "Timer, motion, audio and language controls arrive in the next slice.", 24);
            var close = CreateButton("Close Button", frame.transform, "Back");
            SetPreferredWidth(heading, 680f);
            SetPreferredWidth(note, 680f);
            SetPreferredWidth(close, 420f);
            SetReference(presenter, "closeButton", close);
            return panel;
        }

        private static GameObject CreateScreen(string name, Transform parent)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            Stretch(panel.GetComponent<RectTransform>());
            panel.GetComponent<Image>().color = new Color(0.055f, 0.09f, 0.12f, 1f);
            return panel;
        }

        private static GameObject CreateContentFrame(
            Transform parent, float width = 920f, float height = 600f,
            int horizontalPadding = 36, int verticalPadding = 32, float spacing = 18f)
        {
            var frame = new GameObject("Content Frame", typeof(RectTransform), typeof(Image),
                typeof(VerticalLayoutGroup));
            frame.transform.SetParent(parent, false);
            var rect = frame.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            frame.GetComponent<Image>().color = new Color(0.055f, 0.105f, 0.13f, 0.96f);
            var layout = frame.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(horizontalPadding, horizontalPadding, verticalPadding, verticalPadding);
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return frame;
        }

        private static void SetPreferredWidth(Component component, float width)
        {
            var element = component.GetComponent<LayoutElement>() ??
                component.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = width;
        }

        private static GameObject CreateLegacyLayoutScreen(string name, Transform parent)
        {
            var panel = CreateScreen(name, parent);
            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(120, 120, 70, 70);
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return panel;
        }

        private static Text CreateLabel(
            string name,
            Transform parent,
            string value,
            int fontSize,
            FontStyle style = FontStyle.Normal)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            var layout = gameObject.GetComponent<LayoutElement>();
            layout.preferredHeight = Math.Max(64, fontSize * 3);
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image),
                typeof(Button), typeof(LayoutElement));
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            image.color = new Color(0.1f, 0.52f, 0.42f, 1f);
            var button = gameObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var layout = gameObject.GetComponent<LayoutElement>();
            layout.minHeight = 64f;
            layout.preferredHeight = 72f;
            layout.preferredWidth = 680f;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(gameObject.transform, false);
            Stretch(labelObject.GetComponent<RectTransform>());
            var text = labelObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = label;
            text.fontSize = 24;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        private static void ConfigureVerticalNavigation(params Selectable[] controls)
        {
            for (var index = 0; index < controls.Length; index++)
            {
                var navigation = controls[index].navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnUp = controls[(index - 1 + controls.Length) % controls.Length];
                navigation.selectOnDown = controls[(index + 1) % controls.Length];
                controls[index].navigation = navigation;
            }
        }

        private static void ConfigureHorizontalNavigation(params Selectable[] controls)
        {
            for (var index = 0; index < controls.Length; index++)
            {
                var navigation = controls[index].navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnLeft = index > 0 ? controls[index - 1] : null;
                navigation.selectOnRight = index < controls.Length - 1 ? controls[index + 1] : null;
                controls[index].navigation = navigation;
            }
        }

        private static void Stretch(RectTransform transform)
        {
            transform.anchorMin = Vector2.zero;
            transform.anchorMax = Vector2.one;
            transform.offsetMin = Vector2.zero;
            transform.offsetMax = Vector2.zero;
        }

        private static void SetReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            if (value == null)
            {
                throw new InvalidOperationException($"Cannot assign missing reference '{propertyName}'.");
            }
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetReferenceArray<T>(UnityEngine.Object target, string propertyName, IReadOnlyList<T> values)
            where T : UnityEngine.Object
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            property.arraySize = values.Count;
            for (var index = 0; index < values.Count; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException($"Missing required sprite '{path}'.");
            }
            return sprite;
        }

        private static Sprite[] LoadSprites(string path)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.rect.x)
                .ToArray();
            if (sprites.Length == 0)
            {
                throw new InvalidOperationException($"Missing required sprite sheet '{path}'.");
            }
            return sprites;
        }

        private static void SetJudgeSprites(JudgeReactionView view)
        {
            var sprites = LoadSprites(JudgeArtPath);
            var expected = (JudgeReaction[])Enum.GetValues(typeof(JudgeReaction));
            if (sprites.Length != expected.Length)
            {
                throw new InvalidOperationException(
                    $"Judge sprite sheet must contain {expected.Length} sprites, found {sprites.Length}.");
            }

            var byName = sprites.ToDictionary(sprite => sprite.name, StringComparer.Ordinal);
            var serialized = new SerializedObject(view);
            var spriteSet = serialized.FindProperty("sprites");
            foreach (var reaction in expected)
            {
                if (!byName.TryGetValue(reaction.ToString(), out var sprite))
                {
                    throw new InvalidOperationException($"Missing judge reaction sprite '{reaction}'.");
                }
                spriteSet.FindPropertyRelative(reaction.ToString().ToLowerInvariant())
                    .objectReferenceValue = sprite;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var separator = path.LastIndexOf('/');
            var parent = path.Substring(0, separator);
            var name = path.Substring(separator + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
