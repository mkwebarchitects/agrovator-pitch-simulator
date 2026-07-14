# Navigate the Unity Architecture

## Composition

`Assets/Scenes/Bootstrap.unity` owns the persistent `Bootstrapper`, LMS bridge selection, settings/localization construction, audio service, and additive loading of `Game`. `Assets/Scenes/Game.unity` owns one Canvas, one EventSystem, the `GameScreenRouter`, and five screen panels. `Assets/Scenes/WebIntegrationTest.unity` is a generated diagnostic scene and is excluded from default build order.

## Assembly boundaries

- `Agrovator.PitchSimulator.Core`: state machine, timer, save model; engine-free.
- `Agrovator.PitchSimulator.Dialogue`: DTO validation and immutable runtime graph; engine-free.
- `Agrovator.PitchSimulator.Scoring`: accumulator, confidence, results; engine-free.
- `Agrovator.PitchSimulator.Accessibility`: settings and catalog; engine-free.
- `Agrovator.PitchSimulator.Session`: orchestration across the above and LMS; engine-free.
- `Agrovator.PitchSimulator.Dialogue.Unity`: ScriptableObject import boundary.
- `Agrovator.PitchSimulator.LMS`: Unity JSON and WebGL/native bridge boundary.
- `Agrovator.PitchSimulator.Audio`: plain service plus Unity audio adapters.
- `Agrovator.PitchSimulator.UI`: thin uGUI presenters and composition.

The `PitchSessionController` owns game rules and emits immutable snapshots/events. `Bootstrapper` is the sole production unscaled clock bridge. Presenters render snapshots and forward commands; they do not duplicate session rules.

## Change rules

Keep deterministic logic outside MonoBehaviours, preserve one composition root, maintain assembly direction, and regenerate scenes through `PitchSimulatorProjectBuilder` rather than ad hoc hierarchy drift. See [state flow](06-STATE-SESSION-FLOW.md), [ADR 0002](adr/0002-three-scene-state-driven-ui.md), and [automation ADR](adr/0004-unity-automation-strategy.md).
