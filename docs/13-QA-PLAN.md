# Verify the Vertical Slice

## Automated baseline

The fresh UI-polish checkpoint is EditMode `310/310` and PlayMode `39/39`, with zero failures, skips, or inconclusive tests. NUnit reported `2.3198379 s` and `2.3781601 s`; the canonical wrappers completed in approximately `51 s` and `49.4 s`. Both complete logs contained zero `error CS`, compilation-failure, or unhandled-exception markers. The companion Node source/layout suite passed `11/11`. Run:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Run-UnityTests.ps1 -Platform EditMode
powershell -ExecutionPolicy Bypass -File tools/Run-UnityTests.ps1 -Platform PlayMode
```

The wrappers write XML under `artifacts/test-results` and logs under `artifacts/logs`. Counts can change when tests are added; report fresh XML and complete-log scans rather than repeating this baseline after changes.

## Fresh WebGL build checkpoint

The 2026-07-15 UI-polish development build wrapper exited zero in `395.3 s`. BuildReport was `Succeeded`, `92,374,202` bytes (`88.09 MiB`), and `00:06:03.3210118`, with zero build warnings and zero build errors. Seven files were emitted: `index.html` `5,505`; `style.css` `2,717`; `layout.js` `1,115`; loader `58,622`; framework `711,897`; data `9,293,377`; and wasm `82,300,969` bytes. Re-run `tools/Build-WebGL.ps1` before a later release decision; this local development artifact is not a hosted production build.

## Final local browser matrix

Run the loopback server contract and automated matrix from the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Serve-WebGL.ps1 -SelfTest
node tools/smoke-webgl.mjs --browser matrix --headless --output-dir artifacts/smoke
```

On 2026-07-15 (Asia/Kuala_Lumpur), the self-test passed its harness/build HTML, CSS, JavaScript, data, WebAssembly, favicon, HEAD, traversal, missing-file and method checks. After the final review repairs, Playwright `1.61.1` produced this fresh Windows matrix:

| Browser | Version | Result | Unity ready | Evidence |
| --- | --- | --- | ---: | --- |
| Chrome | `150.0.7871.116` | Passed | `8.901 s` | `artifacts/smoke/chrome-smoke.{json,png}` plus `chrome-tutorial.png` and `chrome-pitch.png` |
| Edge | `150.0.4078.65` | Passed | `8.244 s` | `artifacts/smoke/edge-smoke.{json,png}` |
| Firefox | Not installed at either standard Windows path | Unavailable | — | `artifacts/smoke/matrix.json` |
| Safari | Not available on Windows | Unverified | — | Requires macOS |

Chrome and Edge each used the hosted harness and iframe, completed all three dedicated Tutorial pages plus six scored questions, used a pointer tutorial response and keyboard scored responses, reached Results, observed Failure then successful resubmission, retried from Complete, used Tutorial Skip with pointer only, proved the downstream practice flow through a fresh Question 1 reveal, and recovered from Missing Config through Success plus Resend. The final matrix used measured centered-layout coordinates: Continue `(0.50,0.86)`, practice response `(0.50,0.73)`, and Retry Tutorial Skip `(0.40,0.79)`, each with `120 ms` dwell. Both browsers recorded zero console errors and zero page errors; their complete smoke runs took `38.362 s` and `37.439 s`. The loopback server stopped with zero stderr. Expired submission was not exercised because Success/Failure/Missing Config provide the deterministic smoke contract.

At desktop `1440x1000`, both browsers recorded an iframe viewport of `1006x720` and a contained `978x548.375` canvas. At the `390x844` viewport, both recorded a `324x218` iframe and `280x156.625` canvas at aspect `1.7877094972`; the iframe therefore retained `44px` horizontal and `61.375px` vertical spare space, with no canvas overflow. These are responsive pointer/layout metrics, not native-touch evidence.

The fresh PNGs were inspected with image tooling, not accepted by filename. `chrome-tutorial.png` shows a centered `920x560` page-one card, readable wrapped copy, compact `180/180/420px` navigation, a disabled Back state, and no clipping. `chrome-pitch.png` shows the centered contained PitchRoom card, readable prompt and three wrapped responses, aligned metrics, and no clipping. `chrome-smoke.png` and `edge-smoke.png` show visually equivalent centered Briefing cards after recovery, bounded `520px` actions, consistent spacing, and the visible gold canvas focus outline. No horizontal overflow was visible. The smoke intentionally creates only Chrome tutorial/pitch intermediate checkpoints; Edge has a final smoke PNG but no literal Edge tutorial/pitch equivalents, so no such files are claimed.

The warning collection is non-empty and must remain visible: each passing browser recorded 12 warnings. Eight unique null cue slots were reached (`MusicLoop`, `ButtonPress`, `ResponseSelected`, `JudgeReaction`, `FeedbackOpen`, `ResultsReveal`, `CompletionFailure`, and `CompletionSuccess`); repeated Start/reload paths produced the remaining duplicate cue warnings. `TimerWarning` is covered by its once-per-question EditMode tests because the smoke responds immediately. Each browser also recorded the existing non-root `DontDestroyOnLoad` Bootstrapper warning twice, including after iframe reload.

The 2026-07-15 in-app Browser pass found the default harness visually clean with no horizontal overflow; its `831x720` iframe contained a 16:9 canvas and rendered Title. Before Start there was no app audio warning. Pointer Start reached Briefing and only then emitted the expected missing-`ButtonPress` warning, confirming the user-gesture hook and no autoplay/no blocking with null clips; audible content and human hearing remain unverified. Reload returned Launch configuration sent, bridge ready, frame loaded and a fresh Unity Title.

At a `390x844` override, client width was `375`, the iframe was `309x218.39`, horizontal overflow remained absent, and the measured canvas Start pointer reached Briefing. The in-app Browser API cannot emit native touch events, so this proves touch-sized pointer layout/input only, not real touch hardware. The uniquely located fullscreen control was exercised, but the controlled browser denied it with `TypeError: Permissions check failed`; `fullscreenElement` remained false. Record this as an environment limitation, not fullscreen success. A URL-less MutationObserver injection error came from the in-app tooling layer and was absent from product code and from both automated Chrome/Edge page-error captures.

Firefox, Safari, native touch, fullscreen in an unrestricted browser, reduced-motion/all-timer combinations, qualified Malay review, assistive-technology review and audible final clips remain unverified unless separately evidenced.

## Content and human review

Play every branch including recovery/timeouts; verify pedagogy, no answer-length/order cue, child suitability, English, qualified Malay review, accessibility with relevant assistive technology, visual quality, audio rights/loudness, privacy/security, and production LMS contract.

## Defect evidence

Record environment, commit, exact command/actions, expected/actual result, sanitized screenshot/log excerpt, severity, owner, and retest. Never fabricate a pass or include a launch/completion payload. See [acceptance](18-VERTICAL-SLICE-ACCEPTANCE.md) and [operations](14-OPERATIONS-TROUBLESHOOTING.md).
