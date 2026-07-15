# Pitch Simulator Tutorial and UI Polish Design

**Date:** 2026-07-15  
**Branch:** `codex/pitch-simulator-ui-polish`  
**Base:** completed standalone vertical slice at `3271adf`

## Goal

Make the game immediately understandable and visually restrained. Add a real three-step onboarding sequence, replace full-width learner controls with centered content cards, and apply a consistent modern game-card treatment across every in-game screen without changing scoring, dialogue, LMS payloads, or production-boundary claims.

## Approved learner flow

The existing state sequence remains:

`Title → Briefing → Tutorial → Judge Intro → Practice Question`

`GameState.Tutorial` receives a dedicated screen with three localized pages:

1. **Build your pitch** — introduce the Smart School Garden goal and the low-stakes practice context.
2. **Choose your response** — explain the three response choices and encourage the clearest, most convincing answer.
3. **Learn from feedback** — explain Judge Aya's reaction, feedback and explanation, confidence changes, and the timer.

Back and Next change only the local page index. Back is disabled on page one. Next becomes **Start Practice** on page three. Skip is available on every page. Skip and Start Practice call the existing session `Continue()` once to advance to Judge Intro. Each new attempt, including Retry, shows the tutorial again and resets it to page one.

## Visual system

All screens retain a full-canvas background layer. Learner-facing content moves into a centered dark-teal card approximately `920–960 px` wide on the `1280×720` reference canvas, with `32–40 px` internal padding, cream text, gold accents, subtle borders, and consistent vertical spacing. Existing original pixel art remains in use.

Controls no longer expand across the canvas:

- primary actions: approximately `420–520 px` wide;
- response choices: at most approximately `680 px`, allowing readable wrapped text;
- secondary Back/Skip actions: compact and visually quieter;
- Results actions: a compact centered footer.

Typography uses restrained line lengths, clear heading/body hierarchy, consistent alignment, and localized-text-safe wrapping. Keyboard focus, minimum target sizes, reduced motion, and the responsive 16:9 WebGL canvas contract remain intact.

## Screen treatment

- **Title:** compact hero card with heading, short subtitle, Start, and Settings.
- **Briefing:** heading, concise objective and assignment, one primary action.
- **Tutorial:** step indicator, one focused explanation per page, supporting icon/visual, Back/Next/Skip controls.
- **Pitch Room:** contained main card with Judge Aya, prompt card, compact confidence/timer row, constrained response buttons, and one Continue action.
- **Results:** centered scroll card with clear section separation and compact action footer.
- **Settings:** compact modal-style card instead of a sparse full-screen layout.

The developer LMS harness is intentionally out of scope; this pass covers all in-game screens only.

## Components and ownership

A new `TutorialPresenter` owns presentation state only: page index, localized content, step indicator, labels, navigation availability, and default focus. It does not own dialogue, scoring, timer, attempt, LMS, or persistence state.

`GameScreenRouter` initializes the presenter, maps `GameState.Tutorial` to the dedicated Tutorial panel, and resets/focuses it whenever that state is entered. The generated-scene builder creates the new panel and shared centered-card structures deterministically. Reusable builder helpers define card width, padding, button width, heading treatment, and background styling so individual screens do not duplicate layout constants.

Tutorial copy lives in the localization catalogs. English receives complete copy. Malay follows the existing English-fallback and `pending_human_review` policy; no final Malay approval is claimed.

## Defensive behavior

- Scene-contract validation rejects missing Tutorial references.
- Page navigation remains within the three valid pages.
- Repeated Skip/Start clicks cannot advance more than once because the presenter checks the active state and the controller rejects invalid transitions.
- Missing localization uses the existing fallback behavior.
- Centered cards and constrained buttons remain inside the reference canvas and avoid horizontal overflow at desktop and mobile-sized browser viewports.

## Verification

Implementation is test-driven:

- EditMode: page progression, bounds, reset, localization keys, and state boundary.
- PlayMode: Back/Next/Skip, focus, Retry reset, card containment, constrained buttons, pitch readability, and Results scrolling.
- Builder: all six screens use centered content frames and remain inside `1280×720`.
- Full EditMode and PlayMode regression suites.
- Fresh development WebGL build.
- Chrome and Edge smoke for both full tutorial completion and Skip.
- Desktop and mobile-sized visual screenshots before handoff.

## Out of scope

No changes to scoring, authored dialogue branches, completion payloads, privacy fields, LMS bridge behavior, final audio assets, final Malay approval, production hosting, or the developer harness visual design.
