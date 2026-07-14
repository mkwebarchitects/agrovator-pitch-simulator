# Author and Validate Scenario Content

## Before you edit

Work only in this standalone repository. Keep `Id` and `Version` intentional, obtain learning-owner approval, and preserve reviewed localization keys. The Unity project must be closed or allowed to reimport after JSON changes.

## Authoring workflow

1. Edit `Assets/Content/Scenarios/smart-school-garden.en.json` or add a separately identified scenario.
2. Define project, judge, learning-objective keys, opening node, supported locales, and a content checksum label.
3. Build nodes with stable response IDs and explicit destinations/flags.
4. Add every new key to `Assets/Content/Localization/en.json` and the same key to `ms.json`. Keep Malay status `pending_human_review` until a qualified reviewer approves actual Malay copy.
5. Run focused dialogue, content, localization, and scoring tests before broad suites.
6. Have a human learning reviewer play every route and check factual accuracy, fairness, readability, child suitability, and learning value.

## Safe content changes

Changing visible text behind an existing key is lower risk than changing IDs or score semantics. A released ID/version change needs migration and analytics review. Never add learner free text, names, email addresses, school details, credentials, or secrets to scenario or completion data.

## Acceptance

No validator issues; no orphaned keys; English review recorded; Malay status honest; all paths terminate; answer ordering has no quality bias; selected-response IDs remain compatible with the intended content version.

See [gameplay](03-GAMEPLAY-CONTENT.md), [localization](08-ACCESSIBILITY-LOCALIZATION.md), and [asset manifest/release governance](16-ASSET-MANIFEST.md).
