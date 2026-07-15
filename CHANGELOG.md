# Changelog

All notable changes to the standalone AGROVATOR Pitch Simulator are documented here.

## Unreleased

The Task 20 audit and UI-polish gate record the completed standalone vertical-slice implementation and local development evidence; production delivery gates remain documented below.

### Added

- Unity 6000.5.3f1 project foundation with repeatable EditMode, PlayMode, builder, and WebGL wrapper entry points.
- Engine-free core, dialogue, scoring, accessibility, and session assemblies with explicit state/session orchestration.
- Smart School Garden version-1 scenario, reviewed English localization, pending-human-review Malay fallback catalog, deterministic scoring, confidence, results, review, retry, and completion models.
- Generated Bootstrap/Game uGUI shell, keyboard/focus/timer/confidence accessibility, original pixel-art presentation, typed judge reactions, and browser-safe nine-cue audio hooks with placeholder bindings.
- Validated local LMS DTOs/mock modes, version-1 same-origin WebGL `postMessage` bridge, four-mode local harness, bounded callbacks, and sanitized status display.
- Complete numbered production handoff documents, four architecture decisions, 16-phase roadmap, and constrained Codex/Claude prompt library.
- Deterministic Task 18 WebGL build automation and Task 19 loopback/browser smoke coverage, followed by the Task 20 acceptance, privacy, asset-provenance, and release-record audit.
- Final-review regressions for localized pitch feedback/explanations, successful-completion Retry, all nine gameplay audio hooks, and frame-held WebGL pointer smoke actions.
- Dedicated three-page Tutorial shown on every attempt, including Retry, with Back, Skip, Next, and Start Practice controls; all six screens now use centered, bounded cards, and Results has a 64px scrollbar target with a high-contrast focus rail.
- Tutorial-aware browser evidence with Chrome tutorial and Question 1 checkpoints, per-browser final smoke captures, mobile containment metrics, and test-first corrections to measured pointer coordinates after real-browser smoke exposed stale layout assumptions.

### Fixed

- Pitch Room prompt/outcome text, response labels, and all five confidence labels now render every authored character at the `1280x720` reference instead of truncating vertically; a generated-layout regression covers the complete current English catalog surface used by that screen.
- Final browser capture now waits for recovered Briefing content and control regions to both change from Title and remain stable for three bounded samples, preventing a first partial WebGL repaint from being recorded as visual evidence.
- Measured Continue/response coordinate contracts now inspect exactly one executable `canvas.click` with `120 ms` dwell and reject comment, dead-string, or altered-dwell mutations.

### Security and privacy

- Completion data has no direct identity fields (name, email, school, date of birth, or address), credentials, raw answer text, or open-ended learner input. Pseudonymous IDs, scores, timestamps, and selected response IDs remain learning records requiring approved retention and privacy controls.
- Origin/source/protocol validation, URL-secret avoidance, sanitized errors, and no JavaScript payload logging are implemented in the local bridge/harness.
- The Task 20 source/template scan found no email, high-confidence secret, credential-query shape, secret-named tracked file, direct identity field (name, email, school, date of birth, or address), credential, raw answer text, or open-ended learner input; deliberate pseudonymous mock and malformed negative-test fixtures remain documented as non-production data. Pseudonymous IDs, scores, timestamps, and selected response IDs remain learning records requiring retention/privacy approval.
- The UI-polish re-scan found zero email, AWS/OpenAI/GitHub/private-key, credential-query, or secret-named tracked-file hits. The only bearer/JWT-shaped hits were the documented malformed negative-test literals and the acceptance prose that quotes them; no unexpected secret shape was found.

### Known limitations

- The external LMS/API was not inspected; the local contract does not establish production compatibility or SCORM/xAPI support.
- Final audio clips are absent, Malay requires qualified human review, and real LMS/classroom/accessibility human review is incomplete.
- Firefox was unavailable at standard Windows paths; Safari, native touch and unrestricted fullscreen are unverified. Chrome/Edge evidence is local development smoke, not a production support promise.
- No repository-wide licence exists, and the applicable OpenAI output-terms version/account authority is described but not archived; human legal/release approval remains required before distribution.
