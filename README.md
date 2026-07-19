# AGROVATOR Pitch Simulator

AGROVATOR Pitch Simulator is a standalone Unity WebGL learning game. The current guided experience helps a learner assemble and revise a Smart School Garden pitch with four parts: Problem, Evidence, Solution, and Value.

This repository is intentionally independent of the external AGROVATOR LMS repository. Do not inspect, read, or write that repository while working here.

## Current status

The active guided content is `smart-school-garden` version `2`. A learner explicitly chooses **Primary** or **Secondary** in the game because the unchanged LMS launch contract has no learner-category field. `Elementary -> Primary` is a conceptual programme mapping only; it is not implemented as LMS inference.

Both modes use the same untimed flow and visual theme:

1. Read the Briefing and choose a mode.
2. Learn the four-part framework.
3. Build Problem, Evidence, Solution, and Value through four three-card rounds.
4. Read three-part coaching: what worked, what is missing, and how to improve.
5. Optionally strengthen a part, then present the combined pitch.
6. Answer one cost/uncertainty follow-up and review Pitch Readiness and the final pitch.
7. Submit, resubmit after a failure, or use Retry for a fresh Briefing and mode choice.

Primary uses direct coaching and concrete 12-16 word sentence cards. Secondary distinguishes observation from assumption, uses measurements and qualified claims, and asks for audience relevance and honest uncertainty. Completion depends on assembling, reviewing, presenting, and answering the follow-up, not on a score threshold.

Fresh 2026-07-19 local acceptance evidence for Unity `6000.5.3f1` is EditMode `370/370`, PlayMode `48/48`, Node `19/19`, and a `Succeeded` `92,631,312`-byte WebGL BuildReport in `00:00:02.0019569` with zero warnings/errors. Chrome `150.0.7871.127` passed the Primary keyboard route and Edge `150.0.4078.83` passed the Secondary pointer route with zero console/page errors and executed missing-configuration recovery recorded as `modes.missingConfig: true`. This proves the tested implementation behavior, not classroom learning effectiveness or production readiness.

See [vertical-slice acceptance](docs/18-VERTICAL-SLICE-ACCEPTANCE.md) and [task evidence](TASKS.md).

## Requirements

- Unity `6000.5.3f1`
- Windows PowerShell 5.1 or PowerShell 7+
- Unity WebGL Build Support for that editor version
- Node.js for JavaScript contracts and browser smoke

## Open the project

From this repository root:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.3f1\Editor\Unity.exe' -projectPath $PWD
```

Default build scenes are `Assets/Scenes/Bootstrap.unity` then `Assets/Scenes/Game.unity`. `Assets/Scenes/WebIntegrationTest.unity` is a diagnostic scene excluded from default build order. Bootstrap references exactly one active guided asset: `Assets/Content/Scenarios/guided-pitch-builder.en.json`.

## Run the acceptance checks

```powershell
& .\tools\Run-UnityTests.ps1 -Platform EditMode
& .\tools\Run-UnityTests.ps1 -Platform PlayMode
node --check .\tools\smoke-webgl.mjs
node --test .\tools\tests\*.test.js
& .\tools\Build-WebGL.ps1
& .\tools\Serve-WebGL.ps1 -SelfTest
node .\tools\smoke-webgl.mjs
```

The Unity wrapper writes ignored XML under `artifacts/test-results` and complete logs under `artifacts/logs`. Parse the XML root and inspect the full log; do not rely on Unity's process exit code alone. The build writes ignored output to `Build/WebGL`, and browser evidence goes to `artifacts/smoke`.

## Content and LMS compatibility

The authored v2 file contains `30` stable unique option IDs: `12` four-part choices plus `3` follow-up choices per mode. English and Malay each contain `319` localization entries with exact key and fallback-value parity; Malay remains `pending_human_review` and is not an approved translation.

The LMS DTO shape is unchanged: `LmsLaunchConfig` still has `14` fields and `LmsCompletionPayload` has `19`. Guided completion emits six competencies (`problem`, `evidence`, `solution`, `audience`, `clear_explanation`, and `communication`) and omits Time Management. The learner sees Pitch Readiness. The existing `FinalConfidence` field remains a hidden legacy value with its old confidence-delta semantics; it is not renamed or redefined as readiness. Selection history continues through `SelectedResponseIds` and includes initial choices, any revision choice, and the follow-up ID in order.

## Documentation map

- Product and learner: [overview](docs/01-PRODUCT-OVERVIEW.md), [learner experience](docs/02-LEARNER-EXPERIENCE.md)
- Content and architecture: [gameplay](docs/03-GAMEPLAY-CONTENT.md), [authoring](docs/04-CONTENT-AUTHORING.md), [architecture](docs/05-TECHNICAL-ARCHITECTURE.md), [state flow](docs/06-STATE-SESSION-FLOW.md), [scoring](docs/07-SCORING-RESULTS.md)
- Delivery: [LMS contract](docs/11-LMS-CONTRACT.md), [privacy](docs/12-PRIVACY-SECURITY.md), [QA](docs/13-QA-PLAN.md), [assets](docs/16-ASSET-MANIFEST.md), [acceptance](docs/18-VERTICAL-SLICE-ACCEPTANCE.md)

## Unclaimed boundaries

Primary and Secondary educators or representative learners must review reading level, coaching tone, task length, and transfer usefulness before any pedagogical-effectiveness claim. Qualified Malay review, Firefox/Safari, native touch, a real LMS, unrestricted fullscreen, legal approval, classroom evidence, and human accessibility/assistive-technology review remain unclaimed. The local mock harness, automated tests, and screenshots are implementation evidence only.
