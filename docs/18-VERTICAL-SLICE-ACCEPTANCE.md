# Assess Vertical Slice Acceptance

## Task 20 decision and evidence boundary

On 2026-07-15 (`Asia/Kuala_Lumpur`), Task 20 accepted the standalone vertical-slice implementation and local development evidence for Unity `6000.5.3f1`. This is not production WebGL/LMS release approval. The external AGROVATOR LMS repository was not inspected, and real LMS submission, deployment, legal, language, audio, classroom, accessibility, browser-support, and human release decisions remain outside this acceptance.

Fresh execution evidence:

- Canonical EditMode passed `308/308` in `27.5 s`; canonical PlayMode passed `36/36` in `48.7 s`.
- `tools/Build-WebGL.ps1` exited `0` in `81.1 s`. BuildReport was `Succeeded`, `92,361,436` bytes, `00:00:53.9826352`, with zero warnings and zero errors.
- The build contained exactly seven files: `index.html` `5,367`; `style.css` `2,571`; `layout.js` `1,083`; loader `58,622`; framework `711,897`; data `9,289,560`; wasm `82,292,336` bytes.
- The matrix generated at `2026-07-14T23:33:03.670Z` (`2026-07-15 07:33:03.670` local) used Playwright `1.61.1`. Chrome `150.0.7871.116` passed in `6,566 ms`; Edge `150.0.4078.65` passed in `6,316 ms`. Both recorded zero console/page errors; launch, Success, Failure, Missing Configuration, and successful-completion Retry through a fresh Question 1 reveal were present; the server stopped cleanly with zero stderr.
- Firefox was unavailable at standard Windows paths. Safari is unavailable on Windows. Neither is claimed as passed.

The earlier Task 18 measurements remain historical evidence in [web deployment](10-WEB-DEPLOYMENT.md); they are not substituted for the fresh Task 20 build.

## Line-by-line acceptance

An independent source/test/scenario review plus the final repair review through commit `616e2ed` found direct automated or authored-data evidence for every requested implementation capability. Fresh broad suites, build, and browser smoke above provide the final execution checkpoint.

| Requirement | Task 20 status | Evidence summary |
| --- | --- | --- |
| Title | Pass | Generated Title is initially active/focused; launch-to-Title is covered by Bootstrap and session-controller tests. |
| Briefing | Pass | Start routes to Briefing and focuses Continue; session tests assert fresh tutorial state. |
| Tutorial | Pass | Exactly one zero-timer, zero-score, zero-confidence tutorial opening is authored and traversed without score/confidence change. |
| Judge | Pass | Generated PitchRoom contains configured Judge Aya with active talk-loop behavior. |
| Five-or-more scored questions | Pass | All `729` playable paths terminate with exactly six scored questions. |
| Three choices | Pass | Every scored question has exactly three respectful responses; runtime uses a fixed three-view pool. |
| Two-or-more branches | Pass | Conditional standard/recovery gates provide at least two authored branch behaviors. |
| Recovery | Pass | The unique `weak_claim_made` recovery route is tested through Results history and retry reset. |
| Timers | Pass | Authored `20`, `15`, and `12` second timers, final-five presentation, reduced-motion behavior, and neutral expiry are covered. |
| Confidence | Pass | Exact five bands, fill/art fallback, and scored confidence changes are covered. |
| Reactions | Pass | All 11 typed reactions, authored mappings, fallback, talk/blink, one-shot, and reduced-motion behavior are covered. |
| Audio hooks | Pass for hooks only | Exact nine-hook inventory, first-gesture music, response/reaction, once-per-question timer warning, feedback/results, completion routing, and null safety are covered; final audible/licensed clips are absent. |
| Scoring | Pass | Seven category caps, 100 overall clamp, rollups, confidence bounds, levels, and feedback selection are covered. |
| Results | Pass | Terminal payload, scores, confidence, rollups, two strengths, two improvements, and status rendering are covered. |
| Review | Pass | Ordered response/feedback/explanation history, fixed six-item pool, scrolling, focus, and reset behavior are covered. |
| Retry | Pass | Retry from Results or successful Complete increments the attempt while runtime state, Results view, and review state reset; browser smoke proves the fresh flow through Question 1 reveal. |
| Mock launch/completion | Pass for mock contract | Success, Failure, Expired, Missing Configuration, resend/recovery, payload construction, and browser JSON paths are covered; this is not real LMS proof. |
| WebGL build | Pass for development build | Deterministic Bootstrap/Game build contract and fresh successful seven-file artifact are recorded above. |

## Privacy, secret, and asset audit

The independent pre-record audit searched `Assets`, `WebHarness`, `docs`, `tools`, and the root release records while excluding `.meta` from text-pattern scans. It found zero email-like values, zero production AWS/OpenAI/GitHub/private-key/JWT secrets, zero credential-shaped query parameters, and zero secret-named tracked files. Keyword/pattern review found only policy/test prose plus deliberate malformed rejection fixtures: `Bearer abcdefghijklmnop` and `eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.signature`. Pseudonymous `local_*`, `test_*`, `learner-*`, `session-*`, and `lref_*` fixtures are intentionally non-production. No learner name, email, school, date of birth, address, credential, raw answer text, or open-ended learner input field exists in the launch/completion DTOs.

Selected response IDs remain learning records, so production purpose, retention, access, and privacy approval are still required. Browser/harness sinks remain allowlisted and sanitized; no launch reference is put in URLs or full payloads in logs/UI.

The post-review asset reconciliation found `107` logical files plus `143` meta sidecars, with no missing/orphaned meta. All `13` non-code logical assets are individually covered by [the consolidated asset manifest](16-ASSET-MANIFEST.md). All four PNG paths/dimensions match their detailed provenance records; no third-party binary media, remote font/media dependency, or audio binary was identified.

## Known gaps and warnings

- Intentional null-cue warnings and the existing non-root `DontDestroyOnLoad` warning remain. The fast browser path reaches eight cue slots; `TimerWarning` has focused once-per-question EditMode evidence. Final audio is absent.
- No repository-wide licence exists. The OpenAI output-terms version and generating-account authority are not archived. Human creative/legal/release authority must resolve these before distribution.
- Malay remains `pending_human_review`; no final Malay approval is claimed.
- Real LMS submission/compatibility, production privacy/security controls, hosting, monitoring, rollback, and support ownership remain unverified.
- Firefox, Safari, native touch, unrestricted fullscreen, classroom usability, assistive-technology/accessibility human review, and full manual content/pedagogy review are not claimed as passed.

## Decision

Task 20 is complete for the repository's standalone vertical-slice acceptance record: the implementation checklist, fresh tests, development build, Chrome/Edge local smoke, privacy scan, asset reconciliation, provenance gaps, and known limitations are recorded consistently. The project remains not approved for production WebGL/LMS distribution until the human and environment gates above are closed.

See [QA plan](13-QA-PLAN.md), [deployment](10-WEB-DEPLOYMENT.md), [asset manifest/release governance](16-ASSET-MANIFEST.md), and [production roadmap](15-PRODUCTION-ROADMAP.md).
