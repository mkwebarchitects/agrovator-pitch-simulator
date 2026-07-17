using System;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    public sealed class GuidedPitchResponsiveLayout : MonoBehaviour
    {
        private const float CompactWidthThreshold = 960f;
        private const float CompactAspectThreshold = 1.25f;
        private const float TargetHeight = 96f;

        [SerializeField] private GridLayoutGroup boardGrid;
        [SerializeField] private GridLayoutGroup sentenceCardGrid;
        [SerializeField] private ScrollRect compactScroll;

        private bool? appliedCompact;

        public bool IsCompact => appliedCompact == true;

        public void Configure(GridLayoutGroup board, GridLayoutGroup sentenceCards, ScrollRect scroll)
        {
            boardGrid = board ?? throw new ArgumentNullException(nameof(board));
            sentenceCardGrid = sentenceCards ?? throw new ArgumentNullException(nameof(sentenceCards));
            compactScroll = scroll ?? throw new ArgumentNullException(nameof(scroll));
            appliedCompact = null;
        }

        public bool Apply(Vector2 viewportSize)
        {
            ValidateReferences();
            if (viewportSize.x <= 0f || viewportSize.y <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(viewportSize), "Viewport dimensions must be positive.");
            }

            var compact = viewportSize.x < CompactWidthThreshold ||
                viewportSize.x / viewportSize.y < CompactAspectThreshold;
            if (appliedCompact == compact) return false;

            appliedCompact = compact;
            if (compact)
            {
                boardGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                boardGrid.constraintCount = 2;
                boardGrid.cellSize = new Vector2(Mathf.Max(260f, (viewportSize.x - 72f) / 2f), 128f);
                sentenceCardGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                sentenceCardGrid.constraintCount = 1;
                sentenceCardGrid.cellSize = new Vector2(Mathf.Max(280f, viewportSize.x - 64f), TargetHeight);
                compactScroll.enabled = true;
            }
            else
            {
                boardGrid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                boardGrid.constraintCount = 1;
                boardGrid.cellSize = new Vector2(224f, 140f);
                sentenceCardGrid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                sentenceCardGrid.constraintCount = 1;
                sentenceCardGrid.cellSize = new Vector2(288f, TargetHeight);
                compactScroll.enabled = false;
            }

            return true;
        }

        private void ValidateReferences()
        {
            if (boardGrid == null || sentenceCardGrid == null || compactScroll == null)
            {
                throw new InvalidOperationException("Responsive layout references are incomplete.");
            }
        }
    }
}
