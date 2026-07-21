# Visual Polish and Image Quality Upgrade

## Objective

Make the guided pitch builder look crisp and feel like a game for Primary and
Secondary school learners, without weakening the accessibility, localization, or
LMS contracts already proven by the suites.

Two separate problems are in scope. Fix them in this order, because the first is
a defect with a measurable right answer and the second is design work that
should be judged against a correct image.

1. **Image quality — jagged art.** Diagnosed below. This is a real bug, not
   taste.
2. **Presentation — reads as a form, not a game.** Ranked list below.

## Diagnosis of the jagged art, with evidence

Do not re-guess these. Verify each one still holds, then fix.

**A. The shipping build block-compresses the pixel art.**
`Assets/Scripts/UI/Editor/PixelArtImportPostprocessor.cs:38` sets
`importer.textureCompression = TextureImporterCompression.Uncompressed`, but that
is the *default platform* setting only. The postprocessor never touches
per-platform overrides — `grep` it for `SetPlatformTextureSettings` and you get
nothing. The committed `.meta` files carry a WebGL override that disagrees:

```
buildTarget: WebGL
textureCompression: 1     # 1 = Compressed
```

So every WebGL build ships DXT/ASTC-compressed art. Block compression on flat
colour regions with hard edges — exactly what this art is — produces colour
fringing and chewed-up edges. This is the most likely source of what the project
owner sees as "jagged", and the existing `PixelArtImportTests` do not catch it
because they only assert the default-platform settings.

Fix: have the postprocessor set the WebGL (and Standalone) platform override to
uncompressed explicitly, via `TextureImporterPlatformSettings` with
`overridden = true`. Extend `PixelArtImportTests` to assert the *WebGL override*,
not just the default, so this cannot regress. Expect the release build to grow —
report the new size honestly rather than trading image quality back for bytes
without saying so.

**B. Point filtering at fractional scale.**
Every sprite imports with `FilterMode.Point` (`PixelArtImportPostprocessor.cs:36`).
Point sampling only looks clean at integer scale factors. The canvas uses
`CanvasScaler.ScaleMode.ScaleWithScreenSize` with a 1280x720 reference and
`matchWidthOrHeight = 0.5`, and the WebGL template feeds a continuously variable
device pixel ratio (`TemplateData/layout.js:28-31` clamps DPR to `[1,2]` but does
not quantise it). At any non-integer scale, point sampling duplicates some source
pixels and drops others, so straight edges go visibly uneven and shimmer while
resizing.

Two defensible fixes; pick one and justify it:
- Quantise the effective scale to integers (snap `renderScale`, or drive
  `CanvasScaler.scaleFactor` to an integer and letterbox the remainder), or
- Use `FilterMode.Bilinear` for the large art that is not true low-resolution
  pixel art (see C) while keeping `Point` for genuine sprite-sheet pixel art.

**C. The environment is not really pixel art.**
`Assets/Art/Environment/pitch-room.png` is 1280x720 — a full-resolution
illustration imported at 32 pixels-per-unit with `Point`. `judge-aya-sheet.png`
is 1408x160, i.e. eleven genuine 128x160 pixel-art frames. These two want
different treatment, and the postprocessor currently applies one policy to both.
Treat the background as an illustration (bilinear is fine, it is displayed at
roughly 1:1) and keep the character sheet as strict pixel art.

## Presentation work, ranked by impact per effort

Evidence for each is in `artifacts/smoke/*.png` from a passing smoke run —
regenerate them with `node tools/smoke-webgl.mjs` and look before changing
anything.

1. **Reveal the environment art.** In `chrome-primary-build.png` the pixel-art
   garden room is visible only as two ~90px slivers at the far edges; a nearly
   opaque navy panel (`#0E171F`) covers ~85% of the screen, with dead bands top
   and bottom. Narrow the content frame and/or make the panel semi-transparent so
   the room reads as a place. This is the largest visual gain available and costs
   no new assets.
2. **Replace the placeholder icons.** The four pitch parts currently use the
   literal characters `!`, `?`, `>`, `*`. Author four small icons in Judge Aya's
   style (for example magnifying glass, chart, wrench, star) and wire them through
   `PitchPartVisuals`.
3. **Give Results a reward moment.** `chrome-primary-results.png` shows
   "Pitch Readiness: 100%" as plain bold text on black. Add a segmented meter that
   fills, part cards that appear in sequence, and the already-wired completion
   audio cue.
4. **Make Judge Aya present.** She is ~100px in a corner. Scale her up 1.5-2x and
   move her line into a speech bubble instead of a flat text panel. Her reaction
   states already exist and must keep working.
5. **Add a display typeface for headings.** Everything is currently default
   system sans. Use one warm rounded face for headings only; keep body text as is
   for legibility. Confirm licensing allows redistribution and record it in
   `docs/16-ASSET-MANIFEST.md`.
6. **Fix the compact/mobile layout.** In `chrome-mobile-compact.png` the rail
   labels collapse to bare coloured bars with no text, so the four-part concept —
   the whole point of the game — becomes invisible on a phone, and over half the
   screen is empty. This is the biggest single change; confirm with the educator
   review whether phones are actually in scope before investing in it.

## Hard constraints

- **Never hand-edit scene YAML.** Owned scenes change only by regenerating
  through `PitchSimulatorProjectBuilder.BuildProjectFoundationBatch`.
- **Bump `PitchSimulatorProjectBuilder.GeneratorVersion`** whenever you change what
  the builders generate. `GeneratedSceneFreshnessTests` will fail if the committed
  scenes do not match the current builder, which is the guard that makes a missed
  regeneration impossible to commit.
- **Do not weaken accessibility.** Contrast, focus indicators, and keyboard
  navigation are asserted by
  `GeneratedScenes_MeetTargetContrastNavigationAndPixelArtContracts`. A
  semi-transparent panel must still pass the contrast assertions — adjust the
  panel, not the assertion.
- **Gate every new animation on reduced motion.** `AccessibilitySettings.ReducedMotion`
  arrives from the LMS launch config. Celebration effects must degrade to a static
  end state when it is set.
- **Keep both build flavours working.** `tools/Build-WebGL.ps1` defaults to the
  development build because the smoke and Node contract tests depend on that
  output shape; `-Release` produces the player learners download.
- **Never access the AGROVATOR LMS repository.** Never commit tokens, secrets, or
  learner data.

## Method

Work test-first, one item per commit, in the order given. For each: write the
failing test and capture the RED output, implement the smallest change, then
capture GREEN. Where a change is genuinely not test-observable — a colour choice,
an icon — say so plainly rather than writing a test that passes either way.

Record dated RED/GREEN evidence in `TASKS.md` with exact XML totals, log line
counts, and failure-marker counts, and keep the final line exactly:
`- Next unchecked guided-builder action: Primary/Secondary educator or representative-learner review.`

## Required tests

```
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "tools/Run-UnityTests.ps1" -Platform EditMode
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "tools/Run-UnityTests.ps1" -Platform PlayMode
node --test "tools/tests/*.test.js"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "tools/Build-WebGL.ps1"
node tools/smoke-webgl.mjs
```

Parse `artifacts/test-results/{editmode,playmode}.xml` root attributes and scan the
complete `artifacts/logs/*.log` for `error CS\d+|Compilation failed|Unhandled Exception`.
Never trust Unity's exit code; a sandboxed exit 198 is a licensing failure, not
evidence. Require Chrome and Edge `status: passed`, `modes.missingConfig: true`,
zero console errors, zero page errors.

After any builder run, revert verified-unrelated `ProjectSettings.asset`
normalization (iOS/tvOS/visionOS target strings, `targetPixelDensity`,
`buildNumber`) and delete untracked `ProjectSettings/SceneTemplateSettings.json`.
Never discard guided scene changes.

## Publishing

Do not publish without being asked. When asked, use
`tools/Deploy-Pages.ps1 -Push`, which builds the release flavour itself and
refuses to publish a development build, an unstamped index, or a tree over 40 MB.
Verify afterwards by smoking the live site:

```
node tools/smoke-webgl.mjs --base-url https://mkwebarchitects.github.io/agrovator-pitch-simulator --external-server
```

## Definition of done

Art renders cleanly at the sizes learners actually use, with the WebGL texture
override proven uncompressed by a test rather than by inspection; the environment
is visible behind the content; the four parts carry real icons; Results rewards
completion; every suite passes with evidence recorded; the released build size
change is reported honestly; and no accessibility or reduced-motion contract was
relaxed to achieve any of it.

## Open questions for a human

- Are phones in scope for classroom use? Item 6 is expensive and should not be
  built on an assumption.
- Is there a brand or school-facing style guide the display typeface must match?
- Should the environment change per scenario later, or is the garden room fixed?
