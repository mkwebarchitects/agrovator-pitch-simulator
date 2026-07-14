# Pitch Simulator Vertical Slice Design

**Date:** 2026-07-14  
**Status:** Approved design  
**Project:** AGROVATOR Pitch Simulator  
**Unity:** 6000.5.3f1, WebGL module installed  
**Project location:** `C:\Users\khidz\projects\pitch-simulator`

> **As-built status note:** This file records the approved design target, not a complete statement of current behavior. In the Task 17 vertical slice, the Settings screen provides only Back; timer mode, reduced motion, language, and audio volumes come from launch data or local mock defaults. Initialization failures log and halt rather than transition to `SafeFallback`, and no bundled fallback scenario is selected. Use [`docs/06-STATE-SESSION-FLOW.md`](../../06-STATE-SESSION-FLOW.md) and [`docs/08-ACCESSIBILITY-LOCALIZATION.md`](../../08-ACCESSIBILITY-LOCALIZATION.md) for current behavior.

## 1. Purpose and boundaries

Pitch Simulator is a landscape Unity WebGL educational game for AGROVATOR participants. A student presents a fictional agribusiness idea to a friendly judge, chooses responses under light time pressure, observes the judge's reactions, and receives constructive feedback about pitching and communication.

The first release is a complete vertical slice based on **Smart School Garden**. It demonstrates the reusable dialogue and scoring foundation, one playable scenario, mock LMS launch and completion, automated tests, and a locally served WebGL build.

The Unity source is a standalone repository. The existing WordPress LMS remains unchanged until the standalone vertical slice passes its acceptance checks. A later integration phase will copy versioned WebGL output into the LMS plugin and add a same-origin iframe/REST adapter.

### Confirmed LMS constraints

- WordPress custom plugin and themes, PHP 8.2, vanilla JavaScript and CSS.
- Cookie authentication plus `X-WP-Nonce` for authenticated REST POST requests.
- Existing namespace: `mkwa-lms/v1`.
- Participant, Trainer, Judge and Programme Admin roles.
- Module completion currently requires every lesson completed and every published module quiz passed.
- English-only v1 with translation-ready strings.
- Programme categories are Elementary, Secondary and TVET; the exact age distribution is unavailable.
- No SCORM, xAPI or existing game endpoint was found.

### Design assumptions

- Copy is written for an adaptable 10–17-year-old range until source lesson content is supplied.
- The game is usable independently, while also fitting teacher- or trainer-facilitated sessions.
- The initial runtime content is bundled and works offline after the WebGL payload loads.
- Bahasa Malaysia support is architectural in the vertical slice, but full translation is a later content milestone.
- A final production score-submission endpoint requires an LMS schema decision and is not part of the standalone slice.

## 2. Approaches considered

### A. Standalone Unity repository with controlled LMS handoff — selected

Unity source and generated caches remain outside OneDrive and outside the production LMS repository. A build/export script will later copy only versioned WebGL artifacts into the WordPress plugin. This gives the cleanest ownership boundary and matches the existing Market Rush project convention.

### B. Unity project nested in the LMS repository — rejected

This would simplify relative file paths but would mix Unity metadata, large caches and game history with production PHP deployment files. It would also increase OneDrive churn and the risk of accidental LMS edits.

### C. Shared framework or repository with Market Rush — deferred

Both games can share conventions, but extracting packages before two stable consumers exist would add coupling and slow the vertical slice. Reuse is limited to proven practices such as batch-mode testing, assembly boundaries and Editor-generated scenes.

## 3. Project tier and architectural principles

**Tier:** Small game with a long-lived content framework.

- One explicit bootstrap entry point owns initialization order.
- MonoBehaviours are presentation and composition bridges, not rule containers.
- Dialogue traversal, score calculation, confidence, timers and result generation are plain C#.
- ScriptableObjects store authored configuration and asset references, never mutable session state.
- JSON DTOs provide a portable content format and a future remote-content boundary.
- Dependencies are explicit through constructors or serialized composition references.
- Direct references are preferred within one view; interfaces and events cross module boundaries.
- No service locator, giant GameManager, multiplayer package, heavy render pipeline or runtime reflection framework.

## 4. Runtime modules

### `Agrovator.PitchSimulator.Core`

Owns `GameState`, the deterministic state machine, session state, immutable identifiers, runtime flags and flow commands. It has no UnityEngine dependency.

### `Agrovator.PitchSimulator.Dialogue`

Owns scenario DTOs, compiled runtime scenarios, node lookup, conditional option availability, branch traversal and validation reports. It depends only on Core.

### `Agrovator.PitchSimulator.Scoring`

Owns the 100-point rubric, competency accumulation, confidence clamping, recovery tracking, result-level selection and strengths/improvement generation. It depends on Core and Dialogue contracts, not Unity UI.

### `Agrovator.PitchSimulator.LMS`

Defines `ILmsBridge`, validated launch/completion payloads, `MockLmsBridge` and `WebGlLmsBridge`. The bridge never exposes credentials to gameplay or UI components.

### `Agrovator.PitchSimulator.UI`

Owns screen presenters, response-button views, confidence/timer views, judge portrait animation and navigation. It consumes read-only state snapshots and sends commands to the session controller.

### `Agrovator.PitchSimulator.Accessibility`

Owns timer mode, reduced motion, keyboard focus policy, volume preferences and localization selection. Accessibility settings are applied before the first timed question.

### `Agrovator.PitchSimulator.Audio`

Owns music/SFX channels and named cues. Audio starts only after a user gesture and is never required to understand the game.

## 5. Scene and bootstrap plan

The project uses three scenes rather than five:

1. `Bootstrap` — persistent composition root, content loading, accessibility settings and LMS handshake.
2. `Game` — title, briefing, tutorial, pitch room and results as state-driven panels.
3. `WebIntegrationTest` — local mock-parent harness and bridge diagnostics.

Combining menu, room and results into one scene avoids unnecessary WebGL scene loads and duplicated UI infrastructure. The logical states remain separate and independently testable.

Only `Bootstrapper` receives `[DefaultExecutionOrder(-10000)]`. It initializes services in this order:

1. Load local settings.
2. Initialize localization.
3. Resolve the LMS bridge and validate launch configuration.
4. Load and validate scenario content.
5. Construct the scoring and session services.
6. Load the `Game` scene and attach presenters.
7. Enter `Title` state.

Every frame-based component returns early until initialized. Timers tick only while the state machine is in a timed question state and the game is not paused.

## 6. Gameplay state machine

The minimum states are:

`Booting → Title → Briefing → Tutorial → JudgeIntro → AskingQuestion → AwaitingResponse → ShowingReaction → ShowingFeedback → Results → Submitting → Complete`

Additional transitions:

- Any pre-game load failure → `SafeFallback`.
- Timed response expiry → neutral timeout response → `ShowingReaction`.
- Submission error → `Results` with retry status.
- Retry → fresh `Briefing` with a new attempt number and cleared runtime state.

UI panels never select the next state directly. They submit commands such as `StartScenario`, `SelectResponse`, `Continue` and `Retry`; the state machine validates each command.

## 7. Smart School Garden scenario

The student team proposes sensors and simple irrigation controls to reduce water waste while growing vegetables for the school canteen. The judge is a friendly Malaysian youth innovation mentor whose tone is curious, specific and encouraging.

### Required node sequence

1. **Tutorial:** identify a direct answer; untimed and unscored.
2. **Introduction and problem:** explain who the team is and why water waste matters.
3. **Audience:** explain who benefits; contains the explicit audience-awareness answer.
4. **Evidence branch:**
   - If the first answer was vague or exaggerated, ask for proof and allow recovery.
   - Otherwise ask how the team knows the problem is real.
5. **Practical solution:** explain what sensors do without drowning the listener in facts.
6. **Unknown/cost challenge:** reward honest uncertainty plus a reasonable next step.
7. **Final value question:** connect usefulness, feasibility and the judge's concern.

At least two destinations differ based on flags or confidence. The recovery route records `recovered_after_weak_answer` when the student supplies evidence after an earlier weak claim.

Every question has three plausible choices. Distractors contain teachable weaknesses; none is absurd or insulting. Correct-option positions are deliberately varied, and answer length is not a quality signal.

### Timing

- Tutorial: no timer.
- Early question: 20 seconds.
- Normal questions: 15 seconds.
- Final challenge: 12 seconds.
- Extended mode: 1.5× duration.
- Timer-off mode: permitted by launch/accessibility configuration.

The last five seconds use a number, shrinking bar and gentle animation without flashing. Timeout produces a neutral hesitation response and a modest penalty.

## 8. Scenario data model

`ScenarioDefinition` contains:

- ID, version, title key, briefing key, learning objectives and estimated duration.
- Project definition, judge definition, difficulty and opening node ID.
- Node list, supported locales and content checksum.

`DialogueNodeDefinition` contains:

- ID, node type, speaker, text key, timer seconds and response options.
- Required flags, blocked flags and optional confidence range.

`ResponseOptionDefinition` contains:

- ID, text key and quality tier.
- Rubric deltas for clarity, problem, solution, audience, evidence, communication and time management.
- Confidence change, competency tags, reaction cue, feedback key and explanation key.
- Next-node ID, flags set, required flags and blocked flags.

The import pipeline converts JSON DTOs into a validated runtime graph. ScriptableObject assets wrap the same data for Unity authoring and hold references to portraits/audio. Validation failure returns structured issues and selects a known-good bundled fallback scenario.

## 9. Scoring and confidence

The assessment score is capped at 100:

- Clear explanation: 20.
- Understanding the problem: 15.
- Explaining the solution: 15.
- Knowing the audience: 15.
- Evidence and examples: 15.
- Communication and confidence: 10.
- Time management: 10.

Speed never overrides response quality. A valid answer receives the response's authored time score; answering faster than necessary provides no extra reward beyond the 10-point category.

Secondary scores are Pitching Workshop, Communications, Audience Awareness, Evidence Usage, Clarity and Recovery Ability. Result levels use the supplied encouraging names and configurable thresholds.

Investor confidence is a separate visible 0–100 value. It starts from scenario configuration, clamps after every change and maps to icon-plus-label states. It can affect branches but never reveals the exact hidden rubric score.

## 10. Presentation and interaction

- Landscape 16:9 reference canvas with responsive safe margins.
- uGUI for the first release, matching the proven local Unity WebGL toolchain.
- Pixel-art room and character rendering at integer-friendly scale.
- Modern readable text and smooth panel transitions; pixel styling never reduces legibility.
- Large response buttons with visible hover, pressed and keyboard-focus states.
- Keyboard: arrows/WASD move focus, Enter/Space select, Escape opens settings where safe.
- Mouse and touch use the same command path as keyboard.
- Confidence uses label, icon and bar rather than colour alone.
- Judge reaction set: idle, blink, talk, think, smile, interested, confused, concerned, impressed, encouraging and celebrating.

The initial art may use original project-generated placeholders. Every external asset requires a manifest entry with author, source, licence and modification notes.

## 11. LMS contract

### Launch configuration

The bridge accepts only a validated subset: pseudonymous learner ID, session ID, course/module/lesson/scenario IDs, language, attempt number, timer/accessibility settings, content version and a short-lived token or nonce reference.

### Completion payload

The game returns game/scenario versions, completion status, timestamps, duration, overall and competency scores, final confidence, selected response IDs, timeout count, attempt number and recommended follow-up lesson.

The game does not send response text, names, email addresses or unneeded profile details.

### Browser transport

The future LMS lesson renders a same-origin iframe. A parent-page script sends launch configuration using `postMessage` after validating origin and a one-time handshake. Completion travels back through the parent adapter or an authenticated REST POST using WordPress cookies and `X-WP-Nonce`.

The WebGL client is treated as inspectable and untrusted. The server must validate user capability, session ownership, scenario/version, attempt idempotency and score ranges before recording completion.

## 12. Accessibility and child safety

- Large targets, readable text and strong contrast.
- Complete keyboard operation and visible focus.
- Reduced-motion, extended-timer and permitted timer-off modes.
- Music/SFX sliders and mute; no autoplay before interaction.
- No rapid flashing, hostile failure language or public individual leaderboard.
- No real-money, gambling, loot-box or manipulative retention mechanics.
- Feedback describes the answer, never labels the child.
- Logs omit tokens and private learner data.
- Localization keys exist for all student-facing content.

## 13. Error handling

- Invalid content: show a friendly message, log a non-sensitive validation code and load the bundled fallback.
- Missing LMS configuration: enter clearly labelled Demo Mode using `MockLmsBridge`.
- Expired session: keep results visible and direct the learner to refresh the LMS page.
- Completion failure: preserve the payload in memory and allow an explicit retry; do not claim success.
- Audio failure: continue silently with captions/text intact.
- Missing optional art cue: use a neutral portrait or cue without blocking gameplay.
- Unsupported locale: fall back to English and record the locale key, not learner identity.

## 14. Testing strategy

### Edit Mode

Tests cover duplicate/missing/unreachable nodes, conditional branches, scoring boundaries, confidence clamping, timer modes, timeout behavior, result levels, payload serialization, localization keys and save-data versioning.

### Play Mode

Tests cover game launch, tutorial, scenario start, selection, timeout, reactions, branch traversal, recovery, completion, results, retry, LMS success and LMS failure.

### Web smoke tests

A development WebGL build is served through local HTTP and checked in installed Chrome, Edge and Firefox for loading, audio activation, mouse, keyboard, resizing, fullscreen, refresh, missing configuration, invalid content and submission failure. Safari remains explicitly unverified until macOS/iOS hardware is available.

### Completion evidence

No milestone is reported complete without command output, Unity compile results, test results and—where visual behavior matters—a screenshot or recorded manual observation. Actual compressed size, load time, frame rate and memory measurements replace estimates.

## 15. Performance boundaries

- Built-in 2D rendering; no unnecessary 3D, post-processing or shader variants.
- Target 60 FPS on a normal school desktop, with 30 FPS minimum fallback.
- Initial compressed payload target below 25 MB and total compressed build below 60 MB.
- Sprite atlases and compressed short audio clips.
- No per-frame allocations in timers, dialogue traversal or confidence updates.
- UI animation uses bounded coroutines or a small explicit animation service.

## 16. Delivery sequence

1. Repository and Unity foundation.
2. Pure-C# model and validation using test-first development.
3. Scoring, timer and state machine.
4. ScriptableObject/JSON content pipeline.
5. Original Smart School Garden content.
6. uGUI scene and interaction.
7. Placeholder pixel art, reactions and audio hooks.
8. Mock LMS and browser harness.
9. Edit/Play Mode verification.
10. WebGL build and local browser smoke testing.
11. Documentation, asset manifest and acceptance report.
12. Separate LMS integration phase after standalone acceptance.

## 17. Vertical-slice acceptance

The slice is accepted only when a learner can launch from the mock harness, complete the tutorial and at least five scored questions, traverse two conditional paths, recover from an early weak answer, observe timer/confidence/reaction changes, review educational feedback, retry, and produce a mock completion result. Automated tests must pass, Unity must report no compile errors, and a locally served WebGL build must complete in the available Windows browsers.
