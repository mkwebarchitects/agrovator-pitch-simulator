# Navigate the Unity Architecture

## Composition

`Assets/Scenes/Bootstrap.unity` owns the persistent `Bootstrapper`, LMS bridge selection, settings/localization construction, audio service, and additive loading of `Game`. `Assets/Scenes/Game.unity` owns one Canvas, one EventSystem, the `GameScreenRouter`, and six screen panels: Title, Briefing, Tutorial, PitchRoom, Results, and Settings. `Assets/Scenes/WebIntegrationTest.unity` is a generated diagnostic scene and is excluded from default build order.

## Assembly boundaries

- `Agrovator.PitchSimulator.Core`: state machine, timer, save model; engine-free.
- `Agrovator.PitchSimulator.Dialogue`: DTO validation and immutable runtime graph; engine-free.
- `Agrovator.PitchSimulator.Scoring`: accumulator, confidence, results; engine-free.
- `Agrovator.PitchSimulator.Accessibility`: settings and catalog; engine-free.
- `Agrovator.PitchSimulator.Session`: orchestration across the above and LMS; engine-free.
- `Agrovator.PitchSimulator.Dialogue.Unity`: ScriptableObject import boundary.
- `Agrovator.PitchSimulator.LMS`: Unity JSON and WebGL/native bridge boundary.
- `Agrovator.PitchSimulator.Audio`: plain service plus Unity audio adapters; the UI assembly's `AudioCueDirector` maps user gestures, session events, and the final-five timer threshold onto the nine cue slots.
- `Agrovator.PitchSimulator.UI`: thin uGUI presenters and composition.

The `PitchSessionController` owns game rules and emits immutable snapshots/events. `Bootstrapper` is the sole production unscaled clock bridge. Presenters render snapshots and forward commands; they do not duplicate session rules.

`TutorialPresenter` owns only its three-page local index. Back and Next do not mutate the session; Skip and Start Practice each invoke the existing session Continue command once. Generated screen cards are centered at the 1280x720 reference: Title `760x500`, Briefing `880x520`, Tutorial `920x560`, Settings `720x420`, and PitchRoom/Results at no more than `960x680`. Gameplay responses are capped at `680px`; normal actions are capped at `520px`; Tutorial navigation renders at `180/180/420px` for Back/Skip/Next.

## Change rules

Keep deterministic logic outside MonoBehaviours, preserve one composition root, maintain assembly direction, and regenerate scenes through `PitchSimulatorProjectBuilder` rather than ad hoc hierarchy drift. See [state flow](06-STATE-SESSION-FLOW.md), [ADR 0002](adr/0002-three-scene-state-driven-ui.md), and [automation ADR](adr/0004-unity-automation-strategy.md).
