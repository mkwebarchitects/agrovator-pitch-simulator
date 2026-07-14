# AGROVATOR Pitch Simulator

AGROVATOR Pitch Simulator is a standalone Unity WebGL learning game. The vertical slice guides a learner through a Smart School Garden pitch, branching Judge Aya dialogue, formative scoring, accessible interaction, results/review/retry, and a local mock LMS completion flow.

This repository is intentionally independent of the external AGROVATOR LMS repository. Do not inspect, read, or write that repository while working here.

## Current status

Implementation through Task 16 is complete. The confirmed checkpoint is Unity `6000.5.3f1`, EditMode `296/296`, and PlayMode `35/35`. A development WebGL build is pending Task 18 and browser/manual evidence is pending Task 19. The local same-origin `postMessage` harness is not proof of production LMS compatibility. Malay is `pending_human_review`; audio clip slots are placeholders.

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

At this checkpoint, the wrapper intentionally cannot create `Build/WebGL/index.html` until Task 18 implements the editor build method. After Task 18, serve the repository over HTTP and open `WebHarness/index.html`; do not use a `file:` URL. Follow [web deployment](docs/10-WEB-DEPLOYMENT.md).

## Documentation map

- Product and learner: [overview](docs/01-PRODUCT-OVERVIEW.md), [learner experience](docs/02-LEARNER-EXPERIENCE.md)
- Content and architecture: [gameplay](docs/03-GAMEPLAY-CONTENT.md), [authoring](docs/04-CONTENT-AUTHORING.md), [architecture](docs/05-TECHNICAL-ARCHITECTURE.md)
- Delivery: [LMS discovery](docs/00-LMS-DISCOVERY.md), [QA](docs/13-QA-PLAN.md), [operations](docs/14-OPERATIONS-TROUBLESHOOTING.md), [roadmap](docs/15-PRODUCTION-ROADMAP.md)
- Handoffs: [Codex/Claude workflow](docs/17-CODEX-CLAUDE-WORKFLOW.md) and reusable prompts under `prompts/`

## Known limitations

No production endpoints, credentials, SCORM/xAPI support, compliance certification, WebGL build evidence, or browser support claim is included. Safari is unavailable on the Windows verification host. Final Malay, audio, production LMS, accessibility, privacy/security, hosting, and release approvals require qualified humans.
