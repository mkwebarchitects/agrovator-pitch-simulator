# ADR 0002: Generated Scene Shell and State-Driven UI

## Decision

Use Bootstrap and Game as the default runtime scenes, generate them through one editor builder, and drive five uGUI screens from an explicit state/session model. Keep WebIntegrationTest as a generated diagnostic scene outside build order.

## Context

The game needs persistent composition, deterministic screen/focus contracts, thin presenters, and repeatable scene reconstruction without embedding domain rules in MonoBehaviours.

## Options considered

- One scene with behavior distributed across panels.
- Manually maintained multiple scenes.
- Persistent Bootstrap plus additive Game, generated and state-driven, with separate diagnostics.

## Chosen option

Generated Bootstrap/Game shell with `Bootstrapper`, `PitchSessionController`, and `GameScreenRouter`; separate WebIntegrationTest.

## Why this option won

It keeps composition singular, domain logic testable, scene contracts inspectable, and build order minimal while preserving an integration diagnostic.

## Consequences

Builder changes require scene-contract tests and two-run stability checks. Scene edits outside owned generated roots need care. Presenters must not duplicate state rules.

## Revisit triggers

Revisit for multiple games/scenarios requiring additive content scenes, major UI technology migration, or builder ownership that becomes too coarse.
