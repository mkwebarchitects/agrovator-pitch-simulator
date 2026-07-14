using System;
using Agrovator.PitchSimulator.Core;
using UnityEngine;

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

        private PitchSessionController controller;
        private GameObject panelBeforeSettings;

        public bool IsInitialized { get; private set; }

        public void Initialize(PitchSessionController sessionController)
        {
            if (sessionController == null)
            {
                throw new ArgumentNullException(nameof(sessionController));
            }

            if (controller != null)
            {
                controller.EventPublished -= HandleSessionEvent;
            }

            controller = sessionController;
            titlePresenter.Initialize(controller, Refresh, OpenSettings);
            briefingPresenter.Initialize(controller, Refresh);
            pitchRoomPresenter.Initialize(controller, Refresh);
            resultsPresenter.Initialize(controller, Refresh);
            settingsPresenter.Initialize(CloseSettings);
            controller.EventPublished += HandleSessionEvent;
            IsInitialized = true;
            Refresh();
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
        }
    }
}
