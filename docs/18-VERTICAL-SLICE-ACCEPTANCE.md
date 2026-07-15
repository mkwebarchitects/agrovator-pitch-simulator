# Assess Vertical Slice Acceptance

## Task 20 and UI-polish evidence boundary

On 2026-07-15 (`Asia/Kuala_Lumpur`), Task 20 accepted the standalone vertical-slice implementation and the later UI-polish gate recorded the dedicated Tutorial and centered-card experience for Unity `6000.5.3f1`. This is not production WebGL/LMS release approval. The external AGROVATOR LMS repository was not inspected, and real LMS submission, deployment, legal, language, audio, classroom, accessibility, browser-support, and human release decisions remain outside this acceptance.

Fresh execution evidence:

- Canonical EditMode passed `311/311`; canonical PlayMode passed `39/39`; Node passed `14/14`. Unity XML recorded zero failures, skips, or inconclusive tests, and both complete logs had zero compile/exception markers. The additional generated-layout regression assigns every authored Pitch Room prompt/outcome and response string plus all five confidence labels at `1280x720` and requires complete UGUI TextGenerator character counts.
- `tools/Build-WebGL.ps1` exited `0` in `48.8 s`. BuildReport was `Succeeded`, `92,374,282` bytes, `00:00:07.2820626`, with zero warnings and zero errors.
- The build contained exactly seven files: `index.html` `5,505`; `style.css` `2,717`; `layout.js` `1,115`; loader `58,622`; framework `711,897`; data `9,293,457`; wasm `82,300,969` bytes.
- The repaired matrix generated at `2026-07-15T08:38:15.819Z` (`2026-07-15 16:38:15.819` local) used Playwright `1.61.1`. Chrome `150.0.7871.116` passed with `7,293 ms` Unity-ready and `36.210 s` total; Edge `150.0.4078.65` passed with `7,549 ms` Unity-ready and `36.559 s` total. Both recorded zero console/page errors; all three Tutorial pages, launch, Success, Failure, Missing Configuration, pointer-only Retry Tutorial Skip, and a fresh Question 1 reveal were present; the server stopped cleanly with zero stderr. Final capture followed a bounded three-sample stable-content/control repaint gate. An initial Edge attempt sampled stale desktop canvas bounds before its asynchronous mobile resize; the later screenshot showed the contained mobile canvas, and the unchanged full rerun passed.
- Firefox was unavailable at standard Windows paths. Safari is unavailable on Windows. Neither is claimed as passed.

The earlier Task 18 measurements remain historical evidence in [web deployment](10-WEB-DEPLOYMENT.md); they are not substituted for the fresh UI-polish build.

## Line-by-line acceptance

An independent source/test/scenario review plus the final repair review through commit `616e2ed` found direct automated or authored-data evidence for every requested implementation capability. Fresh broad suites, build, and browser smoke above provide the final execution checkpoint.

| Requirement | Current status | Evidence summary |
| --- | --- | --- |
| Title | Pass | Generated Title is initially active/focused; launch-to-Title is covered by Bootstrap and session-controller tests. |
| Briefing | Pass | Start routes to Briefing and focuses Continue; session tests assert fresh tutorial state. |
| Tutorial | Pass | A dedicated three-page UI appears from page one on every attempt. Back/Next mutate only the page index; Skip is available on every page; Start Practice and Skip each advance once. The existing zero-timer, zero-score, zero-confidence practice opening remains unchanged. |
| Centered layouts | Pass | Title `760x500`, Briefing `880x520`, Tutorial `920x560`, Settings `720x420`, and capped `960x680` PitchRoom/Results cards are centered and contained at the 1280x720 reference; all actions remain at least 64px high, and the Results scrollbar has a 64px target plus `14.09:1` selected-focus contrast. Pitch Room text uses a readable `22px` minimum, 72px response targets, and complete generated character-count coverage. |
| Judge | Pass | Generated PitchRoom contains configured Judge Aya with active talk-loop behavior. |
| Five-or-more scored questions | Pass | All `729` playable paths terminate with exactly six scored questions. |
| Three choices | Pass | Every scored question has exactly three respectful responses; runtime uses a fixed three-view pool. |
| Two-or-more branches | Pass | Conditional standard/recovery gates provide at least two authored branch behaviors. |
| Recovery | Pass | The unique `weak_claim_made` recovery route is tested through Results history and retry reset. |
| Timers | Pass | Authored `20`, `15`, and `12` second timers, final-five presentation, reduced-motion behavior, and neutral expiry are covered. |
| Confidence | Pass | Exact five bands, fill/art fallback, scored confidence changes, and full rendering of every state label are covered. |
| Reactions | Pass | All 11 typed reactions, authored mappings, fallback, talk/blink, one-shot, and reduced-motion behavior are covered. |
| Audio hooks | Pass for hooks only | Exact nine-hook inventory, first-gesture music, response/reaction, once-per-question timer warning, feedback/results, completion routing, and null safety are covered; final audible/licensed clips are absent. |
| Scoring | Pass | Seven category caps, 100 overall clamp, rollups, confidence bounds, levels, and feedback selection are covered. |
| Results | Pass | Terminal payload, scores, confidence, rollups, two strengths, two improvements, and status rendering are covered. |
| Review | Pass | Ordered response/feedback/explanation history, fixed six-item pool, scrolling, focus, and reset behavior are covered. |
| Retry | Pass | Retry from Results or successful Complete increments the attempt while runtime state, Results view, and review state reset; browser smoke proves the fresh flow through Question 1 reveal. |
| Mock launch/completion | Pass for mock contract | Success, Failure, Expired, Missing Configuration, resend/recovery, payload construction, and browser JSON paths are covered; this is not real LMS proof. |
| WebGL build | Pass for development build | Deterministic Bootstrap/Game build contract and fresh successful seven-file artifact are recorded above. |

## Privacy, secret, and asset audit

The fresh UI-polish scan searched `Assets`, `WebHarness`, `docs`, `tools`, and the root release records while excluding `.meta` and PNG binaries. It found zero email-like, AWS, OpenAI, GitHub, private-key, credential-query, or secret-named tracked-file hits. The only bearer/JWT-shaped matches were the deliberate malformed rejection literals `Bearer abcdefghijklmnop` and `eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.signature`, once in the LMS negative test and once in this explanatory record; therefore the unexpected-secret count is zero. Pseudonymous `local_*`, `test_*`, `learner-*`, `session-*`, and `lref_*` fixtures are intentionally non-production. Fresh DTO inspection confirms no learner name, email, school, date of birth, address, credential, raw answer text, or open-ended learner input field exists in launch/completion payloads.

Selected response IDs remain learning records, so production purpose, retention, access, and privacy approval are still required. Browser/harness sinks remain allowlisted and sanitized; no launch reference is put in URLs or full payloads in logs/UI.

The fresh UI-polish asset reconciliation found `109` logical files plus `145` meta sidecars, with no missing/orphaned meta. All `13` non-code logical assets are individually covered by [the consolidated asset manifest](16-ASSET-MANIFEST.md). All four PNG paths/dimensions match their detailed provenance records; no third-party binary media, remote font/media dependency, or audio binary was identified.

## Known gaps and warnings

- Intentional null-cue warnings and the existing non-root `DontDestroyOnLoad` warning remain. The fast browser path reaches eight cue slots; `TimerWarning` has focused once-per-question EditMode evidence. Final audio is absent.
- No repository-wide licence exists. The OpenAI output-terms version and generating-account authority are not archived. Human creative/legal/release authority must resolve these before distribution.
- Malay remains `pending_human_review`; no final Malay approval is claimed.
- Real LMS submission/compatibility, production privacy/security controls, hosting, monitoring, rollback, and support ownership remain unverified.
- Firefox, Safari, native touch, unrestricted fullscreen, classroom usability, assistive-technology/accessibility human review, and full manual content/pedagogy review are not claimed as passed.

## Decision

The standalone vertical slice plus UI-polish implementation record now includes fresh tests, a development build, Chrome/Edge local smoke, stable-ready final PNG inspection, mobile containment metrics, privacy scanning, asset reconciliation, provenance gaps, and known limitations. The regenerated Chrome/Edge finals are byte-identical complete Briefing checkpoints; no partial-repaint image is accepted. This is a local evidence gate only. The project remains not approved for production WebGL/LMS distribution until the human and environment gates above are closed.

See [QA plan](13-QA-PLAN.md), [deployment](10-WEB-DEPLOYMENT.md), [asset manifest/release governance](16-ASSET-MANIFEST.md), and [production roadmap](15-PRODUCTION-ROADMAP.md).
