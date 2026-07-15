# AGROVATOR Pitch Simulator

AGROVATOR Pitch Simulator is a standalone Unity WebGL learning game. The vertical slice guides a learner through a Smart School Garden pitch, branching Judge Aya dialogue, formative scoring, accessible interaction, results/review/retry, and a local mock LMS completion flow.

This repository is intentionally independent of the external AGROVATOR LMS repository. Do not inspect, read, or write that repository while working here.

## Current status

The Task 20 vertical-slice audit and the subsequent UI-polish gate are recorded for Unity `6000.5.3f1`. Fresh 2026-07-15 final-review evidence is EditMode `311/311`, PlayMode `39/39`, Node `14/14`, a `Succeeded` `92,374,282`-byte development WebGL BuildReport in `00:00:07.2820626` with zero warnings/errors, and passing local smoke runs in Chrome `150.0.7871.116` and Edge `150.0.4078.65` with zero console/page errors. The learner now sees a dedicated three-page tutorial on every attempt, with Back, Skip, Next, and Start Practice behavior, while all six game screens use centered, contained cards. The final Pitch Room regression iterates every authored prompt/outcome, response, and confidence label at `1280x720`; the retained Chrome checkpoint visibly includes the complete `system?`, `inconsistent.`, and `Curious` endings. This accepts standalone local implementation evidence, not a production release: the same-origin mock harness is not proof of production LMS compatibility, Malay remains `pending_human_review`, all audio clips remain placeholders, and human release gates remain open.

See [vertical-slice acceptance](docs/18-VERTICAL-SLICE-ACCEPTANCE.md) and [task evidence](TASKS.md).

## Requirements

- Unity `6000.5.3f1`
- Windows PowerShell 5.1 or PowerShell 7+
- Unity WebGL Build Support for that editor version

## Open the project

From this repository root:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.3f1\Editor\Unity.exe' -projectPath $PWD
```

Default build scenes are `Assets/Scenes/Bootstrap.unity` then `Assets/Scenes/Game.unity`. `Assets/Scenes/WebIntegrationTest.unity` is a diagnostic scene excluded from default build order.

## Run tests

```powershell
powershell -ExecutionPolicy Bypass -File tools/Run-UnityTests.ps1 -Platform EditMode
powershell -ExecutionPolicy Bypass -File tools/Run-UnityTests.ps1 -Platform PlayMode
```

The wrapper writes XML to `artifacts/test-results` and logs to `artifacts/logs`. Always report fresh XML totals and scan the complete log; historical counts above are not a substitute after code changes.

## Build WebGL

```powershell
powershell -ExecutionPolicy Bypass -File tools/Build-WebGL.ps1
```

The wrapper creates the ignored development artifact at `Build/WebGL/index.html`. Serve the repository over HTTP and open `WebHarness/index.html`; do not use a `file:` URL. Follow [web deployment](docs/10-WEB-DEPLOYMENT.md) for the exact artifact inventory and local smoke procedure.

## Documentation map

- Product and learner: [overview](docs/01-PRODUCT-OVERVIEW.md), [learner experience](docs/02-LEARNER-EXPERIENCE.md)
- Content and architecture: [gameplay](docs/03-GAMEPLAY-CONTENT.md), [authoring](docs/04-CONTENT-AUTHORING.md), [architecture](docs/05-TECHNICAL-ARCHITECTURE.md)
- Delivery: [LMS discovery](docs/00-LMS-DISCOVERY.md), [QA](docs/13-QA-PLAN.md), [operations](docs/14-OPERATIONS-TROUBLESHOOTING.md), [roadmap](docs/15-PRODUCTION-ROADMAP.md), [assets and release governance](docs/16-ASSET-MANIFEST.md)
- Handoffs: [Codex/Claude workflow](docs/17-CODEX-CLAUDE-WORKFLOW.md) and reusable prompts under `prompts/`

## Known limitations

No production endpoints, credentials, SCORM/xAPI support, compliance certification, production browser-support promise, or repository-wide licence is included. Local development evidence covers Chrome and Edge only; Firefox was unavailable at standard Windows paths and Safari is unavailable on the Windows host. Final Malay, audio, real LMS submission, classroom usability, assistive-technology/accessibility review, native touch, unrestricted fullscreen, privacy/security, hosting, creative/legal authority, and release approval require qualified humans or suitable environments.
