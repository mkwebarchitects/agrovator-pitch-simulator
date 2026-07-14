# Implement One Pitch Simulator Task

## Objective

Implement exactly `[task name and acceptance criteria]` in the standalone Pitch Simulator worktree.

## Safety and scope

- Confirm the absolute worktree path, branch, base commit, and `git status` before editing.
- Never inspect or modify the external AGROVATOR LMS repository.
- Allowed files: `[explicit paths]`. Files not to change: `[explicit paths]` plus unrelated user changes, generated caches, credentials, and external repositories.
- Read actual code/tests/config before treating a plan statement as implemented fact.
- State assumptions and stop for new authority if external access, credentials, production data, or materially wider scope is needed.

## Execution

1. Create a task brief with source-of-truth contracts and expected verification.
2. For runtime behavior, add the smallest focused failing test and capture the expected RED boundary.
3. Implement the minimum coherent change; keep domain logic engine-free and Unity presenters thin where applicable.
4. Run focused GREEN, then relevant canonical EditMode/PlayMode, builder/build/browser checks in proportion to scope.
5. Restore incidental Unity serialization changes without discarding unrelated user work.
6. Give an independent reviewer the task brief, bounded diff, and evidence; fix findings and obtain re-review.
7. Run fresh final verification after the last fix.

## Required evidence

Report exact commands, exit codes, XML totals/failures/skips, complete-log failure-marker scan, build artifacts or browser/version/outcomes where applicable, scope diff, and unrun checks. Never fabricate a pass or copy an old count as fresh evidence.

## Definition of done

Acceptance criteria are implemented; reviewer approves; fresh checks pass; unknowns/limitations are explicit; only scoped files are committed with `[exact commit message]`; worktree state is reported.
