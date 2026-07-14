using System;
using Agrovator.PitchSimulator.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    public sealed class PitchRoomPresenter : MonoBehaviour
    {
        [SerializeField] private Text promptText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button[] responseButtons;
        [SerializeField] private Text[] responseLabels;
        [SerializeField] private Button continueButton;

        private readonly string[] responseIds = new string[3];
        private UnityAction[] responseActions;
        private PitchSessionController controller;
        private Action changed;
        private bool initialized;

        public void Initialize(PitchSessionController sessionController, Action onChanged)
        {
            if (sessionController == null) throw new ArgumentNullException(nameof(sessionController));
            RemoveListeners();
            controller = sessionController;
            changed = onChanged;
            responseActions = new UnityAction[responseButtons.Length];
            for (var index = 0; index < responseButtons.Length; index++)
            {
                var captured = index;
                responseActions[index] = () => Select(captured);
                responseButtons[index].onClick.AddListener(responseActions[index]);
            }
            continueButton.onClick.AddListener(Continue);
            initialized = true;
        }

        public void Refresh(PitchSessionSnapshot snapshot)
        {
            if (!initialized) return;
            promptText.text = snapshot.CurrentNode?.TextKey ?? "Preparing your pitch…";
            statusText.text = $"Score {snapshot.OverallScore}   Confidence {snapshot.Confidence}";
            for (var index = 0; index < responseButtons.Length; index++)
            {
                var available = index < snapshot.AvailableResponses.Count;
                responseIds[index] = available ? snapshot.AvailableResponses[index].Id : null;
                responseLabels[index].text = available ? snapshot.AvailableResponses[index].TextKey : string.Empty;
                responseButtons[index].gameObject.SetActive(available);
                responseButtons[index].interactable = available && snapshot.State == GameState.AwaitingResponse;
            }

            continueButton.gameObject.SetActive(snapshot.State != GameState.AwaitingResponse);
            continueButton.interactable = snapshot.State != GameState.AwaitingResponse;
        }

        private void OnDestroy()
        {
            RemoveListeners();
        }

        private void RemoveListeners()
        {
            if (responseButtons != null && responseActions != null)
            {
                for (var index = 0; index < responseButtons.Length && index < responseActions.Length; index++)
                {
                    if (responseButtons[index] != null && responseActions[index] != null)
                    {
                        responseButtons[index].onClick.RemoveListener(responseActions[index]);
                    }
                }
            }
            responseActions = null;
            if (continueButton != null) continueButton.onClick.RemoveListener(Continue);
        }

        private void Select(int index)
        {
            if (!initialized || index < 0 || index >= responseIds.Length || responseIds[index] == null) return;
            controller.SelectResponse(responseIds[index]);
            changed?.Invoke();
        }

        private void Continue()
        {
            if (!initialized) return;
            controller.Continue();
            changed?.Invoke();
        }
    }
}
