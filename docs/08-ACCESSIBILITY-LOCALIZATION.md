# Operate Accessibility and Localization

## Implemented interaction and layout

The guided activity is untimed. The Briefing states “There is no timer. Take the time you need,” construction does not play `TimerWarning`, and the completion timeout count remains zero. Existing launch-configured timer modes still exist for the legacy experience but do not impose a guided completion deadline.

Keyboard support covers Tab/Shift+Tab, arrow navigation through sentence cards, Enter/Submit, visible gold focus, phase-change focus restoration, and scrolling a focused card into view. Generated buttons remain at least 64px high. Pointer interaction is covered on the assigned Edge route. The opaque navy lesson panel, cream selectable cards, persistent part icon/colour pairs, and controlled garden background keep selection and progress visually distinct.

Responsive behavior uses CSS viewport metrics. At width `<= 960` or aspect ratio `< 1.25`, the board becomes two columns, cards/actions stack, and scroll support is enabled. The WebGL backing scale follows DPR from `1` to a cap of `2`; pixel-art textures keep point filtering while text and controls render at UI resolution.

Fresh runtime evidence measured desktop CSS/backing `1276x918` and compact CSS/backing `380x783` in both Chrome and Edge at DPR/render scale `1`. Both were contained, focused, and had zero inner/outer horizontal overflow. Higher-DPR source/math contracts passed, but a DPR-above-1 browser run remains unclaimed.

## Primary and Secondary reading behavior

Primary cards use direct familiar language and are validator-bounded to 12-16 words. Secondary cards are bounded to 32 words and distinguish observation, measurement, assumption, unsupported/qualified claims, audience relevance, and uncertainty. Both modes share control size, navigation, icon/colour language, feedback order, and completion mechanics.

Automated length and exact-copy tests are implementation checks. They are not proof that either mode is age-appropriate or understandable. Primary and Secondary educators or representative learners must review reading level, coaching tone, task length, and transfer usefulness.

## Localization status

`Assets/Content/Localization/en.json` is the reviewed source catalog with `319` entries. `ms.json` also has `319`; the fresh localization fixture verifies exact ordinal key parity and exact guided fallback-value parity. Malay remains `pending_human_review`, so the current Malay catalog deliberately shows English fallback copy rather than claiming a completed translation.

Supported locale codes are `en` and `ms`, with English fallback for an unsupported or missing localized value. Missing keys produce a visible deterministic token with escaped unsafe characters rather than hidden failure.

## Human verification required

The eleven guided screenshots were inspected at original detail and automated layout/text tests cover wide/compact containment, long Primary/Secondary copy, focus, fixed actions, results scrolling, and DPR formulas. Human accessibility review is still required for keyboard-only use, zoom, comprehension, contrast in context, motion, assistive technology, screen readers, and cognitive load. Native touch, unrestricted fullscreen, final audio/hearing review, qualified Malay review, Firefox, and Safari remain unclaimed.

See [learner instructions](02-LEARNER-EXPERIENCE.md), [art/audio](09-ART-AUDIO.md), and [QA plan](13-QA-PLAN.md).
