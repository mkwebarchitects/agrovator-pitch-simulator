"use strict";

const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const test = require("node:test");

const projectRoot = path.resolve(__dirname, "..", "..");
const deployScriptPath = path.join(projectRoot, "tools", "Deploy-Pages.ps1");

function readDeployScript() {
  return fs.readFileSync(deployScriptPath, "utf8");
}

// Publishing is the one irreversible step in this project, and the artifact it
// publishes is chosen by whatever happens to be sitting in Build/WebGL. The
// smoke and the Node contract tests both leave the 92 MB development player
// there, so a deploy that trusts the folder ships the wrong build to learners
// on school wifi. These pin the guards that make that impossible rather than
// merely discouraged.
// The deploy delegates to Build-WebGL.ps1 rather than re-invoking Unity, so
// log scanning and exit-code handling stay in one place. What matters here is
// that it always asks for the release flavour.
test("deploy script builds the release player itself", () => {
  const source = readDeployScript();

  assert.match(source, /Build-WebGL\.ps1'\)\s+-Release/);
  assert.doesNotMatch(
    source,
    /BuildDevelopment/,
    "The deploy path must never invoke the development build."
  );
});

test("deploy script verifies the output really is the compressed player", () => {
  const source = readDeployScript();

  assert.match(source, /\.unityweb/);
  assert.match(
    source,
    /throw/,
    "Verification must fail the deploy rather than warn."
  );
});

test("deploy script requires the stamped index before publishing", () => {
  const source = readDeployScript();

  assert.match(source, /\?v=/);
});

test("deploy script does not publish unless explicitly asked", () => {
  const source = readDeployScript();

  assert.match(
    source,
    /\[switch\]\$Push/,
    "Publishing must be opt-in so a routine run cannot deploy by accident."
  );
  assert.match(
    source,
    /if\s*\(\s*-not\s+\$Push\s*\)/,
    "The script must short-circuit before pushing when -Push was not passed."
  );
});

// Scoped to push lines specifically: `git worktree remove --force` is routine
// cleanup of a temporary checkout and must not trip this.
test("deploy script never force-pushes the published branch", () => {
  const pushLines = readDeployScript()
    .split("\n")
    .filter(line => /\bgit\s+push\b/.test(line));

  assert.ok(pushLines.length > 0, "The deploy script must contain a push.");
  for (const line of pushLines) {
    assert.doesNotMatch(line, /--force/, `Force push in: ${line.trim()}`);
    assert.doesNotMatch(line, /\s-f\b/, `Force push in: ${line.trim()}`);
  }
});
