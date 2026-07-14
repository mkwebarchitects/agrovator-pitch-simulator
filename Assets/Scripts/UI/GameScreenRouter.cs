using System;
using System.Collections.Generic;
using Agrovator.PitchSimulator.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    public sealed class GameScreenRouter : MonoBehaviour
    {
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject briefingPanel;
        [SerializeField] private GameObject pitchRoomPanel;
        [SerializeField] private GameObject resultsPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private TitlePresenter titlePresenter;
        [SerializeField] private BriefingPresenter briefingPresenter;
        [SerializeField] private PitchRoomPresenter pitchRoomPresenter;
        [SerializeField] private ResultsPresenter resultsPresenter;
        [SerializeField] private SettingsPresenter settingsPresenter;
        [SerializeField] private Selectable titleDefault;
        [SerializeField] private Selectable briefingDefault;
        [SerializeField] private Selectable pitchRoomDefault;
        [SerializeField] private Selectable pitchRoomContinueDefault;
        [SerializeField] private Selectable resultsDefault;
        [SerializeField] private Selectable settingsDefault;

        private PitchSessionController controller;
        private GameObject panelBeforeSettings;

        public bool IsInitialized { get; private set; }

        public void Initialize(PitchSessionController sessionController, Func<string, string> localize = null,
            Action onTitleUserGesture = null)
        {
            if (sessionController == null)
            {
                throw new ArgumentNullException(nameof(sessionController));
            }
            if (!ValidateContract(out var reason))
            {
                throw new InvalidOperationException(reason);
            }

            if (controller != null)
            {
                controller.EventPublished -= HandleSessionEvent;
            }

            controller = sessionController;
            titlePresenter.Initialize(controller, Refresh, OpenSettings, onTitleUserGesture);
            briefingPresenter.Initialize(controller, Refresh);
            pitchRoomPresenter.Initialize(controller, Refresh, localize);
            resultsPresenter.Initialize(controller, Refresh,
                localize ?? (key => "Localization text is unavailable."));
            settingsPresenter.Initialize(CloseSettings);
            controller.EventPublished += HandleSessionEvent;
            IsInitialized = true;
            Refresh();
        }

        public bool ValidateContract(out string reason)
        {
            var panels = new[] { titlePanel, briefingPanel, pitchRoomPanel, resultsPanel, settingsPanel };
            var presenters = new MonoBehaviour[]
            {
                titlePresenter,
                briefingPresenter,
                pitchRoomPresenter,
                resultsPresenter,
                settingsPresenter,
            };
            var defaults = new[]
            {
                titleDefault,
                briefingDefault,
                pitchRoomDefault,
                resultsDefault,
                settingsDefault,
            };

            for (var index = 0; index < panels.Length; index++)
            {
                if (panels[index] == null || presenters[index] == null || defaults[index] == null)
                {
                    reason = $"Router contract reference {index} is incomplete.";
                    return false;
                }
                if (!presenters[index].transform.IsChildOf(panels[index].transform) ||
                    !defaults[index].transform.IsChildOf(panels[index].transform))
                {
                    reason = $"Router contract reference {index} is assigned outside its screen.";
                    return false;
                }
            }

            if (new HashSet<GameObject>(panels).Count != panels.Length)
            {
                reason = "Router screen panels must be distinct.";
                return false;
            }
            if (pitchRoomContinueDefault == null ||
                !pitchRoomContinueDefault.transform.IsChildOf(pitchRoomPanel.transform) ||
                pitchRoomContinueDefault == pitchRoomDefault)
            {
                reason = "Pitch-room Continue focus reference is incomplete or invalid.";
                return false;
            }
            if (!resultsPresenter.ValidateContract())
            {
                reason = "Results presenter contract is incomplete.";
                return false;
            }

            reason = null;
            return true;
        }

        public void TickPresentation(PitchSessionSnapshot snapshot)
        {
            if (!IsInitialized || snapshot == null)
            {
                return;
            }

            pitchRoomPresenter.RefreshTimer(snapshot);
        }

        public void Refresh()
        {
            if (!IsInitialized || controller == null)
            {
                return;
            }

            var snapshot = controller.Snapshot;
            titlePresenter.Refresh(snapshot);
            briefingPresenter.Refresh(snapshot);
            pitchRoomPresenter.Refresh(snapshot);
            resultsPresenter.Refresh(snapshot);
            Show(GetPanel(snapshot.State));
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.EventPublished -= HandleSessionEvent;
            }
        }

        private void HandleSessionEvent(PitchSessionEvent sessionEvent)
        {
            Refresh();
        }

        private void OpenSettings()
        {
            panelBeforeSettings = CurrentPanel();
            Show(settingsPanel);
        }

        private void CloseSettings()
        {
            Show(panelBeforeSettings != null ? panelBeforeSettings : GetPanel(controller.Snapshot.State));
            panelBeforeSettings = null;
        }

        private GameObject GetPanel(GameState state)
        {
            switch (state)
            {
                case GameState.Title:
                case GameState.Booting:
                    return titlePanel;
                case GameState.Briefing:
                    return briefingPanel;
                case GameState.Results:
                case GameState.Submitting:
                case GameState.Complete:
                    return resultsPanel;
                default:
                    return pitchRoomPanel;
            }
        }

        private GameObject CurrentPanel()
        {
            foreach (var panel in new[] { titlePanel, briefingPanel, pitchRoomPanel, resultsPanel })
            {
                if (panel != null && panel.activeSelf)
                {
                    return panel;
                }
            }

            return null;
        }

        private void Show(GameObject selected)
        {
            foreach (var panel in new[] { titlePanel, briefingPanel, pitchRoomPanel, resultsPanel, settingsPanel })
            {
                if (panel != null)
                {
                    panel.SetActive(panel == selected);
                }
            }

            var eventSystem = EventSystem.current;
            var selectable = GetDefault(selected);
            if (eventSystem != null && selectable != null && selectable.gameObject.activeInHierarchy)
            {
                eventSystem.SetSelectedGameObject(null);
                eventSystem.SetSelectedGameObject(selectable.gameObject);
            }
        }

        private Selectable GetDefault(GameObject panel)
        {
            if (panel == titlePanel) return titleDefault;
            if (panel == briefingPanel) return briefingDefault;
            if (panel == pitchRoomPanel)
            {
                return controller != null && controller.Snapshot.State == GameState.AwaitingResponse
                    ? pitchRoomDefault
                    : pitchRoomContinueDefault;
            }
            if (panel == resultsPanel) return resultsDefault;
            if (panel == settingsPanel) return settingsDefault;
            return null;
        }
    }
}
