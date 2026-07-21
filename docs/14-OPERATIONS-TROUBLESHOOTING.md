# Run and Troubleshoot the Local Integration

## Before you start

Install Unity 6000.5.3f1 with WebGL support. The implemented Task 18 wrapper produces the ignored development artifact at `Build/WebGL/index.html`. Use the repository's loopback server or an approved HTTP server rooted at the repository; keep the player and harness same-origin.

## Local operator flow

1. Build with `tools/Build-WebGL.ps1` and confirm its exit status, BuildReport, complete log scan, and artifact inventory.
2. Run `tools/Serve-WebGL.ps1 -SelfTest` and/or start the local server for the browser matrix; record its command/port and verify shutdown.
3. Open `/WebHarness/index.html`.
4. Select Success for the normal flow. Use Failure/Expired to check retained results and resubmission. Use Missing Config, then Success plus Resend Launch, to check recovery.
5. Record browser/version and sanitized outcomes.

## Common problems

### Build output is absent

Read the complete build log and check the editor version, WebGL module, `Agrovator.PitchSimulator.Editor.WebGlBuild.BuildDevelopment` entry point, enabled scenes, and available disk space. Do not reuse an old artifact as fresh evidence.

### Embedded build does not connect

Confirm HTTP rather than `file:`, the iframe path, response MIME/compression, same origin, and no CSP/frame block. Do not change target origin to wildcard.

### Completion does not resolve

Confirm harness mode, request ID shape, and exact protocol version/message names. A native pending request is canceled after 30 seconds; use the UI retry rather than replaying a stale callback.

### No sound

Current clips are placeholders. With future clips, select Start first, verify mute/volumes, and check browser autoplay policy.

### A learner is sent a non-English locale

The game ships in English only. Any launch locale resolves to English rather than a
missing token. Malay was removed on 2026-07-21 because a catalog of English strings
labelled Malay claimed a translation that did not exist.

Escalate with sanitized evidence only. See [deployment](10-WEB-DEPLOYMENT.md), [security](12-PRIVACY-SECURITY.md), and [QA](13-QA-PLAN.md).
