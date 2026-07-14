# Govern Changes and Releases

## Required release record

Record commit/tag, Unity version, content/game version, exact test commands and fresh XML totals, build command/artifact inventory, browser matrix, LMS contract revision/environment, localization approvals, asset provenance, known limitations, reviewers, deployment/rollback owner, and date.

## Change classes

- Runtime/scene/bridge changes require focused RED/GREEN where applicable, broad EditMode/PlayMode, generated-scene checks, build, browser smoke, and independent review.
- Content/scoring/ID changes require graph, localization, scoring, compatibility, and human pedagogy review.
- Documentation-only changes require link/marker/heading/scope validation; do not invent runtime evidence.
- Asset changes require import checks, provenance, visual/audio, accessibility, and browser review.

## Human gates

Product/learning approves scope and rubric; LMS owner approves contract; privacy/security approves data/trust boundaries; language reviewer approves Malay; accessibility and creative reviewers approve experience; operations owns hosting, monitoring, and rollback; release authority makes the final decision.

## Version and rollback

Keep response IDs stable within a content version. Bump/version deliberately for incompatible semantics. Preserve a known-good WebGL artifact and configuration, make deployment atomic where possible, and test rollback before launch. Never claim a release from a local unbuilt checkout.

See [roadmap](15-PRODUCTION-ROADMAP.md), [QA](13-QA-PLAN.md), and [acceptance](18-VERTICAL-SLICE-ACCEPTANCE.md).
