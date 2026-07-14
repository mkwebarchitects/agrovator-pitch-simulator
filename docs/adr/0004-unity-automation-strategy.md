# ADR 0004: Unity Automation Strategy

## Decision

Test domain behavior in engine-free EditMode assemblies, verify scene/browser-facing interaction in PlayMode, generate owned scenes with an idempotent editor builder, and run batch wrappers that parse XML plus complete logs.

## Context

Unity can exit deceptively, serialize incidental project changes, and couple logic to scene lifecycle. The project needs trustworthy checkpoints and reviewable scene output.

## Options considered

- Manual editor verification only.
- End-to-end PlayMode/browser tests only.
- Layered EditMode/PlayMode/builder/build/browser verification.

## Chosen option

Layered automation with focused RED/GREEN, broad canonical suites, builder two-run checks, WebGL build checks, browser smoke, and independent review.

## Why this option won

It localizes failures, keeps rules fast/testable, catches scene wiring, and requires artifact/log evidence instead of trusting process exit alone.

## Consequences

Wrappers and expected evidence must be maintained. Generated scene/settings churn must be scoped carefully. Browser/manual and human quality checks remain necessary.

## Revisit triggers

Revisit when CI infrastructure, Unity version, build platform, UI stack, or test framework changes, or when suite duration prevents practical feedback.
