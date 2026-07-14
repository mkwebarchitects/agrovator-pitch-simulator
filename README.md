# AGROVATOR Pitch Simulator

AGROVATOR Pitch Simulator is a standalone, browser-targeted Unity learning game. Its vertical slice teaches learners to pitch a Smart School Garden through branching judge dialogue, constructive scoring, accessible interaction, and a mock LMS completion flow.

This repository is intentionally independent of the AGROVATOR LMS repository. Do not read from or write to the LMS repository while working here.

## Requirements

- Unity `6000.5.3f1`
- Windows PowerShell 5.1 or PowerShell 7+
- Unity WebGL Build Support for the configured editor version

## Open the project

From the repository root:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.3f1\Editor\Unity.exe' -projectPath $PWD
```

## Run tests

```powershell
powershell -ExecutionPolicy Bypass -File tools/Run-UnityTests.ps1 -Platform EditMode
powershell -ExecutionPolicy Bypass -File tools/Run-UnityTests.ps1 -Platform PlayMode
```

The wrapper writes XML results to `artifacts/test-results` and Unity logs to `artifacts/logs`. It fails on test failures, compilation failures, unhandled exceptions, missing result XML, or a non-zero Unity exit code.

## Build WebGL

```powershell
powershell -ExecutionPolicy Bypass -File tools/Build-WebGL.ps1
```

The build wrapper calls `Agrovator.PitchSimulator.Editor.WebGlBuild.BuildDevelopment`. That editor method is scheduled for Task 18, so the wrapper intentionally fails until the build implementation and `Build/WebGL/index.html` exist.

## Current milestone

Task 1 establishes the Unity `6000.5.3f1` repository foundation, minimum packages, project rules, and repeatable verification commands. See `TASKS.md` for current evidence and the next action.

The planned architecture keeps gameplay and dialogue rules in pure C# modules, uses thin Unity uGUI presenters, and introduces one explicit bootstrap composition root only when the UI shell is implemented.
