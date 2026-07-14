(function (root, factory) {
  "use strict";

  const api = factory();
  if (typeof module === "object" && module.exports) {
    module.exports = api;
  } else {
    root.AgrovatorLayout = api;
  }
})(typeof globalThis !== "undefined" ? globalThis : this, function () {
  "use strict";

  function nonNegative(value) {
    return Number.isFinite(value) ? Math.max(0, value) : 0;
  }

  function calculateStageWidth(metrics) {
    const shellWidth = Math.max(1, nonNegative(metrics.shellWidth));
    const viewportHeight = Math.max(1, nonNegative(metrics.viewportHeight));
    const verticalChrome =
      nonNegative(metrics.bodyPaddingTop) +
      nonNegative(metrics.bodyPaddingBottom) +
      nonNegative(metrics.shellRowGap) +
      nonNegative(metrics.controlHeight) +
      nonNegative(metrics.controlMarginTop) +
      nonNegative(metrics.controlMarginBottom);
    const availableHeight = Math.max(1, viewportHeight - verticalChrome);
    return Math.max(1, Math.floor(Math.min(shellWidth, availableHeight * 16 / 9)));
  }

  return Object.freeze({ calculateStageWidth });
});
