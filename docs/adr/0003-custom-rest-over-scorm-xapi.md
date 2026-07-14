# ADR 0003: Provisional Custom Integration Boundary

## Decision

Use the local custom REST/postMessage-shaped `ILmsBridge` contract provisionally for the vertical slice. Do not claim a production REST API exists. Defer SCORM, xAPI, LTI, or another standard until LMS discovery establishes requirements.

## Context

The external LMS repository/API was deliberately not inspected. The game needs launch configuration, completion submission, retries, expiry, and safe local development now. The implemented harness is a version-1 same-origin mock.

## Options considered

- Adopt SCORM without LMS confirmation.
- Adopt xAPI/LRS without LMS confirmation.
- Build directly against an assumed production REST endpoint.
- Define a narrow game-side interface and provisional postMessage mock.

## Chosen option

Narrow `ILmsBridge` plus validated DTOs, mock modes, and same-origin postMessage harness.

## Why this option won

It enables deterministic gameplay/integration tests without inventing endpoints, credentials, standards support, or production semantics.

## Consequences

The adapter may change after discovery. Synthetic contract tests and DTO mapping isolate that change. No SCORM/xAPI conformance, production compatibility, uptime, or security certification is implied.

## Revisit triggers

Revisit immediately when the LMS owner supplies an approved transport/schema, platform standard requirements, trusted origins, security controls, and a synthetic test environment.
