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

  function calculateStageSize(metrics) {
    const shellWidth = Math.max(1, nonNegative(metrics.shellWidth));
    const viewportHeight = Math.max(1, nonNegative(metrics.viewportHeight));
    const verticalChrome = nonNegative(metrics.verticalChrome);
    const availableHeight = Math.max(1, viewportHeight - verticalChrome);
    return Object.freeze({
      width: Math.floor(shellWidth),
      height: Math.floor(availableHeight),
    });
  }

  function renderScale(devicePixelRatio) {
    const ratio = Number.isFinite(devicePixelRatio) ? devicePixelRatio : 1;
    return Math.min(2, Math.max(1, ratio));
  }

  return Object.freeze({ calculateStageSize, renderScale });
});
