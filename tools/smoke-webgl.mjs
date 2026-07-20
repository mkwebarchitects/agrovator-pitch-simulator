#!/usr/bin/env node

import { createHash } from "node:crypto";
import { spawn } from "node:child_process";
import { existsSync, mkdirSync, readFileSync, readdirSync, writeFileSync } from "node:fs";
import { connect, createServer } from "node:net";
import { dirname, isAbsolute, join, resolve } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const projectRoot = resolve(scriptDirectory, "..");

function parseArguments(argv) {
  const options = {
    baseUrl: null,
    browser: "matrix",
    executablePath: null,
    outputDirectory: join(projectRoot, "artifacts", "smoke"),
    headless: true,
    timeoutMs: 20_000,
    unityTimeoutMs: 180_000,
    playwrightModule: process.env.PITCH_SIMULATOR_PLAYWRIGHT_MODULE || null,
    externalServer: false,
  };

  for (let index = 0; index < argv.length; index += 1) {
    const name = argv[index];
    const value = () => {
      index += 1;
      if (index >= argv.length) throw new Error(`Missing value for ${name}.`);
      return argv[index];
    };
    switch (name) {
      case "--base-url": options.baseUrl = value().replace(/\/$/, ""); options.externalServer = true; break;
      case "--browser": options.browser = value().toLowerCase(); break;
      case "--executable-path": options.executablePath = value(); break;
      case "--output-dir": options.outputDirectory = resolve(value()); break;
      case "--headed": options.headless = false; break;
      case "--headless": options.headless = true; break;
      case "--timeout-ms": options.timeoutMs = Number.parseInt(value(), 10); break;
      case "--unity-timeout-ms": options.unityTimeoutMs = Number.parseInt(value(), 10); break;
      case "--playwright-module": options.playwrightModule = value(); break;
      case "--external-server": options.externalServer = true; break;
      default: throw new Error(`Unknown option '${name}'.`);
    }
  }

  if (!["matrix", "chrome", "edge", "firefox"].includes(options.browser)) {
    throw new Error("--browser must be matrix, chrome, edge or firefox.");
  }
  if (!Number.isFinite(options.timeoutMs) || options.timeoutMs < 1_000 ||
      !Number.isFinite(options.unityTimeoutMs) || options.unityTimeoutMs < 10_000) {
    throw new Error("Timeouts must be finite positive milliseconds.");
  }
  return options;
}

function findBundledPlaywright() {
  const home = process.env.USERPROFILE || process.env.HOME;
  if (!home) return null;
  const pnpm = join(home, ".cache", "codex-runtimes", "codex-primary-runtime", "dependencies", "node", "node_modules", ".pnpm");
  if (!existsSync(pnpm)) return null;
  const packageDirectory = readdirSync(pnpm)
    .filter(name => /^playwright@/.test(name))
    .sort()
    .at(-1);
  if (!packageDirectory) return null;
  const entry = join(pnpm, packageDirectory, "node_modules", "playwright", "index.mjs");
  return existsSync(entry) ? entry : null;
}

async function loadPlaywright(moduleOption) {
  if (moduleOption) {
    const specifier = isAbsolute(moduleOption) ? pathToFileURL(moduleOption).href : moduleOption;
    return import(specifier);
  }
  try {
    return await import("playwright");
  } catch (error) {
    const bundled = findBundledPlaywright();
    if (!bundled) throw error;
    return import(pathToFileURL(bundled).href);
  }
}

function firstExisting(paths) {
  return paths.find(path => existsSync(path)) || null;
}

function browserDefinitions(explicitBrowser, explicitPath) {
  const programFiles = process.env.ProgramFiles || "C:\\Program Files";
  const programFilesX86 = process.env["ProgramFiles(x86)"] || "C:\\Program Files (x86)";
  const definitions = {
    chrome: {
      engine: "chromium",
      required: true,
      path: firstExisting([
        join(programFiles, "Google", "Chrome", "Application", "chrome.exe"),
        join(programFilesX86, "Google", "Chrome", "Application", "chrome.exe"),
      ]),
    },
    edge: {
      engine: "chromium",
      required: false,
      path: firstExisting([
        join(programFilesX86, "Microsoft", "Edge", "Application", "msedge.exe"),
        join(programFiles, "Microsoft", "Edge", "Application", "msedge.exe"),
      ]),
    },
    firefox: {
      engine: "firefox",
      required: false,
      path: firstExisting([
        join(programFiles, "Mozilla Firefox", "firefox.exe"),
        join(programFilesX86, "Mozilla Firefox", "firefox.exe"),
      ]),
    },
  };
  const names = explicitBrowser === "matrix" ? ["chrome", "edge", "firefox"] : [explicitBrowser];
  if (explicitPath && names.length !== 1) throw new Error("--executable-path requires one explicit --browser.");
  if (explicitPath) definitions[names[0]].path = resolve(explicitPath);
  return names.map(name => ({ name, ...definitions[name] }));
}

async function getFreePort() {
  return new Promise((resolvePort, reject) => {
    const server = createServer();
    server.once("error", reject);
    server.listen(0, "127.0.0.1", () => {
      const { port } = server.address();
      server.close(error => error ? reject(error) : resolvePort(port));
    });
  });
}

function waitForPortClosed(port, timeoutMs = 5_000) {
  const deadline = Date.now() + timeoutMs;
  return new Promise(resolveClosed => {
    const probe = () => {
      const socket = connect({ host: "127.0.0.1", port });
      socket.once("connect", () => {
        socket.destroy();
        if (Date.now() >= deadline) resolveClosed(false);
        else setTimeout(probe, 100);
      });
      socket.once("error", () => resolveClosed(true));
    };
    probe();
  });
}

async function startServer(options) {
  if (options.externalServer) {
    if (!options.baseUrl) throw new Error("--external-server requires --base-url.");
    return { child: null, baseUrl: options.baseUrl, port: null, stdout: [], stderr: [] };
  }
  const port = await getFreePort();
  const baseUrl = `http://127.0.0.1:${port}`;
  const stdout = [];
  const stderr = [];
  const child = spawn("powershell.exe", [
    "-NoProfile", "-ExecutionPolicy", "Bypass", "-File",
    join(scriptDirectory, "Serve-WebGL.ps1"), "-Port", String(port), "-Root", projectRoot,
  ], { cwd: projectRoot, windowsHide: true, stdio: ["ignore", "pipe", "pipe"] });
  child.stdout.on("data", chunk => stdout.push(sanitizeText(String(chunk))));
  child.stderr.on("data", chunk => stderr.push(sanitizeText(String(chunk))));

  const deadline = Date.now() + 15_000;
  let ready = false;
  while (Date.now() < deadline && child.exitCode === null) {
    try {
      const response = await fetch(`${baseUrl}/WebHarness/index.html`, { cache: "no-store" });
      if (response.ok) { ready = true; break; }
    } catch {}
    await new Promise(resolveWait => setTimeout(resolveWait, 100));
  }
  if (!ready) {
    child.kill();
    throw new Error(`Loopback server did not become ready. ${stderr.join(" ").slice(0, 500)}`);
  }
  return { child, baseUrl, port, stdout, stderr };
}

async function stopServer(server) {
  if (!server.child) return true;
  if (server.child.exitCode === null) {
    server.child.kill();
    await Promise.race([
      new Promise(resolveExit => server.child.once("exit", resolveExit)),
      new Promise(resolveWait => setTimeout(resolveWait, 5_000)),
    ]);
  }
  return waitForPortClosed(server.port);
}

function sanitizeText(value) {
  const original = String(value);
  if (/(?:PseudonymousLearnerId|LaunchReference)/i.test(original)) {
    return "[browser message containing launch/completion identifiers redacted]";
  }
  return original
    .replace(/(?:local-learner|local-session)-[\w-]+/gi, "[identifier-redacted]")
    .replace(/lref_[\w-]+/gi, "[launch-reference-redacted]")
    .replace(/\s+/g, " ")
    .trim()
    .slice(0, 1_000);
}

function hash(buffer) {
  return createHash("sha256").update(buffer).digest("hex");
}

async function canvasHash(canvas) {
  return hash(await canvas.screenshot({ type: "png" }));
}

async function controlRegionHash(page, canvas) {
  const bounds = await canvas.boundingBox();
  if (!bounds) throw new Error("Unity canvas has no control-region bounds.");
  const clip = {
    x: bounds.x,
    y: bounds.y + (bounds.height * 0.68),
    width: bounds.width,
    height: bounds.height * 0.30,
  };
  return hash(await page.screenshot({ type: "png", clip }));
}

async function contentRegionHash(page, canvas) {
  const bounds = await canvas.boundingBox();
  if (!bounds) throw new Error("Unity canvas has no content-region bounds.");
  const clip = {
    x: bounds.x,
    y: bounds.y + (bounds.height * 0.20),
    width: bounds.width,
    height: bounds.height * 0.44,
  };
  return hash(await page.screenshot({ type: "png", clip }));
}

// Judge Aya's portrait sits at the left of the Aya Row, directly under the
// progress rail. Hashing just her cell turns "the reaction screenshot looks
// right" into an executable assertion: every other region changes when the
// feedback text appears, so only this clip can prove the portrait itself moved.
// Ordering constraint: the lower part of Judge Aya's cell falls inside the
// gated content region, so a GuidedGate.content step must never immediately
// follow a reaction. Her settle back to Encouraging would otherwise satisfy a
// content-only gate on its own and mask a missed press.
async function judgePortraitHash(page, canvas) {
  const bounds = await canvas.boundingBox();
  if (!bounds) throw new Error("Unity canvas has no judge-portrait bounds.");
  const clip = {
    x: bounds.x + (bounds.width * 0.06),
    y: bounds.y + (bounds.height * 0.10),
    width: bounds.width * 0.14,
    height: bounds.height * 0.26,
  };
  return hash(await page.screenshot({ type: "png", clip }));
}

// Rejecting BOTH the resting face and the preceding reaction matters: comparing
// only against the preceding one would pass when the hold has already expired
// and Aya has settled back, which is the exact silent failure this guards.
async function captureJudgeReaction(page, canvas, options, previous, filename) {
  const reacting = await judgePortraitHash(page, canvas);
  if (reacting === previous.resting) {
    throw new Error(`Judge Aya settled back before ${filename}; the capture shows her resting face.`);
  }
  if (reacting === previous.reaction) {
    throw new Error(`Judge Aya did not react before ${filename}; her portrait is unchanged.`);
  }
  await captureCanvas(canvas, options, filename);
  return { resting: previous.resting, reaction: reacting };
}

async function regionHashes(page, canvas) {
  return {
    content: await contentRegionHash(page, canvas),
    controls: await controlRegionHash(page, canvas),
  };
}

async function waitForCanvasChange(canvas, previousHash, timeoutMs, label) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const current = await canvasHash(canvas);
    if (current !== previousHash) return current;
    await new Promise(resolveWait => setTimeout(resolveWait, 100));
  }
  throw new Error(`Canvas did not visibly change after ${label}.`);
}

async function canvasMetrics(page, frame, canvas, viewport) {
  await page.setViewportSize(viewport);
  await frame.waitForFunction(() => {
    const canvas = document.querySelector("#unity-canvas");
    return canvas && canvas.getBoundingClientRect().width > 0;
  });
  const bounds = await canvas.boundingBox();
  const inner = await frame.evaluate(() => ({ width: innerWidth, height: innerHeight }));
  const aspect = bounds.width / bounds.height;
  if (Math.abs(aspect - (16 / 9)) > 0.02) throw new Error(`Canvas aspect drifted to ${aspect}.`);
  if (bounds.width > inner.width + 1 || bounds.height > inner.height + 1) {
    throw new Error(`Canvas ${bounds.width}x${bounds.height} exceeds iframe viewport ${inner.width}x${inner.height}.`);
  }
  return { viewport, iframeViewport: inner, bounds, aspect };
}

async function verifyMissingConfigRecovery(page, options) {
  await setHarnessMode(page, "missing-config");
  const frameElement = page.locator("#game");
  await frameElement.evaluate(element => element.contentWindow.location.reload());
  await page.waitForFunction(() => document.querySelector("#connection")?.textContent.includes("Missing Config"), null,
    { timeout: options.unityTimeoutMs });
  const frame = page.frames().find(candidate => candidate.url().includes("/Build/WebGL/index.html"));
  if (!frame) throw new Error("Reloaded Unity iframe was not found.");
  const canvas = frame.locator("#unity-canvas");
  await frame.locator("#unity-loading").waitFor({ state: "hidden", timeout: options.unityTimeoutMs });
  const waitingHash = await canvasHash(canvas);
  await setHarnessMode(page, "success");
  await page.locator("#resend").dispatchEvent("click");
  await page.waitForFunction(() => document.querySelector("#connection")?.textContent.includes("Launch configuration sent"), null,
    { timeout: options.timeoutMs });
  await waitForCanvasChange(canvas, waitingHash, options.timeoutMs, "Success plus Resend recovery");
  // Reuse the proven settled keyboard step: compose, press the default-focused
  // Start, and gate on Briefing content. A coordinate press against a
  // mid-transition Title can land on Settings and strand the settle gate.
  await keyboardStableStep(page, canvas, "Enter", GuidedGate.content, options, "recovered title start");
  return true;
}

async function waitForStableRegionChange(page, canvas, previous, required, timeoutMs, label) {
  const deadline = Date.now() + timeoutMs;
  let last = null;
  let stableSamples = 0;
  while (Date.now() < deadline) {
    const current = await regionHashes(page, canvas);
    const requiredChanged = (!required.content || current.content !== previous.content) &&
      (!required.controls || current.controls !== previous.controls);
    if (requiredChanged) {
      const matchesLast = last && current.content === last.content && current.controls === last.controls;
      stableSamples = matchesLast ? stableSamples + 1 : 1;
      if (stableSamples >= 3) return current;
    } else {
      stableSamples = 0;
    }
    last = current;
    await new Promise(resolveWait => setTimeout(resolveWait, 80));
  }
  throw new Error(`Required stable content/control regions did not change after ${label}.`);
}

async function waitForStableRegionComposition(page, canvas, timeoutMs, label) {
  const deadline = Date.now() + timeoutMs;
  let last = null;
  let stableSamples = 0;
  while (Date.now() < deadline) {
    const current = await regionHashes(page, canvas);
    const matchesLast = last && current.content === last.content && current.controls === last.controls;
    stableSamples = matchesLast ? stableSamples + 1 : 1;
    if (stableSamples >= 3) return current;
    last = current;
    await new Promise(resolveWait => setTimeout(resolveWait, 80));
  }
  throw new Error(`Canvas content/control composition did not settle for ${label}.`);
}

async function keyboardStableStep(page, canvas, key, required, options, label) {
  await canvas.focus();
  await waitForStableRegionComposition(page, canvas, options.timeoutMs, `${label} focus`);
  const before = await regionHashes(page, canvas);
  await canvas.press(key, { delay: 120 });
  return await waitForStableRegionChange(page, canvas, before, required, options.timeoutMs, label);
}

async function pointerStableStep(page, canvas, normalizedX, normalizedY, required, options, label) {
  await canvas.focus();
  await waitForStableRegionComposition(page, canvas, options.timeoutMs, `${label} focus`);
  const bounds = await canvas.boundingBox();
  if (!bounds) throw new Error(`Unity canvas has no pointer bounds for ${label}.`);
  const position = { x: bounds.width * normalizedX, y: bounds.height * normalizedY };
  await canvas.hover({ position });
  await waitForStableRegionComposition(page, canvas, options.timeoutMs, `${label} hover`);
  const before = await regionHashes(page, canvas);
  await canvas.click({
    position,
    delay: 120,
  });
  return await waitForStableRegionChange(page, canvas, before, required, options.timeoutMs, label);
}

async function captureCanvas(canvas, options, filename) {
  await canvas.screenshot({ path: join(options.outputDirectory, filename), type: "png" });
}

async function waitForSubmission(page, expectedStatus, options, label) {
  await page.waitForFunction(status => [...document.querySelectorAll("#progress li")]
    .some(item => item.textContent.includes(status)), expectedStatus, { timeout: options.timeoutMs });
  const completion = sanitizeText(await page.locator("#completion").innerText());
  if (!completion) throw new Error(`Harness did not expose sanitized completion for ${label}.`);
  return completion;
}

async function setHarnessMode(page, mode) {
  await page.evaluate(nextMode => {
    const select = document.querySelector("#mode");
    select.value = nextMode;
    select.dispatchEvent(new Event("change", { bubbles: true }));
  }, mode);
}

async function useFullscreenHarness(page, viewport) {
  await page.setViewportSize(viewport);
  await page.evaluate(() => {
    let style = document.querySelector("#smoke-fullscreen-harness");
    if (!style) {
      style = document.createElement("style");
      style.id = "smoke-fullscreen-harness";
      style.textContent = `
        html, body { width: 100%; height: 100%; max-width: none; padding: 0; overflow: hidden; }
        header, main > section:not(.player) { display: none; }
        main, .player { display: block; width: 100%; height: 100%; margin: 0; padding: 0; border: 0; }
        iframe { width: 100vw; height: 100vh; min-height: 100vh; border-radius: 0; }
      `;
      document.head.appendChild(style);
    }
  });
}

const GuidedGate = Object.freeze({
  transition: Object.freeze({ content: true, controls: true }),
  focus: Object.freeze({ content: false, controls: true }),
  content: Object.freeze({ content: true, controls: false }),
  controls: Object.freeze({ content: false, controls: true }),
});

async function runPrimaryKeyboardPath(definition, page, frame, canvas, options) {
  if (definition.name !== "chrome") return null;
  const pathContract = { interaction: "keyboard", mode: "Primary" };
  await keyboardStableStep(page, canvas, "Enter", GuidedGate.content, options, "title start");
  await keyboardStableStep(page, canvas, "Enter", GuidedGate.transition, options, "briefing continue");
  await waitForStableRegionComposition(page, canvas, options.timeoutMs, "mode");
  await captureCanvas(canvas, options, "chrome-primary-mode.png");

  const mobile = await verifyCanvasContract(page, frame, canvas, { width: 390, height: 844 });
  await captureCanvas(canvas, options, "chrome-mobile-compact.png");
  const desktop = await verifyCanvasContract(page, frame, canvas, { width: 1440, height: 1000 });

  await keyboardStableStep(page, canvas, "Enter", GuidedGate.transition, options, "learn");
  await keyboardStableStep(page, canvas, "Enter", GuidedGate.transition, options, "build");
  await captureCanvas(canvas, options, "chrome-primary-build.png");
  // Judge Aya rests on every question, so her resting portrait is the baseline
  // each reaction below must differ from.
  const chromeResting = { resting: await judgePortraitHash(page, canvas), reaction: null };
  await keyboardStableStep(page, canvas, "Tab", GuidedGate.focus, options, "problem developing option");
  await keyboardStableStep(page, canvas, "Enter", GuidedGate.transition, options, "problem feedback");
  const chromeDeveloping = await captureJudgeReaction(page, canvas, options, chromeResting,
    "chrome-primary-reaction-developing.png");
  await keyboardStableStep(page, canvas, "Enter", GuidedGate.transition, options, "evidence build");
  await keyboardStableStep(page, canvas, "Enter", GuidedGate.transition, options, "evidence feedback");
  await captureJudgeReaction(page, canvas, options, chromeDeveloping,
    "chrome-primary-reaction-clear.png");
  await keyboardStableStep(page, canvas, "Enter", GuidedGate.transition, options, "solution build");
  await keyboardStableStep(page, canvas, "Enter", GuidedGate.transition, options, "solution feedback");
  await keyboardStableStep(page, canvas, "Enter", GuidedGate.transition, options, "value build");
  await keyboardStableStep(page, canvas, "Enter", GuidedGate.transition, options, "value feedback");
  await keyboardStableStep(page, canvas, "Enter", GuidedGate.transition, options, "improve");
  await captureCanvas(canvas, options, "chrome-primary-improve.png");
  await keyboardStableStep(page, canvas, "Enter", GuidedGate.content, options, "revision options");
  await keyboardStableStep(page, canvas, "Tab", GuidedGate.focus, options, "revision focus present");
  await keyboardStableStep(page, canvas, "Tab", GuidedGate.focus, options, "revision focus clear option");
  await keyboardStableStep(page, canvas, "Enter", GuidedGate.transition, options, "one revision");
  await keyboardStableStep(page, canvas, "Enter", GuidedGate.transition, options, "present");
  await captureCanvas(canvas, options, "chrome-primary-present.png");
  await keyboardStableStep(page, canvas, "Enter", GuidedGate.transition, options, "cost follow-up");
  await keyboardStableStep(page, canvas, "Enter", GuidedGate.transition, options, "cost feedback");
  await keyboardStableStep(page, canvas, "Enter", GuidedGate.transition, options, "results");
  await captureCanvas(canvas, options, "chrome-primary-results.png");

  await setHarnessMode(page, "failure");
  await keyboardStableStep(page, canvas, "Enter", GuidedGate.controls, options, "submission failure");
  await waitForSubmission(page, "failure", options, "failed result");
  await setHarnessMode(page, "success");
  await keyboardStableStep(page, canvas, "Enter", GuidedGate.controls, options, "submission success");
  const completion = await waitForSubmission(page, "success", options, "successful result");
  await keyboardStableStep(page, canvas, "Enter", GuidedGate.transition, options, "retry");
  await keyboardStableStep(page, canvas, "Enter", GuidedGate.transition, options, "fresh mode selection");
  return { ...pathContract, completion, mobile, desktop };
}

async function runSecondaryPointerPath(definition, page, frame, canvas, options) {
  if (definition.name !== "edge") return null;
  const pathContract = { interaction: "pointer", mode: "Secondary" };
  await pointerStableStep(page, canvas, 0.50, 0.50, GuidedGate.content, options, "title start");
  await pointerStableStep(page, canvas, 0.50, 0.60, GuidedGate.transition, options, "briefing continue");
  await waitForStableRegionComposition(page, canvas, options.timeoutMs, "mode");
  await captureCanvas(canvas, options, "edge-secondary-mode.png");

  const mobile = await verifyCanvasContract(page, frame, canvas, { width: 390, height: 844 });
  await captureCanvas(canvas, options, "edge-mobile-compact.png");
  const desktop = await verifyCanvasContract(page, frame, canvas, { width: 1440, height: 1000 });

  await pointerStableStep(page, canvas, 0.70, 0.76, GuidedGate.transition, options, "learn");
  await pointerStableStep(page, canvas, 0.50, 0.86, GuidedGate.transition, options, "build");
  await captureCanvas(canvas, options, "edge-secondary-build.png");
  const edgeResting = { resting: await judgePortraitHash(page, canvas), reaction: null };
  await pointerStableStep(page, canvas, 0.50, 0.70, GuidedGate.transition, options, "problem feedback");
  const edgeFirst = await captureJudgeReaction(page, canvas, options, edgeResting,
    "edge-secondary-reaction-first.png");
  await pointerStableStep(page, canvas, 0.50, 0.86, GuidedGate.transition, options, "evidence build");
  await pointerStableStep(page, canvas, 0.27, 0.70, GuidedGate.transition, options, "evidence feedback");
  await captureJudgeReaction(page, canvas, options, edgeFirst,
    "edge-secondary-reaction-second.png");
  await pointerStableStep(page, canvas, 0.50, 0.86, GuidedGate.transition, options, "solution build");
  await pointerStableStep(page, canvas, 0.27, 0.70, GuidedGate.transition, options, "solution feedback");
  await pointerStableStep(page, canvas, 0.50, 0.86, GuidedGate.transition, options, "value build");
  await pointerStableStep(page, canvas, 0.27, 0.70, GuidedGate.transition, options, "value feedback");
  await pointerStableStep(page, canvas, 0.50, 0.86, GuidedGate.transition, options, "improve");
  await pointerStableStep(page, canvas, 0.50, 0.70, GuidedGate.content, options, "revision options");
  await pointerStableStep(page, canvas, 0.27, 0.60, GuidedGate.transition, options, "one revision");
  await pointerStableStep(page, canvas, 0.50, 0.86, GuidedGate.transition, options, "present");
  await captureCanvas(canvas, options, "edge-secondary-present.png");
  await pointerStableStep(page, canvas, 0.50, 0.86, GuidedGate.transition, options, "cost follow-up");
  await pointerStableStep(page, canvas, 0.27, 0.70, GuidedGate.transition, options, "cost feedback");
  await pointerStableStep(page, canvas, 0.50, 0.86, GuidedGate.transition, options, "results");
  await captureCanvas(canvas, options, "edge-secondary-results.png");

  await setHarnessMode(page, "failure");
  await pointerStableStep(page, canvas, 0.36, 0.82, GuidedGate.controls, options, "submission failure");
  await waitForSubmission(page, "failure", options, "failed result");
  await setHarnessMode(page, "success");
  await pointerStableStep(page, canvas, 0.36, 0.82, GuidedGate.controls, options, "submission success");
  const completion = await waitForSubmission(page, "success", options, "successful result");
  await pointerStableStep(page, canvas, 0.64, 0.82, GuidedGate.transition, options, "retry");
  await pointerStableStep(page, canvas, 0.50, 0.60, GuidedGate.transition, options, "fresh mode selection");
  return { ...pathContract, completion, mobile, desktop };
}

async function verifyCanvasContract(page, frame, canvas, viewport) {
  const previousViewport = page.viewportSize();
  const beforeComposition = await regionHashes(page, canvas);
  const viewportChanged = !previousViewport || previousViewport.width !== viewport.width ||
    previousViewport.height !== viewport.height;
  await useFullscreenHarness(page, viewport);
  await frame.waitForFunction(expected => {
    const canvas = document.querySelector("#unity-canvas");
    const rect = canvas && canvas.getBoundingClientRect();
    return rect && rect.width > 0 && rect.height > 0 &&
      Math.abs(innerWidth - expected.width) < 1 && Math.abs(innerHeight - expected.height) < 1;
  }, viewport, { timeout: 10_000 });
  const sizingDeadline = Date.now() + 10_000;
  let sizingSignature = null;
  let sizingStableSamples = 0;
  while (Date.now() < sizingDeadline && sizingStableSamples < 3) {
    const sizing = await frame.evaluate(() => {
      const canvas = document.querySelector("#unity-canvas");
      const stage = document.querySelector("#unity-stage");
      if (!canvas || !stage || canvas.clientWidth <= 0 || canvas.clientHeight <= 0) return null;
      const scale = window.AgrovatorLayout.renderScale(window.devicePixelRatio || 1);
      const ready = Boolean(stage.style.width && stage.style.height) &&
        Math.abs((canvas.width / canvas.clientWidth) - scale) <= 0.05 &&
        Math.abs((canvas.height / canvas.clientHeight) - scale) <= 0.05;
      return {
        ready,
        signature: `${stage.style.width}|${stage.style.height}|${canvas.clientWidth}|` +
          `${canvas.clientHeight}|${canvas.width}|${canvas.height}|${scale}`,
      };
    });
    if (sizing?.ready) {
      sizingStableSamples = sizing.signature === sizingSignature ? sizingStableSamples + 1 : 1;
      sizingSignature = sizing.signature;
    } else {
      sizingStableSamples = 0;
      sizingSignature = null;
    }
    if (sizingStableSamples < 3) {
      await new Promise(resolveWait => setTimeout(resolveWait, 80));
    }
  }
  if (sizingStableSamples < 3) throw new Error("Canvas CSS/backing sizing did not settle.");
  await waitForStableRegionChange(page, canvas, beforeComposition,
    { content: viewportChanged, controls: viewportChanged }, 10_000, "responsive Unity composition");
  await canvas.focus();
  const metrics = await frame.evaluate(() => {
    const canvas = document.querySelector("#unity-canvas");
    const stage = document.querySelector("#unity-stage");
    const rect = canvas.getBoundingClientRect();
    const stageRect = stage.getBoundingClientRect();
    const renderScale = window.AgrovatorLayout.renderScale(window.devicePixelRatio || 1);
    return {
      css: { width: rect.width, height: rect.height },
      backing: { width: canvas.width, height: canvas.height },
      dpr: window.devicePixelRatio || 1,
      renderScale,
      ratios: { x: canvas.width / rect.width, y: canvas.height / rect.height },
      contained: rect.left >= stageRect.left - 0.5 && rect.top >= stageRect.top - 0.5 &&
        rect.right <= stageRect.right + 0.5 && rect.bottom <= stageRect.bottom + 0.5,
      horizontalOverflow: document.documentElement.scrollWidth - document.documentElement.clientWidth,
      canvasFocused: document.activeElement === canvas,
    };
  });
  const outerOverflow = await page.evaluate(() =>
    document.documentElement.scrollWidth - document.documentElement.clientWidth);
  if (Math.abs(metrics.ratios.x - metrics.renderScale) > 0.05 ||
      Math.abs(metrics.ratios.y - metrics.renderScale) > 0.05) {
    throw new Error(`Canvas backing/CSS metrics did not match render scale: ${JSON.stringify(metrics)}.`);
  }
  if (!metrics.contained || !metrics.canvasFocused || metrics.horizontalOverflow > 0.5 || outerOverflow > 0.5) {
    throw new Error(`Canvas containment/focus/overflow contract failed: ${JSON.stringify({ metrics, outerOverflow })}`);
  }
  return { viewport, ...metrics, outerHorizontalOverflow: outerOverflow };
}

async function runBrowser(playwright, definition, server, options) {
  const startedAt = new Date().toISOString();
  const result = {
    browser: definition.name,
    executablePath: definition.path,
    status: "not-run",
    startedAt,
    headless: options.headless,
    loadDurationMs: null,
    version: null,
    desktop: null,
    mobile: null,
    consoleErrors: [],
    pageErrors: [],
    warnings: [],
    completionSummary: null,
    modes: { success: false, failure: false, missingConfig: false, expired: "not-exercised" },
    interactionContract: definition.name === "chrome"
      ? "Primary path is keyboard-only from Title through fresh Mode Selection; every named action waits for state-specific localized content/control regions stable across three samples."
      : definition.name === "edge"
        ? "Secondary path is pointer-only from Title through fresh Mode Selection; every named action waits for state-specific localized content/control regions stable across three samples."
        : "Availability and canvas sizing only; no guided flow coverage claimed.",
    screenshot: null,
  };
  if (!definition.path || !existsSync(definition.path)) {
    result.status = "unavailable";
    result.reason = "No executable found at the standard Windows installation paths.";
    return result;
  }

  let browser = null;
  let page = null;
  try {
    const engine = playwright[definition.engine];
    browser = await engine.launch({
      executablePath: definition.path,
      headless: options.headless,
      args: definition.engine === "chromium" ? ["--autoplay-policy=user-gesture-required"] : [],
    });
    result.version = browser.version();
    page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
    page.setDefaultTimeout(options.timeoutMs);
    page.on("console", message => {
      const location = message.location()?.url ? ` (${message.location().url})` : "";
      const text = sanitizeText(`${message.text()}${location}`);
      if (message.type() === "error") result.consoleErrors.push(text);
      else if (message.type() === "warning") result.warnings.push(text);
    });
    page.on("pageerror", error => result.pageErrors.push(sanitizeText(error.message)));

    const loadStarted = Date.now();
    await page.goto(`${server.baseUrl}/WebHarness/index.html`, { waitUntil: "domcontentloaded" });
    await page.waitForFunction(() => [...document.querySelectorAll("#progress li")]
      .some(item => item.textContent.includes("Embedded bridge ready")), null,
      { timeout: options.unityTimeoutMs });
    result.loadDurationMs = Date.now() - loadStarted;

    const frame = page.frames().find(candidate => candidate.url().includes("/Build/WebGL/index.html"));
    if (!frame) throw new Error("Hosted Unity iframe was not found.");
    const canvas = frame.locator("#unity-canvas");
    await frame.locator("#unity-loading").waitFor({ state: "hidden", timeout: options.unityTimeoutMs });
    await canvas.waitFor({ state: "visible", timeout: options.timeoutMs });

    result.desktop = await verifyCanvasContract(page, frame, canvas, { width: 1440, height: 1000 });
    const pathResult = await runPrimaryKeyboardPath(definition, page, frame, canvas, options) ||
      await runSecondaryPointerPath(definition, page, frame, canvas, options);
    if (!pathResult) {
      result.status = "not-covered";
      result.reason = "Browser is available, but Task 8 assigns complete guided paths only to Chrome and Edge.";
      return result;
    }
    result.desktop = pathResult.desktop;
    result.mobile = pathResult.mobile;
    result.completionSummary = pathResult.completion;
    result.modes.failure = true;
    result.modes.success = true;
    result.modes.missingConfig = await verifyMissingConfigRecovery(page, options);
    result.screenshot = `artifacts/smoke/${definition.name}-${pathResult.mode.toLowerCase()}-results.png`;
    if (result.consoleErrors.length || result.pageErrors.length) {
      throw new Error(`Browser emitted ${result.consoleErrors.length} console errors and ${result.pageErrors.length} page errors.`);
    }
    result.status = "passed";
  } catch (error) {
    result.status = /executable|browser.*closed|patched/i.test(String(error.message)) ? "incompatible" : "failed";
    result.reason = sanitizeText(error.stack || error.message);
    if (page) {
      try {
        const failurePath = join(options.outputDirectory, `${definition.name}-failure.png`);
        await page.screenshot({ path: failurePath, fullPage: true });
        result.screenshot = failurePath.substring(projectRoot.length + 1).replaceAll("\\", "/");
      } catch {}
    }
  } finally {
    if (browser) await browser.close();
    result.finishedAt = new Date().toISOString();
    writeFileSync(join(options.outputDirectory, `${definition.name}-smoke.json`), `${JSON.stringify(result, null, 2)}\n`);
  }
  return result;
}

async function main() {
  const options = parseArguments(process.argv.slice(2));
  mkdirSync(options.outputDirectory, { recursive: true });
  const resolvedPlaywrightModule = options.playwrightModule || findBundledPlaywright();
  const playwright = await loadPlaywright(resolvedPlaywrightModule);
  const definitions = browserDefinitions(options.browser, options.executablePath);
  const server = await startServer(options);
  const matrix = {
    generatedAt: new Date().toISOString(),
    baseUrl: server.baseUrl,
    playwrightVersion: null,
    results: [],
    server: { pid: server.child?.pid || null, stopped: null, stderr: [] },
  };
  try {
    const packagePath = resolvedPlaywrightModule && existsSync(resolvedPlaywrightModule)
      ? join(dirname(resolvedPlaywrightModule), "package.json")
      : null;
    if (packagePath && existsSync(packagePath)) matrix.playwrightVersion = JSON.parse(readFileSync(packagePath, "utf8")).version;
    for (const definition of definitions) {
      matrix.results.push(await runBrowser(playwright, definition, server, options));
    }
  } finally {
    matrix.server.stopped = await stopServer(server);
    matrix.server.stderr = server.stderr.filter(Boolean);
    writeFileSync(join(options.outputDirectory, "matrix.json"), `${JSON.stringify(matrix, null, 2)}\n`);
  }

  const requiredFailure = matrix.results.some(result => result.browser === "chrome" && result.status !== "passed");
  const availableFailure = matrix.results.some(result =>
    !["passed", "unavailable", "incompatible", "not-covered"].includes(result.status));
  if (!matrix.server.stopped || requiredFailure || availableFailure) process.exitCode = 1;
  console.log(JSON.stringify({
    outputDirectory: options.outputDirectory,
    serverStopped: matrix.server.stopped,
    results: matrix.results.map(({ browser, version, status, loadDurationMs, screenshot, reason }) =>
      ({ browser, version, status, loadDurationMs, screenshot, reason })),
  }, null, 2));
}

main().catch(error => {
  console.error(sanitizeText(error.stack || error.message));
  process.exitCode = 1;
});
