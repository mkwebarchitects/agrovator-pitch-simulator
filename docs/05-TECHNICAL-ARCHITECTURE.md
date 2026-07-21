# Navigate the Guided Pitch Architecture

## Composition

`Assets/Scenes/Bootstrap.unity` owns the persistent `Bootstrapper`, validated LMS bridge, settings, localization, guided content loading, audio service, session controller, and additive loading of `Game`. It references exactly one guided content asset, `Assets/Content/Scenarios/guided-pitch-builder.en.json` (GUID `07bbb68d99b325549a9a4904cfadd53e`). Localization loads before content validation.

`Assets/Scenes/Game.unity` owns one Canvas, one EventSystem, the generated guided screen hierarchy, `GuidedPitchScreenRouter`, thin presenters, responsive layout, and safe-fallback UI. `Assets/Scenes/WebIntegrationTest.unity` remains a diagnostic scene outside default build order.

`PitchSimulatorProjectBuilder` and `GuidedPitchSceneBuilder` own generated scene structure. Regenerate through those builders instead of making untracked hierarchy changes.

## Assembly boundaries

- `Agrovator.PitchSimulator.GuidedPitch`: engine-independent content DTO/load/validation, types, four-part draft, and assessment rules.
- `Agrovator.PitchSimulator.Session`: `GuidedPitchSessionController`, immutable snapshots/events, phase commands, payload construction, reset, and submission callback ownership.
- `Agrovator.PitchSimulator.Accessibility`: validated settings and the English catalog.
- `Agrovator.PitchSimulator.LMS`: the unchanged DTO/serialization and mock/WebGL bridge boundary.
- `Agrovator.PitchSimulator.Audio`: cue service and Unity adapters.
- `Agrovator.PitchSimulator.UI`: thin routing, views, focus, results, recovery, responsive layout, and composition.
- The older Core/Dialogue/Scoring session path has been retired. `GameScreenRouter`, `PitchRoomPresenter`, `ResultsPresenter`, `TutorialPresenter`, `PitchSessionController` and its snapshots/events, the legacy views (`TimerView`, `ConfidenceView`, `ResponseListView`, `ResponseButtonView`, `QuestionReviewItemView`, `KeyboardReviewScrollbar`, `FocusNavigator`), the `GameState`/`GameStateMachine`/`GameCommand`/`QuestionTimer` state machine, the whole `Agrovator.PitchSimulator.Dialogue`, `Agrovator.PitchSimulator.Dialogue.Unity` and `Agrovator.PitchSimulator.Scoring` assemblies, their tests, and the legacy fixture asset `Assets/Content/Scenarios/smart-school-garden.en.json` were all deleted; `AudioCueDirector` lost its legacy `HandleSessionEvent`/`UpdateTimer` entry points with them. `LegacyClusterRetirementTests` keeps them gone, and the generated-scene guard still asserts none of the retired presentation appears in an owned scene. `JudgeReactionView`/`JudgeReactionMapper`/`JudgeReactionSpriteSet` and `AudioCueDirector` itself are live guided-path code and were kept.

The engine-independent `PitchDraft` records each part's initial response ID/mastery, current response ID/mastery, and revision flag. `PitchAssessmentBuilder` computes from the current snapshot. `GuidedPitchSessionController` is the single rule owner; presenters render snapshots and forward commands.

## Responsive and WebGL boundary

The WebGL template sizes the stage from available shell width and viewport height. Its render scale is `clamp(devicePixelRatio, 1, 2)`, so backing dimensions follow displayed resolution while capping memory growth. Three display-only JavaScript exports report CSS width, CSS height, and DPR; they carry no learner, launch, content, or completion data.

Unity switches to compact layout at CSS width `<= 960` or aspect ratio `< 1.25`. Wide mode uses one-row pitch/card layouts. Compact mode uses a two-column board, one-column cards, stacked action groups, and scrolling. Point filtering is retained for pixel art while text and controls render at the full UI backing resolution.

Fresh browser evidence measured `1276x918` CSS/backing at desktop and `380x783` at mobile in both Chrome and Edge, DPR/render scale `1`, focus retained, containment true, and zero inner/outer horizontal overflow. Higher-DPR formulas have automated source/math coverage but were not exercised by that runtime matrix.

## Compatibility boundaries

`LmsLaunchConfig` has no learner-mode/category field. Primary or Secondary is selected locally after Briefing. `Elementary -> Primary` is conceptual documentation, not code. The `14` launch fields and `19` completion fields/types are unchanged from the approved v1 boundary even though supported content is now v2.

The learner sees Pitch Readiness. The payload's existing `FinalConfidence` stays hidden and keeps legacy delta semantics. Selection history uses the existing `SelectedResponseIds` array; no new transport or DTO field was introduced.

See [state flow](06-STATE-SESSION-FLOW.md), [LMS contract](11-LMS-CONTRACT.md), [ADR 0002](adr/0002-three-scene-state-driven-ui.md), and [automation ADR](adr/0004-unity-automation-strategy.md).
