using System;
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
        private const string GuidedContentPath = "Assets/Content/Scenarios/guided-pitch-builder.en.json";

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
                SetReference(bootstrapper, "guidedPitchContentJson",
                    AssetDatabase.LoadAssetAtPath<TextAsset>(GuidedContentPath));
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

                var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem),
                    typeof(StandaloneInputModule));
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

                var references = GuidedPitchSceneBuilder.Create(
                    canvasObject.transform,
                    LoadSprite(EnvironmentArtPath),
                    LoadSprites(JudgeArtPath));
                eventSystem.firstSelectedGameObject = references.TitleDefault.gameObject;
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

        private static GameObject CreateLegacyLayoutScreen(string name, Transform parent)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            Stretch(panel.GetComponent<RectTransform>());
            panel.GetComponent<Image>().color = new Color(0.055f, 0.09f, 0.12f, 1f);
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
