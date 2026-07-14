using System;
using Agrovator.PitchSimulator.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    public sealed class BriefingPresenter : MonoBehaviour
    {
        [SerializeField] private Button continueButton;
        private PitchSessionController controller;
        private Action changed;
        private bool initialized;

        public void Initialize(PitchSessionController sessionController, Action onChanged)
        {
            if (sessionController == null) throw new ArgumentNullException(nameof(sessionController));
            continueButton.onClick.RemoveListener(Continue);
            controller = sessionController;
            changed = onChanged;
            continueButton.onClick.AddListener(Continue);
            initialized = true;
        }

        public void Refresh(PitchSessionSnapshot snapshot)
        {
            if (!initialized) return;
            continueButton.interactable = snapshot.State == GameState.Briefing;
        }

        private void OnDestroy()
        {
            if (continueButton != null) continueButton.onClick.RemoveListener(Continue);
        }

        private void Continue()
        {
            if (!initialized) return;
            controller.Continue();
            changed?.Invoke();
        }
    }
}
