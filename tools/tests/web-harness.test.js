"use strict";

const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const test = require("node:test");

const projectRoot = path.resolve(__dirname, "..", "..");

test("harness suppresses origin-root favicon requests when hosted below a subpath", () => {
  const html = fs.readFileSync(path.join(projectRoot, "WebHarness", "index.html"), "utf8");

  assert.match(html, /<link\s+rel="icon"\s+href="data:,">/);
});

test("harness advertises guided content version two", () => {
  const source = fs.readFileSync(path.join(projectRoot, "WebHarness", "harness.js"), "utf8");

  assert.match(source, /ContentVersion:\s*2,/);
  assert.doesNotMatch(source, /ContentVersion:\s*1,/);
});
