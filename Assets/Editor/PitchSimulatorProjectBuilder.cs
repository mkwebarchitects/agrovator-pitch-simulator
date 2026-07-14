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
                var pitch = CreatePitchRoomScreen(canvasObject.transform, out var pitchPresenter);
                var results = CreateResultsScreen(canvasObject.transform, out var resultsPresenter);
                var settings = CreateSettingsScreen(canvasObject.transform, out var settingsPresenter);

                var titleDefault = title.transform.Find("Start Button").GetComponent<Button>();
                var briefingDefault = briefing.transform.Find("Continue Button").GetComponent<Button>();
                var pitchDefault = pitch.transform.Find("Responses/Response 1").GetComponent<Button>();
                var pitchContinueDefault = pitch.transform.Find("Continue Button").GetComponent<Button>();
                var resultsDefault = results.transform.Find("Submit Button").GetComponent<Button>();
                var settingsDefault = settings.transform.Find("Close Button").GetComponent<Button>();

                SetReference(router, "titlePanel", title);
                SetReference(router, "briefingPanel", briefing);
                SetReference(router, "pitchRoomPanel", pitch);
                SetReference(router, "resultsPanel", results);
                SetReference(router, "settingsPanel", settings);
                SetReference(router, "titlePresenter", titlePresenter);
                SetReference(router, "briefingPresenter", briefingPresenter);
                SetReference(router, "pitchRoomPresenter", pitchPresenter);
                SetReference(router, "resultsPresenter", resultsPresenter);
                SetReference(router, "settingsPresenter", settingsPresenter);
                SetReference(router, "titleDefault", titleDefault);
                SetReference(router, "briefingDefault", briefingDefault);
                SetReference(router, "pitchRoomDefault", pitchDefault);
                SetReference(router, "pitchRoomContinueDefault", pitchContinueDefault);
                SetReference(router, "resultsDefault", resultsDefault);
                SetReference(router, "settingsDefault", settingsDefault);
                eventSystem.firstSelectedGameObject = titleDefault.gameObject;

                title.SetActive(true);
                briefing.SetActive(false);
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

                var panel = CreateScreen("Diagnostics", canvasObject.transform);
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
            CreateLabel("Heading", panel.transform, "Pitch Simulator", 44, FontStyle.Bold);
            CreateLabel("Subtitle", panel.transform, "Practise a clear, confident innovation pitch.", 24);
            var start = CreateButton("Start Button", panel.transform, "Start Pitch");
            var settings = CreateButton("Settings Button", panel.transform, "Settings");
            ConfigureVerticalNavigation(start, settings);
            SetReference(presenter, "startButton", start);
            SetReference(presenter, "settingsButton", settings);
            return panel;
        }

        private static GameObject CreateBriefingScreen(Transform parent, out BriefingPresenter presenter)
        {
            var panel = CreateScreen("Briefing", parent);
            presenter = panel.AddComponent<BriefingPresenter>();
            CreateLabel("Heading", panel.transform, "Your Brief", 40, FontStyle.Bold);
            CreateLabel("Brief", panel.transform,
                "Pitch the Smart School Garden to a friendly youth innovation mentor.", 25);
            var continueButton = CreateButton("Continue Button", panel.transform, "Enter Pitch Room");
            SetReference(presenter, "continueButton", continueButton);
            return panel;
        }

        private static GameObject CreatePitchRoomScreen(Transform parent, out PitchRoomPresenter presenter)
        {
            var panel = CreateScreen("PitchRoom", parent);
            presenter = panel.AddComponent<PitchRoomPresenter>();
            var pitchLayout = panel.GetComponent<VerticalLayoutGroup>();
            pitchLayout.padding = new RectOffset(18, 18, 18, 18);
            pitchLayout.spacing = 6f;
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

            var statusBacking = new GameObject("Status Backing", typeof(RectTransform),
                typeof(Image), typeof(LayoutElement));
            statusBacking.transform.SetParent(panel.transform, false);
            statusBacking.GetComponent<Image>().color = Ink;
            statusBacking.GetComponent<Image>().raycastTarget = false;
            statusBacking.GetComponent<LayoutElement>().preferredHeight = 48f;
            var status = CreateLabel("Status", statusBacking.transform, "Score 0", 22);
            status.color = Cream;
            Stretch(status.GetComponent<RectTransform>());
            var judgeObject = new GameObject("Judge Aya", typeof(RectTransform), typeof(Image),
                typeof(LayoutElement), typeof(JudgeReactionView));
            judgeObject.transform.SetParent(panel.transform, false);
            var judgeLayout = judgeObject.GetComponent<LayoutElement>();
            judgeLayout.preferredWidth = 240f;
            judgeLayout.preferredHeight = 150f;
            var judgeImage = judgeObject.GetComponent<Image>();
            judgeImage.preserveAspect = true;
            judgeImage.raycastTarget = false;
            var judgeView = judgeObject.GetComponent<JudgeReactionView>();
            SetReference(judgeView, "portraitImage", judgeImage);
            SetJudgeSprites(judgeView);

            var dialoguePanel = new GameObject("Dialogue Panel", typeof(RectTransform),
                typeof(Image), typeof(LayoutElement));
            dialoguePanel.transform.SetParent(panel.transform, false);
            var dialogueImage = dialoguePanel.GetComponent<Image>();
            dialogueImage.sprite = LoadSprite(DialoguePanelArtPath);
            dialogueImage.type = Image.Type.Sliced;
            dialogueImage.raycastTarget = false;
            dialoguePanel.GetComponent<LayoutElement>().preferredHeight = 96f;
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
            Stretch(prompt.GetComponent<RectTransform>());

            var confidenceRoot = CreateIndicatorRoot("Confidence", panel.transform);
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
            var confidenceFill = CreateFilledBar("Fill", confidenceRoot.transform);
            var confidenceView = confidenceRoot.AddComponent<ConfidenceView>();
            confidenceRoot.GetComponent<LayoutElement>().preferredHeight = 42f;
            SetReference(confidenceView, "stateLabel", confidenceLabel);
            SetReference(confidenceView, "iconLabel", confidenceIcon);
            SetReference(confidenceView, "iconImage", confidenceArtwork);
            SetReferenceArray(confidenceView, "iconSprites", LoadSprites(ConfidenceArtPath));
            SetReference(confidenceView, "fillImage", confidenceFill);

            var timerRoot = CreateIndicatorRoot("Timer", panel.transform);
            timerRoot.GetComponent<LayoutElement>().preferredHeight = 42f;
            var timerSeconds = CreateLabel("Seconds", timerRoot.transform, "0", 22, FontStyle.Bold);
            timerSeconds.color = Cream;
            var timerFill = CreateFilledBar("Fill", timerRoot.transform);
            var timerView = timerRoot.AddComponent<TimerView>();
            SetReference(timerView, "secondsLabel", timerSeconds);
            SetReference(timerView, "fillImage", timerFill);
            SetReference(timerView, "pulseTarget", timerRoot.GetComponent<RectTransform>());

            var responseRoot = new GameObject("Responses", typeof(RectTransform),
                typeof(VerticalLayoutGroup), typeof(LayoutElement), typeof(ResponseListView));
            responseRoot.transform.SetParent(panel.transform, false);
            var responseLayout = responseRoot.GetComponent<VerticalLayoutGroup>();
            responseLayout.spacing = 12f;
            responseLayout.childAlignment = TextAnchor.MiddleCenter;
            responseLayout.childControlWidth = true;
            responseLayout.childControlHeight = true;
            responseLayout.childForceExpandWidth = true;
            responseLayout.childForceExpandHeight = false;
            responseRoot.GetComponent<LayoutElement>().preferredHeight = 170f;
            var slots = new ResponseButtonView[3];
            for (var index = 0; index < slots.Length; index++)
            {
                var button = CreateButton($"Response {index + 1}", responseRoot.transform,
                    $"Response {index + 1}");
                slots[index] = button.gameObject.AddComponent<ResponseButtonView>();
                SetReference(slots[index], "button", button);
                SetReference(slots[index], "label", button.GetComponentInChildren<Text>());
            }
            var responseList = responseRoot.GetComponent<ResponseListView>();
            SetReferenceArray(responseList, "slots", slots);

            var continueButton = CreateButton("Continue Button", panel.transform, "Continue");
            continueButton.GetComponent<LayoutElement>().preferredHeight = 64f;
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
            layout.childControlWidth = false;
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
            CreateLabel("Heading", panel.transform, "Results", 40, FontStyle.Bold);
            var summary = CreateLabel("Summary", panel.transform, "Pitch complete", 28);
            var submit = CreateButton("Submit Button", panel.transform, "Submit Results");
            var retry = CreateButton("Retry Button", panel.transform, "Try Again");
            ConfigureVerticalNavigation(submit, retry);
            SetReference(presenter, "summaryText", summary);
            SetReference(presenter, "submitButton", submit);
            SetReference(presenter, "retryButton", retry);
            return panel;
        }

        private static GameObject CreateSettingsScreen(Transform parent, out SettingsPresenter presenter)
        {
            var panel = CreateScreen("Settings", parent);
            presenter = panel.AddComponent<SettingsPresenter>();
            CreateLabel("Heading", panel.transform, "Settings", 40, FontStyle.Bold);
            CreateLabel("Foundation Note", panel.transform,
                "Timer, motion, audio and language controls arrive in the next slice.", 24);
            var close = CreateButton("Close Button", panel.transform, "Back");
            SetReference(presenter, "closeButton", close);
            return panel;
        }

        private static GameObject CreateScreen(string name, Transform parent)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(parent, false);
            Stretch(panel.GetComponent<RectTransform>());
            panel.GetComponent<Image>().color = new Color(0.055f, 0.09f, 0.12f, 1f);
            var layout = panel.GetComponent<VerticalLayoutGroup>();
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
