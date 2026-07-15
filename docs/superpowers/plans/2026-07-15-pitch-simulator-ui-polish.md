# Pitch Simulator Tutorial and UI Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a three-step localized tutorial and redesign every in-game screen around clean, centered, non-stretched content cards.

**Architecture:** A new `TutorialPresenter` owns page-only UI state while the existing session state machine continues to own `GameState.Tutorial`. `GameScreenRouter` routes that state to a dedicated generated panel. Shared editor-builder helpers create full-screen backgrounds plus fixed-width centered cards, and existing presenters keep their current gameplay responsibilities.

**Tech Stack:** Unity `6000.5.3f1`, C# UGUI, NUnit EditMode/PlayMode tests, deterministic editor scene generation, PowerShell build/test wrappers, Node.js and Playwright WebGL smoke.

## Global Constraints

- Work only in `C:\Users\khidz\projects\pitch-simulator\worktrees\ui-polish` on `codex/pitch-simulator-ui-polish`.
- Do not inspect, read, or modify the external AGROVATOR LMS repository.
- Preserve scoring, dialogue traversal, LMS payloads, privacy fields, bridge behavior, audio placeholders, and the developer harness design.
- Keep the `1280×720` reference canvas, responsive 16:9 WebGL contract, keyboard navigation, visible focus, minimum 64px primary targets, and reduced-motion semantics.
- English tutorial copy is reviewed project copy. Malay retains exact English fallback parity and `pending_human_review` status.
- Use `apply_patch` for edits. Never hand-edit Unity scene YAML; regenerate scenes through `PitchSimulatorProjectBuilder`.
- After every Unity run, remove only known `ProjectSettings.asset` normalization and generated `SceneTemplateSettings.json` noise.
- Each implementation task follows RED → minimal GREEN → focused review → commit.

---

### Task 1: Localized three-page tutorial presenter

**Files:**
- Create: `Assets/Scripts/UI/TutorialPresenter.cs`
- Create: `Assets/Scripts/UI/TutorialPresenter.cs.meta`
- Create: `Assets/Tests/PlayMode/TutorialPlayModeTests.cs`
- Create: `Assets/Tests/PlayMode/TutorialPlayModeTests.cs.meta`
- Modify: `Assets/Content/Localization/en.json`
- Modify: `Assets/Content/Localization/ms.json`
- Modify: `Assets/Tests/EditMode/Accessibility/LocalizationTests.cs`

**Interfaces:**
- Consumes: `PitchSessionController`, `PitchSessionSnapshot`, `GameState.Tutorial`, `Func<string,string>` localization.
- Produces: `TutorialPresenter.Initialize(PitchSessionController, Action, Func<string,string>)`, `Refresh(PitchSessionSnapshot)`, `CurrentPageIndex`, and `PageCount`.

- [ ] **Step 1: Write the failing localization and presenter tests**

Add these exact required keys to `LocalizationTests.EnglishCatalog_ContainsEveryCurrentResultAndMinimalUiKey`:

```csharp
"ui.back",
"ui.next",
"ui.skip_tutorial",
"ui.start_practice",
"ui.tutorial.step.1",
"ui.tutorial.step.2",
"ui.tutorial.step.3",
"ui.tutorial.goal.title",
"ui.tutorial.goal.body",
"ui.tutorial.choices.title",
"ui.tutorial.choices.body",
"ui.tutorial.feedback.title",
"ui.tutorial.feedback.body",
```

Create `TutorialPlayModeTests` with a test rig containing `Text` fields for step, heading, body and three `Button` fields. Cover these assertions:

```csharp
Assert.That(presenter.PageCount, Is.EqualTo(3));
Assert.That(presenter.CurrentPageIndex, Is.Zero);
Assert.That(step.text, Is.EqualTo("Step 1 of 3"));
Assert.That(heading.text, Is.EqualTo("Build your pitch"));
Assert.That(back.interactable, Is.False);
Assert.That(next.GetComponentInChildren<Text>().text, Is.EqualTo("Next"));

next.onClick.Invoke();
Assert.That(presenter.CurrentPageIndex, Is.EqualTo(1));
Assert.That(heading.text, Is.EqualTo("Choose your response"));
Assert.That(back.interactable, Is.True);

next.onClick.Invoke();
Assert.That(presenter.CurrentPageIndex, Is.EqualTo(2));
Assert.That(next.GetComponentInChildren<Text>().text, Is.EqualTo("Start practice"));

back.onClick.Invoke();
Assert.That(presenter.CurrentPageIndex, Is.EqualTo(1));
```

Add separate tests that Skip advances `Tutorial → JudgeIntro` once and that leaving/re-entering Tutorial through a fresh controller resets `CurrentPageIndex` to zero.

- [ ] **Step 2: Run the tests to verify RED**

Run:

```powershell
& .\tools\Run-UnityTests.ps1 -Platform EditMode
& .\tools\Run-UnityTests.ps1 -Platform PlayMode
```

Expected: EditMode fails because the 13 keys are absent; PlayMode reaches the intended `CS0246` boundary because `TutorialPresenter` does not exist.

- [ ] **Step 3: Add exact catalog entries**

Insert the following entries in both `en.json` and `ms.json`; Malay intentionally mirrors English while its catalog remains `pending_human_review`:

```json
{ "key": "ui.back", "value": "Back" },
{ "key": "ui.next", "value": "Next" },
{ "key": "ui.skip_tutorial", "value": "Skip tutorial" },
{ "key": "ui.start_practice", "value": "Start practice" },
{ "key": "ui.tutorial.step.1", "value": "Step 1 of 3" },
{ "key": "ui.tutorial.step.2", "value": "Step 2 of 3" },
{ "key": "ui.tutorial.step.3", "value": "Step 3 of 3" },
{ "key": "ui.tutorial.goal.title", "value": "Build your pitch" },
{ "key": "ui.tutorial.goal.body", "value": "You are pitching the Smart School Garden to Mentor Aya. This is a practice space: explain the idea clearly, learn from feedback, and improve without penalty." },
{ "key": "ui.tutorial.choices.title", "value": "Choose your response" },
{ "key": "ui.tutorial.choices.body", "value": "Each question gives you three respectful responses. Choose the answer that explains the problem, evidence, solution, or value most clearly and honestly." },
{ "key": "ui.tutorial.feedback.title", "value": "Learn from feedback" },
{ "key": "ui.tutorial.feedback.body", "value": "Mentor Aya reacts first, then explains what worked and how to strengthen the answer. Confidence can change, and scored questions have a visible timer." },
```

- [ ] **Step 4: Implement `TutorialPresenter`**

Create the class with this complete behavior:

```csharp
using System;
using Agrovator.PitchSimulator.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    public sealed class TutorialPresenter : MonoBehaviour
    {
        private static readonly string[] StepKeys =
            { "ui.tutorial.step.1", "ui.tutorial.step.2", "ui.tutorial.step.3" };
        private static readonly string[] HeadingKeys =
            { "ui.tutorial.goal.title", "ui.tutorial.choices.title", "ui.tutorial.feedback.title" };
        private static readonly string[] BodyKeys =
            { "ui.tutorial.goal.body", "ui.tutorial.choices.body", "ui.tutorial.feedback.body" };

        [SerializeField] private Text stepText;
        [SerializeField] private Text headingText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Button backButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Text nextButtonText;

        private PitchSessionController controller;
        private Action changed;
        private Func<string, string> resolveText;
        private int currentPageIndex;
        private bool wasTutorialActive;
        private bool initialized;

        public int CurrentPageIndex => currentPageIndex;
        public int PageCount => StepKeys.Length;

        public void Initialize(PitchSessionController sessionController, Action onChanged,
            Func<string, string> localize = null)
        {
            if (sessionController == null) throw new ArgumentNullException(nameof(sessionController));
            RemoveListeners();
            controller = sessionController;
            changed = onChanged;
            resolveText = localize ?? (key => key);
            backButton.onClick.AddListener(Back);
            skipButton.onClick.AddListener(AdvanceSession);
            nextButton.onClick.AddListener(Next);
            initialized = true;
        }

        public void Refresh(PitchSessionSnapshot snapshot)
        {
            if (!initialized || snapshot == null) return;
            var tutorialActive = snapshot.State == GameState.Tutorial;
            if (tutorialActive && !wasTutorialActive) currentPageIndex = 0;
            wasTutorialActive = tutorialActive;
            if (tutorialActive) RenderPage();
        }

        private void Back()
        {
            if (!initialized || currentPageIndex == 0) return;
            currentPageIndex--;
            RenderPage();
        }

        private void Next()
        {
            if (!initialized) return;
            if (currentPageIndex == PageCount - 1)
            {
                AdvanceSession();
                return;
            }
            currentPageIndex++;
            RenderPage();
        }

        private void AdvanceSession()
        {
            if (!initialized || controller.Snapshot.State != GameState.Tutorial) return;
            if (controller.Continue()) changed?.Invoke();
        }

        private void RenderPage()
        {
            currentPageIndex = Mathf.Clamp(currentPageIndex, 0, PageCount - 1);
            stepText.text = resolveText(StepKeys[currentPageIndex]);
            headingText.text = resolveText(HeadingKeys[currentPageIndex]);
            bodyText.text = resolveText(BodyKeys[currentPageIndex]);
            backButton.interactable = currentPageIndex > 0;
            nextButtonText.text = resolveText(currentPageIndex == PageCount - 1
                ? "ui.start_practice" : "ui.next");
            if (EventSystem.current != null && nextButton.gameObject.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(nextButton.gameObject);
        }

        private void OnDestroy() => RemoveListeners();

        private void RemoveListeners()
        {
            if (backButton != null) backButton.onClick.RemoveListener(Back);
            if (skipButton != null) skipButton.onClick.RemoveListener(AdvanceSession);
            if (nextButton != null) nextButton.onClick.RemoveListener(Next);
        }
    }
}
```

- [ ] **Step 5: Run GREEN and commit**

Run both wrapper commands from Step 2. Expected: both suites pass with the new tests included, zero compilation markers. Commit:

```powershell
git add Assets/Scripts/UI/TutorialPresenter.cs Assets/Scripts/UI/TutorialPresenter.cs.meta Assets/Tests/PlayMode/TutorialPlayModeTests.cs Assets/Tests/PlayMode/TutorialPlayModeTests.cs.meta Assets/Content/Localization/en.json Assets/Content/Localization/ms.json Assets/Tests/EditMode/Accessibility/LocalizationTests.cs
git commit -m "feat(ui): add localized three-step tutorial presenter"
```

---

### Task 2: Route and generate the dedicated Tutorial screen

**Files:**
- Modify: `Assets/Scripts/UI/GameScreenRouter.cs`
- Modify: `Assets/Editor/PitchSimulatorProjectBuilder.cs`
- Modify: `Assets/Tests/EditMode/UI/PitchSimulatorProjectBuilderTests.cs`
- Modify: `Assets/Tests/PlayMode/BootstrapPlayModeTests.cs`
- Modify: `Assets/Scenes/Game.unity`

**Interfaces:**
- Consumes: Task 1 `TutorialPresenter`.
- Produces: serialized `tutorialPanel`, `tutorialPresenter`, `tutorialDefault`; generated `Canvas/Tutorial` hierarchy.

- [ ] **Step 1: Add failing router/builder and bootstrap assertions**

Extend the builder test to require:

```csharp
var tutorial = generated.transform.Find("Canvas/Tutorial");
Assert.That(tutorial, Is.Not.Null);
Assert.That(tutorial.GetComponent<TutorialPresenter>(), Is.Not.Null);
Assert.That(tutorial.Find("Content Frame/Navigation/Next Button").GetComponent<Button>(), Is.Not.Null);
```

Extend bootstrap routing to assert `Briefing Continue` activates Tutorial, selects Next, advances through all three pages, and then activates PitchRoom in `JudgeIntro`:

```csharp
briefingContinue.onClick.Invoke();
yield return null;
var tutorial = canvasRoot.Find("Tutorial");
Assert.That(tutorial.gameObject.activeInHierarchy, Is.True);
var next = tutorial.Find("Content Frame/Navigation/Next Button").GetComponent<Button>();
Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(next.gameObject));
next.onClick.Invoke();
next.onClick.Invoke();
next.onClick.Invoke();
yield return null;
Assert.That(GetController(bootstrapper).Snapshot.State, Is.EqualTo(GameState.JudgeIntro));
```

- [ ] **Step 2: Run RED**

Run full EditMode and PlayMode wrappers. Expected: missing `Canvas/Tutorial` and routing assertions fail.

- [ ] **Step 3: Add router ownership**

Add `tutorialPanel`, `tutorialPresenter`, and `tutorialDefault` serialized fields. Include Tutorial in every panel/presenter/default contract array, initialize and refresh it, map `GameState.Tutorial` explicitly, and include it in `CurrentPanel`, `Show`, and `GetDefault`:

```csharp
tutorialPresenter.Initialize(controller, Refresh, localize);
tutorialPresenter.Refresh(snapshot);

case GameState.Tutorial:
    return tutorialPanel;

if (panel == tutorialPanel) return tutorialDefault;
```

The panel enumeration order is Title, Briefing, Tutorial, PitchRoom, Results, Settings.

- [ ] **Step 4: Generate the Tutorial hierarchy**

Add `CreateTutorialScreen` and call it between Briefing and PitchRoom. Its hierarchy is exact:

```text
Tutorial
└── Content Frame
    ├── Step
    ├── Heading
    ├── Body
    └── Navigation
        ├── Back Button
        ├── Skip Button
        └── Next Button
```

Create it with existing helpers first, using `CreateScreen("Tutorial")`, a child `Content Frame` with `VerticalLayoutGroup`, and a `Navigation` `HorizontalLayoutGroup`. Set presenter references and explicit navigation `Back ↔ Skip ↔ Next`; Next is `tutorialDefault`. Add all router references, initialize Tutorial inactive, and update existing path lookups to include `Content Frame` where used.

- [ ] **Step 5: Regenerate, run GREEN, and commit**

Run the builder through the EditMode test, then full EditMode and PlayMode. Expected: Tutorial routes separately and all suites pass. Commit the five listed files:

```powershell
git commit -m "feat(ui): route generated tutorial screen"
```

---

### Task 3: Shared centered-card primitives and simple screens

**Files:**
- Modify: `Assets/Editor/PitchSimulatorProjectBuilder.cs`
- Modify: `Assets/Tests/EditMode/UI/PitchSimulatorProjectBuilderTests.cs`
- Modify: `Assets/Tests/PlayMode/BootstrapPlayModeTests.cs`
- Modify: `Assets/Scenes/Game.unity`

**Interfaces:**
- Consumes: generated Title, Briefing, Tutorial, Settings panels.
- Produces: `CreateContentFrame`, `SetPreferredWidth`, and constrained-button builder contracts.

- [ ] **Step 1: Write failing centered-frame assertions**

For Title, Briefing, Tutorial, and Settings, assert:

```csharp
var frame = screen.Find("Content Frame").GetComponent<RectTransform>();
Assert.That(frame.rect.width, Is.LessThanOrEqualTo(960f));
Assert.That(frame.rect.width, Is.GreaterThanOrEqualTo(680f));
AssertContained(screen.GetComponent<RectTransform>(), frame);
foreach (var button in frame.GetComponentsInChildren<Button>(true))
    Assert.That(button.GetComponent<RectTransform>().rect.width, Is.LessThanOrEqualTo(520f));
```

Also assert each full-screen panel remains stretched to `1280×720` under the Canvas.

- [ ] **Step 2: Run EditMode RED**

Expected: screens have no consistent frame and buttons remain full-width.

- [ ] **Step 3: Implement shared primitives**

Change `CreateScreen` to create only the stretched background `RectTransform` and `Image`. Add:

```csharp
private static GameObject CreateContentFrame(
    Transform parent, float width = 920f, float height = 600f,
    int horizontalPadding = 36, int verticalPadding = 32, float spacing = 18f)
{
    var frame = new GameObject("Content Frame", typeof(RectTransform), typeof(Image),
        typeof(VerticalLayoutGroup));
    frame.transform.SetParent(parent, false);
    var rect = frame.GetComponent<RectTransform>();
    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
    rect.pivot = new Vector2(0.5f, 0.5f);
    rect.sizeDelta = new Vector2(width, height);
    frame.GetComponent<Image>().color = new Color(0.055f, 0.105f, 0.13f, 0.96f);
    var layout = frame.GetComponent<VerticalLayoutGroup>();
    layout.padding = new RectOffset(horizontalPadding, horizontalPadding, verticalPadding, verticalPadding);
    layout.spacing = spacing;
    layout.childAlignment = TextAnchor.MiddleCenter;
    layout.childControlWidth = true;
    layout.childControlHeight = true;
    layout.childForceExpandWidth = false;
    layout.childForceExpandHeight = false;
    return frame;
}

private static void SetPreferredWidth(Component component, float width)
{
    var element = component.GetComponent<LayoutElement>() ??
        component.gameObject.AddComponent<LayoutElement>();
    element.preferredWidth = width;
}
```

Use a 760×500 frame for Title, 880×520 for Briefing, 920×560 for Tutorial, and 720×420 for Settings. Use 520px primary buttons, 420px secondary buttons, and 680–820px text labels. Preserve explicit navigation.

- [ ] **Step 4: Update hierarchy paths and run GREEN**

Update builder/bootstrap lookups such as `Title/Content Frame/Start Button`, regenerate Game, and run EditMode plus PlayMode. Expected: all simple-screen frames are centered and all buttons satisfy the width cap.

- [ ] **Step 5: Commit**

```powershell
git commit -m "refactor(ui): center title tutorial and settings cards"
```

---

### Task 4: Contained Pitch Room layout

**Files:**
- Modify: `Assets/Editor/PitchSimulatorProjectBuilder.cs`
- Modify: `Assets/Tests/EditMode/UI/PitchSimulatorProjectBuilderTests.cs`
- Modify: `Assets/Tests/PlayMode/BootstrapPlayModeTests.cs`
- Modify: `Assets/Tests/PlayMode/PitchInteractionPlayModeTests.cs`
- Modify: `Assets/Scenes/Game.unity`

**Interfaces:**
- Consumes: Task 3 card primitives.
- Produces: `PitchRoom/Content Frame` capped at `960×680`, response controls capped at 680px.

- [ ] **Step 1: Write failing pitch containment tests**

Require the exact hierarchy and bounds:

```text
PitchRoom
├── Environment
└── Content Frame
    ├── Status Backing
    ├── Judge Aya
    ├── Dialogue Panel
    ├── Metrics
    │   ├── Confidence
    │   └── Timer
    ├── Responses
    └── Continue Button
```

Assert Content Frame is `<=960` wide and `<=680` high, Dialogue/Status `<=860`, Responses and all response buttons `<=680`, Continue `<=520`, all children contained, timer/confidence visible, and existing contrast ratios remain at least 4.5:1.

- [ ] **Step 2: Run EditMode RED**

Expected: direct-child paths and full-width response assertions fail.

- [ ] **Step 3: Rebuild Pitch Room inside the frame**

Keep Environment as a full-screen ignored-layout child. Create `Content Frame` at `960×680` with 24px padding and 8px spacing. Parent all gameplay controls under it. Add a `Metrics` `HorizontalLayoutGroup` with preferred width 680 and height 48; place Confidence and Timer inside with preferred width 330 each. Use these caps:

```csharp
SetPreferredWidth(statusBacking.transform, 860f);
SetPreferredWidth(dialoguePanel.transform, 860f);
SetPreferredWidth(responseRoot.transform, 680f);
SetPreferredWidth(continueButton.transform, 520f);
```

Use Judge height 112, Dialogue 96, Responses 186, Continue 58, and compact status/metric heights so required vertical height remains `<=680`.

- [ ] **Step 4: Update paths, regenerate, and run GREEN**

Update all scene-specific test paths to `PitchRoom/Content Frame/...`; manual presenter rigs remain unchanged. Run EditMode and PlayMode. Expected: containment, focus, timer, reaction, response locking, and localized feedback tests all pass.

- [ ] **Step 5: Commit**

```powershell
git commit -m "refactor(ui): contain pitch room controls"
```

---

### Task 5: Contained Results card and compact footer

**Files:**
- Modify: `Assets/Editor/PitchSimulatorProjectBuilder.cs`
- Modify: `Assets/Tests/EditMode/UI/PitchSimulatorProjectBuilderTests.cs`
- Modify: `Assets/Tests/PlayMode/ResultsPlayModeTests.cs`
- Modify: `Assets/Scenes/Game.unity`

**Interfaces:**
- Consumes: Task 3 card primitives and current `ResultsPresenter` contract.
- Produces: `Results/Content Frame` with bounded scroll and 260px footer actions.

- [ ] **Step 1: Write failing Results layout assertions**

Require `Results/Content Frame/{Heading,Results Scroll,Submission Status,Footer}`. Assert frame `<=960×680`, scroll `<=860` wide, both footer buttons `<=280` wide, scrollbar remains visible/explicitly navigable, six review items remain in the content pool, and all non-scroll chrome fits within 680px.

- [ ] **Step 2: Run EditMode RED**

Expected: the old Results hierarchy is direct under the full screen and footer buttons expand.

- [ ] **Step 3: Move Results into a centered frame**

Create a 960×680 frame with 32px horizontal and 20px vertical padding. Parent Heading, Results Scroll, Submission Status, and Footer under it. Set Results Scroll preferred width 860 and flexible height 1. Configure Footer with `childForceExpandWidth=false`; set Submit and Retry preferred widths to 260 and preferred heights to 64. Keep the six-item scroll pool and `KeyboardReviewScrollbar` contract unchanged.

- [ ] **Step 4: Regenerate and run GREEN**

Update generated-scene paths in tests, regenerate Game, and run EditMode plus Results PlayMode. Expected: all score/review/submission/retry behavior and new width assertions pass.

- [ ] **Step 5: Commit**

```powershell
git commit -m "refactor(ui): center results review card"
```

---

### Task 6: Tutorial-aware WebGL smoke and visual checkpoints

**Files:**
- Create: `tools/tests/smoke-webgl-flow.test.js`
- Modify: `tools/smoke-webgl.mjs`

**Interfaces:**
- Consumes: generated Tutorial and centered-screen hierarchy.
- Produces: full-tutorial and Skip browser paths plus tutorial/pitch screenshots.

- [ ] **Step 1: Add failing Node source-contract tests**

Read `smoke-webgl.mjs` and assert it contains named actions for:

```javascript
assert.match(source, /Tutorial page 1 Next/);
assert.match(source, /Tutorial page 2 Next/);
assert.match(source, /Tutorial page 3 Start Practice/);
assert.match(source, /retry Tutorial Skip/);
assert.match(source, /chrome-tutorial\.png/);
assert.match(source, /chrome-pitch\.png/);
```

- [ ] **Step 2: Run Node RED**

```powershell
node --test tools/tests/*.test.js
```

Expected: the six new source-contract assertions fail.

- [ ] **Step 3: Update the main attempt for all three tutorial pages**

After Briefing, use three frame-held Enter actions:

```javascript
await keyboardAction(page, canvas, "Enter", options.timeoutMs, "Tutorial page 1 Next");
await keyboardAction(page, canvas, "Enter", options.timeoutMs, "Tutorial page 2 Next");
await keyboardAction(page, canvas, "Enter", options.timeoutMs, "Tutorial page 3 Start Practice");
await mouseContinue(page, canvas, options.timeoutMs, "Judge introduction");
await mouseContinue(page, canvas, options.timeoutMs, "Tutorial response reveal", true);
```

Capture `chrome-tutorial.png` before the first Next and `chrome-pitch.png` after Question 1 choices are revealed when the current browser is Chrome.

- [ ] **Step 4: Update Retry to exercise Skip**

After retry Briefing, click the centered Skip control at normalized `(0.50, 0.82)` with `delay:120`, label it `retry Tutorial Skip`, then require downstream Judge Intro, response reveal, practice response, feedback, and fresh Question 1 reveal. A Skip false-positive cannot satisfy the downstream PitchRoom controls.

- [ ] **Step 5: Run Node GREEN and commit**

Run `node --check tools/smoke-webgl.mjs` and `node --test tools/tests/*.test.js`. Expected: all Node tests pass. Commit:

```powershell
git commit -m "test(web): cover tutorial completion and skip"
```

---

### Task 7: Full verification, visual review, and release records

**Files:**
- Modify: `README.md`
- Modify: `CHANGELOG.md`
- Modify: `TASKS.md`
- Modify: `docs/02-LEARNER-EXPERIENCE.md`
- Modify: `docs/05-TECHNICAL-ARCHITECTURE.md`
- Modify: `docs/06-STATE-SESSION-FLOW.md`
- Modify: `docs/08-ACCESSIBILITY-LOCALIZATION.md`
- Modify: `docs/13-QA-PLAN.md`
- Modify: `docs/16-ASSET-MANIFEST.md`
- Modify: `docs/18-VERTICAL-SLICE-ACCEPTANCE.md`

**Interfaces:**
- Consumes: Tasks 1–6 and ignored verification artifacts.
- Produces: current, bounded acceptance evidence for the UI-polish branch.

- [ ] **Step 1: Run canonical Unity suites**

```powershell
& .\tools\Run-UnityTests.ps1 -Platform EditMode
& .\tools\Run-UnityTests.ps1 -Platform PlayMode
```

Expected: all tests pass, zero failures/skips/inconclusive and no `error CS`, compilation failure, or unhandled exception markers.

- [ ] **Step 2: Build WebGL and run server/browser gates**

```powershell
& .\tools\Build-WebGL.ps1
& .\tools\Serve-WebGL.ps1 -SelfTest
node .\tools\smoke-webgl.mjs
```

Expected: BuildReport `Succeeded` with zero warnings/errors; server self-test passes; Chrome and Edge pass with zero console/page errors; Firefox remains honestly unavailable if not installed; server stops with zero stderr.

- [ ] **Step 3: Inspect visual checkpoints**

Inspect `artifacts/smoke/chrome-tutorial.png`, `chrome-pitch.png`, `chrome-smoke.png`, and the Edge equivalents. Confirm centered cards, non-stretched buttons, readable text, no clipping, no horizontal overflow, visible focus, and consistent spacing at desktop and `390×844` browser viewports. Record exact defects rather than accepting screenshots by filename.

- [ ] **Step 4: Update documentation with measured evidence**

Document the three tutorial pages, every-attempt/Skip behavior, centered card widths, current test totals, build size/time, browser versions/times, screenshot paths, asset/meta counts, and remaining production/human gaps. Do not claim final Malay, final audio, Firefox/Safari, native touch, unrestricted fullscreen, real LMS, classroom, accessibility-human, hosting, legal, or release approval.

- [ ] **Step 5: Validate records and commit**

Run relative-link validation, meta reconciliation, privacy/secret scans, `git diff --check`, and `git status --short`. Expected: zero broken links, missing/orphan metas, unexpected secret shapes, or uncommitted normalization noise. Commit:

```powershell
git commit -m "docs: record tutorial and UI polish evidence"
```

- [ ] **Step 6: Independent whole-branch review**

Review `3271adf..HEAD` against the approved design and this plan. The handoff gate requires no actionable P0–P3 findings and explicit confirmation that tutorial completion, Skip, Retry reset, centered layouts, focus, localization parity, full suites, build, browser smoke, and production boundaries are correct.

---

## Self-review checklist

- Design coverage: all approved tutorial pages, every-attempt behavior, Skip, modern game-card style, and all six in-game screens map to Tasks 1–5.
- Isolation: the completed vertical-slice branch/worktree and `master` remain unchanged.
- Type consistency: `TutorialPresenter`, router field names, hierarchy paths, and localization keys are identical across tasks.
- No scope creep: no LMS, scoring, dialogue, harness design, audio asset, or persistence change is included.
- Verification: focused RED/GREEN, canonical Unity, WebGL, server, Chrome/Edge, screenshots, docs, privacy, and independent review all have explicit gates.
