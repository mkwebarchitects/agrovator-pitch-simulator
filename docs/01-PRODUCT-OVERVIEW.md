# Understand the Guided Pitch Builder

## What learners do

AGROVATOR Pitch Simulator is a standalone Unity WebGL activity for individual pitch practice. A learner builds a Smart School Garden pitch from four parts: Problem, Evidence, Solution, and Value. Mentor Aya gives immediate coaching, allows revision, asks one practical cost question, and returns a learner-facing Pitch Readiness result.

The activity is untimed and targets approximately 8-10 minutes. Completion requires assembling and reviewing all four parts, presenting the combined pitch, and reviewing the cost follow-up. It does not require a minimum score.

## Primary and Secondary

The learner chooses **Primary** or **Secondary** after the Briefing. The unchanged `LmsLaunchConfig` has no learner-category field, so the game does not infer a mode from LMS data. The programme concept `Elementary -> Primary` is documentation for future mapping review only.

- Primary uses direct prompts, familiar school examples, and concrete cards of 12-16 words.
- Secondary uses more precise reasoning, observations and measurements, qualified claims, audience relevance, and honest uncertainty.

Both modes use the same framework, mechanics, garden theme, feedback structure, and completion rule. The product does not compare Primary learners with Secondary learners.

## Confirmed implementation scope

- Scenario ID `smart-school-garden`, content version `2`, one Bootstrap-wired guided content asset, and one legacy v1 asset that is not active.
- `30` stable unique options: four groups of three pitch choices plus one group of three follow-up choices in each mode.
- Four persistent parts and prompts: **Problem / Spot it**, **Evidence / Prove it**, **Solution / Solve it**, and **Value / Show why it matters**.
- Diagnostic first choices, three-part coaching, optional revision, combined Present view, cost follow-up, Results, submission/resubmission, and Retry.
- Six completion competency IDs: `problem`, `evidence`, `solution`, `audience`, `clear_explanation`, and `communication`. Time Management is excluded.
- English-only catalog with `321` entries; every key resolves in reviewed English. Malay was removed on 2026-07-21 - the game ships in one language and no longer claims a translation it does not have.
- Wide/compact responsive layouts, DPR-aware crisp rendering, keyboard focus, pointer input, and safe localized recovery.

## Evidence boundary

Fresh automated acceptance passed EditMode `370/370`, PlayMode `48/48`, Node `20/20`, a zero-warning/error WebGL build, Chrome Primary keyboard smoke, and Edge Secondary pointer smoke, including executed missing-configuration recovery in both covered browsers. Those checks demonstrate that the implementation follows the tested rules. They do not demonstrate classroom learning effectiveness.

Primary and Secondary educators or representative learners must still review reading level, coaching tone, task length, and transfer usefulness. Firefox/Safari, native touch, a real LMS, unrestricted fullscreen, legal approval, classroom evidence, and human accessibility review remain unclaimed.

Continue with [learner experience](02-LEARNER-EXPERIENCE.md), [architecture](05-TECHNICAL-ARCHITECTURE.md), or [acceptance status](18-VERTICAL-SLICE-ACCEPTANCE.md).
