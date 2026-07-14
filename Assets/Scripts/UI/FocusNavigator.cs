using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    public static class FocusNavigator
    {
        public static void ConfigureVisible(IReadOnlyList<ResponseButtonView> slots, int visibleCount)
        {
            if (slots == null) throw new ArgumentNullException(nameof(slots));
            if (visibleCount < 0 || visibleCount > slots.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(visibleCount));
            }

            for (var index = 0; index < slots.Count; index++)
            {
                var button = slots[index].Button;
                if (button == null) continue;
                var navigation = button.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                if (index < visibleCount && visibleCount > 0)
                {
                    navigation.selectOnUp = slots[(index - 1 + visibleCount) % visibleCount].Button;
                    navigation.selectOnDown = slots[(index + 1) % visibleCount].Button;
                }
                else
                {
                    navigation.selectOnUp = null;
                    navigation.selectOnDown = null;
                }
                navigation.selectOnLeft = null;
                navigation.selectOnRight = null;
                button.navigation = navigation;
            }
        }

        public static void FocusFirst(IReadOnlyList<ResponseButtonView> slots, int visibleCount)
        {
            if (slots == null || visibleCount <= 0 || EventSystem.current == null)
            {
                return;
            }

            var target = slots[0].Button;
            if (target == null || !target.gameObject.activeInHierarchy || !target.interactable)
            {
                return;
            }

            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(target.gameObject);
        }
    }
}
