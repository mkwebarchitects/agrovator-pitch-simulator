# Hand Work Safely Between Codex, Claude, and Humans

## Choose the owner

Use Codex for bounded repository implementation, tests, builds, static checks, and evidence collection. Use Claude for constrained drafting, content alternatives, review synthesis, and edge-case ideation. Humans own external facts, production credentials/environments, pedagogy, translation, privacy/security, accessibility, creative rights/quality, browser manual checks, and release approval.

## Handoff sequence

1. Start from a named branch/worktree and clean status. Never inspect or modify the external AGROVATOR LMS repository.
2. State one objective, allowed files, files not to change, source-of-truth facts, unknowns, and acceptance criteria.
3. For code, require a failing focused test before production changes when practical, then focused green, broad suites, independent review, and fresh final evidence.
4. For content/docs, require project paths, assumption labels, human-review boundaries, and validation for links/placeholders/scope.
5. Report exact commands, exit status, XML counts or artifact hashes. Say “not run” when evidence is absent.
6. Commit only scoped files; keep generated/cache/test artifacts out unless intentionally required.

## Prompt library

Use `prompts/claude/` for ten constrained product/content/QA handoffs. Use `prompts/codex/01-implement-task.md` for one implementation and `02-verify-unity-milestone.md` for independent milestone verification. Replace bracketed inputs before use and preserve every safety constraint.

## Review rule

The implementer does not approve their own work. A reviewer checks diff and evidence against the task, then the implementer fixes findings and obtains re-review. Human approval remains mandatory where automation cannot establish truth.

See [roadmap](15-PRODUCTION-ROADMAP.md), [QA](13-QA-PLAN.md), and [governance](16-RELEASE-GOVERNANCE.md).
