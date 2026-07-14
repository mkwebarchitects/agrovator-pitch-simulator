# ADR 0001: Standalone Unity Repository

## Decision

Keep Pitch Simulator in its own Unity repository and do not couple its source tree or build to the external AGROVATOR LMS repository.

## Context

The game needs deterministic Unity builds, independent tests, minimal access to learner systems, and an explicit integration boundary. The LMS implementation was unavailable and deliberately not inspected.

## Options considered

- Develop inside the LMS repository.
- Use a shared monorepo/submodule.
- Maintain a standalone Unity repository with a versioned browser contract.

## Chosen option

Standalone Unity repository with synthetic local integration fixtures.

## Why this option won

It reduces accidental coupling/data access, keeps Unity metadata/build ownership clear, and lets both systems evolve behind an explicit contract.

## Consequences

Contract discovery, versioning, end-to-end environments, and coordinated release ownership are mandatory. Local harness success is not production compatibility evidence.

## Revisit triggers

Revisit if governance mandates a shared release system, a supported contract package becomes available, or duplication materially harms delivery without weakening isolation.
