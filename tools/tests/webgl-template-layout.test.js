"use strict";

const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const test = require("node:test");
const {
  canonicalCode,
  extractFunction,
  extractPropertyFunction,
  findCalls,
  lastQuotedValue,
  propertyFunctionNames,
} = require("./javascript-contract.js");

const projectRoot = path.resolve(__dirname, "..", "..");
const templateRoot = path.join(projectRoot, "Assets", "WebGLTemplates", "Agrovator");

test("warning alert starts hidden and is revealed only by executable showBanner code", () => {
  const html = fs.readFileSync(path.join(templateRoot, "index.html"), "utf8");
  const showBanner = canonicalCode(extractFunction(html, "showBanner"));
  assert.match(html, /<div\s+id="unity-warning"[^>]*\shidden(?:\s|>)/);
  assert.match(showBanner, /warning\.hidden = false/);
});

// A bare `.catch(() => ...)` turns every startup failure into the same opaque
// banner with no recorded cause, which is what made a stale cached build
// indistinguishable from a corrupt download during a live incident. The cause
// goes on the DOM, not the console: this template must stay console-free so the
// smoke's zero-console-error gate keeps its meaning.
test("startup failure records the underlying cause instead of swallowing it", () => {
  const html = fs.readFileSync(path.join(templateRoot, "index.html"), "utf8");
  assert.doesNotMatch(html, /\.catch\(\(\)\s*=>/,
    "the loader failure path must receive the error, not discard it");
  assert.match(html, /\.catch\(\(error\)\s*=>/);
  assert.match(html, /failureReason\s*=\s*String\(/);
  assert.doesNotMatch(html.toLowerCase(), /console\./,
    "the template stays console-free");
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

test("template wires executable observer events through requestLayout to fitCanvas", () => {
  const html = fs.readFileSync(path.join(templateRoot, "index.html"), "utf8");
  const fitCanvas = extractFunction(html, "fitCanvas");
  const requestLayout = extractFunction(html, "requestLayout");
  assert.equal(findCalls(requestLayout, "window.requestAnimationFrame", { awaited: false }).length, 1);
  assert.equal(findCalls(requestLayout, "fitCanvas", { awaited: false }).length, 1);
  assert.equal(findCalls(fitCanvas, "AgrovatorLayout.calculateStageSize", { awaited: false }).length, 1);
  assert.match(canonicalCode(fitCanvas), /stage\.style\.width = nextWidth/);
  assert.match(canonicalCode(fitCanvas), /stage\.style\.height = nextHeight/);

  const windowEvents = findCalls(html, "window.addEventListener", { awaited: false });
  assert.deepEqual(windowEvents.map(call => lastQuotedValue(call)), ["resize", "orientationchange"]);
  assert.ok(windowEvents.every(call => /,\s*requestLayout\s*,/.test(call.text)));
  const visualEvents = findCalls(html, "window.visualViewport.addEventListener", { awaited: false });
  assert.deepEqual(visualEvents.map(call => lastQuotedValue(call)), ["resize"]);
  assert.match(canonicalCode(visualEvents[0].text),
    /^window\.visualViewport\.addEventListener\("resize", requestLayout,/);
  const observers = findCalls(html, "ResizeObserver", { awaited: false });
  assert.equal(observers.length, 1);
  assert.match(canonicalCode(observers[0].text), /^ResizeObserver\(requestLayout\)$/);
  assert.equal(findCalls(html, "requestLayout", { awaited: false }).length, 1,
    "observer callback references are not calls; exactly one startup call is required");
});

test("template sends the executable DPR result into Unity config", () => {
  const html = fs.readFileSync(path.join(templateRoot, "index.html"), "utf8");
  const scaleCalls = findCalls(html, "AgrovatorLayout.renderScale", { awaited: false });
  assert.equal(scaleCalls.length, 1);
  assert.match(canonicalCode(scaleCalls[0].text),
    /^AgrovatorLayout\.renderScale\(window\.devicePixelRatio \|\| 1\)$/);
  const code = canonicalCode(html);
  const scaleAssignment = code.indexOf("const renderScale = AgrovatorLayout.renderScale(window.devicePixelRatio || 1)");
  const configAssignment = code.indexOf("config.devicePixelRatio = renderScale");
  assert.ok(scaleAssignment >= 0 && configAssignment > scaleAssignment,
    "computed DPR must flow into config after config creation");
});

test("template keeps variable dimensions and cannot become mobile min-content overflow", () => {
  const css = fs.readFileSync(path.join(templateRoot, "TemplateData", "style.css"), "utf8");
  assert.match(css, /#unity-canvas\s*\{[^}]*width:\s*100%;[^}]*height:\s*100%;/s);
  assert.doesNotMatch(css, /#unity-stage\s*\{[^}]*aspect-ratio\s*:/s);
  assert.match(css, /#unity-shell\s*\{[^}]*min-width:\s*0;/s);
  assert.match(css, /#unity-stage\s*\{[^}]*min-width:\s*0;[^}]*max-width:\s*100%;/s);
});

test("viewport bridge exposes exactly three unique display-only executable exports", () => {
  const builder = fs.readFileSync(path.join(projectRoot, "Assets", "Editor", "GuidedPitchSceneBuilder.cs"), "utf8");
  const bridge = fs.readFileSync(path.join(projectRoot, "Assets", "Plugins", "WebGL", "PitchSimulatorBridge.jslib"), "utf8");
  assert.match(builder, /new GameObject\("Environment Frame"[\s\S]*?AspectRatioFitter/);

  const expected = [
    "PitchSimulatorViewportWidth",
    "PitchSimulatorViewportHeight",
    "PitchSimulatorDevicePixelRatioTimes100",
  ];
  assert.deepEqual(propertyFunctionNames(bridge, "PitchSimulatorViewport"), expected.slice(0, 2));
  assert.deepEqual(propertyFunctionNames(bridge, "PitchSimulatorDevicePixelRatio"), expected.slice(2));

  const width = canonicalCode(extractPropertyFunction(bridge, expected[0]));
  const height = canonicalCode(extractPropertyFunction(bridge, expected[1]));
  const dpr = canonicalCode(extractPropertyFunction(bridge, expected[2]));
  assert.match(width,
    /^PitchSimulatorViewportWidth: function \(\) \{ var canvas = document\.getElementById\("unity-canvas"\); return Math\.max\(1, Math\.round\(canvas \? canvas\.clientWidth : window\.innerWidth\)\); \}$/);
  assert.match(height,
    /^PitchSimulatorViewportHeight: function \(\) \{ var canvas = document\.getElementById\("unity-canvas"\); return Math\.max\(1, Math\.round\(canvas \? canvas\.clientHeight : window\.innerHeight\)\); \}$/);
  assert.match(dpr,
    /^PitchSimulatorDevicePixelRatioTimes100: function \(\) \{ return Math\.max\(100, Math\.round\(\(window\.devicePixelRatio \|\| 1\) \* 100\)\); \}$/);
  for (const body of [width, height, dpr]) {
    assert.doesNotMatch(body, /postMessage|SendMessage|launchConfigJson|completion/i);
  }
});

test("contract parser ignores observer and viewport exports hidden in comments or strings", () => {
  const fixture = `
    // window.addEventListener("resize", requestLayout);
    const dead = 'PitchSimulatorViewportFake: function () {}';
    /* PitchSimulatorViewportComment: function () {} */
    PitchSimulatorViewportWidth: function () { return 1; }
  `;
  assert.equal(findCalls(fixture, "window.addEventListener", { awaited: false }).length, 0);
  assert.deepEqual(propertyFunctionNames(fixture, "PitchSimulatorViewport"), ["PitchSimulatorViewportWidth"]);
});
