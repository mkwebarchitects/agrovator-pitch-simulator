# Operate Accessibility and Localization

## Implemented behavior and launch-configured preferences

Timer modes are Normal, Extended, and Off. Zero duration means no expiry. Extended mode rejects non-finite overflow. Reduced motion keeps the meaning of Judge Aya's reaction while suppressing talk/blink loops. Keyboard focus, explicit navigation, visible countdown, confidence labels/artwork, and contrast backings are implemented for the 1280x720 reference layout. All generated buttons remain at least 64px high. The Results scrollbar has a 64px pointer target, a 48px cream focus rail around its centered 32px track, and measured selected-focus contrast of `14.09:1` against the scroll background.

Music and SFX volumes are independent finite-safe 0-1 values. Browser playback is locked until the learner's Start or Settings action. All nine audio bindings are intentionally empty; silence is expected until licensed, reviewed clips are supplied.

The current Settings screen provides only Back. Learners cannot yet edit timer mode, reduced motion, language, music volume, or SFX volume in the UI. `Bootstrapper` applies these values from validated LMS launch data in WebGL or from the local mock defaults in Editor/non-WebGL players.

## Localization status

`Assets/Content/Localization/en.json` is the reviewed 145-key source catalog. `ms.json` has exact ordinal 145-key parity and status `pending_human_review`; it uses English fallback copy rather than claiming unreviewed Malay translation. The 13 Tutorial/UI-polish keys are covered by exact catalog parity tests. Supported locale codes are `en` and `ms`, with English fallback for missing/unsupported content.

## Human verification required

Fresh Chrome/Edge automation exercised focused keyboard Tutorial and scored-response actions, pointer-only Retry/Skip practice flow, and a visible gold canvas focus outline. A generated active-layout EditMode regression also assigns every authored Pitch Room prompt/outcome and response string plus all five confidence labels at `1280x720`, requiring UGUI `TextGenerator` to report every character rendered. The replacement Chrome Pitch checkpoint visibly preserves the localized endings `system?`, `inconsistent.`, and `Curious`. This is machine evidence only. Use keyboard-only play, screen zoom, reduced-motion comparison, timer modes, control-level focus visibility, comprehension/readability review, and a qualified Malay linguistic review. Automated tests do not certify WCAG conformance, assistive-technology compatibility, or translation quality.

See [learner instructions](02-LEARNER-EXPERIENCE.md), [art/audio](09-ART-AUDIO.md), and [QA plan](13-QA-PLAN.md).
