using System;
using Agrovator.PitchSimulator.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    public sealed class TutorialPresenter : MonoBehaviour
    {
        private static readonly string[] StepKeys =
            { "ui.tutorial.step.1", "ui.tutorial.step.2", "ui.tutorial.step.3" };
        private static readonly string[] HeadingKeys =
            { "ui.tutorial.goal.title", "ui.tutorial.choices.title", "ui.tutorial.feedback.title" };
        private static readonly string[] BodyKeys =
            { "ui.tutorial.goal.body", "ui.tutorial.choices.body", "ui.tutorial.feedback.body" };

        [SerializeField] private Text stepText;
        [SerializeField] private Text headingText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Button backButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Text nextButtonText;

        private PitchSessionController controller;
        private Action changed;
        private Func<string, string> resolveText;
        private int currentPageIndex;
        private bool wasTutorialActive;
        private bool initialized;

        public int CurrentPageIndex => currentPageIndex;
        public int PageCount => StepKeys.Length;

        public void Initialize(PitchSessionController sessionController, Action onChanged,
            Func<string, string> localize = null)
        {
            if (sessionController == null) throw new ArgumentNullException(nameof(sessionController));
            RemoveListeners();
            controller = sessionController;
            changed = onChanged;
            resolveText = localize ?? (key => key);
            backButton.onClick.AddListener(Back);
            skipButton.onClick.AddListener(AdvanceSession);
            nextButton.onClick.AddListener(Next);
            initialized = true;
        }

        public void Refresh(PitchSessionSnapshot snapshot)
        {
            if (!initialized || snapshot == null) return;
            var tutorialActive = snapshot.State == GameState.Tutorial;
            if (tutorialActive && !wasTutorialActive) currentPageIndex = 0;
            wasTutorialActive = tutorialActive;
            if (tutorialActive) RenderPage();
        }

        private void Back()
        {
            if (!initialized || currentPageIndex == 0) return;
            currentPageIndex--;
            RenderPage();
        }

        private void Next()
        {
            if (!initialized) return;
            if (currentPageIndex == PageCount - 1)
            {
                AdvanceSession();
                return;
            }
            currentPageIndex++;
            RenderPage();
        }

        private void AdvanceSession()
        {
            if (!initialized || controller.Snapshot.State != GameState.Tutorial) return;
            if (controller.Continue()) changed?.Invoke();
        }

        private void RenderPage()
        {
            currentPageIndex = Mathf.Clamp(currentPageIndex, 0, PageCount - 1);
            stepText.text = resolveText(StepKeys[currentPageIndex]);
            headingText.text = resolveText(HeadingKeys[currentPageIndex]);
            bodyText.text = resolveText(BodyKeys[currentPageIndex]);
            backButton.interactable = currentPageIndex > 0;
            nextButtonText.text = resolveText(currentPageIndex == PageCount - 1
                ? "ui.start_practice" : "ui.next");
            if (EventSystem.current != null && nextButton.gameObject.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(nextButton.gameObject);
        }

        private void OnDestroy() => RemoveListeners();

        private void RemoveListeners()
        {
            if (backButton != null) backButton.onClick.RemoveListener(Back);
            if (skipButton != null) skipButton.onClick.RemoveListener(AdvanceSession);
            if (nextButton != null) nextButton.onClick.RemoveListener(Next);
        }
    }
}
