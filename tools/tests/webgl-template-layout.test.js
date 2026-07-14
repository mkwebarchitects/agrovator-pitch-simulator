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

test("layout calculator reserves measured chrome on desktop and mobile viewports", () => {
  const { calculateStageWidth } = require(path.join(templateRoot, "TemplateData", "layout.js"));

  const desktop = calculateStageWidth({
    shellWidth: 1280,
    viewportHeight: 798,
    bodyPaddingTop: 12,
    bodyPaddingBottom: 12,
    shellRowGap: 10,
    controlHeight: 44,
    controlMarginTop: 0,
    controlMarginBottom: 0,
  });
  assert.equal(desktop, 1280);

  const constrainedDesktop = calculateStageWidth({
    shellWidth: 1280,
    viewportHeight: 720,
    bodyPaddingTop: 12,
    bodyPaddingBottom: 12,
    shellRowGap: 10,
    controlHeight: 44,
    controlMarginTop: 5,
    controlMarginBottom: 7,
  });
  assert.equal(constrainedDesktop, 1120);
  assert.ok(constrainedDesktop * 9 / 16 + 12 + 12 + 10 + 44 + 5 + 7 <= 720);

  const mobilePortrait = calculateStageWidth({
    shellWidth: 382,
    viewportHeight: 844,
    bodyPaddingTop: 4,
    bodyPaddingBottom: 4,
    shellRowGap: 10,
    controlHeight: 40,
    controlMarginTop: 0,
    controlMarginBottom: 0,
  });
  assert.equal(mobilePortrait, 382);

  const mobileLandscape = calculateStageWidth({
    shellWidth: 836,
    viewportHeight: 390,
    bodyPaddingTop: 4,
    bodyPaddingBottom: 4,
    shellRowGap: 10,
    controlHeight: 40,
    controlMarginTop: 0,
    controlMarginBottom: 0,
  });
  assert.equal(mobileLandscape, 590);
  assert.ok(mobileLandscape * 9 / 16 + 4 + 4 + 10 + 40 <= 390);
});

test("template measures styled chrome and coalesces ResizeObserver writes", () => {
  const html = fs.readFileSync(path.join(templateRoot, "index.html"), "utf8");

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
    "stage.style.width !== nextWidth",
    "new ResizeObserver(requestLayout)",
  ]) {
    assert.ok(html.includes(contract), `missing layout contract: ${contract}`);
  }
});
