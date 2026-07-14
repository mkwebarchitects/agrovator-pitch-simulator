using System.Linq;
using Agrovator.PitchSimulator.Editor;
using Agrovator.PitchSimulator.UI;
using NUnit.Framework;
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

        [Test]
        public void BatchBuild_Twice_PreservesUnownedRootAndKeepsContractSingular()
        {
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
                var preserved = scene.GetRootGameObjects().FirstOrDefault(root => root.name == SentinelName);
                if (preserved != null)
                {
                    Object.DestroyImmediate(preserved);
                }
                EditorSceneManager.SaveScene(scene);
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
