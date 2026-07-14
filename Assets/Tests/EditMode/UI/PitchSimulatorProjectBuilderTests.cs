using System.Collections.Generic;
using System.IO;
using System.Linq;
using Agrovator.PitchSimulator.Editor;
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
                PitchSimulatorProjectBuilder.BuildProjectFoundationBatch();
                AssertContract(scene);
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
