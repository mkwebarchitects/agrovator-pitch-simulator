# Maintain Art and Audio Assets

## Included art

Project-bound original outputs live under `Assets/Art`: a 1408x160 eleven-state Judge Aya sheet, 1280x720 pitch room, 768x384 nine-slice dialogue panel, and 480x96 five-state confidence icons. `Assets/Art/CREDITS.md` and `manifest.json` record prompts, dimensions, processing, and provenance. Import rules use point filtering, no mipmaps, uncompressed textures, clamp wrap mode, and 32 pixels per unit.

Judge reactions are typed; missing/unknown artwork falls back to Encouraging. The authored Curious cue maps to Interested. Reduced motion uses a static semantic reaction.

## Audio status

`Assets/Audio/PLACEHOLDERS.md` defines nine cues: MusicLoop, ButtonPress, ResponseSelected, TimerWarning, JudgeReaction, FeedbackOpen, ResultsReveal, CompletionSuccess, and CompletionFailure. No final clip is included, licensed, or claimed. Runtime null clips warn once in development and remain silent.

## Replacement checklist

Record source, creator/license, permitted use, edit history, and review date. Check child suitability, loudness, loops, browser unlock behavior, independent volume, missing-clip fallback, sprite slicing, contrast, 1280x720 containment, and reduced-motion behavior. A human must visually and audibly approve replacements.

See [accessibility](08-ACCESSIBILITY-LOCALIZATION.md), [QA](13-QA-PLAN.md), and [release governance](16-RELEASE-GOVERNANCE.md).
