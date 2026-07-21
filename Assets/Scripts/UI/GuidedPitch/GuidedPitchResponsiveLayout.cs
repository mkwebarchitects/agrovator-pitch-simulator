using System;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    public sealed class GuidedPitchResponsiveLayout : MonoBehaviour
    {
        // Shared with RotateToPlayRule so the stage that compacts is exactly the
        // stage that can be gated for orientation.
        private const float CompactWidthThreshold = RotateToPlayRule.HandheldWidthThreshold;
        private const float CompactAspectThreshold = 1.25f;
        private const float TargetHeight = 96f;

        [SerializeField] private Canvas viewportCanvas;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private GridLayoutGroup boardGrid;
        [SerializeField] private GridLayoutGroup sentenceCardGrid;
        [SerializeField] private ScrollRect compactScroll;
        [SerializeField] private GuidedPitchFlowLayout modeSelectionControls;
        [SerializeField] private GuidedPitchFlowLayout improveActionControls;
        [SerializeField] private GuidedPitchFlowLayout primaryActionControls;
        [SerializeField] private AspectRatioFitter environmentFitter;

        private bool? appliedCompact;
        private Vector2? appliedViewportSize;
        private RectTransform physicalViewportSource;
        private Func<WebViewportMetrics> viewportMetricsReader = WebViewportMetrics.Read;
        private WebViewportMetrics? observedViewportMetrics;

        public bool IsCompact => appliedCompact == true;

        public void Configure(GridLayoutGroup board, GridLayoutGroup sentenceCards, ScrollRect scroll)
        {
            boardGrid = board ?? throw new ArgumentNullException(nameof(board));
            sentenceCardGrid = sentenceCards ?? throw new ArgumentNullException(nameof(sentenceCards));
            compactScroll = scroll ?? throw new ArgumentNullException(nameof(scroll));
            appliedCompact = null;
            appliedViewportSize = null;
            observedViewportMetrics = null;
        }

        public void Configure(
            Canvas liveCanvas,
            GridLayoutGroup board,
            GridLayoutGroup sentenceCards,
            ScrollRect scroll,
            GuidedPitchFlowLayout modes,
            GuidedPitchFlowLayout improve,
            GuidedPitchFlowLayout primary,
            AspectRatioFitter environment)
        {
            Configure(board, sentenceCards, scroll);
            viewportCanvas = liveCanvas ?? throw new ArgumentNullException(nameof(liveCanvas));
            viewport = liveCanvas.GetComponent<RectTransform>();
            modeSelectionControls = modes ?? throw new ArgumentNullException(nameof(modes));
            improveActionControls = improve ?? throw new ArgumentNullException(nameof(improve));
            primaryActionControls = primary ?? throw new ArgumentNullException(nameof(primary));
            environmentFitter = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        /// <summary>
        /// Supplies a deterministic physical-size source for embedded viewport hosts and test rigs.
        /// Generated ScreenSpace canvases normally use <see cref="Canvas.pixelRect"/> directly.
        /// </summary>
        public void ConfigurePhysicalViewportSource(RectTransform source)
        {
            physicalViewportSource = source ?? throw new ArgumentNullException(nameof(source));
            appliedViewportSize = null;
            observedViewportMetrics = null;
        }

        /// <summary>
        /// Supplies viewport display metrics for embedded hosts and deterministic tests.
        /// The reader is sampled during the existing layout lifecycle and only a changed
        /// CSS width, CSS height, or DPR can trigger another layout application.
        /// </summary>
        public void ConfigureViewportMetricsReader(Func<WebViewportMetrics> reader)
        {
            viewportMetricsReader = reader ?? throw new ArgumentNullException(nameof(reader));
            observedViewportMetrics = null;
        }

        private void OnEnable()
        {
            ApplyCurrentViewport();
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyCurrentViewport();
        }

        private void Update()
        {
            ApplyCurrentViewport();
        }

        /// <summary>
        /// Applies the wide or compact layout rules for the viewport without allocating objects.
        /// Returns true only when the layout mode (wide/compact) changed. Width-dependent cell
        /// sizes are still recomputed whenever the viewport size changes within the same mode,
        /// and repeating identical inputs is a no-op.
        /// </summary>
        public bool Apply(Vector2 viewportSize)
        {
            ValidateApplyReferences();
            if (viewportSize.x <= 0f || viewportSize.y <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(viewportSize), "Viewport dimensions must be positive.");
            }

            // A 960-wide stage is the first constrained fixture: at or below the
            // threshold the guided layout must already stack and scroll.
            var compact = viewportSize.x <= CompactWidthThreshold ||
                viewportSize.x / viewportSize.y < CompactAspectThreshold;
            var modeChanged = appliedCompact != compact;
            if (!modeChanged && appliedViewportSize == viewportSize) return false;

            appliedCompact = compact;
            appliedViewportSize = viewportSize;
            if (compact)
            {
                boardGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                boardGrid.constraintCount = 2;
                boardGrid.cellSize = new Vector2(Mathf.Max(260f, (viewportSize.x - 72f) / 2f), 128f);
                sentenceCardGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                sentenceCardGrid.constraintCount = 1;
                sentenceCardGrid.cellSize = new Vector2(Mathf.Max(280f, viewportSize.x - 64f), TargetHeight);
                compactScroll.enabled = true;
                // Filling a portrait stage would crop over half the room's width
                // away, taking the windows, shelving and plant beds with it, so a
                // compact stage letterboxes and keeps the composition intact.
                SetEnvironmentFill(false);
                SetControlLayout(modeSelectionControls, true);
                SetControlLayout(improveActionControls, true);
                SetControlLayout(primaryActionControls, true);
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
                // A wide stage is close enough to 16:9 that filling it costs little
                // crop and removes the dead bands the room used to sit between.
                SetEnvironmentFill(true);
                SetControlLayout(modeSelectionControls, false);
                SetControlLayout(improveActionControls, false);
                SetControlLayout(primaryActionControls, false);
            }

            return modeChanged;
        }

        public bool ValidateContract(out string reason)
        {
            if (viewportCanvas == null || viewport == null || boardGrid == null ||
                sentenceCardGrid == null || compactScroll == null ||
                modeSelectionControls == null || improveActionControls == null ||
                primaryActionControls == null || environmentFitter == null)
            {
                reason = "responsive_layout_references_missing";
                return false;
            }
            if (viewportCanvas.GetComponent<RectTransform>() != viewport)
            {
                reason = "responsive_viewport_mismatch";
                return false;
            }
            if (modeSelectionControls == improveActionControls ||
                modeSelectionControls == primaryActionControls ||
                improveActionControls == primaryActionControls)
            {
                reason = "responsive_control_groups_not_distinct";
                return false;
            }

            reason = null;
            return true;
        }

        private void ApplyCurrentViewport()
        {
            if (!isActiveAndEnabled || boardGrid == null || sentenceCardGrid == null ||
                compactScroll == null)
            {
                return;
            }

            var metrics = GetViewportMetrics();
            if (observedViewportMetrics.HasValue && observedViewportMetrics.Value == metrics)
            {
                return;
            }

            observedViewportMetrics = metrics;
            // A DPR-only change keeps the CSS size but still represents a new WebGL
            // backing-store contract, so force one application for the new metrics.
            appliedViewportSize = null;
            Apply(metrics.CssSize);
        }

        private WebViewportMetrics GetViewportMetrics()
        {
            if (physicalViewportSource != null)
            {
                var size = physicalViewportSource.rect.size;
                return new WebViewportMetrics(
                    Mathf.Max(1, Mathf.RoundToInt(size.x)),
                    Mathf.Max(1, Mathf.RoundToInt(size.y)),
                    1f);
            }
            if (viewportCanvas != null && viewportCanvas.renderMode == RenderMode.WorldSpace)
            {
                var size = viewport != null ? viewport.rect.size : Vector2.one;
                return new WebViewportMetrics(
                    Mathf.Max(1, Mathf.RoundToInt(size.x)),
                    Mathf.Max(1, Mathf.RoundToInt(size.y)),
                    1f);
            }
            return viewportMetricsReader();
        }

        private void SetEnvironmentFill(bool fill)
        {
            if (environmentFitter == null)
            {
                return;
            }
            environmentFitter.aspectMode = fill
                ? AspectRatioFitter.AspectMode.EnvelopeParent
                : AspectRatioFitter.AspectMode.FitInParent;
        }

        private static void SetControlLayout(GuidedPitchFlowLayout layout, bool compact)
        {
            if (layout == null)
            {
                return;
            }
            layout.SetStacked(compact);
        }

        private void ValidateApplyReferences()
        {
            if (boardGrid == null || sentenceCardGrid == null || compactScroll == null)
            {
                throw new InvalidOperationException("Responsive layout references are incomplete.");
            }
        }
    }
}
