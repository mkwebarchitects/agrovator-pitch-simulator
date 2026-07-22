# Audit Assets and Govern Releases

## Fresh asset reconciliation

The 2026-07-19 Task 9 scan found `142` logical files and `40` logical directories under `Assets` (`182` non-meta entries total), with `182` matching `.meta` files. Missing meta count is zero and orphaned meta count is zero.

The six branch-wide trailing spaces in `Assets/Scripts/UI/GuidedPitch.meta` and `Assets/Tests/PlayMode/GuidedPitch.meta` were removed during acceptance because they made `git diff --check ce7f8ac` fail. Their GUIDs, folder status, and metadata values were not changed.

## Content, media, and scene inventory

| Asset | Provenance/role | Current disposition |
| --- | --- | --- |
| `Assets/Content/Scenarios/guided-pitch-builder.en.json` | First-party guided content | Active Bootstrap reference; `smart-school-garden` v2, 10-minute estimate, `30` tested stable unique option IDs, EN/MS support |
| `Assets/Content/Scenarios/smart-school-garden.en.json` | First-party legacy dialogue content | Retained v1 asset; not wired into the current Bootstrap guided composition |
| `Assets/Content/Localization/en.json` | First-party reviewed source | `319` entries; exact content/UI keys validated |
| `Assets/Art/Characters/judge-aya-sheet.png` | `Assets/Art/manifest.json` and `CREDITS.md`; original OpenAI-generated project output | `1408x160`, `254,633` bytes, SHA-256 `D720AC61FB9E38145559237C73E19CABA86785B36E7DF338D296F97B57CFA417` |
| `Assets/Art/Environment/pitch-room.png` | Same art provenance records | `1280x720`, `1,097,279` bytes, SHA-256 `D03AABFC4B7EA04695A02C8CFDB88FC3CBD65BDDCA5D4DECEAFC0050C9F5902D` |
| `Assets/Art/UI/confidence-icons.png` | Same art provenance records; retained legacy asset | `480x96`, `28,133` bytes, SHA-256 `6A4C255CD283D077E9D79D8AFC56DFA1CB6191E1CD9FCA4C908D8B4DA0331BA0` |
| `Assets/Art/Fonts/MysteryQuest-Regular.ttf` | Third-party, SIL Open Font License 1.1 | Mystery Quest by Font Diner, from Google Fonts (github.com/google/fonts/ofl/mysteryquest); OFL permits bundling and redistribution; `OFL.txt` ships alongside; used for headings and short labels only, body text stays on the built-in font for legibility |
| `Assets/Art/UI/part-icons.png` | First-party original | `384x96`, four named `96x96` cells (`Problem`, `Evidence`, `Solution`, `Value`); authored 2026-07-21 for this project in the existing part palette; no third-party source, so no redistribution restriction |
| `Assets/Art/UI/dialogue-panel.png` | Same art provenance records | `768x384`, `205,025` bytes, SHA-256 `5F9EA03D59C7869E74921060F8F0515DD30508B49C0FE65574497F7312ECA834` |
| `Assets/Art/manifest.json` | Machine-readable first-party provenance | Records prompts, processing, dimensions, source/date, `thirdPartyDerivatives: false`, and an output-terms statement |
| `Assets/Art/CREDITS.md` | Human-readable art provenance | Describes project source; does not replace a repository licence |
| `Assets/Audio/PLACEHOLDERS.md` | First-party placeholder rules | No audio binary is present; all nine runtime clip slots remain null |
| `Assets/Scenes/Bootstrap.unity` | First-party generated scene | References exactly one active guided content asset and tracked scripts |
| `Assets/Scenes/Game.unity` | First-party generated scene | Guided six-screen/router composition with project-owned media/scripts |
| `Assets/Scenes/WebIntegrationTest.unity` | First-party generated diagnostic scene | Excluded from default build order |

The WebGL template and `WebHarness` use system fonts and no remote CDN, font, or media dependency. No third-party binary asset, external logo, watermark, or audio binary was identified. The four PNG paths and dimensions remain covered by the art manifest.

## Generated-output policy

`Library`, `Temp`, `Logs`, `UserSettings`, `Build`, and `artifacts` remain ignored. The fresh seven-file WebGL build and Unity/browser evidence are local verification outputs and must not be committed. Unity-generated `ProjectSettings.asset` normalization and `SceneTemplateSettings.json` editor state were removed after acceptance.

## Licence and provenance gates

- No tracked repository-wide `LICENSE*`, `COPYING*`, or `NOTICE*` exists. Human legal/release authority must resolve this before distribution.
- The art records describe applicable OpenAI output terms but do not archive the terms version or generating-account authority evidence. Human creative/legal approval remains required.
- Malay was removed on 2026-07-21; the game ships in English only. Final audio remains absent and is not a release-ready asset set.
- A replacement asset must record source, creator, licence, permitted use, edit history, checksum where practical, review date, and accessibility/import/browser evidence.

## Required release record

Record commit/tag, Unity version, game/content version, exact test commands and XML totals, build inventory, browser matrix, LMS contract revision/environment, localization approvals, asset provenance, known limitations, reviewers, deployment/rollback owner, and date. Automated implementation evidence must be kept separate from educator/learner learning-effectiveness review.

See [art/audio](09-ART-AUDIO.md), [roadmap](15-PRODUCTION-ROADMAP.md), [QA](13-QA-PLAN.md), and [acceptance](18-VERTICAL-SLICE-ACCEPTANCE.md).
