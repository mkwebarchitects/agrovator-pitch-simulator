# Build and Deploy the WebGL Player

## Current status

Unity `6000.5.3f1` with WebGL Build Support is required. The default build order is `Assets/Scenes/Bootstrap.unity`, then `Assets/Scenes/Game.unity`; `WebIntegrationTest.unity` is diagnostic only. Task 18 produced a reproducible development build through `tools/Build-WebGL.ps1`. No browser result is claimed yet; browser and manual acceptance remain Task 19.

## Build procedure

1. From the repository root, run `powershell -ExecutionPolicy Bypass -File tools/Build-WebGL.ps1`.
2. Confirm the wrapper exits zero, scans the complete Unity log, and produces `Build/WebGL/index.html` plus loader/data/framework/wasm artifacts.
3. Serve the repository over HTTP; do not open the harness or player with a `file:` URL.
4. Open `WebHarness/index.html` through that server. Its iframe targets `../Build/WebGL/index.html`.
5. Exercise all four harness modes and the manual matrix in [QA](13-QA-PLAN.md).

## Measured Task 18 development build

On 2026-07-14, the approved wrapper completed in `377.897` seconds. Unity's `BuildReport` reported `Succeeded`, `92,354,975` bytes, `00:05:44.6730968`, zero build warnings and zero build errors. The complete log contained zero compiler errors, compilation failures, `BuildFailedException`, failed-result markers, unhandled exceptions or batch-abort markers. It did contain two non-build Unity Services network diagnostics (`Curl error 28`) for unreachable analytics/configuration endpoints; they did not affect the player result.

The six emitted files total exactly `92,354,975` bytes:

| Artifact | Bytes |
| --- | ---: |
| `Build/WebGL/index.html` | 4,086 |
| `Build/WebGL/TemplateData/style.css` | 2,571 |
| `Build/WebGL/Build/WebGL.loader.js` | 58,622 |
| `Build/WebGL/Build/WebGL.framework.js` | 711,897 |
| `Build/WebGL/Build/WebGL.data` | 9,288,984 |
| `Build/WebGL/Build/WebGL.wasm` | 82,288,815 |

The development configuration intentionally selects Gzip plus decompression fallback as a same-origin local-harness setting, but this Development build emitted plain `.js`, `.data` and `.wasm` files. Uncompressed payload bytes are `92,348,318`; no `.gz`, `.br` or `.unityweb` artifact was produced, so a compressed-byte total is not applicable. Production compression and hosting headers remain a later deployment decision.

## Hosting requirements to confirm

Use HTTPS in production. Configure correct WebAssembly/content types, compression matching build output, cache-busting for versioned assets, no-cache or short caching for the launch HTML, same-origin framing (or deliberately revise the allowlist contract), CSP/frame policy, and rollback to a known build. These are operator requirements, not confirmed production settings.

## Troubleshooting

If the wrapper reports the scheduled method is absent, complete Task 18. If the iframe stays blank, inspect HTTP status/MIME/compression and browser console without logging launch/completion payloads. If launch is missing, choose Success and Resend Launch. See [operations](14-OPERATIONS-TROUBLESHOOTING.md) and [LMS discovery](00-LMS-DISCOVERY.md).
