# Run and Troubleshoot the Local Integration

## Before you start

Install Unity 6000.5.3f1 with WebGL support. Complete Task 18 before expecting `Build/WebGL/index.html`. Use an HTTP server rooted at the repository; keep the player and harness same-origin.

## Local operator flow

1. Build with `tools/Build-WebGL.ps1` after its Task 18 implementation.
2. Start a local HTTP server using an approved tool and record its command/port.
3. Open `/WebHarness/index.html`.
4. Select Success for the normal flow. Use Failure/Expired to check retained results and resubmission. Use Missing Config, then Success plus Resend Launch, to check recovery.
5. Record browser/version and sanitized outcomes.

## Common problems

### Build output is absent

Task 18 is incomplete or failed. Read the complete build log, check the editor version/module, and do not claim browser readiness.

### Embedded build does not connect

Confirm HTTP rather than `file:`, the iframe path, response MIME/compression, same origin, and no CSP/frame block. Do not change target origin to wildcard.

### Completion does not resolve

Confirm harness mode, request ID shape, and exact protocol version/message names. A native pending request is canceled after 30 seconds; use the UI retry rather than replaying a stale callback.

### No sound

Current clips are placeholders. With future clips, select Start first, verify mute/volumes, and check browser autoplay policy.

### Malay shows English

This is intentional until qualified Malay review replaces fallback copy and changes catalog status.

Escalate with sanitized evidence only. See [deployment](10-WEB-DEPLOYMENT.md), [security](12-PRIVACY-SECURITY.md), and [QA](13-QA-PLAN.md).
