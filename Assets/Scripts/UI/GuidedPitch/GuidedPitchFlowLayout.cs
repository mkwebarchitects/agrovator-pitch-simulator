using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    /// <summary>
    /// Lays out a fixed generated control group as either one row or one stack.
    /// The same serialized children are repositioned in place when the viewport changes.
    /// </summary>
    public sealed class GuidedPitchFlowLayout : LayoutGroup
    {
        [SerializeField] private float itemHeight = 64f;
        [SerializeField] private float itemSpacing = 8f;
        [SerializeField] private bool stacked;

        public bool IsStacked => stacked;

        public void Configure(float height, float spacing)
        {
            itemHeight = Mathf.Max(64f, height);
            itemSpacing = Mathf.Max(0f, spacing);
            SetDirty();
        }

        public void SetStacked(bool value)
        {
            if (stacked == value)
            {
                return;
            }

            stacked = value;
            SetDirty();
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            SetLayoutInputForAxis(padding.horizontal, padding.horizontal, -1f, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            var count = rectChildren.Count;
            var rows = stacked ? count : Mathf.Min(count, 1);
            var height = padding.vertical + rows * itemHeight + Mathf.Max(0, rows - 1) * itemSpacing;
            SetLayoutInputForAxis(height, height, -1f, 1);
        }

        public override void SetLayoutHorizontal()
        {
            var count = rectChildren.Count;
            if (count == 0)
            {
                return;
            }

            var width = rectTransform.rect.width - padding.horizontal;
            var itemWidth = stacked
                ? width
                : Mathf.Max(0f, (width - Mathf.Max(0, count - 1) * itemSpacing) / count);
            for (var index = 0; index < count; index++)
            {
                var position = padding.left + (stacked ? 0f : index * (itemWidth + itemSpacing));
                SetChildAlongAxis(rectChildren[index], 0, position, itemWidth);
            }
        }

        public override void SetLayoutVertical()
        {
            var count = rectChildren.Count;
            for (var index = 0; index < count; index++)
            {
                var position = padding.top + (stacked ? index * (itemHeight + itemSpacing) : 0f);
                var height = stacked
                    ? itemHeight
                    : Mathf.Max(itemHeight, rectTransform.rect.height - padding.vertical);
                SetChildAlongAxis(rectChildren[index], 1, position, height);
            }
        }
    }
}
