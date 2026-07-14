using System.Collections;
using System.Linq;
using Agrovator.PitchSimulator.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.Tests.PlayMode
{
    public sealed class BootstrapPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return ClearLoadedFoundation();
        }

        [UnityTest]
        public IEnumerator Bootstrap_InitializesContract_FocusesTitle_AndRestoresFocusAfterRouting()
        {
            var load = SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null, "Bootstrap must be present in Build Settings.");
            yield return load;

            var deadline = Time.realtimeSinceStartup + 5f;
            Bootstrapper bootstrapper = null;
            while ((bootstrapper == null || !bootstrapper.IsInitialized) &&
                   Time.realtimeSinceStartup < deadline)
            {
                bootstrapper = Object.FindAnyObjectByType<Bootstrapper>(FindObjectsInactive.Include);
                yield return null;
            }

            var bootstrappers = Object.FindObjectsByType<Bootstrapper>(FindObjectsInactive.Include);
            Assert.That(bootstrappers, Has.Length.EqualTo(1));
            Assert.That(bootstrapper.IsInitialized, Is.True);
            Assert.That(SceneManager.GetSceneByName("Game").isLoaded, Is.True);

            var generatedRoot = GameObject.Find("Generated UI");
            Assert.That(generatedRoot, Is.Not.Null);
            var canvasRoot = generatedRoot.transform.Find("Canvas");
            Assert.That(canvasRoot, Is.Not.Null);
            var title = canvasRoot.Find("Title");
            Assert.That(title, Is.Not.Null);
            Assert.That(title.gameObject.activeInHierarchy, Is.True);

            foreach (var screenName in new[] { "Briefing", "PitchRoom", "Results", "Settings" })
            {
                var screen = canvasRoot.Find(screenName);
                Assert.That(screen, Is.Not.Null, $"Missing {screenName} screen.");
                Assert.That(screen.gameObject.activeInHierarchy, Is.False, $"{screenName} must start hidden.");
            }

            Assert.That(Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include),
                Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include),
                Has.Length.EqualTo(1));
            var routers = Object.FindObjectsByType<GameScreenRouter>(FindObjectsInactive.Include);
            Assert.That(routers, Has.Length.EqualTo(1));
            Assert.That(routers[0].IsInitialized, Is.True);

            var startButton = title.Find("Start Button").GetComponent<Button>();
            var settingsButton = title.Find("Settings Button").GetComponent<Button>();
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(startButton.gameObject));
            settingsButton.onClick.Invoke();
            yield return null;
            var settings = canvasRoot.Find("Settings");
            var closeButton = settings.Find("Close Button").GetComponent<Button>();
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(closeButton.gameObject));
            closeButton.onClick.Invoke();
            yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(startButton.gameObject));
            startButton.onClick.Invoke();
            yield return null;
            var briefing = canvasRoot.Find("Briefing");
            var briefingContinue = briefing.Find("Continue Button").GetComponent<Button>();
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(briefingContinue.gameObject));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return ClearLoadedFoundation();
        }

        private static IEnumerator ClearLoadedFoundation()
        {
            var bootstrappers = Object.FindObjectsByType<Bootstrapper>(FindObjectsInactive.Include);
            foreach (var bootstrapper in bootstrappers)
            {
                Object.Destroy(bootstrapper.gameObject);
            }
            if (bootstrappers.Length > 0)
            {
                yield return null;
            }

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            var cleanup = SceneManager.GetSceneByName("PitchSimulatorTestCleanup");
            if (!cleanup.isLoaded)
            {
                cleanup = SceneManager.CreateScene("PitchSimulatorTestCleanup");
            }
            SceneManager.SetActiveScene(cleanup);

            foreach (var sceneName in new[] { "Game", "Bootstrap" })
            {
                var scene = SceneManager.GetSceneByName(sceneName);
                if (scene.isLoaded)
                {
                    var unload = SceneManager.UnloadSceneAsync(scene);
                    if (unload != null) yield return unload;
                }
            }
        }
    }
}
