# Deliver the Pitch Simulator in 16 Phases

Estimates are planning ranges in developer-days, not measured delivery time. Codex may implement and verify bounded repository work; Claude may draft/review content; humans own product, pedagogy, language, privacy, security, visual/audio, LMS, browser, and release decisions.

## Phase 1 — Confirm product and learning outcomes

- **Objective:** approve learner audience, outcome, success criteria, and formative-use boundary.
- **Tasks:** review the Smart School Garden flow, rubric interpretation, child suitability, and exclusions.
- **Dependencies:** current vertical slice and product stakeholders.
- **Deliverables:** signed product/learning brief and decision log.
- **Acceptance criteria:** owners approve scope; assessment is not represented as certification.
- **Risks:** ambiguous outcomes or unintended high-stakes use.
- **Estimate:** 1-2 developer-days plus human review.
- **Ownership:** Codex maps implementation; Claude drafts review questions; humans decide.

## Phase 2 — Discover the LMS contract

- **Objective:** replace assumptions with an approved production integration specification.
- **Tasks:** execute `00-LMS-DISCOVERY.md`, obtain synthetic examples, origins, security, retry, expiry, and hosting rules.
- **Dependencies:** LMS owner and test environment.
- **Deliverables:** dated field/transport/error contract.
- **Acceptance criteria:** LMS, security, and product owners sign; ADR 0003 is revised.
- **Risks:** unavailable owners, incompatible embedding, or hidden data requirements.
- **Estimate:** 2-5 developer-days.
- **Ownership:** Codex compares code; Claude structures findings; humans provide/approve contract.

## Phase 3 — Threat model and privacy approval

- **Objective:** approve minimal data flow and browser trust boundary.
- **Tasks:** map data, retention, access, logging, CSP/frame policy, abuse cases, and incidents.
- **Dependencies:** Phase 2 and privacy/security owners.
- **Deliverables:** threat model, privacy record, remediation list.
- **Acceptance criteria:** no unapproved PII/free text; severity owners and release gates assigned.
- **Risks:** overcollection, token leakage, origin weakening.
- **Estimate:** 2-4 developer-days.
- **Ownership:** Codex inventories fields; Claude drafts scenarios; humans approve controls.

## Phase 4 — Harden content and rubric

- **Objective:** approve pedagogy and fair response design.
- **Tasks:** play every route, review facts, bias, answer cues, feedback tone, deltas, and result language.
- **Dependencies:** Phase 1.
- **Deliverables:** reviewed scenario/rubric revision.
- **Acceptance criteria:** all paths terminate and meet agreed learning criteria; stable IDs/version documented.
- **Risks:** misleading claims or score incentives.
- **Estimate:** 3-6 developer-days.
- **Ownership:** Codex validates graph; Claude supports copy review; humans own pedagogy.

## Phase 5 — Complete English and Malay content

- **Objective:** ship reviewed, culturally appropriate learner copy.
- **Tasks:** English editorial pass, qualified Malay translation/review, key-parity checks, in-context play.
- **Dependencies:** Phase 4.
- **Deliverables:** reviewed `en`/`ms` catalogs with approval evidence.
- **Acceptance criteria:** no fallback appears in Malay; terminology and layout approved.
- **Risks:** translation ambiguity or text overflow.
- **Estimate:** 2-5 developer-days plus translator time.
- **Ownership:** Codex checks parity/layout; Claude may draft only; humans translate and approve.

## Phase 6 — Integrate production LMS transport

- **Objective:** implement the approved contract behind `ILmsBridge`.
- **Tasks:** write failing contract tests, adapt launch/completion/error mapping, protect secrets, preserve retry behavior.
- **Dependencies:** Phases 2-3.
- **Deliverables:** production adapter and synthetic integration tests.
- **Acceptance criteria:** RED/GREEN evidence, sanitized errors, idempotency/retry/expiry proven in test environment.
- **Risks:** async races, schema drift, duplicated completion.
- **Estimate:** 4-8 developer-days.
- **Ownership:** Codex implements/tests; Claude reviews contract docs; humans provide environment and approve.

## Phase 7 — Automate deterministic WebGL builds

- **Objective:** qualify the repeatable Task 18 development build process for production release artifacts.
- **Tasks:** repeat the existing editor method/wrapper on the release candidate, validate scene order/logs/artifacts, and define/approve release compression, symbols, hosting, versioning, and rollback settings.
- **Dependencies:** Unity 6000.5.3f1 WebGL module.
- **Deliverables:** retained build scripts, release-candidate artifact/report, and approved release settings.
- **Acceptance criteria:** a clean release-candidate build exits zero, artifact inventory matches expectations, and human owners approve release settings.
- **Risks:** module/version drift or non-deterministic scene changes.
- **Estimate:** 2-4 developer-days.
- **Ownership:** Codex implements/verifies; Claude reviews runbook; humans approve release settings.

## Phase 8 — Establish deployment environment

- **Objective:** serve player/harness-compatible assets securely.
- **Tasks:** configure HTTPS, MIME/compression/cache/CSP/frame headers, version paths, monitoring, rollback.
- **Dependencies:** Phases 2, 3, and 7.
- **Deliverables:** staging deployment and operator runbook.
- **Acceptance criteria:** clean staging load and rollback drill; headers approved.
- **Risks:** stale caches, wrong compression, blocked frames.
- **Estimate:** 3-6 developer-days.
- **Ownership:** Codex verifies artifacts; Claude refines runbook; humans operate/approve infrastructure.

## Phase 9 — Run desktop browser compatibility

- **Objective:** extend the existing Task 19/20 local Chrome/Edge smoke into an agreed production browser-support decision.
- **Tasks:** repeat Chrome/Edge against staging, add Firefox and Safari on suitable hosts if required, and verify refresh, unrestricted fullscreen, native touch, and failure modes; preserve approved limitations where coverage is unavailable.
- **Dependencies:** Phases 7-8.
- **Deliverables:** staging browser/device evidence matrix, approved limitations, and defects.
- **Acceptance criteria:** agreed browsers pass critical paths or have approved limitations.
- **Risks:** WebGL/browser policy differences.
- **Estimate:** 2-4 developer-days.
- **Ownership:** Codex automates safe checks; Claude summarizes; humans perform/approve manual checks.

## Phase 10 — Validate accessibility

- **Objective:** meet the agreed accessibility target with real users/tools.
- **Tasks:** keyboard, focus, zoom, contrast, reduced motion, timer modes, assistive technology, comprehension.
- **Dependencies:** Phases 5 and 9.
- **Deliverables:** audit, fixes, retest evidence.
- **Acceptance criteria:** no unresolved release-blocking accessibility issue; human sign-off.
- **Risks:** automated checks missing interaction barriers.
- **Estimate:** 3-7 developer-days plus specialist review.
- **Ownership:** Codex tests contracts; Claude drafts scripts; humans audit and approve.

## Phase 11 — Finish licensed audio and visual review

- **Objective:** replace silence/placeholders with approved assets without regressions.
- **Tasks:** source/license nine clips, import, loudness/loop/autoplay checks, final visual review.
- **Dependencies:** product identity and accessibility review.
- **Deliverables:** provenance records and approved assets.
- **Acceptance criteria:** rights verified; child suitability, audio controls, contrast, and reduced motion pass.
- **Risks:** licensing gaps or sensory overload.
- **Estimate:** 2-5 developer-days plus creative production.
- **Ownership:** Codex integrates; Claude maintains records; humans create/license/approve.

## Phase 12 — Add observability and support safeguards

- **Objective:** diagnose failures without exposing learner data.
- **Tasks:** define sanitized events, error IDs, metrics, alert ownership, support evidence template.
- **Dependencies:** Phases 2-3 and deployment design.
- **Deliverables:** observability schema and support guide.
- **Acceptance criteria:** approved allowlist; no full payloads/tokens/PII in telemetry.
- **Risks:** data leakage or insufficient diagnostics.
- **Estimate:** 2-4 developer-days.
- **Ownership:** Codex implements allowlists; Claude drafts guidance; humans approve retention/alerts.

## Phase 13 — Performance and resilience test

- **Objective:** set and demonstrate acceptable load/startup/recovery behavior.
- **Tasks:** define budgets, profile WebGL, throttled-network tests, refresh/session-expiry/retry/cache tests.
- **Dependencies:** staging build and observability.
- **Deliverables:** measured report and prioritized fixes.
- **Acceptance criteria:** human-approved budgets met; no duplicate completion or unrecoverable critical path.
- **Risks:** large assets, memory pressure, flaky networks.
- **Estimate:** 3-6 developer-days.
- **Ownership:** Codex captures repeatable measures; Claude analyzes; humans set budgets/approve.

## Phase 14 — Conduct security and release-candidate QA

- **Objective:** close release-blocking defects on a frozen candidate.
- **Tasks:** dependency/config review, penetration scope, full regression, content/localization/LMS/browser checks.
- **Dependencies:** Phases 3-13.
- **Deliverables:** signed defect list, retests, release candidate.
- **Acceptance criteria:** zero open blockers; accepted residual risks recorded.
- **Risks:** late contract or content changes.
- **Estimate:** 4-8 developer-days plus external review.
- **Ownership:** Codex runs regression; Claude assembles evidence; humans test security and accept risks.

## Phase 15 — Pilot and support readiness

- **Objective:** validate real workflow with a controlled learner cohort.
- **Tasks:** support training, consent/communications, pilot monitoring, feedback, incident/rollback exercise.
- **Dependencies:** approved release candidate.
- **Deliverables:** pilot report and launch decision.
- **Acceptance criteria:** support/operations ready; critical pilot issues resolved.
- **Risks:** environment or comprehension issues absent in lab testing.
- **Estimate:** 3-7 developer-days plus pilot calendar time.
- **Ownership:** Codex supports fixes; Claude synthesizes feedback; humans run pilot/decide.

## Phase 16 — Production release and lifecycle

- **Objective:** release safely and maintain evidence over time.
- **Tasks:** final gates, version/tag, deploy, smoke, monitor, rollback readiness, review cadence, deprecation/migration policy.
- **Dependencies:** Phase 15 approval.
- **Deliverables:** production release record, dashboards, maintenance schedule.
- **Acceptance criteria:** owner sign-offs, production smoke, monitoring and rollback confirmed.
- **Risks:** config drift, stale content, unsupported Unity/browser versions.
- **Estimate:** 2-4 developer-days for release, then recurring maintenance.
- **Ownership:** Codex verifies repository/artifacts; Claude maintains handoff; humans authorize and operate release.

See [workflow](17-CODEX-CLAUDE-WORKFLOW.md), [asset manifest/release governance](16-ASSET-MANIFEST.md), and [acceptance](18-VERTICAL-SLICE-ACCEPTANCE.md).
