# Verify the Vertical Slice

## Automated baseline

The confirmed Task 16 checkpoint is EditMode `296/296` and PlayMode `35/35`, with zero failures, skips, inconclusive tests, or compile/exception markers. Run:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Run-UnityTests.ps1 -Platform EditMode
powershell -ExecutionPolicy Bypass -File tools/Run-UnityTests.ps1 -Platform PlayMode
```

The wrappers write XML under `artifacts/test-results` and logs under `artifacts/logs`. Counts can change when tests are added; report fresh XML rather than repeating this baseline as current evidence.

## Task 18 build checkpoint

Require editor build method success, exit zero, no failure markers, expected WebGL files, correct Bootstrap/Game order, and a recorded artifact inventory. This checkpoint is pending.

## Task 19 browser matrix

Serve over local HTTP and verify current Chrome, Edge, and Firefox: load, title, Success launch, full play, results, completion success, failure retry, expiry, Missing Config recovery, refresh, keyboard-only navigation, reduced motion, all timer modes, and 1280x720 containment. Manually verify audio unlock/volume after real clips exist, fullscreen, touch behavior, and sanitized harness display. Safari is unavailable on Windows and must be recorded as untested or run on macOS.

## Content and human review

Play every branch including recovery/timeouts; verify pedagogy, no answer-length/order cue, child suitability, English, qualified Malay review, accessibility with relevant assistive technology, visual quality, audio rights/loudness, privacy/security, and production LMS contract.

## Defect evidence

Record environment, commit, exact command/actions, expected/actual result, sanitized screenshot/log excerpt, severity, owner, and retest. Never fabricate a pass or include a launch/completion payload. See [acceptance](18-VERTICAL-SLICE-ACCEPTANCE.md) and [operations](14-OPERATIONS-TROUBLESHOOTING.md).
