# Follow the State and Session Flow

## State sequence

The `GameState` enum includes `Booting`, `Title`, `Briefing`, `Tutorial`, `JudgeIntro`, `AskingQuestion`, `AwaitingResponse`, `ShowingReaction`, `ShowingFeedback`, `Results`, `Submitting`, `Complete`, and `SafeFallback`. The current bootstrap flow does not transition to `SafeFallback` or present a safe-fallback screen when initialization fails.

Commands are `FinishBooting`, `StartScenario`, `Continue`, `SelectResponse`, `FinishScenario`, `SubmitResults`, `SubmissionSucceeded`, `SubmissionFailed`, and `Retry`. Invalid transitions are rejected rather than silently advancing.

## Ownership

`Bootstrapper` loads localization and calls `ScenarioJsonLoader.Load`, which parses, validates, and compiles authored JSON into a runtime scenario. `PitchSessionController` receives that compiled scenario, starts questions, accepts one response, advances reaction/feedback, tracks timer expiration, scoring, confidence, flags, and immutable review entries, then builds and submits the LMS completion. Submission failure retains the completed attempt. Retry from Results or successful Complete resets the attempt, history, scoring, flags, timer, and submission state.

`GameScreenRouter` maps initialized session states to Title, Briefing, Tutorial, Pitch Room, or Results and restores a serialized default selectable. Briefing Continue enters Tutorial page one. Tutorial Back/Next change only the local page index; Skip from any page and Start Practice from page three each advance once to Judge Intro. Retry from Results or Complete resets session and tutorial state, returns to Briefing, and shows Tutorial page one again. The three response views lock synchronously after a selection; Continue receives focus for post-response states.

## Failure behavior

Scene-contract, content load/validation/compilation, and rejected launch failures currently write a sanitized Unity error and halt initialization; they do not transition to `SafeFallback` or display recovery UI. A missing WebGL launch configuration is the exception: it is polled and can recover when valid launch data arrives. Editor/non-WebGL missing launch data logs and halts. Native WebGL submissions have a bounded pending lifetime; duplicate/stale callbacks are suppressed and cancellation clears browser ownership.

See [architecture](05-TECHNICAL-ARCHITECTURE.md), [LMS contract](11-LMS-CONTRACT.md), and [QA](13-QA-PLAN.md).
