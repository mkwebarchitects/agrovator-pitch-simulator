# Author and Validate Guided Pitch Content

## Before you edit

Work only in this standalone repository. The active authoring file is `Assets/Content/Scenarios/guided-pitch-builder.en.json`; Bootstrap must keep exactly one active guided reference. Treat scenario/content version, stable IDs, reading rules, assessment meaning, and localization keys as reviewed contracts.

## Authoring workflow

1. Preserve scenario ID `smart-school-garden`, version `2`, the declared locale list, and an intentional content checksum.
2. Keep two modes: Primary and Secondary. In each mode, author Problem, Evidence, Solution, and Value in that exact order.
3. Give each part exactly three options: one `Clear`, one `Developing`, and one `NeedsPractice`. Give the cost follow-up the same three-mastery structure.
4. Keep every option ID stable and ordinal/case-sensitive. New semantics require a new reviewed ID and, when incompatible, a content-version decision.
5. For every option, provide `TextKey`, `Mastery`, `LegacyConfidenceDelta`, `ReactionCue`, and `WorkedKey`/`MissingKey`/`ImproveKey`.
6. Keep Primary sentence cards at 12-16 words. Use familiar observations, one idea per card, and direct coaching. Keep Secondary cards at 32 words or fewer and use measurements, qualified scope, audience relevance, and honest uncertainty where appropriate.
7. Add every learner-facing key to `en.json`. English is the only shipped locale.
8. Run the canonical Unity suites and inspect the relevant fixture totals and complete logs. Do not replace the existing content/localization/reflection tests with an ad hoc parser.
9. Ask Primary and Secondary educators or representative learners to review reading level, coaching tone, task length, and transfer usefulness.

## Assessment compatibility

The learner-facing score uses the current four-part choices. A revision may improve the result, but the initial and replacement IDs both remain in chronological selection history. The cost response is also stored in history. Do not rename the existing completion fields, add Time Management to this untimed activity, or repurpose `FinalConfidence` as Pitch Readiness. `FinalConfidence` remains a hidden legacy calculation based on the current four part deltas plus the follow-up delta.

## Privacy and safe errors

Never add learner free text, names, email addresses, school identifiers, credentials, tokens, complete launch payloads, response-text logs, audio recording, or AI assessment. Validation issues and runtime logs must use stable code/path/severity values without echoing JSON, learner IDs, launch references, or sentence text.

## Acceptance checklist

- Exactly one active Bootstrap-wired content-v2 asset.
- Exactly `30` tested stable unique option IDs and two terminating mode routes.
- Every content/UI key resolves in reviewed English.
- The DTO shape, selected-ID history semantics, and legacy confidence meaning stay compatible.
- Automated checks are recorded as implementation evidence, followed by the required human learning review.

See [gameplay](03-GAMEPLAY-CONTENT.md), [localization](08-ACCESSIBILITY-LOCALIZATION.md), and [asset manifest/release governance](16-ASSET-MANIFEST.md).
