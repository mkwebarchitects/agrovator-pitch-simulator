# Follow the Guided State and Session Flow

## Phase sequence

`GuidedPitchPhase` contains `Booting`, `Title`, `Briefing`, `ModeSelection`, `Learn`, `Build`, `BuildFeedback`, `Improve`, `Present`, `FollowUp`, `FollowUpFeedback`, `Results`, `Submitting`, `Complete`, and `SafeFallback`.

The normal sequence is:

```text
Booting -> Title -> Briefing -> ModeSelection -> Learn
        -> Build <-> BuildFeedback (Problem, Evidence, Solution, Value)
        -> Improve -> Present -> FollowUp -> FollowUpFeedback
        -> Results -> Submitting -> Complete
```

Improve can open one part for replacement before returning to Improve. A draft cannot enter Present until all four current slots are populated. Completion is available after the full assembly/review/follow-up sequence regardless of Pitch Readiness.

## Commands and ownership

`GuidedPitchSessionController` owns `FinishLaunch`, `StartScenario`, `Continue`, `SelectLearnerMode`, `SelectPitchResponse`, `BeginRevision`, `ReplacePitchResponse`, `PresentPitch`, `SelectFollowUpResponse`, `SubmitResults`, and `Retry`. Commands reject invalid phases and unknown IDs.

The controller owns the mode, active part, draft, assessment, feedback, follow-up choice, chronological selection history, attempt number, completion payload, submission state, and sanitized submission error. Snapshots deep-copy mutable arrays and expose read-only collections.

## Diagnostic and final state

An initial part choice records both `InitialResponseId`/mastery and `CurrentResponseId`/mastery. A replacement updates only current values, marks the part revised, and appends the replacement ID to selection history. Results and emitted competencies use the current revised choices. Improvement count increases only when a revised current mastery outranks its initial mastery.

Selection history is chronological: the four initial part IDs, any replacement ID when chosen, and the cost follow-up ID. The completion payload copies that history into the existing `SelectedResponseIds` field.

## Retry and recovery

Retry is accepted from Results or Complete. It increments the attempt, clears mode, draft, revision/history, assessment, feedback, follow-up, completion, and submission state, then returns to Briefing. Continuing from Briefing reaches a fresh ModeSelection; a missing mode never bypasses the selector.

Missing/malformed/invalid guided content, localization mismatch, rejected launch, or scene-contract failure blocks on `SafeFallback` with localized non-sensitive copy. Logs contain only `guided_content_invalid`, `guided_localization_invalid`, `guided_launch_invalid`, or `guided_scene_contract_invalid`, not JSON, IDs, launch references, or sentence text. A later valid WebGL launch can recover missing configuration.

Submission failure returns from Submitting to Results and preserves the completed pitch, assessment, history, attempt, and scroll state for resubmission. A matching success callback reaches Complete. Stale, duplicate, disposed, or wrong-attempt callbacks are ignored.

See [architecture](05-TECHNICAL-ARCHITECTURE.md), [scoring](07-SCORING-RESULTS.md), and [LMS contract](11-LMS-CONTRACT.md).
