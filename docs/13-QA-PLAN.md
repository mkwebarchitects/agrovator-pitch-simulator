# Verify the Guided Pitch Builder

## Canonical acceptance commands

Run these from the repository root:

```powershell
& .\tools\Run-UnityTests.ps1 -Platform EditMode
& .\tools\Run-UnityTests.ps1 -Platform PlayMode
node --check .\tools\smoke-webgl.mjs
node --test .\tools\tests\*.test.js
& .\tools\Build-WebGL.ps1
& .\tools\Serve-WebGL.ps1 -SelfTest
node .\tools\smoke-webgl.mjs
```

Parse the Unity XML `test-run` root and inspect each complete log. Do not treat Unity's process exit code alone as a pass. Keep XML, logs, builds, JSON, and screenshots ignored.

## Fresh 2026-07-19 automated baseline

| Check | Fresh result |
| --- | --- |
| EditMode | XML `Passed`, `370/370`, zero failures/skips/inconclusive, `2.6572753 s`; complete `897`-line log, zero configured compile/exception/failure markers |
| PlayMode | XML `Passed`, `48/48`, zero failures/skips/inconclusive, `1.760119 s`; complete `890`-line log, zero configured compile/exception/failure markers |
| JavaScript | `smoke-webgl.mjs` syntax passed; Node contracts `17/17`, zero failures/skips/todos |
| Content fixture | `GuidedPitchContentTests` `18/18` |
| Localization fixture | `LocalizationTests` `27/27` |
| LMS/reflection fixture | `LmsPayloadTests` `92/92` |
| Active composition fixture | `BootstrapPlayModeTests` `9/9` |
| Guided flow fixture | `GuidedPitchFlowPlayModeTests` `8/8` |

The focused fixtures cover the exact two-mode routes, `30` stable unique option IDs, every content/localization key, 12-16/32-word rules, `319/319` English/Malay parity, Malay `pending_human_review`, one active v2 Bootstrap reference, DTO/reflection/privacy shape, both learner modes, four Build rounds, revision, Present, follow-up, Results, keyboard/focus, safe fallback, submission preservation, and Retry reset.

## Fresh WebGL build and server

BuildReport was `Succeeded`, `92,631,312` bytes, `00:00:02.0019569`, zero warnings, and zero errors. The complete `626`-line build log had zero configured failure markers. The seven-file artifact was:

| File | Bytes |
| --- | ---: |
| `Build/WebGL/index.html` | 5,972 |
| `Build/WebGL/TemplateData/style.css` | 2,730 |
| `Build/WebGL/TemplateData/layout.js` | 1,077 |
| `Build/WebGL/Build/WebGL.loader.js` | 58,622 |
| `Build/WebGL/Build/WebGL.framework.js` | 712,694 |
| `Build/WebGL/Build/WebGL.data` | 9,030,172 |
| `Build/WebGL/Build/WebGL.wasm` | 82,820,045 |

`Serve-WebGL.ps1 -SelfTest` passed on temporary port `58382` for GET/HEAD, MIME, traversal, missing paths, and rejected methods.

## Fresh local browser matrix

The final matrix used Playwright `1.61.1` and temporary port `63464`. An earlier acceptance run had claimed missing-configuration recovery without executing it; `runBrowser` now invokes and records `verifyMissingConfigRecovery`, three Node source contracts keep that step reachable, hidden-control-safe, and press-retried, and the recovered Title Start retries held `120 ms` presses until Briefing content replaces Title content because Unity's frame-polled input can miss a single press during relaunch stalls.

| Browser | Assigned route | Version | Start / finish (UTC) | Load | Result |
| --- | --- | ---: | --- | ---: | --- |
| Chrome | Primary, keyboard-only | `150.0.7871.127` | `15:05:05.793` / `15:05:52.988` | `7,654 ms` | Passed; zero console/page errors |
| Edge | Secondary, pointer-only | `150.0.4078.83` | `15:05:52.988` / `15:06:50.264` | `7,647 ms` | Passed; zero console/page errors |
| Firefox | Availability only | unavailable | - | - | No standard-path executable; no pass claimed |

Each passing route covered Title, Briefing, fresh mode selection, Learn, all four choice/feedback rounds, one revision, Present, cost follow-up, Results, forced submission failure, successful resubmission, Retry with fresh mode selection, executed and recorded missing-configuration recovery (`modes.missingConfig: true` in both browser JSON artifacts), and completion (`100` overall, `6` competencies, `0` timeouts). The server stopped cleanly with zero stderr.

Desktop was viewport `1440x1000` with CSS/backing `1276x918`. Mobile was viewport `390x844` with CSS/backing `380x783`. DPR/render scale and backing/CSS ratios were `1`; containment and canvas focus were true with zero inner/outer horizontal overflow. This is responsive pointer/layout evidence, not native-touch or higher-DPR runtime evidence.

Each browser recorded twelve disclosed non-error warnings. Missing-configuration recovery reloads the player for a second boot, so the root-only `DontDestroyOnLoad`, empty `MusicLoop`, and empty `ButtonPress` warnings appear twice each, alongside single empty-slot warnings for `ResponseSelected`, `JudgeReaction`, `FeedbackOpen`, `ResultsReveal`, `CompletionFailure`, and `CompletionSuccess`. No new warning class appeared. Final licensed/audible audio is absent.

## Original-detail visual review

All eleven required fresh guided screenshots were opened at original detail:

- `artifacts/smoke/chrome-primary-mode.png`
- `artifacts/smoke/chrome-primary-build.png`
- `artifacts/smoke/chrome-primary-improve.png`
- `artifacts/smoke/chrome-primary-present.png`
- `artifacts/smoke/chrome-primary-results.png`
- `artifacts/smoke/chrome-mobile-compact.png`
- `artifacts/smoke/edge-secondary-mode.png`
- `artifacts/smoke/edge-secondary-build.png`
- `artifacts/smoke/edge-secondary-present.png`
- `artifacts/smoke/edge-secondary-results.png`
- `artifacts/smoke/edge-mobile-compact.png`

The review confirmed contained point-filtered garden art, opaque navy lesson surfaces, cream selectable cards, persistent part icons/colours, visible gold focus, complete Primary/Secondary mode cards in compact view, readable wide Build/Improve copy, four complete Present sentences in both modes, complete final pitches including Secondary `beds.`, and fixed submission/Retry actions. The longer Secondary Results content remains vertically scrollable while the fixed actions stay visible.

## Reconciliation and hygiene

- Asset/meta: `142` logical files plus `40` logical directories (`182` entries) and `182` matching `.meta` files; zero missing and zero orphaned.
- Content/localization: one active content-v2 asset, `30` exact stable unique options, every route/key resolved, `319/319` exact English/Malay key and guided fallback-value parity.
- DTOs: launch `14` fields and completion `19` fields/types unchanged; source files have zero diff from the guided baseline.
- Links: `48` tracked Markdown files, `78` relative links checked, zero broken.
- Ignore rules: `Library`, `Temp`, `Logs`, `UserSettings`, `Build`, and `artifacts` are ignored. Generated ProjectSettings/editor noise was removed.
- Privacy: zero unexpected email, credential-query, secret-named-file, secret/private-key, free-text input, private learner value, response-text log, or full-payload log findings after every match was reviewed.
- Branch whitespace: the six known metadata trailing spaces were removed without GUID/content changes; `git diff --check ce7f8ac` exits zero.

## Evidence and human-review boundary

Automated tests, browser smoke, and screenshots demonstrate implementation behavior. They do not demonstrate classroom learning effectiveness. Before such a claim, Primary and Secondary educators or representative learners must review reading level, coaching tone, task length, and transfer usefulness.

Malay human review, Firefox/Safari, native touch, a real LMS, unrestricted fullscreen, legal approval, classroom use, and human accessibility/assistive-technology review remain unclaimed. The lexical source-contract extractor's treatment of calls inside constant-unreachable branches such as `if (false)` is a retained minor for final review triage; runtime smoke is the current behavioral backstop.

Independent Task 9 and whole-branch reviews are post-commit gates and are not claimed in this implementation record.

See [acceptance](18-VERTICAL-SLICE-ACCEPTANCE.md), [privacy](12-PRIVACY-SECURITY.md), and [operations](14-OPERATIONS-TROUBLESHOOTING.md).
