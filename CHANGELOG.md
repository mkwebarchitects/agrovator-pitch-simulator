# Changelog

All notable changes to the standalone AGROVATOR Pitch Simulator are documented here.

## Unreleased

The current vertical slice is implemented through Task 16; production delivery gates remain documented below.

### Added

- Unity 6000.5.3f1 project foundation with repeatable EditMode, PlayMode, builder, and WebGL wrapper entry points.
- Engine-free core, dialogue, scoring, accessibility, and session assemblies with explicit state/session orchestration.
- Smart School Garden version-1 scenario, reviewed English localization, pending-human-review Malay fallback catalog, deterministic scoring, confidence, results, review, retry, and completion models.
- Generated Bootstrap/Game uGUI shell, keyboard/focus/timer/confidence accessibility, original pixel-art presentation, typed judge reactions, and browser-safe nine-cue audio hooks with placeholder bindings.
- Validated local LMS DTOs/mock modes, version-1 same-origin WebGL `postMessage` bridge, four-mode local harness, bounded callbacks, and sanitized status display.
- Complete numbered production handoff documents, four architecture decisions, 16-phase roadmap, and constrained Codex/Claude prompt library.

### Security and privacy

- Completion data is pseudonymous and contains no learner name, email, answer text, credential, or open-ended content.
- Origin/source/protocol validation, URL-secret avoidance, sanitized errors, and no JavaScript payload logging are implemented in the local bridge/harness.

### Known limitations

- Task 18 development WebGL build and Task 19 browser/manual evidence are pending.
- The external LMS/API was not inspected; the local contract does not establish production compatibility or SCORM/xAPI support.
- Final audio clips are absent, Malay requires qualified human review, and Safari testing is unavailable on Windows.
