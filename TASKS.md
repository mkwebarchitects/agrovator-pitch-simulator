# Pitch Simulator Vertical Slice Tasks

This checklist mirrors `docs/plans/2026-07-14-pitch-simulator-vertical-slice.md`. Each task is checked only after its task-specific verification checkpoint succeeds.

## P0 - Domain foundation

- [x] Task 1: Unity repository foundation and repeatable test runner
- [x] Task 2: Assembly boundaries and core game states
- [ ] Task 3: Dialogue DTOs and structural validation
- [ ] Task 4: Runtime dialogue graph, flags and branches
- [ ] Task 5: Scoring, confidence and result feedback
- [ ] Task 6: Timer and accessibility settings
- [ ] Task 7: LMS contracts, serialization and mock bridge
- [ ] Task 8: Localization catalog and save-data versioning
- [ ] Task 9: Smart School Garden content and JSON import
- [ ] Task 10: Session controller orchestration

## P1 - Playable experience and integrations

- [ ] Task 11: Unity composition, scenes and uGUI shell
- [ ] Task 12: Response interaction, timer and confidence presentation
- [ ] Task 13: Original pixel-art presentation and judge reactions
- [ ] Task 14: Browser-safe audio hooks
- [ ] Task 15: WebGL JavaScript bridge and local LMS harness
- [ ] Task 16: Complete results, review and retry flow

## P2 - Delivery and acceptance

- [ ] Task 17: Required project documentation and AI handoffs
- [ ] Task 18: WebGL build automation and development build
- [ ] Task 19: Local HTTP and browser smoke tests
- [ ] Task 20: Final acceptance audit

## Verification evidence

- 2026-07-14 Task 1: Unity `6000.5.3f1` batch import exited `0` and ended with `Exiting batchmode successfully now!`; the full `artifacts/logs/unity-import.log` scan found `0` matches for `error CS\d+|Compilation failed|Unhandled Exception`.
- 2026-07-14 Task 1: `Packages/manifest.json` retains the approved `com.unity.test-framework` `1.5.1` and `com.unity.ugui` `2.0.0` pins with no multiplayer, Addressables, Input System, networking or tween package. Unity 6000.5 resolved its built-in lock entries to test framework `1.7.0` and uGUI `2.5.0`.
- 2026-07-14 Task 1: both PowerShell wrappers parsed with `0` syntax errors; required ignore rules were exercised, while `Assets/**/*.meta`, `Packages/packages-lock.json` and `ProjectSettings/` remained unignored.
- 2026-07-14 Task 1: `tools/Run-UnityTests.ps1 -Platform EditMode` completed end-to-end, created `artifacts/test-results/editmode.xml`, and reported `0/0` passed with `0` failures before any test assembly exists.
- 2026-07-14 Task 1 log note: Unity recovered from transient licensing-client handshake/access-token diagnostics, resolved its assigned Personal licence, and completed successfully; two `Curl error 42: Callback aborted` lines occurred during clean shutdown.
- 2026-07-14 Task 2 RED: the focused `GameStateMachineTests` Edit Mode run produced no XML and stopped at the expected missing-production-types compiler boundary (`CS0234` for `Agrovator.PitchSimulator.Core`) before the Core types existed.
- 2026-07-14 Task 2 focused GREEN: `GameStateMachineTests` reported `5/5` passed with `0` failures and the complete 421-line Unity log contained `0` matches for `error CS\d+|Compilation failed|Unhandled Exception`.
- 2026-07-14 Task 2 all Edit Mode GREEN: the canonical wrapper reported `5/5` passed with `0` failures, skips or inconclusive tests; the final complete 398-line log contained `0` failure-marker matches. The runtime Core assembly declares `noEngineReferences: true` and its sources contain no `UnityEngine` references.

## Next action

Begin Task 3 by writing the failing dialogue DTO and structural-validation tests.
