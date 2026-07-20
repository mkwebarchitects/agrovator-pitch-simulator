# Build and Deploy the WebGL Player

## Current status

Unity `6000.5.3f1` with WebGL Build Support is required. The default build order is `Assets/Scenes/Bootstrap.unity`, then `Assets/Scenes/Game.unity`; `WebIntegrationTest.unity` is diagnostic only. Task 18 produced a reproducible development build through `tools/Build-WebGL.ps1`, Task 19 added loopback/browser smoke coverage, and Task 20 repeated the build and Chrome/Edge matrix. These are local development results, not production hosting or browser-support approval.

## Build procedure

1. For local harness and smoke work, run `powershell -ExecutionPolicy Bypass -File tools/Build-WebGL.ps1`. **To produce the build learners download, add `-Release`.** The development player is roughly `92 MB` uncompressed; the release player is roughly `8 MB`. Never deploy the development build.
2. Confirm the wrapper exits zero, scans the complete Unity log, and produces `Build/WebGL/index.html` plus loader/data/framework/wasm artifacts. A release build writes `WebGL.wasm.unityweb`, `WebGL.data.unityweb` and `WebGL.framework.js.unityweb` alongside an uncompressed `WebGL.loader.js`, and logs to `artifacts/logs/webgl-build-release.log`.
   Release compression is Brotli behind Unity's decompression fallback, which is required because GitHub Pages serves file bytes verbatim and cannot set `Content-Encoding`; the loader decompresses in the browser instead. Do not switch the fallback off while Pages is the host.
3. Serve the repository over HTTP; do not open the harness or player with a `file:` URL.
4. Open `WebHarness/index.html` through that server. Its iframe targets `../Build/WebGL/index.html`.
5. Exercise all four harness modes and the manual matrix in [QA](13-QA-PLAN.md).

## Measured Task 18 development build

On 2026-07-14, the final review-corrected approved wrapper completed in `50.886` seconds with a warm build cache. Unity's `BuildReport` reported `Succeeded`, `92,357,339` bytes, `00:00:04.4515450`, zero build warnings and zero build errors. The complete log contained zero compiler errors, compilation failures, `BuildFailedException`, failed-result markers, unhandled exceptions or batch-abort markers. It did contain two non-build clean-shutdown diagnostics (`Curl error 42: Callback aborted`); they did not affect the player result.

The seven emitted files total exactly `92,357,339` bytes:

| Artifact | Bytes |
| --- | ---: |
| `Build/WebGL/index.html` | 5,367 |
| `Build/WebGL/TemplateData/style.css` | 2,571 |
| `Build/WebGL/TemplateData/layout.js` | 1,083 |
| `Build/WebGL/Build/WebGL.loader.js` | 58,622 |
| `Build/WebGL/Build/WebGL.framework.js` | 711,897 |
| `Build/WebGL/Build/WebGL.data` | 9,288,984 |
| `Build/WebGL/Build/WebGL.wasm` | 82,288,815 |

The development configuration intentionally selects Gzip plus decompression fallback as a same-origin local-harness setting, but this Development build emitted plain `.js`, `.data` and `.wasm` files. Uncompressed payload bytes are `92,349,401`; no `.gz`, `.br` or `.unityweb` artifact was produced, so a compressed-byte total is not applicable. Production compression and hosting headers remain a later deployment decision.

## Fresh post-review Task 20 evidence

On 2026-07-15 (`Asia/Kuala_Lumpur`), the post-review wrapper exited `0` in `81.1 s`. Unity's `BuildReport` reported `Succeeded`, `92,361,436` bytes in `00:00:53.9826352`, zero build warnings, and zero build errors. It emitted seven files: `index.html` `5,367`; `style.css` `2,571`; `layout.js` `1,083`; loader `58,622`; framework `711,897`; data `9,289,560`; and wasm `82,292,336` bytes. The plain player payload is `92,352,415` bytes; no `.gz`, `.br`, or `.unityweb` artifact was emitted.

The final Task 20 browser matrix was generated at `2026-07-14T23:33:03.670Z` (`2026-07-15 07:33:03.670` in `Asia/Kuala_Lumpur`) with Playwright `1.61.1`:

| Browser | Version | Result | Load time | Errors |
| --- | --- | --- | ---: | --- |
| Chrome | `150.0.7871.116` | Passed | `6,566 ms` | Zero console and page errors |
| Edge | `150.0.4078.65` | Passed | `6,316 ms` | Zero console and page errors |
| Firefox | Not found at standard Windows paths | Unavailable | - | Not run |
| Safari | Unavailable on Windows | Unverified | - | Not run |

Both passing runs observed launch, Success, Failure, Missing Configuration, and successful-completion Retry through a fresh Question 1 reveal. Pointer presses were held for `120 ms` so Unity's frame-polled WebGL input observed down/up across frames; the initial instantaneous-click run failed at the tutorial reveal and was retained as RED evidence before this correction. The smoke server stopped cleanly with zero stderr. Expected development warnings covered the non-root `DontDestroyOnLoad` diagnostic and null cue slots reached by the fast path; `TimerWarning` is covered by EditMode because the smoke selects responses before five seconds. Final audible content is absent. This matrix does not establish Firefox, Safari, native touch, unrestricted fullscreen, real LMS submission, classroom usability, or human accessibility approval.

## Hosting requirements to confirm

Use HTTPS in production. Configure correct WebAssembly/content types, compression matching build output, cache-busting for versioned assets, no-cache or short caching for the launch HTML, same-origin framing (or deliberately revise the allowlist contract), CSP/frame policy, and rollback to a known build. These are operator requirements, not confirmed production settings.

## Troubleshooting

If the wrapper reports the scheduled method is absent, verify the current editor version, WebGL module, and `Agrovator.PitchSimulator.Editor.WebGlBuild.BuildDevelopment` entry point. If the iframe stays blank, inspect HTTP status/MIME/compression and browser console without logging launch/completion payloads. If launch is missing, choose Success and Resend Launch. See [operations](14-OPERATIONS-TROUBLESHOOTING.md) and [LMS discovery](00-LMS-DISCOVERY.md).
