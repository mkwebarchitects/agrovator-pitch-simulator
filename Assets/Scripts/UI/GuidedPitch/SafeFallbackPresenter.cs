using System;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    public sealed class SafeFallbackPresenter : MonoBehaviour
    {
        public const string EnglishRecoveryMessage =
            "This pitch activity could not be loaded. Refresh and try again, or ask your teacher for help.";

        private const string RecoveryMessageKey = "guided.recovery.message";
        private const string MissingTokenPrefix = "[[missing:";

        [SerializeField] private Text messageText;

        public Text MessageText => messageText;

        public void Configure(Text message)
        {
            messageText = message ?? throw new ArgumentNullException(nameof(message));
        }

        /// <summary>
        /// Shows the learner-safe recovery message. When localization is unavailable
        /// or the key is missing, the exact English sentence is the last resort.
        /// </summary>
        public void Show(Func<string, string> localize)
        {
            if (messageText == null)
            {
                throw new InvalidOperationException("Safe fallback references are incomplete.");
            }

            string message = null;
            if (localize != null)
            {
                message = localize(RecoveryMessageKey);
            }
            if (string.IsNullOrEmpty(message) ||
                message.StartsWith(MissingTokenPrefix, StringComparison.Ordinal))
            {
                message = EnglishRecoveryMessage;
            }

            messageText.text = message;
            gameObject.SetActive(true);
        }
    }
}
