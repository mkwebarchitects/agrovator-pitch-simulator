using System;
using System.Collections.Generic;
using Agrovator.PitchSimulator.Core;
using Agrovator.PitchSimulator.GuidedPitch;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    /// <summary>
    /// Maps guided session phases to distinct top-level screens. Every phase from
    /// Learn through FollowUpFeedback shares one persistent Guided Pitch panel so
    /// the progress rail and Pitch Board never visually reset mid-flow.
    /// </summary>
    public sealed class GuidedPitchScreenRouter : MonoBehaviour
    {
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject briefingPanel;
        [SerializeField] private GameObject modeSelectionPanel;
        [SerializeField] private GameObject guidedPanel;
        [SerializeField] private GameObject resultsPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject safeFallbackPanel;
        [SerializeField] private TitlePresenter titlePresenter;
        [SerializeField] private BriefingPresenter briefingPresenter;
        [SerializeField] private GuidedPitchPresenter guidedPresenter;
        [SerializeField] private GuidedPitchResultsPresenter resultsPresenter;
        [SerializeField] private SettingsPresenter settingsPresenter;
        [SerializeField] private SafeFallbackPresenter safeFallbackPresenter;
        [SerializeField] private Selectable titleDefault;
        [SerializeField] private Selectable briefingDefault;
        [SerializeField] private Selectable settingsDefault;

        private GuidedPitchSessionController controller;
        private GameObject panelBeforeSettings;
        private GuidedPitchPhase? focusedPhase;
        private bool settingsOpen;

        public GuidedPitchPhase ActivePhase { get; private set; } = GuidedPitchPhase.Booting;

        public bool IsInitialized { get; private set; }

        public void Configure(
            GameObject title,
            GameObject briefing,
            GameObject modeSelection,
            GameObject guided,
            GameObject results,
            GameObject settings,
            GameObject safeFallback,
            TitlePresenter titleScreenPresenter,
            BriefingPresenter briefingScreenPresenter,
            GuidedPitchPresenter guidedScreenPresenter,
            GuidedPitchResultsPresenter resultsScreenPresenter,
            SettingsPresenter settingsScreenPresenter,
            SafeFallbackPresenter safeFallbackScreenPresenter,
            Selectable titleDefaultSelectable,
            Selectable briefingDefaultSelectable,
            Selectable settingsDefaultSelectable)
        {
            titlePanel = title;
            briefingPanel = briefing;
            modeSelectionPanel = modeSelection;
            guidedPanel = guided;
            resultsPanel = results;
            settingsPanel = settings;
            safeFallbackPanel = safeFallback;
            titlePresenter = titleScreenPresenter;
            briefingPresenter = briefingScreenPresenter;
            guidedPresenter = guidedScreenPresenter;
            resultsPresenter = resultsScreenPresenter;
            settingsPresenter = settingsScreenPresenter;
            safeFallbackPresenter = safeFallbackScreenPresenter;
            titleDefault = titleDefaultSelectable;
            briefingDefault = briefingDefaultSelectable;
            settingsDefault = settingsDefaultSelectable;
            if (!ValidateContract(out var reason))
            {
                throw new InvalidOperationException(reason);
            }
        }

        public void Initialize(GuidedPitchSessionController sessionController,
            Func<string, string> localize, Action onTitleUserGesture = null, Action onButtonPress = null)
        {
            if (sessionController == null) throw new ArgumentNullException(nameof(sessionController));
            if (localize == null) throw new ArgumentNullException(nameof(localize));
            if (!ValidateContract(out var reason))
            {
                throw new InvalidOperationException(reason);
            }

            if (controller != null)
            {
                controller.EventPublished -= HandleSessionEvent;
            }

            controller = sessionController;
            titlePresenter.Initialize(
                () => controller.StartScenario(), Refresh, OpenSettings, onTitleUserGesture);
            briefingPresenter.Initialize(() => controller.Continue(), Refresh, localize, onButtonPress);
            guidedPresenter.Initialize(controller, Refresh, localize, onButtonPress);
            resultsPresenter.Initialize(controller, Refresh, localize, onButtonPress);
            settingsPresenter.Initialize(CloseSettings, onButtonPress);
            controller.EventPublished += HandleSessionEvent;
            IsInitialized = true;
            Refresh();
        }

        public bool ValidateContract(out string reason)
        {
            var panels = new[]
            {
                titlePanel, briefingPanel, modeSelectionPanel, guidedPanel,
                resultsPanel, settingsPanel, safeFallbackPanel,
            };
            foreach (var panel in panels)
            {
                if (panel == null)
                {
                    reason = "Guided router screen panels are incomplete.";
                    return false;
                }
            }
            if (new HashSet<GameObject>(panels).Count != panels.Length)
            {
                reason = "Guided router screen panels must be distinct.";
                return false;
            }

            if (titlePresenter == null || briefingPresenter == null || guidedPresenter == null ||
                resultsPresenter == null || settingsPresenter == null || safeFallbackPresenter == null)
            {
                reason = "Guided router presenter references are incomplete.";
                return false;
            }
            if (!titlePresenter.transform.IsChildOf(titlePanel.transform) ||
                !briefingPresenter.transform.IsChildOf(briefingPanel.transform) ||
                !guidedPresenter.transform.IsChildOf(guidedPanel.transform) ||
                !resultsPresenter.transform.IsChildOf(resultsPanel.transform) ||
                !settingsPresenter.transform.IsChildOf(settingsPanel.transform) ||
                !safeFallbackPresenter.transform.IsChildOf(safeFallbackPanel.transform))
            {
                reason = "Guided router presenters must live inside their own screens.";
                return false;
            }

            if (titleDefault == null || briefingDefault == null || settingsDefault == null ||
                !titleDefault.transform.IsChildOf(titlePanel.transform) ||
                !briefingDefault.transform.IsChildOf(briefingPanel.transform) ||
                !settingsDefault.transform.IsChildOf(settingsPanel.transform))
            {
                reason = "Guided router default focus references are incomplete or misplaced.";
                return false;
            }

            if (!resultsPresenter.ValidateContract())
            {
                reason = "Guided results presenter references are incomplete.";
                return false;
            }

            return guidedPresenter.ValidateContract(out reason);
        }

        public void Refresh()
        {
            if (!IsInitialized || controller == null)
            {
                return;
            }

            var snapshot = controller.Snapshot;
            ActivePhase = snapshot.Phase;
            titlePresenter.SetStartInteractable(snapshot.Phase == GuidedPitchPhase.Title);
            briefingPresenter.SetContinueInteractable(snapshot.Phase == GuidedPitchPhase.Briefing);
            guidedPresenter.Refresh(snapshot);
            resultsPresenter.Refresh(snapshot);
            var target = GetPanel(snapshot.Phase);
            if (settingsOpen)
            {
                // An asynchronous session event must not close the Settings
                // overlay; remember the new phase panel for CloseSettings.
                panelBeforeSettings = target;
                return;
            }

            panelBeforeSettings = null;
            Show(target);
        }

        /// <summary>
        /// Moves keyboard focus through the active guided screen. The router owns
        /// the Tab polling because it stays active on every screen, including the
        /// ModeSelection and Results phases where the guided panel and its
        /// presenter are inactive; each screen presenter cycles only its own
        /// active selectables.
        /// </summary>
        public bool MoveFocus(bool backward)
        {
            if (!IsInitialized)
            {
                return false;
            }

            return resultsPanel.activeSelf
                ? resultsPresenter.MoveFocus(backward)
                : guidedPresenter.MoveFocus(backward);
        }

        private const float NavigationAxisDeadzone = 0.5f;

        private float previousHorizontalAxis;
        private float previousVerticalAxis;

        private void Update()
        {
            if (!IsInitialized)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                MoveFocus(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
                return;
            }

            if (NavigationInputPressed())
            {
                EnsureSelection();
            }
        }

        /// <summary>
        /// Edge-triggers on arrow keys/WASD/gamepad stick (bound to the default
        /// Horizontal/Vertical axes) so a single directional press is detected
        /// once, the same way Input.GetButtonDown detects a single key press.
        /// </summary>
        private bool NavigationInputPressed()
        {
            var horizontal = Input.GetAxisRaw("Horizontal");
            var vertical = Input.GetAxisRaw("Vertical");
            var pressed =
                (Mathf.Abs(horizontal) >= NavigationAxisDeadzone && Mathf.Abs(previousHorizontalAxis) < NavigationAxisDeadzone) ||
                (Mathf.Abs(vertical) >= NavigationAxisDeadzone && Mathf.Abs(previousVerticalAxis) < NavigationAxisDeadzone);
            previousHorizontalAxis = horizontal;
            previousVerticalAxis = vertical;
            return pressed;
        }

        /// <summary>
        /// Restores selection to the active screen's default control when a
        /// mouse click elsewhere cleared EventSystem selection, so a directional
        /// press afterward resumes keyboard/gamepad navigation instead of doing
        /// nothing. Public, like <see cref="MoveFocus"/>, so tests can drive it
        /// directly without simulating hardware axis input.
        /// </summary>
        public bool EnsureSelection()
        {
            if (!IsInitialized)
            {
                return false;
            }

            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            var current = eventSystem.currentSelectedGameObject;
            var currentSelectable = current == null ? null : current.GetComponent<Selectable>();
            var currentInvalid = current == null || !current.activeInHierarchy ||
                currentSelectable == null || !currentSelectable.IsInteractable();
            if (!currentInvalid)
            {
                return false;
            }

            // CurrentPanel() deliberately skips settingsPanel (it exists to find
            // the screen underneath Settings for post-close focus restoration),
            // so it must not be used to find Settings' own default while open.
            var target = GetDefault(settingsOpen ? settingsPanel : CurrentPanel());
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                return false;
            }

            eventSystem.SetSelectedGameObject(target.gameObject);
            return true;
        }

        /// <summary>
        /// Blocks the attempt on the SafeFallback screen with the learner-safe
        /// recovery message. Usable before any controller exists.
        /// </summary>
        public void ShowSafeFallback(Func<string, string> localize)
        {
            if (!ValidateContract(out var reason))
            {
                throw new InvalidOperationException(reason);
            }

            ActivePhase = GuidedPitchPhase.SafeFallback;
            settingsOpen = false;
            safeFallbackPresenter.Show(localize);
            foreach (var panel in AllPanels())
            {
                panel.SetActive(panel == safeFallbackPanel);
            }

            var eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                eventSystem.SetSelectedGameObject(null);
            }
            focusedPhase = GuidedPitchPhase.SafeFallback;
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.EventPublished -= HandleSessionEvent;
            }
        }

        private void HandleSessionEvent(GuidedPitchSessionEvent sessionEvent)
        {
            Refresh();
        }

        private void OpenSettings()
        {
            panelBeforeSettings = CurrentPanel();
            settingsOpen = true;
            Show(settingsPanel);
        }

        private void CloseSettings()
        {
            var target = panelBeforeSettings != null ? panelBeforeSettings : GetPanel(ActivePhase);
            panelBeforeSettings = null;
            settingsOpen = false;
            Show(target);
        }

        private GameObject GetPanel(GuidedPitchPhase phase)
        {
            switch (phase)
            {
                case GuidedPitchPhase.Booting:
                case GuidedPitchPhase.Title:
                    return titlePanel;
                case GuidedPitchPhase.Briefing:
                    return briefingPanel;
                case GuidedPitchPhase.ModeSelection:
                    return modeSelectionPanel;
                case GuidedPitchPhase.Results:
                case GuidedPitchPhase.Submitting:
                case GuidedPitchPhase.Complete:
                    return resultsPanel;
                case GuidedPitchPhase.SafeFallback:
                    return safeFallbackPanel;
                default:
                    return guidedPanel;
            }
        }

        private GameObject CurrentPanel()
        {
            foreach (var panel in AllPanels())
            {
                if (panel != null && panel.activeSelf && panel != settingsPanel)
                {
                    return panel;
                }
            }

            return null;
        }

        private IEnumerable<GameObject> AllPanels()
        {
            yield return titlePanel;
            yield return briefingPanel;
            yield return modeSelectionPanel;
            yield return guidedPanel;
            yield return resultsPanel;
            yield return settingsPanel;
            yield return safeFallbackPanel;
        }

        private void Show(GameObject selected)
        {
            foreach (var panel in AllPanels())
            {
                if (panel != null)
                {
                    // A panel stays active when it is the selected screen or an
                    // ancestor of it: the generated scene nests the Mode Selection
                    // section inside the persistent Guided Pitch panel, so showing
                    // the nested section must keep its host screen visible.
                    panel.SetActive(panel == selected ||
                        (selected != null && selected.transform.IsChildOf(panel.transform)));
                }
            }

            FocusDefault(selected);
        }

        private void FocusDefault(GameObject panel)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return;
            }

            var target = GetDefault(panel);
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                return;
            }

            var current = eventSystem.currentSelectedGameObject;
            var currentSelectable = current == null ? null : current.GetComponent<Selectable>();
            var currentInvalid = current == null || !current.activeInHierarchy ||
                currentSelectable == null || !currentSelectable.IsInteractable();
            var isSettings = panel == settingsPanel;
            if (isSettings || focusedPhase != ActivePhase || currentInvalid)
            {
                eventSystem.SetSelectedGameObject(null);
                eventSystem.SetSelectedGameObject(target.gameObject);
            }

            if (!isSettings)
            {
                focusedPhase = ActivePhase;
            }
        }

        private Selectable GetDefault(GameObject panel)
        {
            if (panel == titlePanel)
            {
                return titleDefault;
            }
            if (panel == briefingPanel)
            {
                return briefingDefault;
            }
            if (panel == modeSelectionPanel || panel == guidedPanel)
            {
                return guidedPresenter.GetDefaultSelectable(ActivePhase);
            }
            if (panel == resultsPanel)
            {
                return resultsPresenter.GetDefaultSelectable(ActivePhase);
            }
            if (panel == settingsPanel)
            {
                return settingsDefault;
            }

            return null;
        }
    }
}
