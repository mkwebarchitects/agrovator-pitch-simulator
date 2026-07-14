# Independently Verify a Unity Milestone

## Objective

Verify `[milestone/commit range]` against `[brief/spec/acceptance document]` without changing production behavior.

## Safety and scope

- Work only in the named standalone Pitch Simulator worktree; record absolute path, branch, HEAD, base, and initial status.
- Never inspect or modify the external AGROVATOR LMS repository.
- Treat existing reports as claims to reproduce, not proof. Do not stage/commit unless explicitly authorized.
- Preserve unrelated changes and classify any Unity-generated churn before restoring it safely.

## Verification procedure

1. Read the acceptance criteria, bounded diff, runtime code, tests, scene/build settings, and relevant docs.
2. Trace each criterion to code plus an observable test or manual check; flag untestable claims.
3. Run focused fixtures for changed behavior. If a missing boundary is found and test edits are authorized, demonstrate RED before asking for an implementation fix.
4. Run fresh canonical EditMode and PlayMode suites.
5. If in scope, run the builder twice, development WebGL build, artifact inventory, local HTTP browser matrix, and manual accessibility/audio/fullscreen/refresh/touch checks.
6. Parse XML and complete logs independently; scan for compile failures, unhandled exceptions, missing results, failures, skips, and inconclusive tests.
7. Check `git diff --check`, required/forbidden files, build order, secret/PII/logging patterns, documentation links/claims, and final status.

## Independent review output

List findings by severity with file/line and reproduction evidence. Then give a criterion matrix: Passed, Failed, or Not Run. Include exact commands, exit codes, fresh counts, browser/version, artifact paths/hashes, and limitations. Never fabricate or infer a pass from source alone.

## Definition of done

Every milestone criterion has evidence/status, all blocking findings are fixed and re-reviewed or explicitly remain open, final evidence is fresh after fixes, and the release recommendation matches the documented limitations.
