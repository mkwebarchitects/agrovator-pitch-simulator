# Build and Deploy the WebGL Player

## Current status

Unity `6000.5.3f1` with WebGL Build Support is required. The default build order is `Assets/Scenes/Bootstrap.unity`, then `Assets/Scenes/Game.unity`; `WebIntegrationTest.unity` is diagnostic only. At the Task 17 checkpoint, `tools/Build-WebGL.ps1` exists but its editor build implementation and `Build/WebGL/index.html` do not. Task 18 must create and verify the development build. No browser result is claimed yet.

## Build procedure after Task 18

1. From the repository root, run `powershell -ExecutionPolicy Bypass -File tools/Build-WebGL.ps1`.
2. Confirm the wrapper exits zero, scans the complete Unity log, and produces `Build/WebGL/index.html` plus loader/data/framework/wasm artifacts.
3. Serve the repository over HTTP; do not open the harness or player with a `file:` URL.
4. Open `WebHarness/index.html` through that server. Its iframe targets `../Build/WebGL/index.html`.
5. Exercise all four harness modes and the manual matrix in [QA](13-QA-PLAN.md).

## Hosting requirements to confirm

Use HTTPS in production. Configure correct WebAssembly/content types, compression matching build output, cache-busting for versioned assets, no-cache or short caching for the launch HTML, same-origin framing (or deliberately revise the allowlist contract), CSP/frame policy, and rollback to a known build. These are operator requirements, not confirmed production settings.

## Troubleshooting

If the wrapper reports the scheduled method is absent, complete Task 18. If the iframe stays blank, inspect HTTP status/MIME/compression and browser console without logging launch/completion payloads. If launch is missing, choose Success and Resend Launch. See [operations](14-OPERATIONS-TROUBLESHOOTING.md) and [LMS discovery](00-LMS-DISCOVERY.md).
