using System.Collections;
using System.Linq;
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
        [UnityTest]
        public IEnumerator Bootstrap_LoadsGame_AndShowsOnlyTitle()
        {
            var load = SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null, "Bootstrap must be present in Build Settings.");
            yield return load;

            var deadline = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetSceneByName("Game").isLoaded == false &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            var bootstrappers = Object.FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(component => component.GetType().FullName ==
                    "Agrovator.PitchSimulator.UI.Bootstrapper")
                .ToArray();
            Assert.That(bootstrappers, Has.Length.EqualTo(1));
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

            Assert.That(Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
            var routers = Object.FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Count(component => component.GetType().FullName ==
                    "Agrovator.PitchSimulator.UI.GameScreenRouter");
            Assert.That(routers, Is.EqualTo(1));
        }
    }
}
