"use strict";

const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const test = require("node:test");

const projectRoot = path.resolve(__dirname, "..", "..");
const templateRoot = path.join(projectRoot, "Assets", "WebGLTemplates", "Agrovator");

test("warning alert starts hidden and is revealed only by showBanner", () => {
  const html = fs.readFileSync(path.join(templateRoot, "index.html"), "utf8");

  assert.match(html, /<div\s+id="unity-warning"[^>]*\shidden(?:\s|>)/);
  assert.match(html, /function showBanner[\s\S]*warning\.hidden = false/);
});

test("layout calculator uses the full available portrait or landscape stage", () => {
  const { calculateStageSize } = require(path.join(templateRoot, "TemplateData", "layout.js"));

  assert.deepEqual(calculateStageSize({ shellWidth: 1280, viewportHeight: 798, verticalChrome: 78 }),
    { width: 1280, height: 720 });
  assert.deepEqual(calculateStageSize({ shellWidth: 382, viewportHeight: 844, verticalChrome: 54 }),
    { width: 382, height: 790 });
});

test("render scale accounts for DPR while capping WebGL memory growth", () => {
  const { renderScale } = require(path.join(templateRoot, "TemplateData", "layout.js"));

  assert.equal(renderScale(1), 1);
  assert.equal(renderScale(1.5), 1.5);
  assert.equal(renderScale(3), 2);
});

test("template applies variable stage dimensions, capped DPR and coalesced viewport observers", () => {
  const html = fs.readFileSync(path.join(templateRoot, "index.html"), "utf8");
  const css = fs.readFileSync(path.join(templateRoot, "TemplateData", "style.css"), "utf8");

  for (const contract of [
    "getComputedStyle(document.body)",
    "getComputedStyle(shell)",
    "getComputedStyle(fullscreen)",
    "bodyPaddingTop",
    "bodyPaddingBottom",
    "shellRowGap",
    "controlMarginTop",
    "controlMarginBottom",
    "window.visualViewport",
    "requestAnimationFrame",
    "AgrovatorLayout.calculateStageSize",
    "AgrovatorLayout.renderScale(window.devicePixelRatio || 1)",
    "config.devicePixelRatio = renderScale",
    "stage.style.width !== nextWidth",
    "stage.style.height !== nextHeight",
    "new ResizeObserver(requestLayout)",
  ]) {
    assert.ok(html.includes(contract), `missing layout contract: ${contract}`);
  }

  assert.match(css, /#unity-canvas\s*\{[^}]*width:\s*100%;[^}]*height:\s*100%;/s);
  assert.doesNotMatch(css, /#unity-stage\s*\{[^}]*aspect-ratio\s*:/s);
});

test("inline desktop stage dimensions cannot become mobile min-content overflow", () => {
  const css = fs.readFileSync(path.join(templateRoot, "TemplateData", "style.css"), "utf8");

  assert.match(css, /#unity-shell\s*\{[^}]*min-width:\s*0;/s);
  assert.match(css, /#unity-stage\s*\{[^}]*min-width:\s*0;[^}]*max-width:\s*100%;/s);
});

test("Unity owns the 16:9 environment frame and viewport bridge exposes display metrics only", () => {
  const builder = fs.readFileSync(path.join(projectRoot, "Assets", "Editor", "GuidedPitchSceneBuilder.cs"), "utf8");
  const bridge = fs.readFileSync(path.join(projectRoot, "Assets", "Plugins", "WebGL", "PitchSimulatorBridge.jslib"), "utf8");

  assert.match(builder, /new GameObject\("Environment Frame"[\s\S]*?AspectRatioFitter/);
  for (const name of [
    "PitchSimulatorViewportWidth",
    "PitchSimulatorViewportHeight",
    "PitchSimulatorDevicePixelRatioTimes100",
  ]) {
    assert.match(bridge, new RegExp(`${name}\\s*:\\s*function\\s*\\(`));
  }
  const viewportFunctions = bridge.slice(bridge.indexOf("PitchSimulatorViewportWidth"));
  assert.doesNotMatch(viewportFunctions, /postMessage|SendMessage|launchConfigJson|completion/i);
});
