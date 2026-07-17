using System;
using System.Collections.Generic;
using Agrovator.PitchSimulator.GuidedPitch;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    public sealed class SentenceCardListView : MonoBehaviour
    {
        private const int RequiredCardCount = 3;

        [SerializeField] private SentenceCardView[] cards = new SentenceCardView[RequiredCardCount];

        private Action<string> selected;
        private bool initialized;

        public IReadOnlyList<SentenceCardView> Cards => cards;
        public bool IsSelectionLocked { get; private set; }

        public void Configure(SentenceCardView[] cardPool)
        {
            cards = cardPool == null ? null : (SentenceCardView[])cardPool.Clone();
            ValidateCards();
        }

        public void Initialize(Action<string> onSelected)
        {
            ValidateCards();
            selected = onSelected ?? throw new ArgumentNullException(nameof(onSelected));
            initialized = true;
        }

        public void Render(IReadOnlyList<GuidedPitchOption> options, string selectedId,
            bool interactable, Func<string, string> localize)
        {
            if (!initialized)
            {
                throw new InvalidOperationException("Sentence card list must be initialized before rendering.");
            }
            if (localize == null) throw new ArgumentNullException(nameof(localize));

            options = options ?? Array.Empty<GuidedPitchOption>();
            if (options.Count > RequiredCardCount)
            {
                throw new ArgumentOutOfRangeException(nameof(options), "At most three sentence cards can be rendered.");
            }

            IsSelectionLocked = !interactable;
            for (var index = 0; index < cards.Length; index++)
            {
                if (index < options.Count)
                {
                    var option = options[index] ?? throw new ArgumentException("Options cannot contain null.", nameof(options));
                    cards[index].Show(option.Id, localize(option.TextKey),
                        string.Equals(option.Id, selectedId, StringComparison.Ordinal), interactable, HandleSelected);
                }
                else
                {
                    cards[index].ClearAndHide();
                }
            }

            ConfigureNavigation(options.Count);
        }

        public void Clear()
        {
            if (cards == null) return;
            foreach (var card in cards)
            {
                card?.ClearAndHide();
            }
            IsSelectionLocked = true;
        }

        private void HandleSelected(SentenceCardView card)
        {
            if (IsSelectionLocked || card == null || string.IsNullOrEmpty(card.ResponseId)) return;

            IsSelectionLocked = true;
            foreach (var candidate in cards)
            {
                candidate.SetSelected(ReferenceEquals(candidate, card));
                candidate.SetInteractable(false);
            }
            selected(card.ResponseId);
        }

        private void ConfigureNavigation(int visibleCount)
        {
            for (var index = 0; index < cards.Length; index++)
            {
                var navigation = cards[index].Button.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnLeft = null;
                navigation.selectOnRight = null;
                navigation.selectOnUp = index > 0 && index < visibleCount ? cards[index - 1].Button : null;
                navigation.selectOnDown = index >= 0 && index < visibleCount - 1 ? cards[index + 1].Button : null;
                cards[index].Button.navigation = navigation;
            }
        }

        private void ValidateCards()
        {
            if (cards == null || cards.Length != RequiredCardCount)
            {
                throw new InvalidOperationException("Sentence card list requires exactly three prebuilt cards.");
            }

            var unique = new HashSet<SentenceCardView>();
            foreach (var card in cards)
            {
                if (card == null || card.Button == null || !unique.Add(card))
                {
                    throw new InvalidOperationException("Sentence cards must be non-null, configured, and distinct.");
                }
            }
        }
    }
}
