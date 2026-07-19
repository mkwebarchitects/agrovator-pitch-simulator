using System;
using System.Collections.Generic;
using Agrovator.PitchSimulator.GuidedPitch;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    [Serializable]
    public sealed class ModeSelectionCard
    {
        [SerializeField] private LearnerMode mode;
        [SerializeField] private Button button;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Image background;

        public ModeSelectionCard(LearnerMode mode, Button button, Text titleText,
            Text descriptionText, Image background)
        {
            this.mode = mode;
            this.button = button;
            this.titleText = titleText;
            this.descriptionText = descriptionText;
            this.background = background;
        }

        public LearnerMode Mode => mode;
        public Button Button => button;
        public Text TitleText => titleText;
        public Text DescriptionText => descriptionText;
        public Image Background => background;

        internal bool IsValid => button != null && titleText != null &&
            descriptionText != null && background != null;

        internal void ApplySharedStyle()
        {
            button.targetGraphic = background;
            var colours = button.colors;
            colours.normalColor = Color.white;
            colours.highlightedColor = Color.white;
            colours.selectedColor = Color.white;
            colours.pressedColor = Color.white;
            colours.disabledColor = new Color(1f, 1f, 1f, 0.68f);
            button.colors = colours;
            background.color = PitchPartVisuals.CardCream;
            titleText.color = PitchPartVisuals.CardText;
            descriptionText.color = PitchPartVisuals.CardText;
        }

        internal void Render(Func<string, string> localize)
        {
            var keyPart = mode == LearnerMode.Primary ? "primary" : "secondary";
            titleText.text = localize($"guided.mode.{keyPart}.title");
            descriptionText.text = localize($"guided.mode.{keyPart}.parts");
            button.interactable = true;
            button.gameObject.SetActive(true);
        }
    }

    public sealed class ModeSelectionView : MonoBehaviour
    {
        private const int RequiredCardCount = 2;

        [SerializeField] private ModeSelectionCard[] cards = new ModeSelectionCard[RequiredCardCount];

        private Action<LearnerMode> selected;
        private bool listening;

        public IReadOnlyList<ModeSelectionCard> Cards => cards;

        public void Configure(ModeSelectionCard[] modeCards)
        {
            RemoveListeners();
            cards = modeCards == null ? null : (ModeSelectionCard[])modeCards.Clone();
            ValidateCards();
            foreach (var card in cards)
            {
                card.ApplySharedStyle();
            }
        }

        public void Initialize(Action<LearnerMode> onSelected)
        {
            ValidateCards();
            selected = onSelected ?? throw new ArgumentNullException(nameof(onSelected));
            RemoveListeners();
            cards[0].Button.onClick.AddListener(SelectPrimary);
            cards[1].Button.onClick.AddListener(SelectSecondary);
            listening = true;
        }

        public void Render(Func<string, string> localize)
        {
            if (localize == null) throw new ArgumentNullException(nameof(localize));
            ValidateCards();
            foreach (var card in cards)
            {
                card.Render(localize);
            }
        }

        private void OnDestroy()
        {
            RemoveListeners();
        }

        private void SelectPrimary()
        {
            selected?.Invoke(LearnerMode.Primary);
        }

        private void SelectSecondary()
        {
            selected?.Invoke(LearnerMode.Secondary);
        }

        private void RemoveListeners()
        {
            if (!listening || cards == null)
            {
                return;
            }

            if (cards.Length > 0 && cards[0] != null && cards[0].Button != null)
            {
                cards[0].Button.onClick.RemoveListener(SelectPrimary);
            }
            if (cards.Length > 1 && cards[1] != null && cards[1].Button != null)
            {
                cards[1].Button.onClick.RemoveListener(SelectSecondary);
            }
            listening = false;
        }

        private void ValidateCards()
        {
            if (cards == null || cards.Length != RequiredCardCount)
            {
                throw new InvalidOperationException("Mode selection requires exactly two prebuilt cards.");
            }

            if (cards[0] == null || cards[1] == null || !cards[0].IsValid || !cards[1].IsValid ||
                cards[0].Mode != LearnerMode.Primary || cards[1].Mode != LearnerMode.Secondary ||
                ReferenceEquals(cards[0].Button, cards[1].Button))
            {
                throw new InvalidOperationException(
                    "Mode selection cards must be valid, distinct, and ordered Primary then Secondary.");
            }
        }
    }
}
