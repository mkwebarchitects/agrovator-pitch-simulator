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
        /// <summary>
        /// Bump whenever a builder change must reach the saved scenes. Every
        /// owned root records this value and every contract check compares it
        /// first, so a builder change can no longer silently no-op behind the
        /// hand-maintained property whitelists below.
        ///
        /// The bump is manual and nothing enforces it: forgetting it can still
        /// leave a saved scene stale, exactly as the `semanticHoldSeconds`
        /// regression did. Treat bumping this as part of changing anything
        /// this class or <see cref="GuidedPitchSceneBuilder"/> generates.
        /// </summary>
        public const int GeneratorVersion = 5;

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
                if (HasCurrentBootstrapContract(scene))
                {
                    return;
                }

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
                if (HasCurrentGameContract(scene))
                {
                    return;
                }

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
                if (HasCurrentWebIntegrationContract(scene))
                {
                    return;
                }

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
            var stamp = new SerializedObject(root.AddComponent<GeneratedSceneStamp>());
            stamp.FindProperty("generatorVersion").intValue = GeneratorVersion;
            stamp.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        /// <summary>
        /// Compared before every property whitelist below. A bumped
        /// <see cref="GeneratorVersion"/> forces every owned scene to
        /// regenerate even when the whitelist happens not to inspect whatever
        /// the builder change touched.
        /// </summary>
        private static bool HasCurrentGeneratorVersion(GameObject root)
        {
            var stamp = root.GetComponent<GeneratedSceneStamp>();
            return stamp != null && stamp.GeneratorVersion == GeneratorVersion;
        }

        private static bool HasCurrentBootstrapContract(Scene scene)
        {
            if (!TryGetSingleOwnedRoot(scene, "Generated Bootstrap", out var root) ||
                !HasCurrentGeneratorVersion(root))
            {
                return false;
            }

            var bootstrappers = root.GetComponentsInChildren<Bootstrapper>(true);
            if (bootstrappers.Length != 1 || bootstrappers[0].GetComponents<AudioSource>().Length != 2)
            {
                return false;
            }

            var serialized = new SerializedObject(bootstrappers[0]);
            var content = serialized.FindProperty("guidedPitchContentJson")?.objectReferenceValue;
            var english = serialized.FindProperty("englishCatalogJson")?.objectReferenceValue;
            var bindings = serialized.FindProperty("audioCueBindings");
            return AssetDatabase.GetAssetPath(content) == GuidedContentPath &&
                AssetDatabase.GetAssetPath(english) == "Assets/Content/Localization/en.json" &&
                bindings != null && bindings.arraySize == Enum.GetValues(typeof(AudioCue)).Length;
        }

        private static bool HasCurrentGameContract(Scene scene)
        {
            if (!TryGetSingleOwnedRoot(scene, "Generated UI", out var root) ||
                !HasCurrentGeneratorVersion(root))
            {
                return false;
            }

            var canvases = root.GetComponentsInChildren<Canvas>(true);
            var eventSystems = root.GetComponentsInChildren<EventSystem>(true);
            var routers = root.GetComponentsInChildren<GuidedPitchScreenRouter>(true);
            var responsive = root.GetComponentsInChildren<GuidedPitchResponsiveLayout>(true);
            var selectables = root.GetComponentsInChildren<Selectable>(true);
            var flowLayouts = root.GetComponentsInChildren<GuidedPitchFlowLayout>(true);
            if (canvases.Length != 1 || eventSystems.Length != 1 || routers.Length != 1 ||
                responsive.Length != 1 || flowLayouts.Length != 3 ||
                flowLayouts.Any(layout => layout.GetComponent<LayoutElement>() != null) ||
                selectables.Length == 0 ||
                root.GetComponentsInChildren<SelectableFocusIndicator>(true).Length != selectables.Length ||
                selectables.Any(selectable => selectable.targetGraphic == null ||
                    selectable.targetGraphic.GetComponent<Outline>() == null))
            {
                return false;
            }

            var canvas = canvases[0];
            var scaler = canvas.GetComponent<CanvasScaler>();
            var environment = canvas.transform.Find("Guided Pitch/Environment Frame");
            var environmentImage = environment != null ? environment.GetComponent<Image>() : null;
            var environmentAspect = environment != null
                ? environment.GetComponent<AspectRatioFitter>()
                : null;
            var modeLayout = canvas.transform.Find(
                "Guided Pitch/Content Frame/Phase Scroll/Viewport/Content/Mode Selection")
                ?.GetComponent<GuidedPitchFlowLayout>();
            var presentLayout = canvas.transform.Find(
                "Guided Pitch/Content Frame/Phase Scroll/Viewport/Content/Present Pitch")
                ?.GetComponent<VerticalLayoutGroup>();
            var resultsLayout = canvas.transform.Find(
                "Results/Content Frame/Results Scroll/Viewport/Content")
                ?.GetComponent<VerticalLayoutGroup>();
            var screens = canvas.transform.Cast<Transform>().Select(child => child.name).ToArray();
            var expectedScreens = new[]
            {
                "Title", "Briefing", "Guided Pitch", "Results", "Settings", "Safe Fallback",
                // Not a screen. Last so it draws over whichever screen is current.
                "Rotate To Play",
            };
            return scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize &&
                scaler.referenceResolution == new Vector2(1280f, 720f) &&
                responsive[0].ValidateContract(out _) &&
                screens.SequenceEqual(expectedScreens) &&
                environmentImage != null && environmentImage.sprite != null &&
                environmentAspect != null &&
                environmentAspect.aspectMode == AspectRatioFitter.AspectMode.EnvelopeParent &&
                Mathf.Approximately(environmentAspect.aspectRatio, 16f / 9f) &&
                canvas.transform.Find("Guided Pitch")?.GetComponent<Image>()?.color ==
                    new Color32(0x0E, 0x17, 0x1F, 0xFF) &&
                modeLayout != null && Mathf.Approximately(modeLayout.StackedItemHeight, 96f) &&
                presentLayout != null && presentLayout.padding.top == 14 &&
                presentLayout.padding.bottom == 14 &&
                resultsLayout != null && Mathf.Approximately(resultsLayout.spacing, 5f) &&
                routers[0].ValidateContract(out _) &&
                HasCurrentJudgeContract(canvas.transform) &&
                eventSystems[0].firstSelectedGameObject ==
                    canvas.transform.Find("Title/Content Frame/Start Button")?.gameObject;
        }

        /// <summary>
        /// Judge Aya's reaction wiring and tuning are part of what makes a saved
        /// scene current. Without this, the idempotence guard above would treat a
        /// scene built by an older builder as up to date and silently keep serving
        /// tuning values that no longer exist in any source file.
        /// </summary>
        private static bool HasCurrentJudgeContract(Transform canvas)
        {
            var judge = canvas.Find("Guided Pitch/Content Frame/Aya Row/Judge Aya");
            var view = judge != null ? judge.GetComponent<JudgeReactionView>() : null;
            var image = judge != null ? judge.GetComponent<Image>() : null;
            if (view == null || image == null || !view.IsConfigured)
            {
                return false;
            }

            // A renamed field must fail loudly here rather than throw an opaque
            // null reference from inside the idempotence guard.
            var serialized = new SerializedObject(view);
            if (RequireProperty(serialized, "portraitImage").objectReferenceValue !=
                    (UnityEngine.Object)image ||
                !Mathf.Approximately(RequireProperty(serialized, "blinkIntervalSeconds").floatValue,
                    GuidedPitchSceneBuilder.JudgeBlinkIntervalSeconds) ||
                !Mathf.Approximately(RequireProperty(serialized, "blinkDurationSeconds").floatValue,
                    GuidedPitchSceneBuilder.JudgeBlinkDurationSeconds) ||
                !Mathf.Approximately(RequireProperty(serialized, "talkFrameSeconds").floatValue,
                    GuidedPitchSceneBuilder.JudgeTalkFrameSeconds) ||
                !Mathf.Approximately(RequireProperty(serialized, "semanticHoldSeconds").floatValue,
                    GuidedPitchSceneBuilder.JudgeSemanticHoldSeconds))
            {
                return false;
            }

            var sprites = RequireProperty(serialized, "sprites");
            foreach (JudgeReaction reaction in Enum.GetValues(typeof(JudgeReaction)))
            {
                var field = char.ToLowerInvariant(reaction.ToString()[0]) +
                    reaction.ToString().Substring(1);
                var property = sprites.FindPropertyRelative(field);
                if (property == null)
                {
                    throw new InvalidOperationException(
                        $"JudgeReactionSpriteSet has no field '{field}' for reaction {reaction}.");
                }

                var assigned = property.objectReferenceValue as Sprite;
                if (assigned == null ||
                    !string.Equals(assigned.name, reaction.ToString(), StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return image.sprite != null &&
                string.Equals(image.sprite.name, nameof(JudgeReaction.Encouraging),
                    StringComparison.Ordinal);
        }

        private static SerializedProperty RequireProperty(SerializedObject target, string name)
        {
            var property = target.FindProperty(name);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{target.targetObject.GetType().Name} has no serialized field '{name}'.");
            }

            return property;
        }

        private static bool HasCurrentWebIntegrationContract(Scene scene)
        {
            if (!TryGetSingleOwnedRoot(scene, "Generated Web Integration Test", out var root) ||
                !HasCurrentGeneratorVersion(root))
            {
                return false;
            }

            var hosts = root.GetComponentsInChildren<WebGlLmsBridgeHost>(true);
            if (hosts.Length != 1)
            {
                return false;
            }

            var diagnostics = new SerializedObject(hosts[0]).FindProperty("diagnosticsLabel")
                ?.objectReferenceValue as Text;
            return diagnostics != null && root.GetComponentsInChildren<Canvas>(true).Length == 1;
        }

        private static bool TryGetSingleOwnedRoot(Scene scene, string rootName, out GameObject root)
        {
            var matches = scene.GetRootGameObjects()
                .Where(candidate => string.Equals(candidate.name, rootName, StringComparison.Ordinal))
                .ToArray();
            root = matches.Length == 1 ? matches[0] : null;
            return root != null;
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
