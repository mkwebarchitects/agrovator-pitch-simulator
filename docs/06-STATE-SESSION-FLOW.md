# Follow the State and Session Flow

## State sequence

The explicit states are `Booting`, `Title`, `Briefing`, `Tutorial`, `JudgeIntro`, `AskingQuestion`, `AwaitingResponse`, `ShowingReaction`, `ShowingFeedback`, `Results`, `Submitting`, `Complete`, and `SafeFallback`.

Commands are `FinishBooting`, `StartScenario`, `Continue`, `SelectResponse`, `FinishScenario`, `SubmitResults`, `SubmissionSucceeded`, `SubmissionFailed`, and `Retry`. Invalid transitions are rejected rather than silently advancing.

## Ownership

`PitchSessionController` compiles the scenario, starts questions, accepts one response, advances reaction/feedback, tracks timer expiration, scoring, confidence, flags, and immutable review entries, then builds and submits the LMS completion. Submission failure retains the completed attempt. Retry resets the attempt, history, scoring, flags, timer, and submission state.

`GameScreenRouter` maps state to Title, Briefing, Pitch Room, Results, or safe fallback UI and restores a serialized default selectable. The three response views lock synchronously after a selection; Continue receives focus for post-response states.

## Failure behavior

Missing/invalid content or scene contracts stop initialization in `SafeFallback`. Missing launch configuration can be polled and later recovered. Native WebGL submissions have a bounded pending lifetime; duplicate/stale callbacks are suppressed and cancellation clears browser ownership.

See [architecture](05-TECHNICAL-ARCHITECTURE.md), [LMS contract](11-LMS-CONTRACT.md), and [QA](13-QA-PLAN.md).
