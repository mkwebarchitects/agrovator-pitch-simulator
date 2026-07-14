# Operate Accessibility and Localization

## Implemented behavior and launch-configured preferences

Timer modes are Normal, Extended, and Off. Zero duration means no expiry. Extended mode rejects non-finite overflow. Reduced motion keeps the meaning of Judge Aya's reaction while suppressing talk/blink loops. Keyboard focus, explicit navigation, visible countdown, confidence labels/artwork, and contrast backings are implemented for the 1280x720 reference layout.

Music and SFX volumes are independent finite-safe 0-1 values. Browser playback is locked until the learner's Start or Settings action. All nine audio bindings are intentionally empty; silence is expected until licensed, reviewed clips are supplied.

The current Settings screen provides only Back. Learners cannot yet edit timer mode, reduced motion, language, music volume, or SFX volume in the UI. `Bootstrapper` applies these values from validated LMS launch data in WebGL or from the local mock defaults in Editor/non-WebGL players.

## Localization status

`Assets/Content/Localization/en.json` is the reviewed source catalog. `ms.json` has exact ordinal key parity and status `pending_human_review`; it uses English fallback copy rather than claiming unreviewed Malay translation. Supported locale codes are `en` and `ms`, with English fallback for missing/unsupported content.

## Human verification required

Use keyboard-only play, screen zoom, reduced-motion comparison, timer modes, focus visibility, comprehension/readability review, and a qualified Malay linguistic review. Automated tests do not certify WCAG conformance, assistive-technology compatibility, or translation quality.

See [learner instructions](02-LEARNER-EXPERIENCE.md), [art/audio](09-ART-AUDIO.md), and [QA plan](13-QA-PLAN.md).
