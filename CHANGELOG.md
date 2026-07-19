# Changelog

All notable changes to the standalone AGROVATOR Pitch Simulator are documented here.

## Unreleased

### Added

- Added the content-v2 Smart School Garden guided pitch builder with explicit Primary and Secondary mode selection, a four-part Learn/Build/Improve/Present flow, one cost/uncertainty follow-up, Results, transfer coaching, submission recovery, and Retry.
- Added `30` stable unique authored option IDs: three choices for each of four pitch parts plus three follow-up choices in each mode.
- Added responsive wide/compact guided layouts, persistent part icons/colours, cream selection cards, keyboard/focus support, CSS-sized viewport metrics, and device-pixel rendering capped at scale `2`.
- Added diagnostic-first feedback and revised-current-choice assessment. Results emit Problem, Evidence, Solution, Audience, Clear Explanation, and Communication; Time Management is intentionally omitted from the untimed activity.
- Expanded each localization catalog to `319` entries. English is reviewed project copy; Malay has exact key/value fallback parity and remains `pending_human_review`.

### Changed

- Bootstrap now wires exactly one active guided content asset, `Assets/Content/Scenarios/guided-pitch-builder.en.json`, with scenario/content version `2`. The legacy v1 scenario remains tracked but is not the Bootstrap content reference.
- The learner-facing Confidence display is replaced by Pitch Readiness. The unchanged LMS `FinalConfidence` field remains hidden and keeps legacy confidence-delta semantics; it has not been redefined as readiness.
- `SelectedResponseIds` now carries the guided chronological selection history: initial four-part choices, any replacement choice, and the follow-up ID. The `14`-field launch and `19`-field completion DTO shapes and types remain unchanged.
- Retry clears mode, draft, revision/history, assessment, follow-up, and submission state, then returns to Briefing so a fresh mode must be selected.

### Fixed

- Generated guided layouts keep both modes, complete four-sentence presentations, long Secondary Results, fixed actions, compact cards, focus states, and garden composition contained at the verified wide and compact sizes.
- Browser evidence now uses state-specific stable content/control gates, assigned keyboard-only and pointer-only routes, exact guided captures, responsive settlement, and display-only DPR metrics.
- Removed six branch-wide trailing spaces from `Assets/Scripts/UI/GuidedPitch.meta` and `Assets/Tests/PlayMode/GuidedPitch.meta` without changing either GUID or metadata meaning, so the complete branch diff check is clean.

### Acceptance evidence

- Fresh EditMode passed `370/370` in `2.6572753 s`; PlayMode passed `48/48` in `1.760119 s`. Both XML roots were `Passed` with zero failures/skips/inconclusive cases, and their complete `897`/`890`-line logs had zero configured compile/exception/failure markers.
- JavaScript syntax passed and the Node contract suite passed `19/19` with zero failures/skips/todos, including two repair contracts that require the guided browser path to execute and record reachable missing-configuration recovery through the hidden fullscreen harness controls.
- WebGL BuildReport was `Succeeded`, `92,631,312` bytes, `00:00:02.0019569`, zero warnings/errors. The server self-test passed.
- The original acceptance matrix did not exercise missing-configuration recovery (`modes.missingConfig` was `false`); `runBrowser` was repaired to invoke and record `verifyMissingConfigRecovery`, which now drives the hidden harness controls via `setHarnessMode` and a dispatched `#resend` click.
- The repaired matrix passed Chrome `150.0.7871.127` on the Primary keyboard path (`6,971 ms` load) and Edge `150.0.4078.83` on the Secondary pointer path (`6,590 ms`). Each recorded zero console/page errors, desktop/mobile containment, failure-to-success recovery, Retry/mode reset, and `modes.missingConfig: true` missing-configuration recovery.
- All eleven required guided screenshots were reviewed at original detail. Automated evidence demonstrates implementation behavior only, not classroom learning effectiveness.

### Security and privacy

- No learner free text, audio/AI scoring, personal details, new transport, response sentence text, or full launch/completion payload logging was added.
- Fresh scans found no unexpected email, credential-query, secret-named file, secret/private-key shape, learner name, or school identifier. The only bearer/JWT-shaped values are deliberate malformed negative-test fixtures and explanatory acceptance prose.
- Completion still uses pseudonymous identifiers, scores, timestamps, stable selected response IDs, and counts. These remain learning records requiring approved purpose, retention, access, and deletion controls.

### Known limitations

- Primary/Secondary educator or representative-learner review has not occurred. No claim is made about reading-level suitability, coaching tone, task length, transfer usefulness, or learning effectiveness.
- Malay human review, Firefox/Safari, native touch, real LMS behavior, unrestricted fullscreen, legal/release approval, classroom use, and human accessibility/assistive-technology review remain unclaimed.
- The lexical JavaScript source-contract extractor can treat calls in constant-unreachable branches such as `if (false)` as reachable. Runtime smoke provides the current behavioral backstop; this retained minor is pending final review triage.
