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

async function waitForCanvasChange(canvas, previousHash, timeoutMs, label) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const current = await canvasHash(canvas);
    if (current !== previousHash) return current;
    await new Promise(resolveWait => setTimeout(resolveWait, 100));
  }
  throw new Error(`Canvas did not visibly change after ${label}.`);
}

async function keyboardAction(page, canvas, key, timeoutMs, label, expectControlChange = true) {
  const before = expectControlChange ? await controlRegionHash(page, canvas) : null;
  await canvas.focus();
  // Unity's frame-polled WebGL input must observe keydown before keyup.
  await canvas.press(key, { delay: 120 });
  if (!expectControlChange) {
    await new Promise(resolveWait => setTimeout(resolveWait, 180));
    return null;
  }
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const current = await controlRegionHash(page, canvas);
    if (current !== before) {
      await new Promise(resolveWait => setTimeout(resolveWait, 220));
      return current;
    }
    await new Promise(resolveWait => setTimeout(resolveWait, 100));
  }
  throw new Error(`Stable control region did not change after ${label}.`);
}

async function mouseResponse(page, canvas, timeoutMs) {
  const before = await controlRegionHash(page, canvas);
  const bounds = await canvas.boundingBox();
  if (!bounds) throw new Error("Unity canvas has no pointer bounds.");
  // Measured generated 1280x720 contract: tutorial response spans ~73%-84% canvas height.
  await canvas.click({ position: { x: bounds.width * 0.5, y: bounds.height * 0.78 } });
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const current = await controlRegionHash(page, canvas);
    if (current !== before) {
      await new Promise(resolveWait => setTimeout(resolveWait, 220));
      return current;
    }
    await new Promise(resolveWait => setTimeout(resolveWait, 100));
  }
  throw new Error("Stable control region did not change after mouse response selection.");
}

async function mouseContinue(page, canvas, timeoutMs, label, expectControlChange = false) {
  const before = expectControlChange ? await controlRegionHash(page, canvas) : null;
  const bounds = await canvas.boundingBox();
  if (!bounds) throw new Error("Unity canvas has no Continue-control bounds.");
  // Generated layout contract: active pitch-room Continue spans the bottom control row.
  await canvas.click({ position: { x: bounds.width * 0.5, y: bounds.height * 0.91 } });
  if (!expectControlChange) {
    await new Promise(resolveWait => setTimeout(resolveWait, 180));
    return null;
  }
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const current = await controlRegionHash(page, canvas);
    if (current !== before) {
      await new Promise(resolveWait => setTimeout(resolveWait, 220));
      return current;
    }
    await new Promise(resolveWait => setTimeout(resolveWait, 100));
  }
  throw new Error(`Stable control region did not change after ${label}.`);
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

async function playAttempt(page, frame, canvas, options) {
  const titleBounds = await canvas.boundingBox();
  if (!titleBounds) throw new Error("Unity canvas has no Title-control bounds.");
  const titleBefore = await canvasHash(canvas);
  await canvas.click({ position: { x: titleBounds.width * 0.5, y: titleBounds.height * 0.61 } });
  await waitForCanvasChange(canvas, titleBefore, options.timeoutMs, "Title pointer Start");
  await new Promise(resolveWait => setTimeout(resolveWait, 300));
  const briefingBounds = await canvas.boundingBox();
  if (!briefingBounds) throw new Error("Unity canvas has no Briefing-control bounds.");
  const briefingBefore = await canvasHash(canvas);
  await canvas.click({ position: { x: briefingBounds.width * 0.5, y: briefingBounds.height * 0.66 } });
  await waitForCanvasChange(canvas, briefingBefore, options.timeoutMs, "Briefing pointer Continue");
  await new Promise(resolveWait => setTimeout(resolveWait, 300));

  await mouseContinue(page, canvas, options.timeoutMs, "Tutorial");
  await mouseContinue(page, canvas, options.timeoutMs, "Judge introduction");
  await mouseContinue(page, canvas, options.timeoutMs, "Tutorial response reveal", true);
  await mouseResponse(page, canvas, options.timeoutMs);
  await mouseContinue(page, canvas, options.timeoutMs, "Tutorial reaction");
  await mouseContinue(page, canvas, options.timeoutMs, "Tutorial feedback");
  await mouseContinue(page, canvas, options.timeoutMs, "Question 1 response reveal", true);

  for (let question = 1; question <= 6; question += 1) {
    await keyboardAction(page, canvas, "Enter", options.timeoutMs, `question ${question} keyboard response`);
    await mouseContinue(page, canvas, options.timeoutMs, `question ${question} reaction`);
    await mouseContinue(page, canvas, options.timeoutMs,
      question === 6 ? "question 6 results transition" : `question ${question} feedback`, question === 6);
    if (question < 6) {
      await mouseContinue(page, canvas, options.timeoutMs, `question ${question + 1} response reveal`, true);
    }
  }

  await page.locator("#mode").selectOption("failure");
  await keyboardAction(page, canvas, "Enter", options.timeoutMs, "failed completion submit", false);
  await page.waitForFunction(() => document.querySelector("#progress li")?.textContent.includes("failure"), null,
    { timeout: options.timeoutMs });

  await page.locator("#mode").selectOption("success");
  await keyboardAction(page, canvas, "Enter", options.timeoutMs, "successful completion resubmit", false);
  await page.waitForFunction(() => document.querySelector("#progress li")?.textContent.includes("success"), null,
    { timeout: options.timeoutMs });
  const completion = await page.locator("#completion").innerText();
  if (!completion.includes("Completed") || !completion.includes("Overall score")) {
    throw new Error("Sanitized completion summary was not rendered after successful resubmit.");
  }

  await keyboardAction(page, canvas, "Enter", options.timeoutMs, "Results Retry");
  await keyboardAction(page, canvas, "Enter", options.timeoutMs, "retry Briefing Continue");
  await keyboardAction(page, canvas, "Enter", options.timeoutMs, "retry Tutorial", false);
  return sanitizeText(completion);
}

async function verifyMissingConfigRecovery(page, options) {
  await page.locator("#mode").selectOption("missing-config");
  const frameElement = page.locator("#game");
  await frameElement.evaluate(element => element.contentWindow.location.reload());
  await page.waitForFunction(() => document.querySelector("#connection")?.textContent.includes("Missing Config"), null,
    { timeout: options.unityTimeoutMs });
  const frame = page.frames().find(candidate => candidate.url().includes("/Build/WebGL/index.html"));
  if (!frame) throw new Error("Reloaded Unity iframe was not found.");
  const canvas = frame.locator("#unity-canvas");
  await frame.locator("#unity-loading").waitFor({ state: "hidden", timeout: options.unityTimeoutMs });
  const waitingHash = await canvasHash(canvas);
  await page.locator("#mode").selectOption("success");
  await page.locator("#resend").click();
  await page.waitForFunction(() => document.querySelector("#connection")?.textContent.includes("Launch configuration sent"), null,
    { timeout: options.timeoutMs });
  await waitForCanvasChange(canvas, waitingHash, options.timeoutMs, "Success plus Resend recovery");
  const recoveredBounds = await canvas.boundingBox();
  if (!recoveredBounds) throw new Error("Recovered Unity canvas has no Title-control bounds.");
  const recoveredBefore = await canvasHash(canvas);
  await canvas.click({ position: { x: recoveredBounds.width * 0.5, y: recoveredBounds.height * 0.61 } });
  await waitForCanvasChange(canvas, recoveredBefore, options.timeoutMs, "recovered Title pointer Start");
  return true;
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
    interactionContract: "Measured Start pointer at (0.50, 0.61), Briefing pointer at (0.50, 0.66); pitch-room Continue pointer at (0.50, 0.91) x3 to tutorial; tutorial pointer response at (0.50, 0.78); Continue x3 to Q1; six focused Enter responses held 120ms; Continue x3 between Q1-Q5 and x2 after Q6 to Results. Stable lower-control-region changes plus 220ms settle gate observable swaps; 180ms bounded waits cover internal reaction/feedback transitions.",
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

    result.desktop = await canvasMetrics(page, frame, canvas, { width: 1440, height: 1000 });
    result.mobile = await canvasMetrics(page, frame, canvas, { width: 390, height: 844 });
    await canvasMetrics(page, frame, canvas, { width: 1440, height: 1000 });

    result.completionSummary = await playAttempt(page, frame, canvas, options);
    result.modes.failure = true;
    result.modes.success = true;
    result.modes.missingConfig = await verifyMissingConfigRecovery(page, options);

    const screenshotPath = join(options.outputDirectory, `${definition.name}-smoke.png`);
    await page.screenshot({ path: screenshotPath, fullPage: true });
    result.screenshot = screenshotPath.substring(projectRoot.length + 1).replaceAll("\\", "/");
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
  const availableFailure = matrix.results.some(result => !["passed", "unavailable", "incompatible"].includes(result.status));
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
