# Integrate the Local LMS Boundary

## Contract status

This document describes the implemented local mock, not a production AGROVATOR contract. Protocol version `1` uses exact same-origin `postMessage` events between the embedded player and `WebHarness`.

## Messages

- Player to parent: `pitch-simulator.lms.ready`.
- Parent to player: `pitch-simulator.lms.launch` with `LmsLaunchConfig` or null for Missing Config.
- Player to parent: `pitch-simulator.lms.completion-submit` with integer `requestId` and completion payload.
- Parent to player: `pitch-simulator.lms.completion-result` with the same request ID and `success`, `failure`, `expired`, or `missing-config`.

Both JavaScript sides require `event.origin === window.location.origin` and the expected source window. JavaScript contains no console calls. Query strings carry no launch reference or credential.

## Launch fields

`PseudonymousLearnerId`, `SessionId`, `CourseId`, `ModuleId`, `LessonId`, `ScenarioId`, `Language`, `AttemptNumber`, `TimerMode`, `ReducedMotion`, `MusicVolume`, `SfxVolume`, `ContentVersion`, and opaque `LaunchReference`.

## Completion fields

The same learning identifiers plus `GameVersion`, content/completion status, UTC start/completion timestamps, duration, overall score, competency scores, final confidence, selected response IDs, timeout count, attempt number, and optional recommended follow-up lesson ID. Validation runs before serialization/submission.

## Runtime behavior

WebGL polls recoverably for late launch data every 0.2 unscaled seconds. Native submissions expire after 30 seconds; duplicate/stale callbacks are ignored and canceled browser entries are deleted. Editor/non-WebGL uses `MockLmsBridge`.

Production work starts with [discovery](00-LMS-DISCOVERY.md), [ADR 0003](adr/0003-custom-rest-over-scorm-xapi.md), and [privacy review](12-PRIVACY-SECURITY.md).
