using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Agrovator.PitchSimulator.Editor;
using Agrovator.PitchSimulator.Audio;
using Agrovator.PitchSimulator.LMS;
using Agrovator.PitchSimulator.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.Tests.EditMode.UI
{
    public sealed class PitchSimulatorProjectBuilderTests
    {
        private const string GamePath = "Assets/Scenes/Game.unity";
        private const string SentinelName = "Review Sentinel - Preserve Me";
        private static readonly string[] GeneratedScenePaths =
        {
            "Assets/Scenes/Bootstrap.unity",
            GamePath,
            "Assets/Scenes/WebIntegrationTest.unity",
        };

        [Test]
        public void GeneratedPitchRoom_UsesContainedFrameAtReferenceResolution()
        {
            var scene = EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Single);

            AssertPitchRoomFrame(scene);
        }

        [Test]
        public void BatchBuild_Twice_PreservesUnownedRootAndKeepsContractSingular()
        {
            var originalSceneBytes = new Dictionary<string, byte[]>();
            foreach (var path in GeneratedScenePaths)
            {
                originalSceneBytes.Add(path, File.ReadAllBytes(path));
            }
            var scene = EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Single);
            var sentinel = new GameObject(SentinelName);
            SceneManager.MoveGameObjectToScene(sentinel, scene);
            EditorSceneManager.SaveScene(scene);

            try
            {
                PitchSimulatorProjectBuilder.BuildProjectFoundationBatch();
                AssertContract(scene);
                AssertBootstrapAudioContract();
                AssertWebIntegrationContract();
                AssertExactBuildSettings();
                PitchSimulatorProjectBuilder.BuildProjectFoundationBatch();
                AssertContract(scene);
                AssertBootstrapAudioContract();
                AssertWebIntegrationContract();
                AssertExactBuildSettings();
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                foreach (var pair in originalSceneBytes)
                {
                    File.WriteAllBytes(pair.Key, pair.Value);
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }
        }

        private static void AssertWebIntegrationContract()
        {
            var scene = EditorSceneManager.OpenScene(
                "Assets/Scenes/WebIntegrationTest.unity", OpenSceneMode.Additive);
            try
            {
                var ownedRoots = scene.GetRootGameObjects()
                    .Where(root => root.name == "Generated Web Integration Test")
                    .ToArray();
                Assert.That(ownedRoots, Has.Length.EqualTo(1));
                var root = ownedRoots[0];
                var hosts = root.GetComponentsInChildren<WebGlLmsBridgeHost>(true);
                Assert.That(hosts, Has.Length.EqualTo(1));

                var serialized = new SerializedObject(hosts[0]);
                var diagnostics = serialized.FindProperty("diagnosticsLabel").objectReferenceValue as Text;
                Assert.That(diagnostics, Is.Not.Null);
                Assert.That(diagnostics.transform.IsChildOf(root.transform), Is.True);

                var missingScripts = scene.GetRootGameObjects()
                    .SelectMany(sceneRoot => sceneRoot.GetComponentsInChildren<Transform>(true))
                    .Sum(item => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject));
                Assert.That(missingScripts, Is.Zero);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void AssertExactBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;
            Assert.That(scenes, Has.Length.EqualTo(2));
            Assert.That(scenes.All(scene => scene.enabled), Is.True);
            Assert.That(scenes.Select(scene => scene.path), Is.EqualTo(new[]
            {
                "Assets/Scenes/Bootstrap.unity",
                "Assets/Scenes/Game.unity",
            }));
        }

        private static void AssertBootstrapAudioContract()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Bootstrap.unity", OpenSceneMode.Additive);
            try
            {
                var generated = scene.GetRootGameObjects().Single(root => root.name == "Generated Bootstrap");
                var bootstrapper = generated.GetComponentInChildren<Bootstrapper>(true);
                var sources = bootstrapper.GetComponents<AudioSource>();
                Assert.That(sources, Has.Length.EqualTo(2));
                Assert.That(sources.All(source => !source.playOnAwake), Is.True);
                Assert.That(sources.All(source => source.spatialBlend == 0f), Is.True);

                var serialized = new SerializedObject(bootstrapper);
                var music = serialized.FindProperty("musicSource").objectReferenceValue as AudioSource;
                var sfx = serialized.FindProperty("sfxSource").objectReferenceValue as AudioSource;
                Assert.That(music, Is.Not.Null.And.Not.SameAs(sfx));
                Assert.That(music.loop, Is.True);
                Assert.That(sfx.loop, Is.False);
                var bindings = serialized.FindProperty("audioCueBindings");
                Assert.That(bindings.arraySize, Is.EqualTo(Enum.GetValues(typeof(AudioCue)).Length));
                for (var index = 0; index < bindings.arraySize; index++)
                {
                    Assert.That(bindings.GetArrayElementAtIndex(index)
                        .FindPropertyRelative("clip").objectReferenceValue, Is.Null);
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void AssertContract(Scene scene)
        {
            Assert.That(scene.GetRootGameObjects().Count(root => root.name == "Generated UI"), Is.EqualTo(1));
            Assert.That(scene.GetRootGameObjects().Count(root => root.name == SentinelName), Is.EqualTo(1));
            Assert.That(FindInScene<Canvas>(scene), Has.Length.EqualTo(1));
            Assert.That(FindInScene<EventSystem>(scene), Has.Length.EqualTo(1));
            Assert.That(FindInScene<GameScreenRouter>(scene), Has.Length.EqualTo(1));
            var generated = scene.GetRootGameObjects().Single(root => root.name == "Generated UI");
            var canvas = generated.transform.Find("Canvas");
            Assert.That(canvas, Is.Not.Null);
            AssertSimpleScreenFrames(canvas);
            var tutorial = generated.transform.Find("Canvas/Tutorial");
            Assert.That(tutorial, Is.Not.Null);
            Assert.That(tutorial.GetComponent<TutorialPresenter>(), Is.Not.Null);
            Assert.That(tutorial.Find("Content Frame/Navigation/Next Button").GetComponent<Button>(), Is.Not.Null);
            var pitchRoom = generated.transform.Find("Canvas/PitchRoom");
            Assert.That(pitchRoom, Is.Not.Null);
            var scaler = canvas.GetComponent<CanvasScaler>();
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1280f, 720f)));
            Assert.That(pitchRoom.Find("Environment").GetComponent<Image>().sprite.name,
                Is.EqualTo("pitch-room"));
            Assert.That(pitchRoom.Find("Content Frame/Dialogue Panel").GetComponent<Image>().type,
                Is.EqualTo(Image.Type.Sliced));
            var judge = pitchRoom.Find("Content Frame/Judge Aya").GetComponent<JudgeReactionView>();
            Assert.That(judge, Is.Not.Null);
            Assert.That(judge.IsConfigured, Is.True);
            Assert.That(pitchRoom.Find("Content Frame/Metrics/Confidence/Artwork Icon")
                .GetComponent<Image>(), Is.Not.Null);
            AssertPitchRoomFrame(scene);

            var results = generated.transform.Find("Canvas/Results");
            Assert.That(results, Is.Not.Null);
            var resultsPresenter = results.GetComponent<ResultsPresenter>();
            Assert.That(resultsPresenter, Is.Not.Null);
            Assert.That(resultsPresenter.ValidateContract(), Is.True);
            var resultsScroll = results.Find("Results Scroll").GetComponent<ScrollRect>();
            Assert.That(resultsScroll, Is.Not.Null);
            Assert.That(resultsScroll.vertical, Is.True);
            Assert.That(resultsScroll.horizontal, Is.False);
            Assert.That(resultsScroll.viewport, Is.Not.Null);
            Assert.That(resultsScroll.content, Is.Not.Null);
            Assert.That(resultsScroll.verticalScrollbar, Is.Not.Null);
            Assert.That(resultsScroll.verticalScrollbar, Is.TypeOf<KeyboardReviewScrollbar>());
            Assert.That(resultsScroll.verticalScrollbar.gameObject.activeSelf, Is.True);
            Assert.That(resultsScroll.verticalScrollbar.interactable, Is.True);
            Assert.That(resultsScroll.verticalScrollbar.direction, Is.EqualTo(Scrollbar.Direction.BottomToTop));
            Assert.That(resultsScroll.verticalScrollbar.navigation.mode, Is.EqualTo(Navigation.Mode.Explicit));
            Assert.That(resultsScroll.content.GetComponentsInChildren<QuestionReviewItemView>(true),
                Has.Length.EqualTo(6));
            Assert.That(results.Find("Footer/Submit Button").GetComponent<Button>().navigation.mode,
                Is.EqualTo(Navigation.Mode.Explicit));
            Assert.That(results.Find("Footer/Retry Button").GetComponent<Button>().navigation.mode,
                Is.EqualTo(Navigation.Mode.Explicit));

            var resultsRect = results.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(resultsRect);
            Assert.That(RequiredVerticalHeight(results), Is.LessThanOrEqualTo(720f),
                "Results chrome must fit the 1280x720 reference while review content scrolls.");
            foreach (Transform child in results)
            {
                AssertContained(resultsRect, child.GetComponent<RectTransform>());
            }

        }

        private static void AssertPitchRoomFrame(Scene scene)
        {
            var generated = scene.GetRootGameObjects().Single(root => root.name == "Generated UI");
            var canvas = generated.transform.Find("Canvas");
            var canvasRect = canvas.GetComponent<RectTransform>();
            canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            canvasRect.sizeDelta = canvas.GetComponent<CanvasScaler>().referenceResolution;

            var pitchRoom = canvas.Find("PitchRoom");
            Assert.That(pitchRoom, Is.Not.Null);
            var wasActive = pitchRoom.gameObject.activeSelf;
            pitchRoom.gameObject.SetActive(true);
            try
            {
                var environment = pitchRoom.Find("Environment");
                var frame = pitchRoom.Find("Content Frame");
                Assert.That(environment, Is.Not.Null);
                Assert.That(environment.parent, Is.EqualTo(pitchRoom));
                Assert.That(environment.GetComponent<LayoutElement>().ignoreLayout, Is.True);
                Assert.That(frame, Is.Not.Null, "PitchRoom must own a direct Content Frame child.");
                Assert.That(frame.parent, Is.EqualTo(pitchRoom));
                Assert.That(pitchRoom.Cast<Transform>().Select(child => child.name),
                    Is.EqualTo(new[] { "Environment", "Content Frame" }));
                Assert.That(frame.Cast<Transform>().Select(child => child.name),
                    Is.EqualTo(new[]
                    {
                        "Status Backing",
                        "Judge Aya",
                        "Dialogue Panel",
                        "Metrics",
                        "Responses",
                        "Continue Button",
                    }));

                var status = frame.Find("Status Backing");
                var judge = frame.Find("Judge Aya");
                var dialogue = frame.Find("Dialogue Panel");
                var metrics = frame.Find("Metrics");
                var confidence = metrics.Find("Confidence");
                var timer = metrics.Find("Timer");
                var responses = frame.Find("Responses");
                var continueButton = frame.Find("Continue Button");
                Assert.That(new[] { status, judge, dialogue, metrics, confidence, timer, responses, continueButton },
                    Has.None.Null);
                Assert.That(metrics.Cast<Transform>().Select(child => child.name),
                    Is.EqualTo(new[] { "Confidence", "Timer" }));

                var pitchRect = pitchRoom.GetComponent<RectTransform>();
                var environmentRect = environment.GetComponent<RectTransform>();
                var frameRect = frame.GetComponent<RectTransform>();
                var metricsRect = metrics.GetComponent<RectTransform>();
                var responsesRect = responses.GetComponent<RectTransform>();
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(pitchRect);
                LayoutRebuilder.ForceRebuildLayoutImmediate(frameRect);
                LayoutRebuilder.ForceRebuildLayoutImmediate(metricsRect);
                LayoutRebuilder.ForceRebuildLayoutImmediate(responsesRect);
                Canvas.ForceUpdateCanvases();

                Assert.That(environmentRect.rect.width, Is.EqualTo(pitchRect.rect.width).Within(0.1f));
                Assert.That(environmentRect.rect.height, Is.EqualTo(pitchRect.rect.height).Within(0.1f));
                Assert.That(frameRect.rect.width, Is.LessThanOrEqualTo(960f));
                Assert.That(frameRect.rect.height, Is.LessThanOrEqualTo(680f));
                var frameLayout = frame.GetComponent<VerticalLayoutGroup>();
                Assert.That(frameLayout.padding.left, Is.EqualTo(24));
                Assert.That(frameLayout.padding.right, Is.EqualTo(24));
                Assert.That(frameLayout.padding.top, Is.EqualTo(24));
                Assert.That(frameLayout.padding.bottom, Is.EqualTo(24));
                Assert.That(frameLayout.spacing, Is.EqualTo(8f));
                Assert.That(status.GetComponent<RectTransform>().rect.width, Is.LessThanOrEqualTo(860f));
                Assert.That(dialogue.GetComponent<RectTransform>().rect.width, Is.LessThanOrEqualTo(860f));
                Assert.That(metricsRect.rect.width, Is.LessThanOrEqualTo(680f),
                    $"Metrics rendered {metricsRect.rect.width:F1}px; layout min/preferred " +
                    $"{LayoutUtility.GetMinWidth(metricsRect):F1}/{LayoutUtility.GetPreferredWidth(metricsRect):F1}, " +
                    $"Confidence {LayoutUtility.GetMinWidth(confidence.GetComponent<RectTransform>()):F1}/" +
                    $"{LayoutUtility.GetPreferredWidth(confidence.GetComponent<RectTransform>()):F1}, " +
                    $"Timer {LayoutUtility.GetMinWidth(timer.GetComponent<RectTransform>()):F1}/" +
                    $"{LayoutUtility.GetPreferredWidth(timer.GetComponent<RectTransform>()):F1}.");
                Assert.That(confidence.GetComponent<RectTransform>().rect.width, Is.LessThanOrEqualTo(330f));
                Assert.That(timer.GetComponent<RectTransform>().rect.width, Is.LessThanOrEqualTo(330f));
                Assert.That(responsesRect.rect.width, Is.LessThanOrEqualTo(680f));
                Assert.That(continueButton.GetComponent<RectTransform>().rect.width, Is.LessThanOrEqualTo(520f));
                Assert.That(continueButton.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(64f));
                foreach (var response in responses.GetComponentsInChildren<Button>(true))
                {
                    var responseRect = response.GetComponent<RectTransform>();
                    Assert.That(responseRect.rect.width, Is.LessThanOrEqualTo(680f),
                        response.name + " escaped the response width cap.");
                    Assert.That(responseRect.rect.height, Is.GreaterThanOrEqualTo(64f),
                        response.name + " must remain a 64px minimum target.");
                    AssertContained(responsesRect, responseRect);
                }

                foreach (var child in frame.GetComponentsInChildren<RectTransform>(true))
                {
                    if (child == frameRect) continue;
                    AssertContained(frameRect, child);
                }
                AssertContained(pitchRect, frameRect);
                Assert.That(confidence.gameObject.activeInHierarchy, Is.True);
                Assert.That(timer.gameObject.activeInHierarchy, Is.True);
                Assert.That(confidence.GetComponent<RectTransform>().rect.width, Is.GreaterThan(0f));
                Assert.That(timer.GetComponent<RectTransform>().rect.width, Is.GreaterThan(0f));

                AssertTextContrast(
                    status.Find("Status").GetComponent<Text>(), status.GetComponent<Image>(), 4.5f);
                AssertTextContrast(
                    dialogue.Find("Prompt Backing/Prompt").GetComponent<Text>(),
                    dialogue.Find("Prompt Backing").GetComponent<Image>(), 4.5f);
                AssertTextContrast(
                    confidence.Find("Label").GetComponent<Text>(), confidence.GetComponent<Image>(), 4.5f);
                AssertTextContrast(
                    timer.Find("Seconds").GetComponent<Text>(), timer.GetComponent<Image>(), 4.5f);
            }
            finally
            {
                pitchRoom.gameObject.SetActive(wasActive);
            }
        }

        private static void AssertSimpleScreenFrames(Transform canvas)
        {
            var canvasRect = canvas.GetComponent<RectTransform>();
            var canvasComponent = canvas.GetComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.WorldSpace;
            canvasRect.sizeDelta = canvas.GetComponent<CanvasScaler>().referenceResolution;
            var effectiveWidthMismatches = new List<string>();
            var expectedFrameSizes = new Dictionary<string, Vector2>
            {
                { "Title", new Vector2(760f, 500f) },
                { "Briefing", new Vector2(880f, 520f) },
                { "Tutorial", new Vector2(920f, 560f) },
                { "Settings", new Vector2(720f, 420f) },
            };
            foreach (var pair in expectedFrameSizes)
            {
                var screenName = pair.Key;
                var screen = canvas.Find(screenName);
                Assert.That(screen, Is.Not.Null, $"Missing {screenName} screen.");
                var wasActive = screen.gameObject.activeSelf;
                screen.gameObject.SetActive(true);
                try
                {
                    var screenRect = screen.GetComponent<RectTransform>();
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(screenRect);
                    Assert.That(screenRect.anchorMin, Is.EqualTo(Vector2.zero),
                        $"{screenName} must remain stretched under the Canvas.");
                    Assert.That(screenRect.anchorMax, Is.EqualTo(Vector2.one),
                        $"{screenName} must remain stretched under the Canvas.");
                    Assert.That(screenRect.rect.width, Is.EqualTo(canvasRect.rect.width).Within(0.1f));
                    Assert.That(screenRect.rect.height, Is.EqualTo(canvasRect.rect.height).Within(0.1f));

                    var frameTransform = screen.Find("Content Frame");
                    Assert.That(frameTransform, Is.Not.Null, $"{screenName} must own a Content Frame.");
                    var frame = frameTransform.GetComponent<RectTransform>();
                    Assert.That(frame.rect.width, Is.EqualTo(pair.Value.x).Within(0.1f));
                    Assert.That(frame.rect.height, Is.EqualTo(pair.Value.y).Within(0.1f));
                    AssertContained(screenRect, frame);
                    foreach (var button in frame.GetComponentsInChildren<Button>(true))
                    {
                        var buttonRect = button.GetComponent<RectTransform>();
                        Assert.That(buttonRect.rect.width, Is.LessThanOrEqualTo(520f),
                            $"{screenName}/{button.name} must use a constrained card width.");
                        Assert.That(buttonRect.rect.height, Is.GreaterThanOrEqualTo(64f),
                            $"{screenName}/{button.name} must remain a 64px minimum target.");
                    }

                    if (screenName == "Tutorial")
                    {
                        CollectEffectiveWidthMismatch(
                            frameTransform, "Navigation/Back Button", 180f, effectiveWidthMismatches);
                        CollectEffectiveWidthMismatch(
                            frameTransform, "Navigation/Skip Button", 180f, effectiveWidthMismatches);
                        CollectEffectiveWidthMismatch(
                            frameTransform, "Navigation/Next Button", 420f, effectiveWidthMismatches);
                    }
                    else if (screenName == "Settings")
                    {
                        CollectEffectiveWidthMismatch(
                            frameTransform, "Heading", 680f, effectiveWidthMismatches);
                        CollectEffectiveWidthMismatch(
                            frameTransform, "Foundation Note", 680f, effectiveWidthMismatches);
                    }
                }
                finally
                {
                    screen.gameObject.SetActive(wasActive);
                }
                Assert.That(screen.gameObject.activeSelf, Is.EqualTo(wasActive),
                    $"{screenName} active state must be restored after layout assertions.");
            }

            Assert.That(effectiveWidthMismatches, Is.Empty,
                string.Join(Environment.NewLine, effectiveWidthMismatches));
        }

        private static void CollectEffectiveWidthMismatch(
            Transform parent, string path, float expected, ICollection<string> mismatches)
        {
            var target = parent.Find(path);
            Assert.That(target, Is.Not.Null, $"Missing {parent.name}/{path}.");
            var actual = target.GetComponent<RectTransform>().rect.width;
            if (Mathf.Abs(actual - expected) > 0.1f)
            {
                mismatches.Add($"{parent.name}/{path} expected {expected:F1}px but rendered {actual:F3}px.");
            }
        }

        private static void AssertTextContrast(Text text, Image backing, float minimum)
        {
            Assert.That(text, Is.Not.Null);
            Assert.That(backing, Is.Not.Null);
            Assert.That(backing.color.a, Is.EqualTo(1f), backing.name + " must be opaque.");
            var ratio = ContrastRatio(text.color, backing.color);
            Assert.That(ratio, Is.GreaterThanOrEqualTo(minimum),
                $"{text.name} contrast was {ratio:F2}:1.");
        }

        private static float RequiredVerticalHeight(Transform root)
        {
            var layout = root.GetComponent<VerticalLayoutGroup>();
            var height = (float)(layout.padding.top + layout.padding.bottom);
            var count = 0;
            foreach (Transform child in root)
            {
                if (!child.gameObject.activeSelf) continue;
                var element = child.GetComponent<LayoutElement>();
                if (element != null && element.ignoreLayout) continue;
                height += LayoutUtility.GetPreferredHeight(child.GetComponent<RectTransform>());
                count++;
            }
            return height + Mathf.Max(0, count - 1) * layout.spacing;
        }

        private static void AssertContained(RectTransform parent, RectTransform child)
        {
            Assert.That(child, Is.Not.Null);
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                var local = parent.InverseTransformPoint(corner);
                Assert.That(local.x, Is.InRange(parent.rect.xMin - 0.1f, parent.rect.xMax + 0.1f),
                    child.name + " escaped horizontally.");
                Assert.That(local.y, Is.InRange(parent.rect.yMin - 0.1f, parent.rect.yMax + 0.1f),
                    child.name + " escaped vertically.");
            }
        }

        private static float ContrastRatio(Color first, Color second)
        {
            var firstLuminance = RelativeLuminance(first);
            var secondLuminance = RelativeLuminance(second);
            return (Mathf.Max(firstLuminance, secondLuminance) + 0.05f) /
                (Mathf.Min(firstLuminance, secondLuminance) + 0.05f);
        }

        private static float RelativeLuminance(Color color)
        {
            return 0.2126f * Linear(color.r) + 0.7152f * Linear(color.g) +
                0.0722f * Linear(color.b);
        }

        private static float Linear(float value)
        {
            return value <= 0.04045f
                ? value / 12.92f
                : Mathf.Pow((value + 0.055f) / 1.055f, 2.4f);
        }

        private static T[] FindInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
        }
    }
}
