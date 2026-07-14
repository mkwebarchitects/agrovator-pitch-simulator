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
        }

        private static T[] FindInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
        }
    }
}
