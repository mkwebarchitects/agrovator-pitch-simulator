"use strict";

const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const test = require("node:test");

const projectRoot = path.resolve(__dirname, "..", "..");
const source = fs.readFileSync(path.join(projectRoot, "tools", "smoke-webgl.mjs"), "utf8");

function extractFunction(value, name, nextName) {
  const start = value.indexOf(`async function ${name}(`);
  const end = value.indexOf(`async function ${nextName}(`, start);
  assert.notEqual(start, -1, `missing ${name}`);
  assert.notEqual(end, -1, `missing boundary after ${name}`);
  return value.slice(start, end);
}

function assertOrderedLabels(value, labels) {
  let previous = -1;
  for (const label of labels) {
    const occurrences = value.split(`"${label}"`).length - 1;
    assert.equal(occurrences, 1, `expected one executable checkpoint labelled ${label}`);
    const position = value.indexOf(`"${label}"`);
    assert.ok(position > previous, `${label} is out of order`);
    previous = position;
  }
}

const requiredCheckpoints = [
  "mode", "learn", "build", "problem feedback", "evidence feedback", "solution feedback",
  "value feedback", "improve", "present", "cost follow-up", "results", "submission failure",
  "submission success", "retry", "fresh mode selection",
];

test("Chrome owns exactly one complete Primary keyboard guided path", () => {
  const primary = extractFunction(source, "runPrimaryKeyboardPath", "runSecondaryPointerPath");
  assert.match(primary, /definition\.name\s*!==\s*"chrome"/);
  assert.match(primary, /interaction\s*:\s*"keyboard"/);
  assert.match(primary, /mode\s*:\s*"Primary"/);
  assert.match(primary, /keyboardStableStep\(canvas,\s*"Tab",\s*options,\s*"problem developing option"\)/);
  assert.doesNotMatch(primary, /keyboardStableStep\(canvas,\s*"ArrowDown"/);
  assert.match(primary,
    /"revision options"\);[\s\S]*?"Tab",\s*options,\s*"revision focus present"\);[\s\S]*?"Tab",\s*options,\s*"revision focus clear option"\);[\s\S]*?"one revision"\);/);
  assertOrderedLabels(primary, requiredCheckpoints);
});

test("Edge owns exactly one complete Secondary pointer guided path", () => {
  const secondary = extractFunction(source, "runSecondaryPointerPath", "verifyCanvasContract");
  assert.match(secondary, /definition\.name\s*!==\s*"edge"/);
  assert.match(secondary, /interaction\s*:\s*"pointer"/);
  assert.match(secondary, /mode\s*:\s*"Secondary"/);
  assert.match(secondary,
    /pointerStableStep\(canvas,\s*0\.50,\s*0\.60,\s*options,\s*"briefing continue"\)/);
  assert.match(secondary,
    /pointerStableStep\(canvas,\s*0\.70,\s*0\.76,\s*options,\s*"learn"\)/);
  for (const label of [
    "build", "evidence build", "solution build", "value build", "improve", "present",
    "cost follow-up", "results",
  ]) {
    assert.match(secondary,
      new RegExp(`pointerStableStep\\(canvas,\\s*0\\.50,\\s*0\\.86,\\s*options,\\s*"${label}"\\)`),
      `${label} must hit the center of the persistent guided action bar`);
  }
  for (const [x, label] of [
    ["0.50", "problem feedback"],
    ["0.27", "evidence feedback"],
    ["0.27", "solution feedback"],
    ["0.27", "value feedback"],
    ["0.27", "one revision"],
    ["0.27", "cost feedback"],
  ]) {
    assert.match(secondary,
      new RegExp(`pointerStableStep\\(canvas,\\s*${x.replace(".", "\\.")},\\s*0\\.70,\\s*options,\\s*"${label}"\\)`),
      `${label} must hit the center of its sentence card`);
  }
  assertOrderedLabels(secondary, requiredCheckpoints);
});

test("smoke uses stable visual gates and captures the required evidence set", () => {
  assert.match(source, /async function waitForStableRegionChange/);
  const primary = extractFunction(source, "runPrimaryKeyboardPath", "runSecondaryPointerPath");
  const secondary = extractFunction(source, "runSecondaryPointerPath", "verifyCanvasContract");
  assert.doesNotMatch(primary + secondary, /setTimeout/);
  assert.doesNotMatch(primary + secondary, /locator\("#mode"\)\.selectOption/);
  assert.equal((primary.match(/await setHarnessMode\(/g) || []).length, 2);
  assert.equal((secondary.match(/await setHarnessMode\(/g) || []).length, 2);
  for (const filename of [
    "chrome-primary-mode.png", "chrome-primary-build.png", "chrome-primary-improve.png",
    "chrome-primary-present.png", "chrome-primary-results.png", "edge-secondary-mode.png",
    "edge-secondary-build.png", "edge-secondary-present.png", "edge-secondary-results.png",
    "chrome-mobile-compact.png", "edge-mobile-compact.png",
  ]) {
    assert.equal(source.split(`"${filename}"`).length - 1, 1, `missing unique capture ${filename}`);
  }
});

test("input visual gates establish canvas focus before taking their baseline", () => {
  for (const [name, next] of [
    ["keyboardStableStep", "pointerStableStep"],
    ["pointerStableStep", "captureCanvas"],
  ]) {
    const step = extractFunction(source, name, next);
    const focus = step.indexOf("await canvas.focus()");
    const baseline = step.indexOf("const before = await canvasHash(canvas)");
    assert.ok(focus >= 0 && baseline >= 0 && focus < baseline,
      `${name} focus tint must not satisfy a guided state-change gate`);
    if (name === "pointerStableStep") {
      const hover = step.indexOf("await canvas.hover(");
      assert.ok(hover > focus && hover < baseline,
        "pointer hover tint must settle before the click baseline");
    }
  }
});
