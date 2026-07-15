"use strict";

const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const test = require("node:test");

const projectRoot = path.resolve(__dirname, "..", "..");
const source = fs.readFileSync(path.join(projectRoot, "tools", "smoke-webgl.mjs"), "utf8");

const lexicalToken = /"(?:\\[\s\S]|[^"\\])*"|'(?:\\[\s\S]|[^'\\])*'|`(?:\\[\s\S]|[^`\\])*`|\/\/[^\r\n]*|\/\*[\s\S]*?\*\//g;
const blankToken = token => token.replace(/[^\r\n]/g, " ");
const maskNonCode = value => value.replace(lexicalToken, blankToken);
const stripComments = value => value.replace(lexicalToken,
  token => token.startsWith("//") || token.startsWith("/*") ? blankToken(token) : token);

function matchingDelimiter(masked, openIndex, open, close) {
  let depth = 0;
  for (let index = openIndex; index < masked.length; index += 1) {
    if (masked[index] === open) depth += 1;
    if (masked[index] === close) depth -= 1;
    if (depth === 0) return index;
  }
  throw new Error(`Unmatched ${open} at ${openIndex}.`);
}

function escapeRegExp(value) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

function escapedCallee(callee) {
  return callee.split(".")
    .map(escapeRegExp)
    .join("\\s*\\.\\s*");
}

function findCalls(value, callee, { awaited = true, constructed = false } = {}) {
  const masked = maskNonCode(value);
  const prefix = awaited ? "\\bawait\\s+" : "\\b";
  const constructor = constructed ? "new\\s+" : "";
  const pattern = new RegExp(`${prefix}${constructor}${escapedCallee(callee)}\\s*\\(`, "g");
  const calls = [];
  let match;

  while ((match = pattern.exec(masked)) !== null) {
    const openIndex = masked.indexOf("(", match.index);
    const closeIndex = matchingDelimiter(masked, openIndex, "(", ")");
    calls.push({
      start: match.index,
      end: closeIndex + 1,
      text: stripComments(value.slice(match.index, closeIndex + 1)),
    });
    pattern.lastIndex = closeIndex + 1;
  }

  return calls;
}

function findIfBlocks(value) {
  const masked = maskNonCode(value);
  const pattern = /\bif\s*\(/g;
  const blocks = [];
  let match;

  while ((match = pattern.exec(masked)) !== null) {
    const conditionOpen = masked.indexOf("(", match.index);
    const conditionClose = matchingDelimiter(masked, conditionOpen, "(", ")");
    const blockOpen = masked.indexOf("{", conditionClose);
    if (blockOpen < 0 || masked.slice(conditionClose + 1, blockOpen).trim() !== "") continue;
    const blockClose = matchingDelimiter(masked, blockOpen, "{", "}");
    blocks.push({
      start: match.index,
      end: blockClose + 1,
      header: stripComments(value.slice(match.index, blockOpen)),
    });
    pattern.lastIndex = blockClose + 1;
  }

  return blocks;
}

function extractFunction(value, name, nextName) {
  const start = value.indexOf(`async function ${name}(`);
  const end = value.indexOf(`async function ${nextName}(`, start);
  assert.notEqual(start, -1, `missing ${name}`);
  assert.notEqual(end, -1, `missing boundary after ${name}`);
  return value.slice(start, end);
}

function callWithLabel(calls, label) {
  const quotedLabel = new RegExp(`["']${escapeRegExp(label)}["']`);
  const matches = calls.filter(call => quotedLabel.test(call.text));
  assert.equal(matches.length, 1, `expected one executable call labelled ${label}`);
  return matches[0];
}

function assertOrdered(...positions) {
  for (let index = 1; index < positions.length; index += 1) {
    assert.ok(positions[index - 1] < positions[index], "executable calls are out of order");
  }
}

const playAttempt = extractFunction(source, "playAttempt", "verifyMissingConfigRecovery");
const mouseResponse = extractFunction(source, "mouseResponse", "mouseContinue");
const mouseContinue = extractFunction(source, "mouseContinue", "canvasMetrics");

function assertMeasuredCanvasClick(value, normalizedY) {
  const calls = findCalls(value, "canvas.click");
  assert.equal(calls.length, 1, "expected exactly one executable canvas click");
  assert.match(calls[0].text, new RegExp(
    `^await\\s+canvas\\.click\\s*\\(\\s*\\{\\s*position\\s*:\\s*\\{` +
    `\\s*x\\s*:\\s*bounds\\.width\\s*\\*\\s*0\\.5\\s*,` +
    `\\s*y\\s*:\\s*bounds\\.height\\s*\\*\\s*${escapeRegExp(normalizedY)}\\s*\\}` +
    `\\s*,\\s*delay\\s*:\\s*120\\s*\\}\\s*\\)$`, "s"));
}

test("practice response click stays inside the measured centered control", () => {
  assertMeasuredCanvasClick(mouseResponse, "0.73");
});

test("pitch-room Continue click stays inside the measured centered control", () => {
  assertMeasuredCanvasClick(mouseContinue, "0.86");
});

test("measured click contract rejects comments and dead strings", () => {
  const responseCall = "await canvas.click({ position: { x: bounds.width * 0.5, y: bounds.height * 0.73 }, delay: 120 });";
  const continueCall = "await canvas.click({ position: { x: bounds.width * 0.5, y: bounds.height * 0.86 }, delay: 120 });";
  assert.ok(mouseResponse.includes(responseCall));
  assert.ok(mouseContinue.includes(continueCall));

  assert.throws(() => assertMeasuredCanvasClick(mouseResponse.replace(responseCall, `// ${responseCall}`), "0.73"));
  assert.throws(() => assertMeasuredCanvasClick(mouseContinue.replace(continueCall, `const dead = '${continueCall}'`), "0.86"));
});

test("measured click contract requires the 120ms pointer dwell", () => {
  const mutated = mouseResponse.replace("delay: 120", "delay: 0");
  assert.throws(() => assertMeasuredCanvasClick(mutated, "0.73"));
});

test("final browser screenshot waits for stable recovered Briefing content and controls", () => {
  const recovery = extractFunction(source, "verifyMissingConfigRecovery", "runBrowser");
  const runBrowser = extractFunction(source, "runBrowser", "main");
  const stableCalls = findCalls(recovery, "waitForStableCanvasRegions");
  assert.equal(stableCalls.length, 1, "missing recovered Briefing visual-ready gate");
  assert.match(stableCalls[0].text,
    /^await\s+waitForStableCanvasRegions\s*\(\s*page\s*,\s*canvas\s*,\s*recoveredRegionsBefore\s*,\s*options\.timeoutMs\s*,\s*["']recovered Briefing["']\s*\)$/s);

  const stableGate = extractFunction(source, "waitForStableCanvasRegions", "canvasMetrics");
  assert.equal(findCalls(stableGate, "contentRegionHash").length, 1);
  assert.equal(findCalls(stableGate, "controlRegionHash").length, 1);
  assert.match(stableGate,
    /current\.content\s*!==\s*previous\.content\s*&&\s*current\.controls\s*!==\s*previous\.controls/s);
  assert.match(stableGate, /stableSamples\s*>=\s*3/);
  assert.match(stableGate, /const\s+deadline\s*=\s*Date\.now\(\)\s*\+\s*timeoutMs/);
  assert.ok(findCalls(stableGate, "Promise", { constructed: true })
    .some(call => /setTimeout\s*\([^,]+,\s*100\s*\)/s.test(call.text)));

  const recoveryCall = findCalls(runBrowser, "verifyMissingConfigRecovery")[0];
  const finalScreenshot = findCalls(runBrowser, "page.screenshot")
    .find(call => /path\s*:\s*screenshotPath/.test(call.text));
  assert.ok(recoveryCall && finalScreenshot, "missing recovery or final screenshot call");
  assertOrdered(recoveryCall.end, finalScreenshot.start);
});

function assertTutorialContract(attempt) {
  const screenshots = findCalls(attempt, "page.screenshot");
  const tutorialScreenshot = callWithLabel(screenshots, "chrome-tutorial.png");
  const judgeIntroduction = callWithLabel(findCalls(attempt, "mouseContinue"), "Judge introduction");
  const insideTutorialWindow = call =>
    call.start > tutorialScreenshot.end && call.end < judgeIntroduction.start;
  const enterCalls = findCalls(attempt, "keyboardAction")
    .filter(insideTutorialWindow)
    .filter(call => /^await\s+keyboardAction\s*\(\s*page\s*,\s*canvas\s*,\s*["']Enter["']\s*,/s
      .test(call.text));
  const waits = findCalls(attempt, "Promise", { constructed: true })
    .filter(insideTutorialWindow)
    .filter(call => /^await\s+new\s+Promise\s*\(\s*\(?\s*([A-Za-z_$][\w$]*)\s*\)?\s*=>\s*setTimeout\s*\(\s*\1\s*,\s*250\s*\)\s*\)$/s
      .test(call.text));

  assert.equal(enterCalls.length, 3, "exactly three executable Tutorial Enter actions are required");
  assert.equal(waits.length, 3, "exactly three awaited 250ms Tutorial settles are required");
  for (const [index, label] of [
    "Tutorial page 1 Next",
    "Tutorial page 2 Next",
    "Tutorial page 3 Start Practice",
  ].entries()) {
    assert.match(enterCalls[index].text,
      new RegExp(`^await\\s+keyboardAction\\s*\\(\\s*page\\s*,\\s*canvas\\s*,\\s*["']Enter["']\\s*,\\s*options\\.timeoutMs\\s*,\\s*["']${label}["']\\s*\\)$`));
  }
  assert.match(tutorialScreenshot.text,
    /path\s*:\s*join\s*\(\s*options\.outputDirectory\s*,\s*["']chrome-tutorial\.png["']\s*\)/s);
  assertOrdered(
    tutorialScreenshot.end,
    enterCalls[0].start, waits[0].start,
    enterCalls[1].start, waits[1].start,
    enterCalls[2].start, waits[2].start,
    judgeIntroduction.start,
  );

  const chromeGuards = findIfBlocks(attempt)
    .filter(block => /^if\s*\(\s*browserName\s*===\s*["']chrome["']\s*\)\s*$/.test(block.header));
  assert.ok(chromeGuards.some(block =>
    tutorialScreenshot.start > block.start && tutorialScreenshot.end < block.end));
}

test("call extraction rejects comments and dead strings", () => {
  const fixture = `
    // await keyboardAction(page, canvas, "Enter", timeout, "comment");
    const dead = 'await keyboardAction(page, canvas, "Enter", timeout, "dead")';
    /* await keyboardAction(page, canvas, "Enter", timeout, "block"); */
    await keyboardAction(page, canvas, "Enter", timeout, "executable");
  `;

  const calls = findCalls(fixture, "keyboardAction");
  assert.equal(calls.length, 1);
  assert.match(calls[0].text, /"executable"/);
});

test("main attempt captures Chrome tutorial before three ordered Enter advances with settles", () => {
  assertTutorialContract(playAttempt);
});

test("tutorial contract rejects an extra executable Enter inside the tutorial window", () => {
  const firstEnter = callWithLabel(findCalls(playAttempt, "keyboardAction"), "Tutorial page 1 Next");
  const statementEnd = playAttempt.indexOf(";", firstEnter.end) + 1;
  const mutated = `${playAttempt.slice(0, statementEnd)}
  await keyboardAction(page, canvas, "Enter", options.timeoutMs, "Unexpected Enter");${playAttempt.slice(statementEnd)}`;

  assert.throws(() => assertTutorialContract(mutated), /exactly three executable Tutorial Enter actions/);
});

test("tutorial contract rejects an unawaited 250ms settle", () => {
  const awaitedSettle = "await new Promise(resolveWait => setTimeout(resolveWait, 250))";
  assert.ok(playAttempt.includes(awaitedSettle), "missing mutation source settle");
  const mutated = playAttempt.replace(awaitedSettle, "setTimeout(resolveWait, 250)");

  assert.throws(() => assertTutorialContract(mutated), /exactly three awaited 250ms Tutorial settles/);
});

test("main attempt captures Chrome pitch after the Question 1 reveal gate", () => {
  const screenshots = findCalls(playAttempt, "page.screenshot");
  const pitchScreenshot = callWithLabel(screenshots, "chrome-pitch.png");
  const q1Reveal = callWithLabel(findCalls(playAttempt, "mouseContinue"), "Question 1 response reveal");

  assert.match(q1Reveal.text, /,\s*true\s*\)$/s);
  assert.ok(q1Reveal.end < pitchScreenshot.start);
  assert.match(pitchScreenshot.text,
    /path\s*:\s*join\s*\(\s*options\.outputDirectory\s*,\s*["']chrome-pitch\.png["']\s*\)/s);

  const chromeGuards = findIfBlocks(playAttempt)
    .filter(block => /^if\s*\(\s*browserName\s*===\s*["']chrome["']\s*\)\s*$/.test(block.header));
  assert.ok(chromeGuards.some(block =>
    pitchScreenshot.start > block.start && pitchScreenshot.end < block.end));
});

test("Retry uses pointerAction Skip and remains pointer-only through fresh Question 1", () => {
  const keyboardCalls = findCalls(playAttempt, "keyboardAction");
  const retry = callWithLabel(keyboardCalls, "Results Retry");
  const retryBriefing = callWithLabel(findCalls(playAttempt, "waitForCanvasChange"),
    "retry Briefing pointer Continue");
  const skip = callWithLabel(findCalls(playAttempt, "pointerAction"), "retry Tutorial Skip");
  const mouseContinueCalls = findCalls(playAttempt, "mouseContinue");
  const judge = callWithLabel(mouseContinueCalls, "retry Judge introduction");
  const reveal = callWithLabel(mouseContinueCalls, "retry Tutorial response reveal");
  const response = findCalls(playAttempt, "mouseResponse")
    .find(call => call.start > reveal.start);
  const reaction = callWithLabel(mouseContinueCalls, "retry Tutorial reaction");
  const feedback = callWithLabel(mouseContinueCalls, "retry Tutorial feedback");
  const freshQ1 = callWithLabel(mouseContinueCalls, "retry Question 1 response reveal");

  assert.match(skip.text,
    /^await\s+pointerAction\s*\(\s*page\s*,\s*0\.40\s*,\s*0\.79\s*,\s*canvas\s*,\s*options\.timeoutMs\s*,\s*["']retry Tutorial Skip["']\s*,\s*\{\s*delay\s*:\s*120\s*\}\s*\)$/);
  assert.ok(response, "missing executable retry practice response");
  assert.match(reveal.text, /,\s*true\s*\)$/s);
  assert.match(freshQ1.text, /,\s*true\s*\)$/s);
  assertOrdered(retry.start, retryBriefing.start, skip.start, judge.start, reveal.start,
    response.start, reaction.start, feedback.start, freshQ1.start);

  const retryProof = playAttempt.slice(retry.end, freshQ1.end);
  assert.equal(findCalls(retryProof, "keyboardAction").length, 0,
    "retry proof must remain pointer-only after activating Retry");
});
