# Discover the Production LMS Contract

## Audience and purpose

This is the required discovery checklist for the LMS owner, integration developer, and security reviewer before production integration. The external AGROVATOR LMS repository and API were deliberately not inspected while building this standalone Unity project.

## Confirmed locally

- `Assets/Scripts/LMS/ILmsBridge.cs` defines the game-side launch and completion boundary.
- `Assets/Plugins/WebGL/PitchSimulatorBridge.jslib` and `WebHarness/harness.js` implement a version-1, same-origin `postMessage` mock.
- The four harness outcomes are Success, Failure, Expired, and Missing Config.
- Launch references match `lref_` plus 12-60 URL-safe identifier characters. They are not sent in query strings.
- Completion data contains identifiers, scores, timing, selected response IDs, and counts; it contains no learner name, email, answer text, or free text.

These facts prove the local contract only. They do not prove production API, SCORM, xAPI, authentication, availability, or compliance behavior.

## Ask the LMS owner

1. Confirm launch transport, exact field names, types, size limits, locale values, timer modes, and content-version policy.
2. Confirm completion transport, response/error model, idempotency key, retry window, timeout, and session-expiry behavior.
3. Confirm trusted origins, frame policy, content-security policy, HTTPS/TLS requirements, token lifecycle, and whether cookies are required.
4. Confirm pseudonymous identifier creation, retention, deletion, audit logging, and incident-response owners.
5. Confirm deployment ownership, build hosting path, cache headers, rollback mechanism, and support escalation.
6. Obtain example payloads with synthetic values and a test environment; never request production learner data for development.

## Record the answers

Create a dated, reviewed contract revision. Map each external field to `LmsLaunchConfig` or `LmsCompletionPayload`, document deviations, add contract tests, and update [ADR 0003](adr/0003-custom-rest-over-scorm-xapi.md). Human sign-off is required from LMS, privacy/security, and learning-product owners before production traffic.

## Known unknowns

Production endpoints, credentials, retry semantics, service levels, browser embedding headers, and compliance decisions remain unknown. See [LMS contract](11-LMS-CONTRACT.md), [privacy and security](12-PRIVACY-SECURITY.md), and [deployment](10-WEB-DEPLOYMENT.md).
