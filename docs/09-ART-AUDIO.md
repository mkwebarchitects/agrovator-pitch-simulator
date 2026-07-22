# Maintain Art and Audio Assets

## Included art

Project-bound original outputs live under `Assets/Art`: a 1408x160 eleven-state Judge Aya sheet, 1280x720 pitch room, 768x384 nine-slice dialogue panel, and 480x96 five-state confidence icons. `Assets/Art/CREDITS.md` and `manifest.json` record prompts, dimensions, processing, and provenance. Import rules use point filtering, no mipmaps, uncompressed textures, clamp wrap mode, and 32 pixels per unit.

Judge reactions are typed; missing/unknown artwork falls back to Encouraging. The authored Curious cue maps to Interested. Reduced motion uses a static semantic reaction.

## Audio status

`Assets/Audio/PLACEHOLDERS.md` defines eleven cues: MusicLoop, ButtonPress, ResponseSelected, TimerWarning, JudgeReactionImpressed, FeedbackOpen, ResultsReveal, CompletionSuccess, CompletionFailure, JudgeReactionInterested, and JudgeReactionConcerned (the last two appended after the original nine to keep every already-bound cue's serialized index unchanged). `AudioCueDirector` connects them to the first handled user gesture, button gestures, response/reaction events (the judge cue resolved from the same typed mapping the portrait uses, so the sound always matches the face), the once-per-question final-five threshold, feedback/results reveal, and completion callbacks. No final clip is included, licensed, or claimed. Runtime null clips warn once per player instance in development and remain silent.

## Replacement checklist

Record source, creator/license, permitted use, edit history, and review date. Check child suitability, loudness, loops, browser unlock behavior, independent volume, missing-clip fallback, sprite slicing, contrast, 1280x720 containment, and reduced-motion behavior. A human must visually and audibly approve replacements.

See [accessibility](08-ACCESSIBILITY-LOCALIZATION.md), [QA](13-QA-PLAN.md), and [asset manifest/release governance](16-ASSET-MANIFEST.md).
