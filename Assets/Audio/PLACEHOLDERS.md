# Audio placeholders

No final audio files are included, licensed, or claimed by this repository. The generated Bootstrap scene intentionally binds every cue to a null clip so missing audio cannot block play. The names below are reserved integration targets only.

| Cue | Future filename | Channel | Loop policy |
| --- | --- | --- | --- |
| `MusicLoop` | `music-pitch-room-loop.ogg` | Music | Seamless loop; never autoplay before a user gesture |
| `ButtonPress` | `sfx-button-press.wav` | SFX | One shot |
| `ResponseSelected` | `sfx-response-selected.wav` | SFX | One shot |
| `TimerWarning` | `sfx-timer-warning.wav` | SFX | One short one shot; do not repeat as an alarm |
| `JudgeReaction` | `sfx-judge-reaction.wav` | SFX | One shot |
| `FeedbackOpen` | `sfx-feedback-open.wav` | SFX | One shot |
| `ResultsReveal` | `sfx-results-reveal.wav` | SFX | One shot |
| `CompletionSuccess` | `sfx-completion-success.wav` | SFX | One shot |
| `CompletionFailure` | `sfx-completion-failure.wav` | SFX | One shot; neutral and non-punitive |

## Delivery and import requirements

- Deliver lossless PCM WAV masters at 44.1 or 48 kHz, 16- or 24-bit. A loop may also ship as a tested Ogg Vorbis derivative for source control and WebGL size.
- Import as 2D audio with `Force To Mono` when stereo positioning is not meaningful. Disable `Play On Awake`. Use compressed-in-memory Vorbis for short SFX and streaming for a longer music loop only after profiling the WebGL build.
- Trim silence and clicks, provide a sample-accurate loop for `MusicLoop`, and avoid normalization settings that introduce clipping. Re-test decoding and loop boundaries in the target browsers.
- Music and SFX must remain independently adjustable; the master mute must not overwrite those stored levels.

## Licensing and provenance

Before any clip is committed, record its creator, source URL or contract, acquisition date, exact license/version, attribution text, allowed project/distribution scope, and proof of purchase or permission in the project credits and asset manifest. Use original, commissioned, CC0, or explicitly compatible commercial audio. Do not use ripped, unverified, attribution-incompatible, or generative audio without written rights and provenance suitable for client distribution.

## Loudness and child-safety review

Target a comfortable, consistent mix rather than maximum loudness. As an initial review reference, keep music near -20 LUFS integrated and short effects near -18 LUFS integrated, with true peaks at or below -1 dBTP; validate the final mix on classroom laptops and headphones. Avoid startling transients, high-pitched repeated alarms, aggressive failure stings, voices that shame the learner, and sounds that mask spoken instruction. `TimerWarning` and `CompletionFailure` require an explicit low-stress child-safety review before release.
