# Changelog

All notable changes to the standalone AGROVATOR Pitch Simulator are documented here.

## Unreleased

The Task 20 audit records the completed standalone vertical-slice implementation and local development evidence; production delivery gates remain documented below.

### Added

- Unity 6000.5.3f1 project foundation with repeatable EditMode, PlayMode, builder, and WebGL wrapper entry points.
- Engine-free core, dialogue, scoring, accessibility, and session assemblies with explicit state/session orchestration.
- Smart School Garden version-1 scenario, reviewed English localization, pending-human-review Malay fallback catalog, deterministic scoring, confidence, results, review, retry, and completion models.
- Generated Bootstrap/Game uGUI shell, keyboard/focus/timer/confidence accessibility, original pixel-art presentation, typed judge reactions, and browser-safe nine-cue audio hooks with placeholder bindings.
- Validated local LMS DTOs/mock modes, version-1 same-origin WebGL `postMessage` bridge, four-mode local harness, bounded callbacks, and sanitized status display.
- Complete numbered production handoff documents, four architecture decisions, 16-phase roadmap, and constrained Codex/Claude prompt library.
- Deterministic Task 18 WebGL build automation and Task 19 loopback/browser smoke coverage, followed by the Task 20 acceptance, privacy, asset-provenance, and release-record audit.

### Security and privacy

- Completion data has no direct identity fields (name, email, school, date of birth, or address), credentials, raw answer text, or open-ended learner input. Pseudonymous IDs, scores, timestamps, and selected response IDs remain learning records requiring approved retention and privacy controls.
- Origin/source/protocol validation, URL-secret avoidance, sanitized errors, and no JavaScript payload logging are implemented in the local bridge/harness.
- The Task 20 source/template scan found no email, high-confidence secret, credential-query shape, secret-named tracked file, direct identity field (name, email, school, date of birth, or address), credential, raw answer text, or open-ended learner input; deliberate pseudonymous mock and malformed negative-test fixtures remain documented as non-production data. Pseudonymous IDs, scores, timestamps, and selected response IDs remain learning records requiring retention/privacy approval.

### Known limitations

- The external LMS/API was not inspected; the local contract does not establish production compatibility or SCORM/xAPI support.
- Final audio clips are absent, Malay requires qualified human review, and real LMS/classroom/accessibility human review is incomplete.
- Firefox was unavailable at standard Windows paths; Safari, native touch and unrestricted fullscreen are unverified. Chrome/Edge evidence is local development smoke, not a production support promise.
- No repository-wide licence exists, and the applicable OpenAI output-terms version/account authority is described but not archived; human legal/release approval remains required before distribution.
