# Repository Instructions

These instructions apply to the entire repository.

## Development workflow

- Work test-first: add or update a focused test, confirm the expected RED result, implement the smallest change, then run focused and full relevant suites.
- Use `tools/Run-UnityTests.ps1` as the canonical Unity test entry point. Parse its XML result and inspect its log; do not rely on Unity's process exit code alone.
- Keep reusable rules in pure C# where practical. Keep `MonoBehaviour` classes thin, dependencies explicit, and startup sequencing owned by one future bootstrap composition root.
- Use only the dependencies justified by the approved implementation plan. Do not introduce a framework or package opportunistically.
- Update `TASKS.md` before every task commit with fresh verification evidence and the next unchecked action.
- Bump `PitchSimulatorProjectBuilder.GeneratorVersion` whenever you change what the scene builders generate. Owned scenes only regenerate when that stamp differs, so skipping the bump leaves the saved scene stale with no failing test.
- Build the WebGL player learners download with `tools/Build-WebGL.ps1 -Release`. The default development build is roughly `92 MB` against the release build's `8 MB` and must never be deployed.

## Repository boundary and privacy

- Never access, inspect, modify, execute commands in, or copy files from the AGROVATOR LMS repository while working on this project.
- Never commit access tokens, session tokens, secrets, learner names, email addresses, school identifiers, or other private learner data.
- Use pseudonymous identifiers and sanitized logs. Do not log full LMS launch or completion payloads.
- Keep generated `Library`, build output, test artifacts, and local editor state untracked.
