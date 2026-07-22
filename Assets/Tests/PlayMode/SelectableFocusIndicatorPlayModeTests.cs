using System.Collections.Generic;
using Agrovator.PitchSimulator.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.Tests.PlayMode
{
    /// <summary>
    /// The reusable focus outline every generated Selectable owns
    /// (GuidedPitchSceneBuilder.AddFocusIndicator) previously only reacted to
    /// keyboard/gamepad EventSystem selection, unlike SentenceCardView's
    /// separate hand-rolled outline, which already showed on mouse hover too.
    /// That made hover feedback inconsistent across the game's buttons. These
    /// tests pin that the shared indicator now shows on hover exactly like it
    /// does on keyboard focus, and stays off for a non-interactable control.
    /// </summary>
    public sealed class SelectableFocusIndicatorPlayModeTests
    {
        private readonly List<Object> owned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var item in owned)
            {
                if (item != null) Object.DestroyImmediate(item);
            }
            owned.Clear();
        }

        [Test]
        public void PointerEnterAndExit_ToggleTheOutline_LikeKeyboardSelectionAlreadyDoes()
        {
            var (button, indicator, outline) = BuildRig(interactable: true);

            Assert.That(outline.enabled, Is.False, "The outline must start hidden.");

            Enter(button.gameObject);
            Assert.That(outline.enabled, Is.True, "Hovering must show the outline.");
            Assert.That(indicator.IsFocused, Is.True);

            Exit(button.gameObject);
            Assert.That(outline.enabled, Is.False, "Leaving the control must hide the outline again.");
            Assert.That(indicator.IsFocused, Is.False);
        }

        [Test]
        public void PointerEnter_OnANonInteractableControl_DoesNotShowTheOutline()
        {
            var (button, indicator, outline) = BuildRig(interactable: false);

            Enter(button.gameObject);
            Assert.That(outline.enabled, Is.False,
                "A disabled control must not show hover feedback it cannot act on.");
            Assert.That(indicator.IsFocused, Is.False);
        }

        [Test]
        public void HoverAndKeyboardFocus_Combine_SoLeavingWhileStillSelectedKeepsTheOutlineOn()
        {
            var (button, _, outline) = BuildRig(interactable: true);
            var eventSystem = EventSystem.current;

            eventSystem.SetSelectedGameObject(button.gameObject);
            Assert.That(outline.enabled, Is.True, "Keyboard selection must show the outline.");

            Enter(button.gameObject);
            Exit(button.gameObject);
            Assert.That(outline.enabled, Is.True,
                "The outline must stay on while the control is still keyboard-selected, " +
                "even after the pointer leaves.");

            eventSystem.SetSelectedGameObject(null);
            Assert.That(outline.enabled, Is.False);
        }

        private (Button button, SelectableFocusIndicator indicator, Outline outline) BuildRig(bool interactable)
        {
            var canvasRoot = Track(new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)));
            Track(new GameObject("EventSystem", typeof(EventSystem)));

            var buttonObject = new GameObject("Button", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(canvasRoot.transform, false);
            var image = buttonObject.GetComponent<Image>();
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.interactable = interactable;

            var outline = buttonObject.AddComponent<Outline>();
            outline.enabled = false;
            var indicator = buttonObject.AddComponent<SelectableFocusIndicator>();
            indicator.Configure(button, outline);

            return (button, indicator, outline);
        }

        private static void Enter(GameObject target)
        {
            ExecuteEvents.Execute(target, new PointerEventData(EventSystem.current),
                ExecuteEvents.pointerEnterHandler);
        }

        private static void Exit(GameObject target)
        {
            ExecuteEvents.Execute(target, new PointerEventData(EventSystem.current),
                ExecuteEvents.pointerExitHandler);
        }

        private GameObject Track(GameObject go)
        {
            owned.Add(go);
            return go;
        }
    }
}
