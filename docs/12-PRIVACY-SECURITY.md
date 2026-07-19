# Protect Learner Data and the Browser Boundary

## Data minimization

The guided builder adds no learner free text, audio recording, AI scoring, personal profile field, or new transport. Completion contains pseudonymous learner/session/course/module/lesson identifiers, scenario/game/content versions, status/timestamps/duration, readiness and six competency scores, chronological selected option IDs, timeout/attempt counts, hidden legacy confidence, and an optional follow-up lesson ID.

It contains no learner name, email, school identifier, credential, token, response sentence text, or open-ended learner content. `LmsLaunchConfig` and `LmsCompletionPayload` retain their approved `14`/`19` field shapes and types. Selected option IDs and scores remain learning records; production purpose, retention, deletion, access, and audit controls require approval.

## Logging and display rules

Never put a launch reference, token, learner ID, session ID, full launch/completion payload, or response sentence text in URLs, console logs, analytics events, screenshots, or support tickets. The harness renders only allowlisted status, overall score, attempt, competency count, and timeout count.

Guided startup failures log only one stable code: `guided_content_invalid`, `guided_localization_invalid`, `guided_launch_invalid`, or `guided_scene_contract_invalid`. Recovery UI uses localized generic copy. Browser smoke artifacts contain layout/error/warning metadata and sanitized status, not full learner payloads.

## Browser and viewport boundary

The local JavaScript contract accepts messages only from the expected frame/parent and exact same origin, validates protocol/type/request ID, and does not log payloads. The three viewport exports report only CSS width, CSS height, and DPR. They cannot read or send learner, launch, content, or completion state.

Production origin, framing, CSP, retention, and API controls remain unknown until LMS discovery. Do not weaken origin checks to `*`.

## Fresh audit evidence

The exact Task 9 privacy scan returned only reviewed explanatory text, a historical Unity licensing diagnostic note, the validator's blocked `email` field name, and plan text. A broader scan found:

- zero email-like values;
- zero credential-query shapes;
- zero secret-named tracked files;
- zero free-text input controls;
- zero unexpected AWS/OpenAI/GitHub/private-key shapes;
- only the deliberate malformed bearer/JWT rejection fixtures and the acceptance prose that names them;
- only stable Bootstrap error codes/audio labels and sanitized smoke metrics at logging/writing sinks.

Every match was reviewed; no secret, private learner value, response-text log, or full launch/completion payload sink was found. Automated scans reduce accidental exposure risk but are not a privacy/security certification.

## Required human decisions

Privacy/security owners must approve purpose, lawful basis where relevant, data ownership, retention/deletion, access/audit controls, incident response, regional requirements, vendor terms, threat model, and penetration-test scope. Real LMS and production hosting behavior remain unverified.

See [LMS discovery](00-LMS-DISCOVERY.md), [contract](11-LMS-CONTRACT.md), and [asset manifest/release governance](16-ASSET-MANIFEST.md).
