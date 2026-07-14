# Verify the Vertical Slice

## Automated baseline

The fresh Task 20 checkpoint is EditMode `300/300` in `53 s` and PlayMode `35/35` in `50.6 s`, with zero failures. Run:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Run-UnityTests.ps1 -Platform EditMode
powershell -ExecutionPolicy Bypass -File tools/Run-UnityTests.ps1 -Platform PlayMode
```

The wrappers write XML under `artifacts/test-results` and logs under `artifacts/logs`. Counts can change when tests are added; report fresh XML and complete-log scans rather than repeating this baseline after changes.

## Task 18 build checkpoint

The 2026-07-14 development build checkpoint passed: the wrapper exited zero, BuildReport was `Succeeded` with zero warnings/errors, Bootstrap/Game were the only enabled scenes, and seven expected files were recorded. Re-run `tools/Build-WebGL.ps1` before a release decision; historical build evidence is not a substitute for a fresh build.

## Task 19 browser matrix

Run the loopback server contract and automated matrix from the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Serve-WebGL.ps1 -SelfTest
node tools/smoke-webgl.mjs --browser matrix --headless --output-dir artifacts/smoke
```

On 2026-07-15 (Asia/Kuala_Lumpur), the self-test passed its harness/build HTML, CSS, JavaScript, data, WebAssembly, favicon, HEAD, traversal, missing-file and method checks. Playwright `1.61.1` then produced this fresh Windows matrix:

| Browser | Version | Result | Unity ready | Evidence |
| --- | --- | --- | ---: | --- |
| Chrome | `150.0.7871.115` | Passed | `6.957 s` | `artifacts/smoke/chrome-smoke.{json,png}` |
| Edge | `150.0.4078.65` | Passed | `7.452 s` | `artifacts/smoke/edge-smoke.{json,png}` |
| Firefox | Not installed at either standard Windows path | Unavailable | — | `artifacts/smoke/matrix.json` |
| Safari | Not available on Windows | Unverified | — | Requires macOS |

Chrome and Edge each used the hosted harness and iframe, completed the tutorial plus six scored questions, used a pointer tutorial response and keyboard scored responses, reached Results, observed Failure then successful resubmission, retried, and recovered from Missing Config through Success plus Resend. Desktop `1440x1000` and mobile-emulated `390x844` canvases stayed contained and approximately 16:9. Both recorded zero console errors and zero page errors. Expired submission was not exercised because the required Success/Failure/Missing Config modes already provide the deterministic smoke contract.

The warning collection is non-empty and must remain visible: all final audio clips are intentionally null, so the first user gesture reports the expected missing `ButtonPress` clip warning; the development build also reports that `Bootstrapper` calls `DontDestroyOnLoad` while not a root GameObject, including after iframe reload. Task 19 does not change runtime or scenes, so that lifecycle warning remains for final triage.

The 2026-07-15 in-app Browser pass found the default harness visually clean with no horizontal overflow; its `831x720` iframe contained a 16:9 canvas and rendered Title. Before Start there was no app audio warning. Pointer Start reached Briefing and only then emitted the expected missing-`ButtonPress` warning, confirming the user-gesture hook and no autoplay/no blocking with null clips; audible content and human hearing remain unverified. Reload returned Launch configuration sent, bridge ready, frame loaded and a fresh Unity Title.

At a `390x844` override, client width was `375`, the iframe was `309x218.39`, horizontal overflow remained absent, and the measured canvas Start pointer reached Briefing. The in-app Browser API cannot emit native touch events, so this proves touch-sized pointer layout/input only, not real touch hardware. The uniquely located fullscreen control was exercised, but the controlled browser denied it with `TypeError: Permissions check failed`; `fullscreenElement` remained false. Record this as an environment limitation, not fullscreen success. A URL-less MutationObserver injection error came from the in-app tooling layer and was absent from product code and from both automated Chrome/Edge page-error captures.

Firefox, Safari, native touch, fullscreen in an unrestricted browser, reduced-motion/all-timer combinations, qualified Malay review, assistive-technology review and audible final clips remain unverified unless separately evidenced.

## Content and human review

Play every branch including recovery/timeouts; verify pedagogy, no answer-length/order cue, child suitability, English, qualified Malay review, accessibility with relevant assistive technology, visual quality, audio rights/loudness, privacy/security, and production LMS contract.

## Defect evidence

Record environment, commit, exact command/actions, expected/actual result, sanitized screenshot/log excerpt, severity, owner, and retest. Never fabricate a pass or include a launch/completion payload. See [acceptance](18-VERTICAL-SLICE-ACCEPTANCE.md) and [operations](14-OPERATIONS-TROUBLESHOOTING.md).
