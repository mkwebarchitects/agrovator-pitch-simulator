# Understand the Pitch Simulator

## What the vertical slice does

AGROVATOR Pitch Simulator is a standalone Unity WebGL learning game. A learner pitches a Smart School Garden to mentor Judge Aya, chooses authored responses, sees reactions and feedback, receives a rubric-based result, reviews every choice, and submits a completion through a mock LMS boundary.

## Confirmed scope

- Scenario ID: `smart-school-garden`, content version `1`, beginner difficulty, authored estimate eight minutes.
- One tutorial, branching questions, a recovery route after an unsupported claim, and a terminal node.
- Seven scoring categories, confidence from 0-100, four result levels, timeout handling, retry, and completion resubmission.
- English is reviewed. Malay has exact key parity but is marked `pending_human_review` and currently falls back to reviewed English copy.
- Keyboard navigation and visible focus are implemented. Runtime settings support Normal/Extended/Off timers, reduced motion, language, and independent music/SFX volume, but the current Settings screen exposes only Back; values come from the launch configuration or the local mock defaults.
- Default scenes are `Assets/Scenes/Bootstrap.unity` then `Assets/Scenes/Game.unity`.

## Local evidence and remaining gaps

Task 18 and Task 20 recorded successful local development WebGL builds, and Task 19/20 recorded passing local Chrome and Edge smoke evidence with zero console/page errors. These results do not establish production hosting, a production browser-support promise, or real LMS compatibility. Firefox was unavailable at standard Windows paths, Safari is unavailable on the Windows verification machine, and final audio clips are not included. Native touch, unrestricted fullscreen, classroom usability, assistive-technology/accessibility human review, final Malay approval, and human release approval remain unverified.

## Audiences

- Learners play the scenario and review feedback.
- Learning/content staff review pedagogy, localization, and child suitability.
- Developers maintain Unity, content, and the browser bridge.
- Operators build, host, smoke-test, monitor, and roll back releases.

Start with [learner experience](02-LEARNER-EXPERIENCE.md), [architecture](05-TECHNICAL-ARCHITECTURE.md), or [acceptance status](18-VERTICAL-SLICE-ACCEPTANCE.md).
