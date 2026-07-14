using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    public sealed class KeyboardReviewScrollbar : Scrollbar
    {
        private const float BoundaryEpsilon = 0.0001f;

        public override void OnMove(AxisEventData eventData)
        {
            if (!IsActive() || !IsInteractable() || eventData == null)
            {
                base.OnMove(eventData);
                return;
            }

            if (eventData.moveDir == MoveDirection.Down)
            {
                if (value > BoundaryEpsilon)
                {
                    value = Mathf.Max(0f, value - KeyboardStep());
                    eventData.Use();
                    return;
                }

                NavigateIfUsable(navigation.selectOnDown, eventData);
                return;
            }

            if (eventData.moveDir == MoveDirection.Up)
            {
                if (value < 1f - BoundaryEpsilon)
                {
                    value = Mathf.Min(1f, value + KeyboardStep());
                    eventData.Use();
                    return;
                }

                NavigateIfUsable(navigation.selectOnUp, eventData);
                return;
            }

            base.OnMove(eventData);
        }

        private float KeyboardStep()
        {
            return numberOfSteps > 1 ? 1f / (numberOfSteps - 1) : 0.1f;
        }

        private void NavigateIfUsable(Selectable target, AxisEventData eventData)
        {
            if (target != null && target.IsActive() && target.IsInteractable() &&
                target.gameObject.activeInHierarchy)
            {
                EventSystem.current?.SetSelectedGameObject(target.gameObject, eventData);
            }

            eventData.Use();
        }
    }
}
