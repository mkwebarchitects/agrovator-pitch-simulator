using System.Reflection;
using System.Collections;
using System.Linq;
using Agrovator.PitchSimulator.Core;
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

            briefingContinue.onClick.Invoke();
            yield return null;
            var pitchRoom = canvasRoot.Find("PitchRoom");
            Assert.That(pitchRoom.gameObject.activeInHierarchy, Is.True);
            var pitchContinue = pitchRoom.Find("Continue Button").GetComponent<Button>();
            for (var step = 0; step < 3; step++)
            {
                Assert.That(pitchContinue.gameObject.activeInHierarchy, Is.True);
                pitchContinue.onClick.Invoke();
                yield return null;
            }

            var responseList = pitchRoom.Find("Responses").GetComponent<ResponseListView>();
            var responseViews = pitchRoom.Find("Responses").GetComponentsInChildren<ResponseButtonView>(true);
            Assert.That(responseViews, Has.Length.EqualTo(3));
            Assert.That(responseViews.Count(view => view.gameObject.activeSelf), Is.EqualTo(1));
            responseViews[0].Button.onClick.Invoke();
            Assert.That(responseList.IsSelectionLocked, Is.True);
            for (var step = 0; step < 3; step++)
            {
                Assert.That(pitchContinue.gameObject.activeInHierarchy, Is.True);
                pitchContinue.onClick.Invoke();
                yield return null;
            }

            Assert.That(responseViews.All(view => view.gameObject.activeSelf), Is.True);
            Assert.That(responseViews.Select(view => view.DisplayText),
                Is.EqualTo(new[]
                {
                    "1. Our logs show dry beds after assembly, wet beds after rain, and students carrying watering cans, so the timing is inconsistent.",
                    "2. We water on fixed schedules even when soil is wet, wasting water and weakening canteen crops.",
                    "3. Our invention will cut the school's water bill by 90% and produce enough vegetables for everyone.",
                }));
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.EqualTo(responseViews[0].Button.gameObject));
            Assert.That(pitchRoom.GetComponentInChildren<TimerView>(), Is.Not.Null);
            Assert.That(pitchRoom.GetComponentInChildren<ConfidenceView>(), Is.Not.Null);

            responseViews[0].Button.onClick.Invoke();
            responseViews[0].Button.onClick.Invoke();
            Assert.That(responseList.IsSelectionLocked, Is.True);
            Assert.That(responseViews.All(view => !view.Button.interactable), Is.True);
            Assert.That(pitchContinue.gameObject.activeInHierarchy, Is.True);
            Assert.That(pitchContinue.interactable, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.EqualTo(pitchContinue.gameObject));

            var controller = GetController(bootstrapper);
            Assert.That(controller.Snapshot.State, Is.EqualTo(GameState.ShowingReaction));
            ExecuteEvents.Execute(
                pitchContinue.gameObject,
                new BaseEventData(EventSystem.current),
                ExecuteEvents.submitHandler);
            Assert.That(controller.Snapshot.State, Is.EqualTo(GameState.ShowingFeedback));
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.EqualTo(pitchContinue.gameObject));
        }

        [UnityTest]
        public IEnumerator Bootstrap_CountdownRendersAcrossFrames_AndPulseStopsOnReaction()
        {
            var load = SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null);
            yield return load;

            var initializationDeadline = Time.realtimeSinceStartup + 5f;
            Bootstrapper bootstrapper = null;
            while ((bootstrapper == null || !bootstrapper.IsInitialized) &&
                   Time.realtimeSinceStartup < initializationDeadline)
            {
                bootstrapper = Object.FindAnyObjectByType<Bootstrapper>(FindObjectsInactive.Include);
                yield return null;
            }
            Assert.That(bootstrapper, Is.Not.Null);
            Assert.That(bootstrapper.IsInitialized, Is.True);

            var canvas = GameObject.Find("Generated UI").transform.Find("Canvas");
            var title = canvas.Find("Title");
            title.Find("Start Button").GetComponent<Button>().onClick.Invoke();
            yield return null;
            var briefing = canvas.Find("Briefing");
            briefing.Find("Continue Button").GetComponent<Button>().onClick.Invoke();
            yield return null;

            var pitchRoom = canvas.Find("PitchRoom");
            var pitchContinue = pitchRoom.Find("Continue Button").GetComponent<Button>();
            for (var step = 0; step < 3; step++)
            {
                pitchContinue.onClick.Invoke();
                yield return null;
            }

            var responseViews = pitchRoom.Find("Responses").GetComponentsInChildren<ResponseButtonView>(true);
            responseViews[0].Button.onClick.Invoke();
            for (var step = 0; step < 3; step++)
            {
                pitchContinue.onClick.Invoke();
                yield return null;
            }

            var timer = pitchRoom.GetComponentInChildren<TimerView>();
            var timerFill = pitchRoom.Find("Timer/Fill").GetComponent<Image>();
            var initialSeconds = timer.DisplayedSeconds;
            var initialFill = timerFill.fillAmount;
            Assert.That(initialSeconds, Is.GreaterThan(5));
            Assert.That(initialFill, Is.GreaterThan(0f));

            var countdownDeadline = Time.realtimeSinceStartup + 1.5f;
            var renderedFrames = 0;
            while (timer.DisplayedSeconds >= initialSeconds &&
                   Time.realtimeSinceStartup < countdownDeadline)
            {
                renderedFrames++;
                yield return null;
            }

            Assert.That(renderedFrames, Is.GreaterThan(1));
            Assert.That(timer.DisplayedSeconds, Is.LessThan(initialSeconds));
            Assert.That(timerFill.fillAmount, Is.LessThan(initialFill));

            var controller = GetController(bootstrapper);
            controller.Tick(controller.Snapshot.TimerRemainingSeconds - 4.25d);
            yield return null;
            Assert.That(timer.DisplayedSeconds, Is.EqualTo(5));
            Assert.That(timer.IsPulsing, Is.True);
            Assert.That(timer.transform.localScale.x, Is.GreaterThan(1f));

            responseViews[0].Button.onClick.Invoke();
            Assert.That(controller.Snapshot.State, Is.EqualTo(GameState.ShowingReaction));
            Assert.That(timer.IsPulsing, Is.False);
            Assert.That(timer.transform.localScale, Is.EqualTo(Vector3.one));
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

        private static PitchSessionController GetController(Bootstrapper bootstrapper)
        {
            return (PitchSessionController)typeof(Bootstrapper)
                .GetField("controller", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(bootstrapper);
        }
    }
}
