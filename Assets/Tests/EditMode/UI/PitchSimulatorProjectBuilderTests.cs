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
            var pitchRoom = generated.transform.Find("Canvas/PitchRoom");
            Assert.That(pitchRoom, Is.Not.Null);
            var scaler = generated.transform.Find("Canvas").GetComponent<CanvasScaler>();
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1280f, 720f)));
            Assert.That(pitchRoom.Find("Environment").GetComponent<Image>().sprite.name,
                Is.EqualTo("pitch-room"));
            Assert.That(pitchRoom.Find("Dialogue Panel").GetComponent<Image>().type,
                Is.EqualTo(Image.Type.Sliced));
            var judge = pitchRoom.Find("Judge Aya").GetComponent<JudgeReactionView>();
            Assert.That(judge, Is.Not.Null);
            Assert.That(judge.IsConfigured, Is.True);
            Assert.That(pitchRoom.Find("Confidence/Artwork Icon").GetComponent<Image>(), Is.Not.Null);

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

            AssertTextContrast(
                pitchRoom.Find("Status Backing/Status").GetComponent<Text>(),
                pitchRoom.Find("Status Backing").GetComponent<Image>(), 4.5f);
            AssertTextContrast(
                pitchRoom.Find("Dialogue Panel/Prompt Backing/Prompt").GetComponent<Text>(),
                pitchRoom.Find("Dialogue Panel/Prompt Backing").GetComponent<Image>(), 4.5f);
            AssertTextContrast(
                pitchRoom.Find("Confidence/Label").GetComponent<Text>(),
                pitchRoom.Find("Confidence").GetComponent<Image>(), 4.5f);
            AssertTextContrast(
                pitchRoom.Find("Timer/Seconds").GetComponent<Text>(),
                pitchRoom.Find("Timer").GetComponent<Image>(), 4.5f);

            var pitchRect = pitchRoom.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(pitchRect);
            var requiredHeight = RequiredVerticalHeight(pitchRoom);
            Assert.That(requiredHeight, Is.LessThanOrEqualTo(720f),
                "PitchRoom layout must fit the 1280x720 reference canvas.");
            foreach (Transform child in pitchRoom)
            {
                var element = child.GetComponent<LayoutElement>();
                if (element != null && element.ignoreLayout) continue;
                AssertContained(pitchRect, child.GetComponent<RectTransform>());
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
