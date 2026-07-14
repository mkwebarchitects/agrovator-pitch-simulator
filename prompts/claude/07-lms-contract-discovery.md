# Structure LMS Contract Discovery

## Objective

Turn owner-provided production LMS answers and synthetic examples into a gap analysis against the provisional local bridge.

## Allowed Files

Read `docs/00-LMS-DISCOVERY.md`, `docs/11-LMS-CONTRACT.md`, ADR 0003, local LMS DTO/interface/bridge sources, and sanitized materials supplied directly by the owner.

## Files Not To Change

Do not inspect or modify the external AGROVATOR LMS repository, request production data/credentials, invent endpoints, or change code in this analysis task.

## Required Output

Provide field/error/transport/origin/retry mappings, confirmed/unknown/conflict labels, risks, and decisions requiring LMS/security owners.

## Required Tests

Define synthetic contract cases for every confirmed outcome and malformed boundary; do not claim they pass until executed.

## Definition of Done

Every external fact cites supplied evidence, provisional assumptions are visible, secrets/PII are absent, and ADR revision inputs are ready.
