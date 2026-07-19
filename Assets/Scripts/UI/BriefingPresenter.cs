using System;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    /// <summary>
    /// Briefing screen binding expressed as callbacks so it depends on neither the
    /// legacy nor the guided session controller. Optional localized lines let a
    /// composition wire copy keys without coupling this presenter to content.
    /// </summary>
    public sealed class BriefingPresenter : MonoBehaviour
    {
        [SerializeField] private Button continueButton;
        [SerializeField] private Text[] lineTexts = Array.Empty<Text>();
        [SerializeField] private string[] lineKeys = Array.Empty<string>();
        private Action continueAction;
        private Action changed;
        private bool initialized;

        public void Initialize(Action continueAction, Action changed, Func<string, string> localize)
        {
            if (continueAction == null) throw new ArgumentNullException(nameof(continueAction));
            continueButton.onClick.RemoveListener(Continue);
            this.continueAction = continueAction;
            this.changed = changed;
            continueButton.onClick.AddListener(Continue);
            RenderLines(localize);
            initialized = true;
        }

        public void SetContinueInteractable(bool value)
        {
            if (!initialized) return;
            continueButton.interactable = value;
        }

        private void RenderLines(Func<string, string> localize)
        {
            if (localize == null || lineTexts == null || lineKeys == null) return;
            var count = Math.Min(lineTexts.Length, lineKeys.Length);
            for (var index = 0; index < count; index++)
            {
                if (lineTexts[index] != null && !string.IsNullOrEmpty(lineKeys[index]))
                {
                    lineTexts[index].text = localize(lineKeys[index]);
                }
            }
        }

        private void OnDestroy()
        {
            if (continueButton != null) continueButton.onClick.RemoveListener(Continue);
        }

        private void Continue()
        {
            if (!initialized) return;
            continueAction();
            changed?.Invoke();
        }
    }
}
