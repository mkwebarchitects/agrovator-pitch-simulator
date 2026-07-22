"use strict";

const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const test = require("node:test");
const {
  canonicalCode,
  extractFunction,
  findCalls,
  lastQuotedValue,
  maskNonCode,
  quotedValues,
} = require("./javascript-contract.js");

const projectRoot = path.resolve(__dirname, "..", "..");
const source = fs.readFileSync(path.join(projectRoot, "tools", "smoke-webgl.mjs"), "utf8");

const primaryLabels = [
  "title start", "briefing continue", "learn", "build", "problem developing option",
  "problem feedback", "evidence build", "evidence feedback", "solution build",
  "solution feedback", "value build", "value feedback", "improve", "focus present", "present",
  "cost follow-up", "cost feedback", "results", "submission failure", "submission success",
  "retry", "fresh mode selection",
];
const secondaryLabels = [
  "title start", "briefing continue", "learn", "build", "problem feedback", "evidence build",
  "evidence feedback", "solution build", "solution feedback", "value build", "value feedback",
  "improve", "present", "cost follow-up", "cost feedback",
  "results", "submission failure", "submission success", "retry", "fresh mode selection",
];
const primaryGates = primaryLabels.map(label =>
  ["problem developing option", "focus present"].includes(label) ? "focus"
    : label === "title start" ? "content"
      : label.startsWith("submission ") ? "controls"
        : "transition");
const secondaryGates = secondaryLabels.map(label =>
  label === "title start" ? "content"
    : label.startsWith("submission ") ? "controls" : "transition");
const secondaryCoordinates = [
  ["0.50", "0.50"], ["0.50", "0.60"], ["0.70", "0.76"], ["0.50", "0.86"],
  ["0.50", "0.70"], ["0.50", "0.86"], ["0.27", "0.70"], ["0.50", "0.86"],
  ["0.27", "0.70"], ["0.50", "0.86"], ["0.27", "0.70"], ["0.50", "0.86"],
  ["0.50", "0.86"], ["0.50", "0.86"],
  ["0.27", "0.70"], ["0.50", "0.86"], ["0.36", "0.86"], ["0.36", "0.86"],
  ["0.64", "0.86"], ["0.50", "0.60"],
];

function assertCallLabels(value, callee, expected) {
  const calls = findCalls(value, callee);
  assert.deepEqual(calls.map(lastQuotedValue), expected,
    `${callee} must have the exact executable label sequence`);
  return calls;
}

function gateNames(calls) {
  return calls.map(call => /GuidedGate\.(transition|focus|content|controls)/.exec(call.text)?.[1] || null);
}

function assertOrdered(...positions) {
  for (let index = 1; index < positions.length; index += 1) {
    assert.ok(positions[index - 1] < positions[index], "executable calls are out of order");
  }
}

function assertReachableMissingConfigRecovery(value) {
  const runBrowser = extractFunction(value, "runBrowser");
  const calls = findCalls(runBrowser, "verifyMissingConfigRecovery");
  assert.equal(calls.length, 1,
    "each covered guided browser must execute missing-configuration recovery exactly once");
  assert.match(canonicalCode(calls[0].text),
    /^await verifyMissingConfigRecovery\(page, options\)$/);
  assert.match(canonicalCode(runBrowser),
    /result\.modes\.failure = true; result\.modes\.success = true; result\.modes\.missingConfig = await verifyMissingConfigRecovery\(page, options\); result\.screenshot =/,
    "missing-configuration recovery must be a reachable recorded step in the covered path");
}

function assertSettledKeyboardRecoveredStart(value) {
  const recovery = extractFunction(value, "verifyMissingConfigRecovery");
  const recoveryCode = canonicalCode(recovery);
  assert.match(recoveryCode,
    /await keyboardStableStep\(page, canvas, "Enter", GuidedGate\.content, options, "recovered title start"\)/,
    "the recovered Start must reuse the settled keyboard step that composes, presses the default-focused Start, and gates on Briefing content");
  assert.doesNotMatch(recoveryCode, /canvas\.click\(/,
    "coordinate presses on the recovered Title are forbidden; a mid-transition press can hit Settings");
  assert.doesNotMatch(recoveryCode, /pressCanvasUntilContentChange\(/,
    "the retired coordinate-retry helper must not return");
  assert.doesNotMatch(recoveryCode, /await waitForCanvasChange\(canvas, recoveredBefore/,
    "the whole-canvas change gate alone cannot prove the recovered Start was observed");
}

function assertHiddenHarnessRecovery(value) {
  const recovery = extractFunction(value, "verifyMissingConfigRecovery");
  assert.deepEqual(findCalls(recovery, "setHarnessMode").map(call => canonicalCode(call.text)), [
    'await setHarnessMode(page, "missing-config")',
    'await setHarnessMode(page, "success")',
  ]);
  assert.doesNotMatch(canonicalCode(recovery), /\.selectOption\(/);
  assert.match(canonicalCode(recovery),
    /await page\.locator\("#resend"\)\.dispatchEvent\("click"\)/,
    "recovery must dispatch resend without requiring hidden harness controls to be visible");
}

test("call extraction ignores comments and dead strings while retaining executable duplicates", () => {
  const fixture = `
    // await keyboardStableStep(page, canvas, "Enter", options, "comment");
    const dead = 'await keyboardStableStep(page, canvas, "Enter", options, "dead")';
    /* await keyboardStableStep(page, canvas, "Enter", options, "block"); */
    await keyboardStableStep(page, canvas, "Enter", options, "executable");
    if (false) await keyboardStableStep(page, canvas, "Enter", options, "executable duplicate");
  `;
  assert.deepEqual(findCalls(fixture, "keyboardStableStep").map(lastQuotedValue),
    ["executable", "executable duplicate"]);
});

test("Chrome owns one exact Primary keyboard path with no pointer actions", () => {
  const primary = extractFunction(source, "runPrimaryKeyboardPath");
  assert.match(canonicalCode(primary), /if \(definition\.name !== "chrome"\) return null/);
  assert.match(canonicalCode(primary), /interaction: "keyboard", mode: "Primary"/);
  assert.equal(findCalls(primary, "pointerStableStep").length, 0);
  assert.equal(findCalls(primary, "canvas.click").length, 0);
  assert.equal(findCalls(primary, "canvas.hover").length, 0);
  const calls = assertCallLabels(primary, "keyboardStableStep", primaryLabels);
  assert.deepEqual(gateNames(calls), primaryGates);
  const expectedKeys = primaryLabels.map(label =>
    label === "problem developing option" ? "Tab"
      : label === "focus present" ? "Shift+Tab" : "Enter");
  assert.deepEqual(calls.map(call => quotedValues(call.text).at(-2)), expectedKeys);
  assert.deepEqual(calls.map((call, index) => canonicalCode(call.text)),
    primaryLabels.map((label, index) =>
      `await keyboardStableStep(page, canvas, "${expectedKeys[index]}", ` +
      `GuidedGate.${primaryGates[index]}, options, "${label}")`));
  assert.match(canonicalCode(calls[4].text),
    /^await keyboardStableStep\(page, canvas, "Tab", GuidedGate\.focus, options, "problem developing option"\)$/);
  assert.equal(calls.some(call => /"ArrowDown"/.test(call.text)), false);
  for (const call of calls.filter(call => lastQuotedValue(call) === "focus present")) {
    assert.match(canonicalCode(call.text),
      /^await keyboardStableStep\(page, canvas, "Shift\+Tab", GuidedGate\.focus, options,/);
  }
  assert.deepEqual(findCalls(primary, "setHarnessMode").map(call => canonicalCode(call.text)), [
    'await setHarnessMode(page, "failure")', 'await setHarnessMode(page, "success")',
  ]);
});

test("Edge owns one exact Secondary pointer path with no keyboard actions", () => {
  const secondary = extractFunction(source, "runSecondaryPointerPath");
  assert.match(canonicalCode(secondary), /if \(definition\.name !== "edge"\) return null/);
  assert.match(canonicalCode(secondary), /interaction: "pointer", mode: "Secondary"/);
  assert.equal(findCalls(secondary, "keyboardStableStep").length, 0);
  assert.equal(findCalls(secondary, "canvas.press").length, 0);
  const calls = assertCallLabels(secondary, "pointerStableStep", secondaryLabels);
  assert.deepEqual(gateNames(calls), secondaryGates);
  assert.deepEqual(calls.map((call, index) => canonicalCode(call.text)),
    secondaryLabels.map((label, index) => {
      const [x, y] = secondaryCoordinates[index];
      return `await pointerStableStep(page, canvas, ${x}, ${y}, ` +
        `GuidedGate.${secondaryGates[index]}, options, "${label}")`;
    }));
  assert.deepEqual(findCalls(secondary, "setHarnessMode").map(call => canonicalCode(call.text)), [
    'await setHarnessMode(page, "failure")', 'await setHarnessMode(page, "success")',
  ]);
});

test("covered guided browsers execute and record reachable missing-configuration recovery", () => {
  assertReachableMissingConfigRecovery(source);

  const assignment = "result.modes.missingConfig = await verifyMissingConfigRecovery(page, options);";
  assert.throws(() => assertReachableMissingConfigRecovery(
    source.replace(assignment, `if (false) ${assignment}`)), /reachable recorded step/);
  assert.throws(() => assertReachableMissingConfigRecovery(
    source.replace(assignment, "await verifyMissingConfigRecovery(page, options);")),
  /reachable recorded step/);
});

test("missing-configuration recovery operates the hidden fullscreen harness controls", () => {
  assertHiddenHarnessRecovery(source);

  const dispatch = 'await page.locator("#resend").dispatchEvent("click")';
  const visibleClickMutation = source.replace(dispatch, 'await page.locator("#resend").click()');
  assert.throws(() => assertHiddenHarnessRecovery(visibleClickMutation),
    /without requiring hidden harness controls/);

  const hiddenModeSwitch = 'await setHarnessMode(page, "missing-config")';
  const visibleSelectMutation = source.replace(hiddenModeSwitch,
    'await page.locator("#mode").selectOption("missing-config")');
  assert.throws(() => assertHiddenHarnessRecovery(visibleSelectMutation),
    undefined,
    "a visible selectOption mode switch must fail the hidden-control contract");
});

test("recovered Title Start uses the settled keyboard step, never coordinate presses", () => {
  assertSettledKeyboardRecoveredStart(source);

  const settledStep =
    /await keyboardStableStep\(page, canvas, "Enter", GuidedGate\.content, options, "recovered title start"\);/;
  const coordinatePress = 'const recoveredBefore = await canvasHash(canvas);\n' +
    '  await canvas.click({ position: { x: 320, y: 240 }, delay: 120 });\n' +
    '  await waitForCanvasChange(canvas, recoveredBefore, options.timeoutMs, ' +
    '"recovered Title pointer Start");';
  const coordinateMutation = source.replace(settledStep, coordinatePress);
  assert.notEqual(coordinateMutation, source, "the coordinate-press mutation must apply to the source");
  assert.throws(() => assertSettledKeyboardRecoveredStart(coordinateMutation),
    undefined,
    "a coordinate press behind the whole-canvas gate must fail the contract");
});

test("guided actions require stable localized content and control changes after focus and hover", () => {
  const gate = extractFunction(source, "waitForStableRegionChange");
  const gateCode = maskNonCode(gate);
  assert.match(gateCode, /required\.content/);
  assert.match(gateCode, /required\.controls/);
  assert.match(gateCode, /stableSamples\s*>=\s*3/);
  assert.match(canonicalCode(source),
    /transition: Object\.freeze\(\{ content: true, controls: true \}\), focus: Object\.freeze\(\{ content: false, controls: true \}\), content: Object\.freeze\(\{ content: true, controls: false \}\), controls: Object\.freeze\(\{ content: false, controls: true \}\)/);

  for (const [name, action] of [["keyboardStableStep", "canvas.press"], ["pointerStableStep", "canvas.click"]]) {
    const step = extractFunction(source, name);
    const focus = findCalls(step, "canvas.focus")[0];
    const baseline = findCalls(step, "regionHashes")[0];
    const actionCall = findCalls(step, action)[0];
    const changed = findCalls(step, "waitForStableRegionChange")[0];
    assert.ok(focus && baseline && actionCall && changed, `${name} must execute focus, baseline, action and gate`);
    assertOrdered(focus.start, baseline.start, actionCall.start, changed.start);
    assert.match(canonicalCode(changed.text),
      /^await waitForStableRegionChange\(page, canvas, before, required, options\.timeoutMs, label\)$/);
    if (name === "pointerStableStep") {
      const hover = findCalls(step, "canvas.hover")[0];
      assert.ok(hover && focus.start < hover.start && hover.start < baseline.start,
        "pointer hover must settle before the localized baseline");
    }
  }
});

test("viewport verification settles changed compact Unity content and controls before capture", () => {
  const verify = extractFunction(source, "verifyCanvasContract");
  const before = findCalls(verify, "regionHashes")[0];
  const resize = findCalls(verify, "useFullscreenHarness")[0];
  const settle = findCalls(verify, "waitForStableRegionChange")[0];
  assert.ok(before && resize && settle);
  assertOrdered(before.start, resize.start, settle.start);
  assert.match(canonicalCode(settle.text),
    /^await waitForStableRegionChange\(page, canvas, beforeComposition, \{ content: viewportChanged, controls: viewportChanged \}, 10_000, "responsive Unity composition"\)$/);

  // Both orientations are required. Checking portrait alone would prove the gate
  // while leaving the compact layout it gates - stacked board, single-column
  // cards, enabled scroll - unrendered anywhere in the suite, which is exactly
  // what happened when the rotate prompt first shipped.
  for (const pathName of ["runPrimaryKeyboardPath", "runSecondaryPointerPath"]) {
    const guidedPath = extractFunction(source, pathName);
    const portraitVerify = findCalls(guidedPath, "verifyRotatePrompt")
      .find(call => /width:\s*390,\s*height:\s*844/.test(call.text));
    const portraitCapture = findCalls(guidedPath, "captureCanvas")
      .find(call => /mobile-portrait\.png/.test(call.text));
    assert.ok(portraitVerify && portraitCapture && portraitVerify.end < portraitCapture.start,
      `${pathName} must prove the rotate prompt covers a portrait handheld`);

    const mobileVerify = findCalls(guidedPath, "verifyPlayableStage")
      .find(call => /width:\s*844,\s*height:\s*390/.test(call.text));
    const mobileCapture = findCalls(guidedPath, "captureCanvas")
      .find(call => /mobile-compact\.png/.test(call.text));
    assert.ok(mobileVerify && mobileCapture && mobileVerify.end < mobileCapture.start,
      `${pathName} compact capture must follow Unity composition settlement`);
    assert.ok(portraitCapture.start < mobileCapture.start,
      `${pathName} must check portrait before turning the stage sideways`);
  }
});

// The two verifiers must stay opposites. If either stopped asserting, the suite
// would keep passing while proving nothing about which state is on screen.
test("orientation verifiers assert opposite canvas uniformity", () => {
  const prompt = extractFunction(source, "verifyRotatePrompt");
  const playable = extractFunction(source, "verifyPlayableStage");
  assert.match(canonicalCode(prompt), /if \(new Set\(corners\)\.size !== 1\)/,
    "verifyRotatePrompt must require a uniform canvas");
  assert.match(canonicalCode(playable), /if \(new Set\(corners\)\.size === 1\)/,
    "verifyPlayableStage must reject a uniform canvas");
  for (const body of [prompt, playable]) {
    assert.match(canonicalCode(body), /throw new Error\(/,
      "each orientation verifier must fail loudly rather than return a flag");
  }
});

test("guided paths own exactly the required executable evidence captures", () => {
  const primary = extractFunction(source, "runPrimaryKeyboardPath");
  const secondary = extractFunction(source, "runSecondaryPointerPath");
  assert.deepEqual(findCalls(primary, "captureCanvas").map(lastQuotedValue), [
    "chrome-primary-mode.png", "chrome-mobile-portrait.png", "chrome-mobile-compact.png",
    "chrome-primary-build.png",
    "chrome-primary-improve.png", "chrome-primary-present.png", "chrome-primary-results.png",
  ]);
  assert.deepEqual(findCalls(secondary, "captureCanvas").map(lastQuotedValue), [
    "edge-secondary-mode.png", "edge-mobile-portrait.png", "edge-mobile-compact.png",
    "edge-secondary-build.png",
    "edge-secondary-present.png", "edge-secondary-results.png",
  ]);
  assert.equal(findCalls(source, "captureCanvas").length, 14);

  // Reaction evidence sits at feedback moments, because every capture above
  // lands on a question where Judge Aya rests. It must go through the verifying
  // helper: a plain screenshot cannot fail when the portrait did not react, so
  // a slow machine would silently bank four pictures of the resting face.
  assert.deepEqual(findCalls(primary, "captureJudgeReaction").map(lastQuotedValue), [
    "chrome-primary-reaction-developing.png", "chrome-primary-reaction-clear.png",
  ]);
  assert.deepEqual(findCalls(secondary, "captureJudgeReaction").map(lastQuotedValue), [
    "edge-secondary-reaction-first.png", "edge-secondary-reaction-second.png",
  ]);
  for (const guidedPath of [primary, secondary]) {
    const baseline = findCalls(guidedPath, "judgePortraitHash");
    const reactions = findCalls(guidedPath, "captureJudgeReaction");
    assert.equal(baseline.length, 1, "each path takes one resting-portrait baseline");
    assert.ok(baseline[0].end < reactions[0].start,
      "the resting baseline must be sampled before the first reaction");
  }
  const helper = canonicalCode(extractFunction(source, "captureJudgeReaction"));
  // The resting check is the load-bearing proof: it fails whenever Aya has
  // settled back to her resting face, so a slow machine cannot silently bank a
  // picture of the neutral portrait. A reaction-vs-reaction check is deliberately
  // absent because randomized option order means two consecutive selections can
  // legitimately share a mastery tier, and therefore an identical reaction face.
  assert.match(helper, /if \(reacting === previous\.resting\) \{ throw new Error/,
    "captureJudgeReaction must throw when Aya has settled back to her resting face");
  assert.doesNotMatch(helper, /reacting === previous\.reaction/,
    "captureJudgeReaction must not compare two reactions now that option order is randomized");
  assert.equal(findCalls(primary, "setTimeout", { awaited: false }).length, 0);
  assert.equal(findCalls(secondary, "setTimeout", { awaited: false }).length, 0);
  assert.equal(findCalls(primary, "page.locator", { awaited: false }).length, 0);
  assert.equal(findCalls(secondary, "page.locator", { awaited: false }).length, 0);

  for (const [guidedPath, actionName, milestones] of [
    [primary, "keyboardStableStep", [
      ["briefing continue", "chrome-primary-mode.png", "learn"],
      ["build", "chrome-primary-build.png", "problem developing option"],
      ["improve", "chrome-primary-improve.png", "focus present"],
      ["present", "chrome-primary-present.png", "cost follow-up"],
      ["results", "chrome-primary-results.png", "submission failure"],
    ]],
    [secondary, "pointerStableStep", [
      ["briefing continue", "edge-secondary-mode.png", "learn"],
      ["build", "edge-secondary-build.png", "problem feedback"],
      ["present", "edge-secondary-present.png", "cost follow-up"],
      ["results", "edge-secondary-results.png", "submission failure"],
    ]],
  ]) {
    const actions = findCalls(guidedPath, actionName);
    const captures = findCalls(guidedPath, "captureCanvas");
    for (const [beforeLabel, filename, afterLabel] of milestones) {
      const before = actions.find(call => lastQuotedValue(call) === beforeLabel);
      const capture = captures.find(call => lastQuotedValue(call) === filename);
      const after = actions.find(call => lastQuotedValue(call) === afterLabel);
      assert.ok(before && capture && after);
      assertOrdered(before.end, capture.start, after.start);
    }
  }
});

test("matrix metadata describes localized state gates rather than obsolete canvas hashes", () => {
  const runBrowser = canonicalCode(extractFunction(source, "runBrowser"));
  assert.doesNotMatch(runBrowser, /changed canvas hash/);
  assert.match(runBrowser,
    /Primary path is keyboard-only from Title through fresh Mode Selection; every named action waits for state-specific localized content\/control regions stable across three samples\./);
  assert.match(runBrowser,
    /Secondary path is pointer-only from Title through fresh Mode Selection; every named action waits for state-specific localized content\/control regions stable across three samples\./);
});
