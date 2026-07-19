# Integrate the Local LMS Boundary

## Contract status

This document describes the implemented local mock boundary, not a production AGROVATOR LMS contract. The external LMS was not inspected. Protocol version `1` still uses exact same-origin `postMessage` events between the embedded player and `WebHarness`; supported scenario content is now version `2`.

The learner chooses Primary or Secondary inside the game. `LmsLaunchConfig` has no category or learner-mode field. `Elementary -> Primary` is a conceptual future mapping boundary only and must not be inferred from current launch data.

## Messages

- Player to parent: `pitch-simulator.lms.ready`.
- Parent to player: `pitch-simulator.lms.launch` with `LmsLaunchConfig` or null for Missing Config.
- Player to parent: `pitch-simulator.lms.completion-submit` with integer `requestId` and completion payload.
- Parent to player: `pitch-simulator.lms.completion-result` with the same request ID and `success`, `failure`, `expired`, or `missing-config`.

Both JavaScript sides require `event.origin === window.location.origin` and the expected source window. Query strings carry no launch reference or credential. UI/logging exposes only allowlisted status and sanitized errors, not full launch or completion payloads.

## Unchanged launch DTO

`LmsLaunchConfig` still has these `14` public fields and types:

- `string`: `PseudonymousLearnerId`, `SessionId`, `CourseId`, `ModuleId`, `LessonId`, `ScenarioId`, `Language`, `TimerMode`, `LaunchReference`
- `int`: `AttemptNumber`, `ContentVersion`
- `bool`: `ReducedMotion`
- `float`: `MusicVolume`, `SfxVolume`

Version `2` is accepted and retired version `1` is rejected by the current validator. `LaunchReference` remains a constrained opaque reference, not a raw token.

## Unchanged completion DTO

`LmsCompletionPayload` still has these `19` public fields and types:

- `string`: `PseudonymousLearnerId`, `SessionId`, `CourseId`, `ModuleId`, `LessonId`, `ScenarioId`, `GameVersion`, `CompletionStatus`, `StartedAtUtc`, `CompletedAtUtc`, `RecommendedFollowUpLessonId`
- `int`: `ContentVersion`, `OverallScore`, `FinalConfidence`, `TimeoutCount`, `AttemptNumber`
- `double`: `DurationSeconds`
- `LmsCompetencyScore[]`: `CompetencyScores`
- `string[]`: `SelectedResponseIds`

`LmsCompetencyScore` remains `string CompetencyId` plus `int Score`. No field was added, removed, renamed, or retyped for the guided builder.

Guided completion maps Pitch Readiness to `OverallScore` and emits `problem`, `evidence`, `solution`, `audience`, `clear_explanation`, and `communication`. Time Management is omitted and timeout count is zero. `SelectedResponseIds` contains chronological initial choices, any revision replacement, and the follow-up ID.

The learner-facing product does not show confidence. `FinalConfidence` remains a hidden legacy field: `50` plus the retained deltas for the four current choices and follow-up, clamped to `0-100`. It is not Pitch Readiness and has not acquired a new contract meaning.

## Runtime behavior

WebGL polls recoverably for late launch data every `0.2` unscaled seconds. Native submissions expire after `30` seconds; duplicate/stale callbacks are ignored and canceled browser entries are deleted. Submission failure preserves Results for resubmission. Editor/non-WebGL uses `MockLmsBridge`.

The fresh `LmsPayloadTests` fixture passed `92/92`, including reflection inventories, serialization, v2 validation, privacy shape, constrained launch references, and sanitized mock errors. Source comparison also confirms both DTO files are unchanged from the approved guided baseline. This is local contract evidence only; a real LMS remains unverified.

Production work starts with [discovery](00-LMS-DISCOVERY.md), [ADR 0003](adr/0003-custom-rest-over-scorm-xapi.md), and [privacy review](12-PRIVACY-SECURITY.md).
