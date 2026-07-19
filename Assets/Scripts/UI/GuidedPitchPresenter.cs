using System;
using System.Collections.Generic;
using Agrovator.PitchSimulator.Core;
using Agrovator.PitchSimulator.GuidedPitch;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    /// <summary>
    /// Shared Tab/Shift+Tab cycle over the active, interactable selectables
    /// under one screen root, wrapping at both ends. Used by every guided
    /// screen presenter behind the always-active router's Tab polling.
    /// </summary>
    internal static class GuidedFocusCycle
    {
        internal static bool MoveFocus(Transform root, bool backward)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null || root == null)
            {
                return false;
            }

            var candidates = root.GetComponentsInChildren<Selectable>(false);
            var cycle = new List<Selectable>();
            foreach (var candidate in candidates)
            {
                if (candidate.IsInteractable() && candidate.gameObject.activeInHierarchy)
                {
                    cycle.Add(candidate);
                }
            }
            if (cycle.Count == 0)
            {
                return false;
            }

            var current = eventSystem.currentSelectedGameObject;
            var index = -1;
            for (var candidate = 0; candidate < cycle.Count; candidate++)
            {
                if (cycle[candidate].gameObject == current)
                {
                    index = candidate;
                    break;
                }
            }

            int next;
            if (index < 0)
            {
                next = backward ? cycle.Count - 1 : 0;
            }
            else
            {
                next = (index + (backward ? -1 : 1) + cycle.Count) % cycle.Count;
            }

            eventSystem.SetSelectedGameObject(null);
            eventSystem.SetSelectedGameObject(cycle[next].gameObject);
            return true;
        }
    }

    /// <summary>
    /// Thin binding between the guided pitch views and the session controller.
    /// It maps clicks to controller commands and mirrors snapshot state into the
    /// fixed view pools; it never computes mastery, readiness, phase transitions,
    /// revision eligibility, or payload values itself.
    /// The positive execution order guarantees this presenter's LateUpdate runs
    /// after ScrollRect's (default order 0), so the keep-focused-card-visible
    /// adjustment is the final content position of the frame instead of racing
    /// the ScrollRect's elastic spring.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class GuidedPitchPresenter : MonoBehaviour
    {
        private const int PartCount = 4;

        [SerializeField] private ModeSelectionView modeSelection;
        [SerializeField] private LearnPitchView learn;
        [SerializeField] private PitchProgressRailView rail;
        [SerializeField] private PitchBoardView board;
        [SerializeField] private SentenceCardListView cards;
        [SerializeField] private PitchFeedbackView feedback;
        [SerializeField] private Text questionText;
        [SerializeField] private Text hintText;
        [SerializeField] private Text presentationText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button presentButton;
        [SerializeField] private Button[] strengthenButtons = new Button[PartCount];
        [SerializeField] private Text[] strengthenLabels = new Text[PartCount];
        [SerializeField] private ScrollRect cardsScroll;

        private readonly UnityAction[] strengthenHandlers = new UnityAction[PartCount];
        private GuidedPitchSessionController controller;
        private Action changed;
        private Func<string, string> localize;
        private GuidedPitchSessionSnapshot lastSnapshot;
        private GameObject lastScrollTarget;
        private bool initialized;
        private bool listening;

        public void Configure(
            ModeSelectionView modeSelectionView,
            LearnPitchView learnView,
            PitchProgressRailView railView,
            PitchBoardView boardView,
            SentenceCardListView cardListView,
            PitchFeedbackView feedbackView,
            Text question,
            Text hint,
            Text presentation,
            Button continueControl,
            Button presentControl,
            Button[] strengthenControls,
            Text[] strengthenControlLabels,
            ScrollRect cardsScrollRect)
        {
            RemoveListeners();
            modeSelection = modeSelectionView;
            learn = learnView;
            rail = railView;
            board = boardView;
            cards = cardListView;
            feedback = feedbackView;
            questionText = question;
            hintText = hint;
            presentationText = presentation;
            continueButton = continueControl;
            presentButton = presentControl;
            strengthenButtons = strengthenControls == null ? null : (Button[])strengthenControls.Clone();
            strengthenLabels = strengthenControlLabels == null ? null : (Text[])strengthenControlLabels.Clone();
            cardsScroll = cardsScrollRect;
            if (!ValidateContract(out var reason))
            {
                throw new InvalidOperationException(reason);
            }
        }

        public void Initialize(GuidedPitchSessionController sessionController, Action onChanged,
            Func<string, string> localizeText)
        {
            if (sessionController == null) throw new ArgumentNullException(nameof(sessionController));
            if (localizeText == null) throw new ArgumentNullException(nameof(localizeText));
            if (!ValidateContract(out var reason))
            {
                throw new InvalidOperationException(reason);
            }

            RemoveListeners();
            controller = sessionController;
            changed = onChanged;
            localize = localizeText;
            modeSelection.Initialize(HandleModeSelected);
            cards.Initialize(HandleCardSelected);
            continueButton.onClick.AddListener(HandleContinue);
            presentButton.onClick.AddListener(HandlePresent);
            for (var index = 0; index < PartCount; index++)
            {
                var part = (PitchPart)index;
                strengthenHandlers[index] = () => HandleStrengthen(part);
                strengthenButtons[index].onClick.AddListener(strengthenHandlers[index]);
            }

            listening = true;
            initialized = true;
        }

        public bool ValidateContract(out string reason)
        {
            if (modeSelection == null || learn == null || rail == null || board == null ||
                cards == null || feedback == null || questionText == null || hintText == null ||
                presentationText == null || continueButton == null || presentButton == null ||
                cardsScroll == null)
            {
                reason = "Guided presenter view references are incomplete.";
                return false;
            }

            if (strengthenButtons == null || strengthenLabels == null ||
                strengthenButtons.Length != PartCount || strengthenLabels.Length != PartCount)
            {
                reason = "Guided presenter requires exactly four strengthen controls.";
                return false;
            }

            var uniqueButtons = new HashSet<Button>();
            for (var index = 0; index < PartCount; index++)
            {
                if (strengthenButtons[index] == null || strengthenLabels[index] == null ||
                    !uniqueButtons.Add(strengthenButtons[index]))
                {
                    reason = "Guided presenter strengthen controls must be non-null and distinct.";
                    return false;
                }
            }

            reason = null;
            return true;
        }

        public void Refresh(GuidedPitchSessionSnapshot snapshot)
        {
            if (!initialized)
            {
                return;
            }
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            var phaseChanged = lastSnapshot == null || lastSnapshot.Phase != snapshot.Phase;
            lastSnapshot = snapshot;
            var phase = snapshot.Phase;

            var showModeSelection = phase == GuidedPitchPhase.ModeSelection;
            modeSelection.gameObject.SetActive(showModeSelection);
            if (showModeSelection)
            {
                modeSelection.Render(localize);
            }

            var guidedVisible = phase >= GuidedPitchPhase.Learn && phase <= GuidedPitchPhase.FollowUpFeedback;
            if (!guidedVisible)
            {
                learn.gameObject.SetActive(false);
                cards.Clear();
                feedback.Clear();
                SetText(questionText, null);
                SetText(hintText, null);
                SetText(presentationText, null);
                continueButton.gameObject.SetActive(false);
                presentButton.gameObject.SetActive(false);
                foreach (var button in strengthenButtons)
                {
                    button.gameObject.SetActive(false);
                }
                return;
            }

            learn.gameObject.SetActive(phase == GuidedPitchPhase.Learn);
            if (phase == GuidedPitchPhase.Learn)
            {
                learn.Render(localize);
            }

            rail.Render(RailCurrent(snapshot), snapshot.Draft, localize);
            board.Render(snapshot.Draft, localize);
            board.SetRevisionSelection(
                phase == GuidedPitchPhase.Improve ? snapshot.ActivePart : null);

            var revisionListOpen = phase == GuidedPitchPhase.Improve &&
                snapshot.ActivePart.HasValue && snapshot.Feedback == null;
            var showCards = phase == GuidedPitchPhase.Build ||
                phase == GuidedPitchPhase.FollowUp || revisionListOpen;
            if (showCards)
            {
                cards.Render(snapshot.AvailableOptions, null, true, localize);
            }
            else
            {
                cards.Clear();
            }

            if (snapshot.Feedback != null &&
                (phase == GuidedPitchPhase.BuildFeedback ||
                 phase == GuidedPitchPhase.FollowUpFeedback ||
                 phase == GuidedPitchPhase.Improve))
            {
                feedback.Render(snapshot.Feedback, localize);
            }
            else
            {
                feedback.Clear();
            }

            SetText(questionText, QuestionFor(snapshot));
            SetText(hintText, revisionListOpen
                ? localize(PartKey(snapshot.ActivePart.Value, "hint"))
                : null);
            SetText(presentationText, phase == GuidedPitchPhase.Present
                ? ComposePresentation(snapshot)
                : null);

            continueButton.gameObject.SetActive(
                phase == GuidedPitchPhase.Learn ||
                phase == GuidedPitchPhase.BuildFeedback ||
                phase == GuidedPitchPhase.Present ||
                phase == GuidedPitchPhase.FollowUpFeedback);

            var improve = phase == GuidedPitchPhase.Improve;
            presentButton.gameObject.SetActive(improve);
            if (improve)
            {
                presentButton.interactable = snapshot.Draft.IsComplete;
            }

            for (var index = 0; index < PartCount; index++)
            {
                var section = snapshot.Draft[(PitchPart)index];
                var show = improve && section.IsPopulated &&
                    section.CurrentMastery != MasteryState.Clear;
                strengthenButtons[index].gameObject.SetActive(show);
                if (show)
                {
                    strengthenLabels[index].text = localize(
                        PitchPartVisuals.MasteryLabelKey(section.CurrentMastery.Value));
                }
            }

            if (phaseChanged)
            {
                ResetPhaseScroll();
            }
        }

        public Selectable GetDefaultSelectable(GuidedPitchPhase phase)
        {
            switch (phase)
            {
                case GuidedPitchPhase.ModeSelection:
                    return modeSelection.Cards.Count > 0 ? modeSelection.Cards[0].Button : null;
                case GuidedPitchPhase.Learn:
                case GuidedPitchPhase.BuildFeedback:
                case GuidedPitchPhase.Present:
                case GuidedPitchPhase.FollowUpFeedback:
                    return continueButton;
                case GuidedPitchPhase.Build:
                case GuidedPitchPhase.FollowUp:
                    return cards.Cards.Count > 0 ? cards.Cards[0].Button : null;
                case GuidedPitchPhase.Improve:
                    if (lastSnapshot != null && lastSnapshot.ActivePart.HasValue &&
                        lastSnapshot.Feedback == null && cards.Cards.Count > 0)
                    {
                        return cards.Cards[0].Button;
                    }
                    foreach (var button in strengthenButtons)
                    {
                        if (button != null && button.gameObject.activeInHierarchy && button.IsInteractable())
                        {
                            return button;
                        }
                    }
                    return presentButton;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Moves keyboard focus to the next (Tab) or previous (Shift+Tab) active
        /// selectable of the current guided screen, wrapping at both ends. The
        /// always-active router polls Tab and forwards here, because this
        /// presenter's own panel is inactive during the ModeSelection phase.
        /// </summary>
        public bool MoveFocus(bool backward)
        {
            if (lastSnapshot == null)
            {
                return false;
            }

            var root = lastSnapshot.Phase == GuidedPitchPhase.ModeSelection
                ? modeSelection.transform
                : transform;
            return GuidedFocusCycle.MoveFocus(root, backward);
        }

        private void LateUpdate()
        {
            var eventSystem = EventSystem.current;
            var selected = eventSystem == null ? null : eventSystem.currentSelectedGameObject;
            if (selected == lastScrollTarget)
            {
                return;
            }

            lastScrollTarget = selected;
            if (selected == null || cardsScroll == null || cardsScroll.content == null)
            {
                return;
            }

            var target = selected.transform as RectTransform;
            if (target == null || !target.IsChildOf(cardsScroll.content))
            {
                return;
            }

            EnsureVisibleInScroll(target);
        }

        private void OnDestroy()
        {
            RemoveListeners();
        }

        private void EnsureVisibleInScroll(RectTransform target)
        {
            var viewport = cardsScroll.viewport != null
                ? cardsScroll.viewport
                : (RectTransform)cardsScroll.transform;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, target);
            var viewRect = viewport.rect;
            var delta = 0f;
            if (bounds.min.y < viewRect.yMin)
            {
                delta = viewRect.yMin - bounds.min.y;
            }
            else if (bounds.max.y > viewRect.yMax)
            {
                delta = viewRect.yMax - bounds.max.y;
            }

            if (!Mathf.Approximately(delta, 0f))
            {
                // Zero any residual drag/elastic velocity so the ScrollRect does
                // not carry stale motion into the frames after this adjustment.
                cardsScroll.StopMovement();
                cardsScroll.content.anchoredPosition += new Vector2(0f, delta);
            }
        }

        private void ResetPhaseScroll()
        {
            if (cardsScroll == null || cardsScroll.content == null)
            {
                return;
            }

            cardsScroll.StopMovement();
            cardsScroll.verticalNormalizedPosition = 1f;
            var anchored = cardsScroll.content.anchoredPosition;
            cardsScroll.content.anchoredPosition = new Vector2(anchored.x, 0f);
            lastScrollTarget = null;
        }

        private void HandleModeSelected(LearnerMode mode)
        {
            if (!initialized)
            {
                return;
            }

            controller.SelectLearnerMode(mode);
            changed?.Invoke();
        }

        private void HandleCardSelected(string responseId)
        {
            if (!initialized || lastSnapshot == null)
            {
                return;
            }

            switch (lastSnapshot.Phase)
            {
                case GuidedPitchPhase.Build:
                    controller.SelectPitchResponse(responseId);
                    break;
                case GuidedPitchPhase.Improve:
                    controller.ReplacePitchResponse(responseId);
                    break;
                case GuidedPitchPhase.FollowUp:
                    controller.SelectFollowUpResponse(responseId);
                    break;
                default:
                    return;
            }

            changed?.Invoke();
        }

        private void HandleContinue()
        {
            if (!initialized)
            {
                return;
            }

            controller.Continue();
            changed?.Invoke();
        }

        private void HandlePresent()
        {
            if (!initialized)
            {
                return;
            }

            controller.PresentPitch();
            changed?.Invoke();
        }

        private void HandleStrengthen(PitchPart part)
        {
            if (!initialized)
            {
                return;
            }

            controller.BeginRevision(part);
            changed?.Invoke();
        }

        private string QuestionFor(GuidedPitchSessionSnapshot snapshot)
        {
            switch (snapshot.Phase)
            {
                case GuidedPitchPhase.Build:
                    return snapshot.ActivePart.HasValue
                        ? localize(PartKey(snapshot.ActivePart.Value, "question"))
                        : null;
                case GuidedPitchPhase.FollowUp:
                    return localize("guided.follow_up.question");
                case GuidedPitchPhase.Improve:
                    return localize("guided.improve.instruction");
                default:
                    return null;
            }
        }

        private string ComposePresentation(GuidedPitchSessionSnapshot snapshot)
        {
            return PitchPartVisuals.ComposeCurrentSentences(snapshot.Draft, localize)
                .Replace("\n\n", "\n");
        }

        private static PitchPart? RailCurrent(GuidedPitchSessionSnapshot snapshot)
        {
            switch (snapshot.Phase)
            {
                case GuidedPitchPhase.Build:
                case GuidedPitchPhase.BuildFeedback:
                case GuidedPitchPhase.Improve:
                    return snapshot.ActivePart;
                default:
                    return null;
            }
        }

        private static string PartKey(PitchPart part, string suffix)
        {
            switch (part)
            {
                case PitchPart.Problem:
                    return "guided.part.problem." + suffix;
                case PitchPart.Evidence:
                    return "guided.part.evidence." + suffix;
                case PitchPart.Solution:
                    return "guided.part.solution." + suffix;
                case PitchPart.Value:
                    return "guided.part.value." + suffix;
                default:
                    throw new ArgumentOutOfRangeException(nameof(part), part, "Unknown pitch part.");
            }
        }

        private static void SetText(Text target, string value)
        {
            var visible = !string.IsNullOrEmpty(value);
            target.text = visible ? value : string.Empty;
            target.gameObject.SetActive(visible);
        }

        private void RemoveListeners()
        {
            if (!listening)
            {
                return;
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(HandleContinue);
            }
            if (presentButton != null)
            {
                presentButton.onClick.RemoveListener(HandlePresent);
            }
            if (strengthenButtons != null)
            {
                for (var index = 0; index < strengthenButtons.Length && index < PartCount; index++)
                {
                    if (strengthenButtons[index] != null && strengthenHandlers[index] != null)
                    {
                        strengthenButtons[index].onClick.RemoveListener(strengthenHandlers[index]);
                    }
                }
            }

            listening = false;
        }
    }
}
