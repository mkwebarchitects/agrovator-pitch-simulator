# Protect Learner Data and the Browser Boundary

## Data minimization

Completion payloads use a pseudonymous learner ID and stable course/session/content identifiers. They contain no name, email, school, credential, response text, or open-ended learner content. Keep it that way. Selected response IDs are learning records and should be retained only under an approved policy.

`LaunchReference` is opaque and short-lived by design. Never put it, tokens, learner IDs, session IDs, or complete payloads in URLs, console logs, analytics events, screenshots, or support tickets. The harness UI renders only allowlisted status, score, attempt, competency count, and timeout count.

## Browser boundary

The local JavaScript contract accepts messages only from the expected frame/parent and exact same origin, checks protocol version/type/request ID, and does not log payloads. Production origin and frame policies are unknown until LMS discovery. Do not weaken origin checks to `*` to make deployment convenient.

## Required human decisions

Privacy/security owners must approve purpose, lawful basis where relevant, data owner, retention/deletion, access/audit controls, incident response, regional requirements, vendor terms, threat model, and penetration-test scope. This repository does not provide a compliance certification.

## Incident handling

If sensitive data appears in logs or payloads, stop distribution, preserve minimal evidence securely, notify the responsible owner, rotate exposed references/credentials, remove the source, and document remediation without copying learner data into public issues.

See [LMS discovery](00-LMS-DISCOVERY.md), [contract](11-LMS-CONTRACT.md), and [asset manifest/release governance](16-ASSET-MANIFEST.md).
