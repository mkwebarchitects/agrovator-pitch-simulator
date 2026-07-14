# Pitch Simulator Vertical Slice Implementation Plan

> **For Agent:** REQUIRED: Read and follow `executing-plans/SKILL.md` to implement this plan task-by-task.

**Goal:** Build and verify a browser-playable Smart School Garden pitching game with branching dialogue, scoring, accessibility controls, original presentation, mock LMS integration and a development WebGL build.

**Architecture:** A pure-C# core owns state, dialogue traversal, validation, scoring, confidence and timers. Thin Unity uGUI presenters render immutable snapshots and send commands to an explicit session controller. Authored ScriptableObjects and JSON map into the same validated runtime model; LMS communication is isolated behind `ILmsBridge`.

**Tech Stack:** Unity 6000.5.3f1, C#/.NET Standard 2.1, Unity Test Framework 1.5.1/NUnit, uGUI 2.0.0, Built-in Render Pipeline, Unity WebGL, JavaScript `postMessage` harness, PowerShell build/test scripts.

**Skills to Reference:**
- `superpowers:test-driven-development`
- `superpowers:verification-before-completion`
- `unity-project-scout`
- `unity-architecture`
- `unity-script-roles`
- `unity-testability`
- `unity-asmdef`
- `unity-scene-contracts`
- `unity-ui`
- `unity-test`
- `imagegen`

---

## Execution conventions

Project root:

```text
C:\Users\khidz\projects\pitch-simulator
```

Unity executable:

```text
C:\Program Files\Unity\Hub\Editor\6000.5.3f1\Editor\Unity.exe
```

Every implementation task follows RED → GREEN → REFACTOR. Before each commit:

1. Run the focused test assembly or fixture.
2. Run all Edit Mode tests.
3. Search the Unity log for `error CS`, `Compilation failed` and unhandled exceptions.
4. Update `TASKS.md` with evidence and the next unchecked action.
5. Commit only files owned by that task.

The test wrapper created in Task 1 is the canonical command:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Run-UnityTests.ps1 -Platform EditMode
```

Expected success: Unity exits, `artifacts/test-results/editmode.xml` exists, XML reports zero failures, and `artifacts/logs/editmode.log` contains no compilation errors.

Play Mode:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Run-UnityTests.ps1 -Platform PlayMode
```

WebGL build:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Build-WebGL.ps1
```

Never treat Unity's process exit code alone as proof. Parse test XML and inspect the log.

## Target file structure

```text
pitch-simulator/
  Assets/
    Art/
      Characters/
      Environment/
      UI/
    Audio/
    Content/
      Scenarios/
      Localization/
    Editor/
      PitchSimulatorProjectBuilder.cs
      WebGlBuild.cs
    Plugins/WebGL/
      PitchSimulatorBridge.jslib
    Scenes/
      Bootstrap.unity
      Game.unity
      WebIntegrationTest.unity
    Scripts/
      Core/
      Dialogue/
      Scoring/
      LMS/
      Accessibility/
      Audio/
      UI/
    Tests/
      EditMode/
      PlayMode/
  Packages/
  ProjectSettings/
  WebHarness/
  docs/
  prompts/
  tools/
  artifacts/                 # ignored
  Build/                     # ignored
```

---

### Task 1: Unity repository foundation and repeatable test runner

**Files:**
- Create: `.gitignore`
- Create: `.gitattributes`
- Create: `README.md`
- Create: `AGENTS.md`
- Create: `CLAUDE.md`
- Create: `TASKS.md`
- Create: `CHANGELOG.md`
- Create: `tools/Run-UnityTests.ps1`
- Create: `tools/Build-WebGL.ps1`
- Unity creates: `Assets/`, `Packages/`, `ProjectSettings/`

**Skill Reference:** `unity-project-scout`, `unity-architecture`

- [ ] **Step 1: Create the Unity project.**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.3f1\Editor\Unity.exe' -batchmode -nographics -quit -createProject 'C:\Users\khidz\projects\pitch-simulator' -logFile 'C:\Users\khidz\projects\pitch-simulator-create.log'
```

Expected: `ProjectSettings/ProjectVersion.txt` contains `6000.5.3f1`.

- [ ] **Step 2: Pin the minimum packages.**

Ensure `Packages/manifest.json` includes:

```json
{
  "dependencies": {
    "com.unity.test-framework": "1.5.1",
    "com.unity.ugui": "2.0.0"
  }
}
```

Preserve Unity-created built-in module entries. Do not add networking, Addressables, Input System or a tween package.

- [ ] **Step 3: Add Unity and project ignores.**

`.gitignore` must ignore `[Ll]ibrary/`, `[Tt]emp/`, `[Oo]bj/`, `[Ll]ogs/`, `[Uu]ser[Ss]ettings/`, `Build/`, `artifacts/`, IDE files and OS junk. Do not ignore `Assets/**/*.meta`, `Packages/packages-lock.json` or `ProjectSettings/`.

`.gitattributes`:

```gitattributes
* text=auto
*.cs text eol=lf
*.json text eol=lf
*.md text eol=lf
*.unity text eol=lf
*.prefab text eol=lf
*.asset text eol=lf
*.png binary
*.wav binary
*.ogg binary
```

- [ ] **Step 4: Create the test wrapper.**

`tools/Run-UnityTests.ps1` accepts `-Platform EditMode|PlayMode`, creates `artifacts/logs` and `artifacts/test-results`, invokes Unity with `-runTests`, loads the XML, throws when `failures > 0`, and throws when the log matches `error CS\d+|Compilation failed|Unhandled Exception`.

- [ ] **Step 5: Create the build wrapper.**

`tools/Build-WebGL.ps1` invokes `Agrovator.PitchSimulator.Editor.WebGlBuild.BuildDevelopment`, writes `artifacts/logs/webgl-build.log` and throws if `Build/WebGL/index.html` is missing or the log contains build failure markers.

- [ ] **Step 6: Add repository guidance.**

`README.md` records purpose, Unity version, open/test/build commands and current milestone. `AGENTS.md` and `CLAUDE.md` require test-first changes, prohibit editing the LMS repository from this project, protect tokens/private data, and require `TASKS.md` updates. `CHANGELOG.md` starts with `Unreleased`. `TASKS.md` mirrors this plan with P0/P1/P2 checklists.

- [ ] **Step 7: Verify and commit.**

Run Unity once with `-batchmode -quit`. Expected: project imports and the log contains no compile errors.

Commit:

```powershell
git add .gitignore .gitattributes README.md AGENTS.md CLAUDE.md TASKS.md CHANGELOG.md Assets Packages ProjectSettings tools
git commit -m "chore: scaffold Unity project and verification scripts"
```

---

### Task 2: Assembly boundaries and core game states

**Files:**
- Create: `Assets/Scripts/Core/Agrovator.PitchSimulator.Core.asmdef`
- Create: `Assets/Scripts/Core/GameState.cs`
- Create: `Assets/Scripts/Core/GameCommand.cs`
- Create: `Assets/Scripts/Core/GameStateMachine.cs`
- Create: `Assets/Tests/EditMode/Agrovator.PitchSimulator.EditModeTests.asmdef`
- Create: `Assets/Tests/EditMode/Core/GameStateMachineTests.cs`
- Modify: `TASKS.md`

**Skill Reference:** `unity-asmdef`, `unity-testability`, `superpowers:test-driven-development`

- [ ] **Step 1: Create the test assembly and write failing state tests.**

Required tests:

```csharp
[Test]
public void StartScenario_MovesTitleToBriefing()
{
    var machine = new GameStateMachine(GameState.Title);
    Assert.That(machine.TryApply(GameCommand.StartScenario), Is.True);
    Assert.That(machine.Current, Is.EqualTo(GameState.Briefing));
}

[Test]
public void SelectResponse_IsRejectedOutsideAwaitingResponse()
{
    var machine = new GameStateMachine(GameState.Briefing);
    Assert.That(machine.TryApply(GameCommand.SelectResponse), Is.False);
    Assert.That(machine.Current, Is.EqualTo(GameState.Briefing));
}
```

Also test the complete approved state path, submission failure returning to Results, and retry returning to Briefing.

- [ ] **Step 2: Run focused Edit Mode tests and confirm RED.**

Expected: compilation fails because `GameStateMachine` is missing.

- [ ] **Step 3: Implement the smallest explicit transition table.**

`GameState` contains `Booting`, `Title`, `Briefing`, `Tutorial`, `JudgeIntro`, `AskingQuestion`, `AwaitingResponse`, `ShowingReaction`, `ShowingFeedback`, `Results`, `Submitting`, `Complete` and `SafeFallback`.

`GameStateMachine.TryApply` looks up a `(state, command)` transition and returns false without mutation when absent. Do not permit arbitrary `SetState`.

- [ ] **Step 4: Run focused and all Edit Mode tests.**

Expected: all state-machine tests pass with zero compile errors.

- [ ] **Step 5: Commit.**

```powershell
git add Assets/Scripts/Core Assets/Tests/EditMode TASKS.md
git commit -m "feat(core): explicit pitch session state machine"
```

---

### Task 3: Dialogue DTOs and structural validation

**Files:**
- Create: `Assets/Scripts/Dialogue/Agrovator.PitchSimulator.Dialogue.asmdef`
- Create: `Assets/Scripts/Dialogue/ScenarioDtos.cs`
- Create: `Assets/Scripts/Dialogue/ValidationIssue.cs`
- Create: `Assets/Scripts/Dialogue/ScenarioValidator.cs`
- Create: `Assets/Tests/EditMode/Dialogue/ScenarioValidatorTests.cs`
- Modify: `Assets/Tests/EditMode/Agrovator.PitchSimulator.EditModeTests.asmdef`
- Modify: `TASKS.md`

**Skill Reference:** `unity-script-roles`, `unity-testability`

- [ ] **Step 1: Write failing validator tests.**

Test valid minimal scenario plus:

- duplicate node IDs;
- missing opening node;
- response destination missing;
- unreachable node;
- duplicate response IDs within a node;
- invalid timer below zero;
- score delta outside configured category range;
- missing localization key;
- confidence delta outside -100…100.

Example:

```csharp
[Test]
public void Validate_ReportsMissingDestination()
{
    var scenario = ScenarioFactory.Minimal();
    scenario.Nodes[0].Responses[0].NextNodeId = "missing";
    var issues = ScenarioValidator.Validate(scenario, ScenarioFactory.Keys());
    Assert.That(issues, Has.Some.Matches<ValidationIssue>(
        i => i.Code == "dialogue.destination_missing"));
}
```

- [ ] **Step 2: Run and confirm RED.**

Expected: missing `ScenarioDefinitionDto` and validator types.

- [ ] **Step 3: Implement serializable DTOs.**

Use arrays/lists and primitive fields compatible with `JsonUtility`. Keep DTOs mutable for deserialization, then validate and compile into runtime objects in Task 4.

- [ ] **Step 4: Implement deterministic validation.**

Return all issues in node/response order. `ValidationIssue` contains `Code`, `Path` and `Severity`; it never stores learner data.

- [ ] **Step 5: Run focused and all Edit Mode tests; commit.**

```powershell
git add Assets/Scripts/Dialogue Assets/Tests/EditMode TASKS.md
git commit -m "feat(dialogue): serializable scenario schema and validation"
```

---

### Task 4: Runtime dialogue graph, flags and branches

**Files:**
- Create: `Assets/Scripts/Dialogue/RuntimeScenario.cs`
- Create: `Assets/Scripts/Dialogue/DialogueSession.cs`
- Create: `Assets/Scripts/Dialogue/ResponseAvailability.cs`
- Create: `Assets/Tests/EditMode/Dialogue/DialogueSessionTests.cs`
- Modify: `TASKS.md`

**Skill Reference:** `unity-architecture`, `unity-testability`

- [ ] **Step 1: Write failing traversal tests.**

Test:

- opening node selected;
- selecting a response sets flags and moves to the destination;
- required flag exposes a response;
- blocked flag hides a response;
- confidence range selects the eligible path;
- terminal node completes;
- response selection by unknown ID is rejected without mutation;
- recovery flag is set only after a weak answer followed by an evidence response.

- [ ] **Step 2: Run and confirm RED.**

- [ ] **Step 3: Compile validated DTOs into immutable lookup dictionaries.**

`RuntimeScenario.Compile` throws only when passed an invalid scenario; callers must validate first. Runtime nodes expose read-only collections.

- [ ] **Step 4: Implement `DialogueSession`.**

The session owns current node ID and a case-sensitive `HashSet<string>` of flags. `Select(responseId, confidence)` returns a result containing the selected response and new node; it does not calculate scores.

- [ ] **Step 5: Run tests and commit.**

```powershell
git add Assets/Scripts/Dialogue Assets/Tests/EditMode/Dialogue TASKS.md
git commit -m "feat(dialogue): conditional traversal and recovery flags"
```

---

### Task 5: Scoring, confidence and result feedback

**Files:**
- Create: `Assets/Scripts/Scoring/Agrovator.PitchSimulator.Scoring.asmdef`
- Create: `Assets/Scripts/Scoring/ScoreCategory.cs`
- Create: `Assets/Scripts/Scoring/ScoreAccumulator.cs`
- Create: `Assets/Scripts/Scoring/ConfidenceMeter.cs`
- Create: `Assets/Scripts/Scoring/ResultLevel.cs`
- Create: `Assets/Scripts/Scoring/ResultBuilder.cs`
- Create: `Assets/Tests/EditMode/Scoring/ScoringTests.cs`
- Modify: `TASKS.md`

**Skill Reference:** `unity-testability`, `superpowers:test-driven-development`

- [ ] **Step 1: Write failing scoring tests.**

Cover all seven rubric caps, overall cap at 100, no negative category totals, response quality dominating speed, confidence clamping, exact label boundaries, pitching/communications rollups and recovery strength generation.

```csharp
[TestCase(-5, 0)]
[TestCase(101, 100)]
[TestCase(60, 60)]
public void Confidence_Clamps(int value, int expected)
{
    Assert.That(new ConfidenceMeter(value).Value, Is.EqualTo(expected));
}
```

- [ ] **Step 2: Run and confirm RED.**

- [ ] **Step 3: Implement category-limited accumulation.**

Maximums are Clear Explanation 20, Problem 15, Solution 15, Audience 15, Evidence 15, Communication 10 and Time 10. `Apply(ResponseScoreDelta)` clamps each category and records competency tags.

- [ ] **Step 4: Implement result generation.**

Result thresholds are data-backed constants with tests. Strengths are the highest categories above their threshold; improvements are the lowest categories below theirs. Copy is returned as localization keys, not English sentences.

- [ ] **Step 5: Run tests and commit.**

```powershell
git add Assets/Scripts/Scoring Assets/Tests/EditMode/Scoring TASKS.md
git commit -m "feat(scoring): rubric confidence and constructive results"
```

---

### Task 6: Timer and accessibility settings

**Files:**
- Create: `Assets/Scripts/Accessibility/Agrovator.PitchSimulator.Accessibility.asmdef`
- Create: `Assets/Scripts/Accessibility/TimerMode.cs`
- Create: `Assets/Scripts/Accessibility/AccessibilitySettings.cs`
- Create: `Assets/Scripts/Core/QuestionTimer.cs`
- Create: `Assets/Tests/EditMode/Core/QuestionTimerTests.cs`
- Create: `Assets/Tests/EditMode/Accessibility/AccessibilitySettingsTests.cs`
- Modify: `TASKS.md`

**Skill Reference:** `unity-async`, `unity-testability`

- [ ] **Step 1: Write failing timer tests.**

Test start duration, pause, resume, expiry once, clamp at zero, no-timer mode, 1.5× extended mode and tutorial duration zero.

- [ ] **Step 2: Run and confirm RED.**

- [ ] **Step 3: Implement pure timer logic.**

`Tick(double seconds)` rejects negative delta, allocates nothing and fires `Expired` once. Unity `Update` will call it only in `AwaitingResponse`.

- [ ] **Step 4: Implement validated settings.**

Include timer mode, reduced motion, music/SFX volume and locale. Clamp volumes to 0…1 and unsupported locales to English.

- [ ] **Step 5: Run tests and commit.**

```powershell
git add Assets/Scripts/Core Assets/Scripts/Accessibility Assets/Tests/EditMode TASKS.md
git commit -m "feat(accessibility): configurable low-stress question timer"
```

---

### Task 7: LMS contracts, serialization and mock bridge

**Files:**
- Create: `Assets/Scripts/LMS/Agrovator.PitchSimulator.LMS.asmdef`
- Create: `Assets/Scripts/LMS/ILmsBridge.cs`
- Create: `Assets/Scripts/LMS/LmsLaunchConfig.cs`
- Create: `Assets/Scripts/LMS/LmsCompletionPayload.cs`
- Create: `Assets/Scripts/LMS/LmsPayloadValidator.cs`
- Create: `Assets/Scripts/LMS/MockLmsBridge.cs`
- Create: `Assets/Tests/EditMode/LMS/LmsPayloadTests.cs`
- Modify: `TASKS.md`

**Skill Reference:** `unity-testability`

- [ ] **Step 1: Write failing payload tests.**

Test valid round-trip JSON, missing session/scenario, unsupported content version, score ranges, timestamps, no private fields, success/failure/expired mock modes and attempt-number preservation.

- [ ] **Step 2: Run and confirm RED.**

- [ ] **Step 3: Implement contracts.**

`ILmsBridge`:

```csharp
public interface ILmsBridge
{
    LmsLaunchConfig GetLaunchConfig();
    void SubmitCompletion(
        LmsCompletionPayload payload,
        Action onSuccess,
        Action<LmsSubmissionError> onFailure);
}
```

The DTO contains pseudonymous IDs only. It has no name, email, school, raw token or answer text field.

- [ ] **Step 4: Implement deterministic mock modes.**

`MockLmsBridge` exposes Success, Failure, Expired and MissingConfiguration modes for Edit/Play Mode tests.

- [ ] **Step 5: Run tests and commit.**

```powershell
git add Assets/Scripts/LMS Assets/Tests/EditMode/LMS TASKS.md
git commit -m "feat(lms): validated launch completion and mock bridge"
```

---

### Task 8: Localization catalog and save-data versioning

**Files:**
- Create: `Assets/Scripts/Core/SaveData.cs`
- Create: `Assets/Scripts/Core/SaveDataMigrator.cs`
- Create: `Assets/Scripts/Accessibility/LocalizationCatalog.cs`
- Create: `Assets/Content/Localization/en.json`
- Create: `Assets/Content/Localization/ms.json`
- Create: `Assets/Tests/EditMode/Accessibility/LocalizationTests.cs`
- Create: `Assets/Tests/EditMode/Core/SaveDataTests.cs`
- Modify: `TASKS.md`

**Skill Reference:** `unity-testability`

- [ ] **Step 1: Write failing localization and migration tests.**

Test every English key resolves, Bahasa catalog contains the same keys, missing locale falls back to English, missing key returns a visible diagnostic token, current save version round-trips and version 0 migrates to version 1.

- [ ] **Step 2: Run and confirm RED.**

- [ ] **Step 3: Implement catalog loading and fallback.**

The initial `ms.json` may use reviewed English fallback values, but it must contain every key and be clearly marked `translationStatus: pending_human_review`. Do not fabricate a final Malay translation.

- [ ] **Step 4: Implement versioned settings-only save data.**

Store accessibility/audio settings and last selected locale. Do not store learner identity, tokens or authoritative completion.

- [ ] **Step 5: Run tests and commit.**

```powershell
git add Assets/Scripts Assets/Content/Localization Assets/Tests/EditMode TASKS.md
git commit -m "feat(content): localization catalog and versioned preferences"
```

---

### Task 9: Smart School Garden content and JSON import

**Files:**
- Create: `Assets/Content/Scenarios/smart-school-garden.en.json`
- Create: `Assets/Scripts/Dialogue/ScenarioJsonLoader.cs`
- Create: `Assets/Scripts/Dialogue/ScenarioAsset.cs`
- Create: `Assets/Tests/EditMode/Dialogue/SmartSchoolGardenContentTests.cs`
- Modify: `Assets/Content/Localization/en.json`
- Modify: `Assets/Content/Localization/ms.json`
- Modify: `TASKS.md`

**Skill Reference:** `unity-scriptableobject`, `unity-testability`

- [ ] **Step 1: Write failing content acceptance tests.**

Load the real JSON and assert:

- scenario ID/version and opening node;
- at least five scored questions plus tutorial;
- exactly three responses per question;
- at least two conditional destinations;
- one recovery path;
- timer pattern includes 20, 15 and 12 seconds;
- option positions for strongest choices are not constant;
- required competency and special-answer tags exist;
- every text key resolves.

- [ ] **Step 2: Run and confirm RED because the content file is absent.**

- [ ] **Step 3: Author the complete English scenario.**

Use the approved sequence. Include believable weaknesses, constructive feedback, an impressive unsupported claim, poorly communicated useful facts, audience awareness, honest uncertainty and recovery evidence. No choice ridicules the learner.

- [ ] **Step 4: Implement JSON loader and ScriptableObject wrapper.**

`ScenarioAsset` stores `TextAsset json` and optional judge/audio references. `ScenarioJsonLoader` deserializes, validates and returns either a runtime graph or structured errors.

- [ ] **Step 5: Run focused/all tests and commit.**

```powershell
git add Assets/Content Assets/Scripts/Dialogue Assets/Tests/EditMode/Dialogue TASKS.md
git commit -m "feat(content): Smart School Garden branching scenario"
```

---

### Task 10: Session controller orchestration

**Files:**
- Create: `Assets/Scripts/Core/PitchSessionController.cs`
- Create: `Assets/Scripts/Core/PitchSessionSnapshot.cs`
- Create: `Assets/Scripts/Core/PitchSessionEvents.cs`
- Create: `Assets/Tests/EditMode/Core/PitchSessionControllerTests.cs`
- Modify: `TASKS.md`

**Skill Reference:** `unity-architecture`, `unity-testability`

- [ ] **Step 1: Write failing orchestration tests.**

Test launch-to-title, scenario start, tutorial no-score, response score/confidence application, timer expiry neutral outcome, reaction then feedback ordering, branch traversal, results payload creation and retry clearing all session state.

- [ ] **Step 2: Run and confirm RED.**

- [ ] **Step 3: Implement controller with explicit collaborators.**

Constructor receives runtime scenario, score service, accessibility settings, timer and LMS bridge. It publishes immutable snapshots/events. It never references Unity UI classes.

- [ ] **Step 4: Refactor duplicated fixture builders only after GREEN.**

- [ ] **Step 5: Run tests and commit.**

```powershell
git add Assets/Scripts/Core Assets/Tests/EditMode/Core TASKS.md
git commit -m "feat(core): orchestrate complete pitch session"
```

---

### Task 11: Unity composition, scenes and uGUI shell

**Files:**
- Create: `Assets/Scripts/UI/Agrovator.PitchSimulator.UI.asmdef`
- Create: `Assets/Scripts/UI/Bootstrapper.cs`
- Create: `Assets/Scripts/UI/GameScreenRouter.cs`
- Create: `Assets/Scripts/UI/TitlePresenter.cs`
- Create: `Assets/Scripts/UI/BriefingPresenter.cs`
- Create: `Assets/Scripts/UI/PitchRoomPresenter.cs`
- Create: `Assets/Scripts/UI/ResultsPresenter.cs`
- Create: `Assets/Scripts/UI/SettingsPresenter.cs`
- Create: `Assets/Editor/PitchSimulatorProjectBuilder.cs`
- Create: `Assets/Scenes/Bootstrap.unity`
- Create: `Assets/Scenes/Game.unity`
- Create: `Assets/Scenes/WebIntegrationTest.unity`
- Create: `Assets/Tests/PlayMode/Agrovator.PitchSimulator.PlayModeTests.asmdef`
- Create: `Assets/Tests/PlayMode/BootstrapPlayModeTests.cs`
- Modify: `TASKS.md`

**Skill Reference:** `unity-scene-contracts`, `unity-ui`, `unity-script-roles`

- [ ] **Step 1: Write a failing Play Mode bootstrap test.**

Load `Bootstrap`, wait for initialization, and assert one `Bootstrapper` exists, `Game` loads, and the visible screen is Title.

- [ ] **Step 2: Run Play Mode and confirm RED because scenes/components are absent.**

- [ ] **Step 3: Implement presenters with serialized references and guards.**

Each presenter has `Initialize(...)` and returns early until initialized. Buttons invoke controller commands. Presenters do not calculate score, branches or timeouts.

- [ ] **Step 4: Implement idempotent Editor scene builder.**

Menu method `Pitch Simulator/Build Project Foundation` creates all scenes, Canvas/EventSystem, responsive panels, navigation groups and serialized references. Re-running updates/replaces generated objects deterministically.

- [ ] **Step 5: Run the builder in batch mode.**

Expected: all scenes exist, are in Build Settings in Bootstrap/Game order, and compile cleanly.

- [ ] **Step 6: Run Play Mode test and commit.**

```powershell
git add Assets/Scripts/UI Assets/Editor Assets/Scenes Assets/Tests/PlayMode TASKS.md ProjectSettings
git commit -m "feat(ui): bootstrap scenes and state-driven game shell"
```

---

### Task 12: Response interaction, timer and confidence presentation

**Files:**
- Create: `Assets/Scripts/UI/ResponseButtonView.cs`
- Create: `Assets/Scripts/UI/ResponseListView.cs`
- Create: `Assets/Scripts/UI/TimerView.cs`
- Create: `Assets/Scripts/UI/ConfidenceView.cs`
- Create: `Assets/Scripts/UI/FocusNavigator.cs`
- Create: `Assets/Tests/PlayMode/PitchInteractionPlayModeTests.cs`
- Modify: `Assets/Editor/PitchSimulatorProjectBuilder.cs`
- Modify: `TASKS.md`

**Skill Reference:** `unity-ui`, `unity-testability`

- [ ] **Step 1: Write failing Play Mode tests.**

Test three buttons render, keyboard focus begins on the first option, click and Enter select once, buttons disable during reaction, timer expires into neutral outcome, confidence label/icon/bar update together and reduced motion skips nonessential tweening.

- [ ] **Step 2: Run and confirm RED.**

- [ ] **Step 3: Implement pooled response buttons and explicit focus.**

The list reuses three fixed button instances. Accessible labels include option number and text. Selection locks synchronously before sending the command.

- [ ] **Step 4: Implement timer/confidence views.**

The timer shows number and normalized bar. The final-five-seconds animation is a gentle scale pulse, disabled by reduced motion. Confidence maps to the five approved labels with a non-colour icon.

- [ ] **Step 5: Rebuild scenes, run Play Mode/all tests and commit.**

```powershell
git add Assets/Scripts/UI Assets/Editor Assets/Scenes Assets/Tests/PlayMode TASKS.md
git commit -m "feat(ui): accessible responses timer and confidence feedback"
```

---

### Task 13: Original pixel-art presentation and judge reactions

**Files:**
- Create: `Assets/Art/Characters/judge-aya-sheet.png`
- Create: `Assets/Art/Environment/pitch-room.png`
- Create: `Assets/Art/UI/dialogue-panel.png`
- Create: `Assets/Art/UI/confidence-icons.png`
- Create: `Assets/Art/CREDITS.md`
- Create: `Assets/Scripts/UI/JudgeReactionView.cs`
- Create: `Assets/Scripts/UI/PixelArtImportPostprocessor.cs`
- Create: `Assets/Tests/PlayMode/JudgeReactionPlayModeTests.cs`
- Modify: `Assets/Editor/PitchSimulatorProjectBuilder.cs`
- Modify: `TASKS.md`

**Skill Reference:** `imagegen`, `unity-importer`, `unity-ui`

- [ ] **Step 1: Write failing reaction mapping tests.**

Every required reaction enum must resolve to a sprite/animation; unknown cues fall back to Encouraging.

- [ ] **Step 2: Generate original assets.**

Use the approved AGROVATOR palette and original Malaysian school-agritech setting. Generate no copyrighted characters, interfaces or commercial sprite derivatives.

- [ ] **Step 3: Import pixel-perfect.**

Postprocessor sets Sprite type, point filtering, no mipmaps, no compression for small UI sprites and consistent pixels-per-unit. Environment art may use platform-appropriate compression after visual verification.

- [ ] **Step 4: Implement reaction state changes.**

Idle blink is low-frequency; talk loops only while question text appears; reaction cue plays once then settles to Idle/Encouraging.

- [ ] **Step 5: Update asset credits and manifest, rebuild, visually inspect, test and commit.**

```powershell
git add Assets/Art Assets/Scripts/UI Assets/Editor Assets/Scenes Assets/Tests/PlayMode TASKS.md
git commit -m "feat(art): original pixel pitch room and animated judge"
```

---

### Task 14: Browser-safe audio hooks

**Files:**
- Create: `Assets/Scripts/Audio/Agrovator.PitchSimulator.Audio.asmdef`
- Create: `Assets/Scripts/Audio/AudioCue.cs`
- Create: `Assets/Scripts/Audio/AudioService.cs`
- Create: `Assets/Audio/PLACEHOLDERS.md`
- Create: `Assets/Tests/EditMode/Audio/AudioServiceTests.cs`
- Modify: `Assets/Scripts/UI/Bootstrapper.cs`
- Modify: `TASKS.md`

**Skill Reference:** `unity-architecture`, `unity-testability`

- [ ] **Step 1: Write failing tests.**

Test cue lookup, mute, independent music/SFX volume, clamping, no playback before unlock and safe missing-clip behavior.

- [ ] **Step 2: Run and confirm RED.**

- [ ] **Step 3: Implement two-channel audio service.**

Required named cues match the brief. `UnlockAfterUserGesture` is called by the first title-screen interaction. Missing clips log once in development and never block progression.

- [ ] **Step 4: Document placeholder filenames and licensing requirements.**

- [ ] **Step 5: Run tests and commit.**

```powershell
git add Assets/Scripts/Audio Assets/Audio Assets/Tests/EditMode/Audio Assets/Scripts/UI TASKS.md
git commit -m "feat(audio): browser-safe music and SFX integration hooks"
```

---

### Task 15: WebGL JavaScript bridge and local LMS harness

**Files:**
- Create: `Assets/Plugins/WebGL/PitchSimulatorBridge.jslib`
- Create: `Assets/Scripts/LMS/WebGlLmsBridge.cs`
- Create: `WebHarness/index.html`
- Create: `WebHarness/harness.css`
- Create: `WebHarness/harness.js`
- Create: `Assets/Tests/PlayMode/LmsBridgePlayModeTests.cs`
- Modify: `Assets/Editor/PitchSimulatorProjectBuilder.cs`
- Modify: `TASKS.md`

**Skill Reference:** `unity-testability`, `unity-scene-contracts`

- [ ] **Step 1: Write failing Play Mode bridge tests using an injected transport.**

Test launch config, successful completion, submission failure, expired session and missing config. Native Editor uses fake transport; JavaScript interop is compiled only for WebGL player.

- [ ] **Step 2: Run and confirm RED.**

- [ ] **Step 3: Implement `WebGlLmsBridge` and `.jslib`.**

Use JSON strings and callbacks. Validate `event.origin` in the harness, never put a token in query parameters, and avoid logging the full launch payload.

- [ ] **Step 4: Build harness controls.**

Controls select Success, Failure, Expired and Missing Config; the page embeds the Unity build, shows sanitized progress/completion events and can resend a launch configuration.

- [ ] **Step 5: Run tests and commit.**

```powershell
git add Assets/Plugins Assets/Scripts/LMS Assets/Tests/PlayMode WebHarness Assets/Editor TASKS.md
git commit -m "feat(web): mock LMS postMessage bridge and harness"
```

---

### Task 16: Complete results, review and retry flow

**Files:**
- Modify: `Assets/Scripts/UI/ResultsPresenter.cs`
- Create: `Assets/Scripts/UI/QuestionReviewItemView.cs`
- Create: `Assets/Tests/PlayMode/ResultsPlayModeTests.cs`
- Modify: `Assets/Editor/PitchSimulatorProjectBuilder.cs`
- Modify: `TASKS.md`

**Skill Reference:** `unity-ui`, `unity-testability`

- [ ] **Step 1: Write failing end-to-end Play Mode tests.**

Drive the scenario through one strong path and one recovery path. Assert overall/final confidence, pitching/communications values, two strengths, two improvements, question review, stronger-answer explanation, retry clearing prior choices and mock completion success/failure messages.

- [ ] **Step 2: Run and confirm RED.**

- [ ] **Step 3: Implement results view and review list.**

Question review shows selected response, concise feedback and explanation without exposing hidden scoring weights. Submission status remains visible and retryable.

- [ ] **Step 4: Rebuild scenes and run all Edit/Play Mode tests.**

- [ ] **Step 5: Commit.**

```powershell
git add Assets/Scripts/UI Assets/Tests/PlayMode Assets/Editor Assets/Scenes TASKS.md
git commit -m "feat(results): learning review LMS completion and retry"
```

---

### Task 17: Required project documentation and AI handoffs

**Files:**
- Create: `docs/00-LMS-DISCOVERY.md` through `docs/18-VERTICAL-SLICE-ACCEPTANCE.md`
- Create: `docs/adr/0001-standalone-unity-repository.md`
- Create: `docs/adr/0002-three-scene-state-driven-ui.md`
- Create: `docs/adr/0003-custom-rest-over-scorm-xapi.md`
- Create: `docs/adr/0004-unity-automation-strategy.md`
- Create: `docs/17-CODEX-CLAUDE-WORKFLOW.md`
- Create: `prompts/claude/01-investor-personas.md` through `10-qa-edge-cases.md`
- Create: `prompts/codex/01-implement-task.md`
- Create: `prompts/codex/02-verify-unity-milestone.md`
- Update: `README.md`, `TASKS.md`, `CHANGELOG.md`

**Skill Reference:** `write-user-docs`, `unity-adr`

- [ ] **Step 1: Write project-specific documents.**

Each required document records confirmed findings, explicit assumptions, actual implemented paths and measured/unknown values. No empty templates or fabricated metrics.

- [ ] **Step 2: Write the 16-phase roadmap.**

For each phase include objectives, tasks, dependencies, deliverables, acceptance criteria, risks, developer-day estimate and Codex/Claude/human ownership.

- [ ] **Step 3: Write Claude handoff prompts.**

Every prompt includes Objective, Allowed Files, Files Not To Change, Required Output, Required Tests and Definition of Done.

- [ ] **Step 4: Validate document completeness.**

Run a script or PowerShell check that every required file exists and no file contains `TBD`, `TODO`, `FIXME` or empty required headings.

- [ ] **Step 5: Commit.**

```powershell
git add README.md TASKS.md CHANGELOG.md docs prompts
git commit -m "docs: complete Pitch Simulator production handoff set"
```

---

### Task 18: WebGL build automation and development build

**Files:**
- Create: `Assets/Editor/WebGlBuild.cs`
- Create: `Assets/WebGLTemplates/Agrovator/index.html`
- Create: `Assets/WebGLTemplates/Agrovator/TemplateData/style.css`
- Modify: `ProjectSettings/ProjectSettings.asset` through Unity APIs
- Modify: `TASKS.md`

**Skill Reference:** `unity-project`, `unity-test`, `superpowers:verification-before-completion`

- [ ] **Step 1: Implement build method.**

`WebGlBuild.BuildDevelopment` verifies scene order, switches to WebGL, applies development settings, uses the AGROVATOR template, builds to `Build/WebGL` and throws when `BuildReport.summary.result` is not Succeeded.

- [ ] **Step 2: Configure WebGL safely.**

Use WebAssembly, exception support suitable for development, decompression fallback for the local harness, responsive canvas and no autoplay audio. Production compression is a later hosting-specific decision.

- [ ] **Step 3: Run all Edit and Play Mode tests.**

Expected: both XML files report zero failures and logs have zero compile errors.

- [ ] **Step 4: Produce the development WebGL build.**

Run `tools/Build-WebGL.ps1`. Record build result, uncompressed and compressed sizes, duration and warnings in `docs/10-WEB-DEPLOYMENT.md` and `docs/18-VERTICAL-SLICE-ACCEPTANCE.md`.

- [ ] **Step 5: Commit source/configuration, not Build output.**

```powershell
git add Assets/Editor Assets/WebGLTemplates ProjectSettings TASKS.md docs/10-WEB-DEPLOYMENT.md docs/18-VERTICAL-SLICE-ACCEPTANCE.md
git commit -m "build(webgl): reproducible AGROVATOR development build"
```

---

### Task 19: Local HTTP and browser smoke tests

**Files:**
- Create: `tools/Serve-WebGL.ps1`
- Create: `tools/smoke-webgl.mjs`
- Create: `artifacts/smoke/` outputs (ignored)
- Modify: `docs/13-QA-PLAN.md`
- Modify: `docs/18-VERTICAL-SLICE-ACCEPTANCE.md`
- Modify: `TASKS.md`

**Skill Reference:** `browser:control-in-app-browser`, `superpowers:verification-before-completion`

- [ ] **Step 1: Add a local server with correct MIME types.**

Serve `WebHarness` and `Build/WebGL` from one origin. Include `application/wasm` and correct JavaScript/data content types.

- [ ] **Step 2: Add automated smoke assertions.**

The browser script waits for Unity readiness, asserts no console/page errors, checks canvas size after resize, starts a mock session, selects responses using mouse and keyboard, verifies results, retries, and exercises success/failure/missing-config harness modes.

- [ ] **Step 3: Run Chrome smoke test.**

Record browser version, pass/fail, load time and screenshot.

- [ ] **Step 4: Run Edge and Firefox smoke tests where installed.**

Record unavailable browsers honestly. Safari remains unverified on Windows.

- [ ] **Step 5: Manually verify audio unlock, fullscreen, refresh and touch emulation.**

- [ ] **Step 6: Update QA/acceptance evidence and commit.**

```powershell
git add tools docs/13-QA-PLAN.md docs/18-VERTICAL-SLICE-ACCEPTANCE.md TASKS.md
git commit -m "test(web): local browser smoke matrix and evidence"
```

---

### Task 20: Final acceptance audit

**Files:**
- Modify: `README.md`
- Modify: `TASKS.md`
- Modify: `CHANGELOG.md`
- Modify: `docs/16-ASSET-MANIFEST.md`
- Modify: `docs/18-VERTICAL-SLICE-ACCEPTANCE.md`

**Skill Reference:** `superpowers:verification-before-completion`, `superpowers:requesting-code-review`, `superpowers:finishing-a-development-branch`

- [ ] **Step 1: Run the full evidence suite fresh.**

Run Edit Mode, Play Mode, WebGL build and browser smoke commands without reusing previous outputs.

- [ ] **Step 2: Check acceptance line by line.**

Confirm title, briefing, tutorial, judge, five scored questions, three choices, two branches, recovery, timers, confidence, reactions, audio hooks, scoring, results, review, retry, mock launch/completion and WebGL build.

- [ ] **Step 3: Audit privacy and licences.**

Search source/build templates for emails, tokens, secret-like strings and private student fields. Verify every non-code asset has a manifest entry and acceptable licence/provenance.

- [ ] **Step 4: Record actual performance measurements and known gaps.**

Do not mark Safari, real LMS submission, final Malay translation, classroom usability or final audio complete without evidence.

- [ ] **Step 5: Request code review and address findings.**

- [ ] **Step 6: Update release records and commit.**

```powershell
git add README.md TASKS.md CHANGELOG.md docs
git commit -m "chore: record vertical slice acceptance evidence"
```

## Definition of done

The plan is complete only when the fresh verification run proves:

- Unity reports no compile errors.
- All Edit Mode and Play Mode tests pass.
- The Smart School Garden scenario satisfies structural content tests.
- A development WebGL build exists and its measured size is documented.
- The local harness runs the game over HTTP.
- Available Windows browser smoke tests pass, with unavailable platforms clearly marked.
- The full required documentation and AI handoff set exists.
- The LMS repository has not been modified.
- The standalone repository is clean after the final commit.
