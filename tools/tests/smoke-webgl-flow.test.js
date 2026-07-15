"use strict";

const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const test = require("node:test");

const projectRoot = path.resolve(__dirname, "..", "..");
const source = fs.readFileSync(path.join(projectRoot, "tools", "smoke-webgl.mjs"), "utf8");

test("smoke flow advances tutorial page 1", () => {
  assert.match(source, /Tutorial page 1 Next/);
});

test("smoke flow advances tutorial page 2", () => {
  assert.match(source, /Tutorial page 2 Next/);
});

test("smoke flow starts practice from tutorial page 3", () => {
  assert.match(source, /Tutorial page 3 Start Practice/);
});

test("smoke flow skips the tutorial by pointer after retry", () => {
  assert.match(source, /retry Tutorial Skip/);
});

test("smoke flow captures the Chrome tutorial checkpoint", () => {
  assert.match(source, /chrome-tutorial\.png/);
});

test("smoke flow captures the Chrome pitch checkpoint", () => {
  assert.match(source, /chrome-pitch\.png/);
});
