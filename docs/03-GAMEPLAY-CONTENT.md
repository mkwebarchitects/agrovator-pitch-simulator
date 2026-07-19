# Maintain the Guided Smart School Garden Content

## Active content

`Assets/Content/Scenarios/guided-pitch-builder.en.json` is the only guided content asset wired into Bootstrap. It has ID `smart-school-garden`, version `2`, an authored 10-minute estimate, locales `en` and `ms`, and checksum label `guided-pitch-builder-v2-authored`. The tracked `smart-school-garden.en.json` v1 dialogue graph is legacy content and is not the current Bootstrap reference.

Each mode has four ordered pitch parts with three options per part, followed by one three-option cost question. That produces `15` options per mode and `30` stable unique option IDs overall. Every group contains one `Clear`, one `Developing`, and one `NeedsPractice` choice. Every option has a localized sentence, three feedback keys, a typed reaction cue, and a retained legacy confidence delta.

## Learning design

- Problem / Spot it asks what happens and why it matters.
- Evidence / Prove it asks what the team saw, counted, or measured.
- Solution / Solve it asks how the sensor system works during a school day.
- Value / Show why it matters asks who benefits and how.
- The follow-up asks about cost and what to do when the final amount is unknown.

Primary keeps each sentence concrete and within 12-16 words. Secondary uses no more than 32 words and explicitly tests observation versus assumption, measurement, credible scope, practical operation, audience value, and uncertainty. Both routes terminate through the same mechanics and visual system.

The first four choices are diagnostic. Results assess each part's current choice after any revision and count a strengthened part only when mastery increased. The chronological history still retains both the initial choice and the replacement. The follow-up supports adaptability practice but does not change the four-part Pitch Readiness score.

## Content rules

- Keep all released option IDs stable. `SelectedResponseIds` stores them as learning records.
- Preserve exactly Primary and Secondary, four ordered parts, three options per part, and three follow-up options unless an approved versioned design changes the contract.
- Keep one option at each mastery level in every group. Do not reveal mastery through placement, card length, or insulting language.
- Keep Primary cards at 12-16 words and Secondary cards at 32 words or fewer.
- Provide `worked`, `missing`, and `improve` feedback keys for every option.
- Resolve every content/UI key in reviewed English and the pending-review Malay fallback catalog.
- Do not add free text, personal details, audio capture, AI scoring, or direct learner-mode comparisons.

## Validation and evidence boundary

The fresh `GuidedPitchContentTests` fixture passed `18/18`, including exact structure, both terminating routes, the 30-ID inventory, exact sentences/mastery/reactions, all key resolution, feedback-key patterns, reading limits, and structured invalid-content errors. `LocalizationTests` passed `27/27`.

These tests prove authored structure and implemented behavior. They do not prove that the reading level, coaching tone, task length, or transfer prompt improves learning. Primary and Secondary educators or representative learners must review those questions before any pedagogical-effectiveness claim.

See [content authoring](04-CONTENT-AUTHORING.md), [scoring](07-SCORING-RESULTS.md), and [QA](13-QA-PLAN.md).
