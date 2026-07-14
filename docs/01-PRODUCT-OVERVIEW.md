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

## Not yet demonstrated

No development WebGL build or browser smoke evidence exists at this checkpoint. Final audio clips are not included. Production LMS compatibility is unconfirmed. Safari testing is unavailable on the Windows verification machine.

## Audiences

- Learners play the scenario and review feedback.
- Learning/content staff review pedagogy, localization, and child suitability.
- Developers maintain Unity, content, and the browser bridge.
- Operators build, host, smoke-test, monitor, and roll back releases.

Start with [learner experience](02-LEARNER-EXPERIENCE.md), [architecture](05-TECHNICAL-ARCHITECTURE.md), or [acceptance status](18-VERTICAL-SLICE-ACCEPTANCE.md).
