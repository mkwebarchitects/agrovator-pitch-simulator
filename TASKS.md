# Pitch Simulator Vertical Slice Tasks

This checklist mirrors `docs/plans/2026-07-14-pitch-simulator-vertical-slice.md`. Each task is checked only after its task-specific verification checkpoint succeeds.

## P0 - Domain foundation

- [x] Task 1: Unity repository foundation and repeatable test runner
- [x] Task 2: Assembly boundaries and core game states
- [x] Task 3: Dialogue DTOs and structural validation
- [x] Task 4: Runtime dialogue graph, flags and branches
- [x] Task 5: Scoring, confidence and result feedback
- [x] Task 6: Timer and accessibility settings
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
- 2026-07-14 Task 3 RED: the focused `ScenarioValidatorTests` run produced no XML and reached the expected missing-production-types compiler boundary (`CS0234` for the absent Dialogue namespace and `CS0246` for `ScenarioDefinitionDto`, `DialogueNodeDto` and `ResponseOptionDto`); the complete 720-line log ended with `Scripts have compiler errors.`
- 2026-07-14 Task 3 focused GREEN: `ScenarioValidatorTests` reported `13/13` passed with `0` failures, skips or inconclusive tests, and the complete 420-line Unity log contained `0` matches for `error CS\d+|Compilation failed|Unhandled Exception`.
- 2026-07-14 Task 3 all Edit Mode GREEN: the canonical wrapper reported `18/18` passed with `0` failures, skips or inconclusive tests; the complete 366-line log contained `0` failure-marker matches.
- 2026-07-14 Task 3 structural checks: the runtime Dialogue assembly declares `noEngineReferences: true`, references only Core and contains `0` `UnityEngine`, `MonoBehaviour` or `ScriptableObject` references. Validator coverage confirms every required issue code, inclusive confidence bounds, complete issue collection and stable node/response ordering; `ValidationIssue` exposes only `Code`, `Path` and `Severity`.
- 2026-07-14 Task 4 RED: the focused `DialogueSessionTests` run produced no XML and reached the expected missing-production-type boundary (`CS0246` for the absent `DialogueSession`); the complete 699-line log ended with `Scripts have compiler errors.`
- 2026-07-14 Task 4 focused GREEN: `DialogueSessionTests` reported `11/11` passed with `0` failures, skips or inconclusive tests; the complete 402-line Unity log contained `0` matches for `error CS\d+|Compilation failed|Unhandled Exception`.
- 2026-07-14 Task 4 all Edit Mode GREEN: the canonical wrapper reported `29/29` passed with `0` failures, skips or inconclusive tests; independent XML parsing confirmed `Passed`, and the complete 365-line log contained `0` failure-marker matches.
- 2026-07-14 Task 4 structural checks: runtime scenario data is copied into get-only objects backed by read-only collections and an ordinal dictionary; session response IDs and flags use ordinal case-sensitive identity. Rejected unknown and unavailable selections preserve both current node and flags, and the recovery test proves the flag is authored by the weak-answer then evidence-response sequence. The Dialogue runtime and its Task 4 tests contain `0` `UnityEngine`, `MonoBehaviour` or `ScriptableObject` references.
- 2026-07-14 Task 4 review RED: the focused fixture reported `15/19` passed and exactly four expected failures: terminal responses remained available, and compile accepted null, empty and exact-duplicate response IDs. The complete 386-line log contained `0` compile-failure markers.
- 2026-07-14 Task 4 review GREEN: the focused fixture reported `19/19` passed and the canonical full Edit Mode suite reported `37/37` passed, both with `0` failures, skips or inconclusive tests. Independent XML parsing confirmed `Passed`; the complete focused/full logs were 381/365 lines with `0` matches for `error CS\d+|Compilation failed|Unhandled Exception`.
- 2026-07-14 Task 5 RED: the focused `ScoringTests` run produced no XML and reached the expected missing-production-types compiler boundary (`CS0234` for the absent Scoring namespace plus `CS0246`/`CS0103` for its contracts); the complete log ended with `Scripts have compiler errors.`
- 2026-07-14 Task 5 runtime seam RED: after the baseline scoring suite was green, the new immutable runtime-delta integration case reached the expected `CS1503` boundary because only the authored DTO overload existed; restoring the mapped `RuntimeResponseScoreDelta` overload made the case pass without introducing a second score model.
- 2026-07-14 Task 5 focused GREEN: `ScoringTests` reported `31/31` passed with `0` failures, skips or inconclusive tests; independent XML parsing confirmed `Passed`, and the complete 392-line Unity log contained `0` matches for `error CS\d+|Compilation failed|Unhandled Exception`.
- 2026-07-14 Task 5 all Edit Mode GREEN: the final canonical wrapper reported `68/68` passed with `0` failures, skips or inconclusive tests; independent XML parsing confirmed `Passed`, and the complete 393-line log contained `0` failure-marker matches.
- 2026-07-14 Task 5 scoring checks: all seven authored deltas clamp to explicit 20/15/15/15/15/10/10 caps and floor at zero; overall caps at 100, confidence at 0…100, level boundaries are inclusive at 0/40/60/80, category strength thresholds are 12/9/9/9/9/6/6, and normalized strength/improvement ties use stable rubric order. Recovery produces the localized `result.strength.recovery` key; all result copy remains localization keys. Scoring declares `noEngineReferences: true` and the Scoring sources/tests contain `0` `UnityEngine`, `MonoBehaviour` or `ScriptableObject` references.
- 2026-07-14 Task 6 RED: the focused timer/settings run produced no XML and reached the expected missing-production-types compiler boundary (`CS0234`/`CS0246`/`CS0103` for the absent Accessibility namespace, `AccessibilitySettings` and `TimerMode`); the 719-line log ended with `Scripts have compiler errors.`
- 2026-07-14 Task 6 focused GREEN: the final post-refactor fixture run reported `35/35` passed with `0` failures, skips or inconclusive tests; independent XML parsing confirmed `Passed`, and the complete 388-line log contained `0` matches for `error CS\d+|Compilation failed|Unhandled Exception`.
- 2026-07-14 Task 6 all Edit Mode GREEN: the canonical wrapper reported `103/103` passed with `0` failures, skips or inconclusive tests; independent XML parsing confirmed `Passed`, and the complete 365-line log contained `0` failure-marker matches.
- 2026-07-14 Task 6 timer/accessibility checks: Core consumes only an effective duration and contains `0` Accessibility/`TimerMode` references; the Accessibility assembly points only to Core and both runtime assemblies declare `noEngineReferences: true`. Zero duration is an explicitly disabled timer that never expires, while positive timers clamp at zero and emit `Expired` once. Negative/non-finite deltas throw before mutation, and a 1,000-tick allocation test measured no managed allocation. Settings cover Normal/Extended/Off, tutorial zero duration, reduced motion, finite-safe `0…1` audio volumes and exact initial `en`/`ms` locale support with English fallback.
- 2026-07-14 Task 6 review fix: focused RED reported `39/40` passed with the sole expected failure that Extended duration overflow returned instead of throwing; safe-boundary and timer reentrancy/lifecycle characterization cases already passed. After guarding the computed result, focused GREEN reported `40/40` and full Edit Mode reported `108/108`, each with `0` failures, skips or inconclusive tests and `0` log failure markers. Extended mode accepts `1.1984620899082103E+308` (finite result `1.7976931348623155E+308`) and rejects the first overflowing authored value `1.1984620899082105E+308` with `ArgumentOutOfRangeException`.

## Next action

Begin Task 7 by writing the failing LMS contract, payload validation and mock bridge tests.
