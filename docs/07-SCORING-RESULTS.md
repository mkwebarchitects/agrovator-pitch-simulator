# Interpret Guided Pitch Readiness and Results

## Readiness formula

Pitch Readiness is the sum of the four current part values:

| Current mastery | Readiness contribution | Part competency score |
| --- | ---: | ---: |
| Needs Practice | 10 | 40 |
| Developing | 20 | 70 |
| Clear | 25 | 100 |

Four Clear current choices produce `100` readiness. Four Needs Practice choices produce `40`. The follow-up response does not change readiness or the four learner-facing dimensions.

Problem clarity, Evidence quality, Solution fit, and Audience value use the current Problem, Evidence, Solution, and Value mastery scores. `clear_explanation` and `communication` both use the arithmetic mean of populated part competency values, rounded away from zero at the midpoint. Results are reached only after all four parts are populated, but the pure assessment rule also handles partial/empty drafts safely.

## Diagnostic-first revision

The first choice is diagnostic. Aya provides what worked, what is missing, and how to improve. If the learner replaces a part, final readiness and competencies use the replacement. `ImprovedPartCount` recognizes only a revision whose current mastery is higher than the initial mastery. Rewording at the same level or selecting a lower level does not count as strengthened.

The earlier choice is not erased from transport history. `SelectedResponseIds` contains the chronological initial choices, any replacement, and the cost follow-up. This preserves the existing selection-history meaning without adding response sentence text.

## Completion payload

Guided completion emits exactly six competency IDs:

- `problem`
- `evidence`
- `solution`
- `audience`
- `clear_explanation`
- `communication`

Time Management is omitted because pitch construction is untimed. `OverallScore` equals Pitch Readiness. `TimeoutCount` is `0`. Completion is based on assembly and review, not a readiness threshold.

The existing `FinalConfidence` field remains in the unchanged DTO but is not shown to the learner. It starts at `50`, adds the legacy confidence delta from each of the four current part options and the follow-up option, then clamps to `0-100`. This hidden compatibility value is not Pitch Readiness and must not be renamed or given a new production meaning without formal LMS, privacy, and learning-product review.

## Results screen

Results reuses the four part icons/colours and shows the current sentence and mastery for every part, a strengthened marker where applicable, Pitch Readiness, strengthened-part count, the complete final pitch, one transfer prompt, submission status/actions, and Retry. Submission failure preserves the result for resubmission. Retry resets the attempt and mode flow.

## Interpretation limits

These values are deterministic authored formative feedback. Automated tests verify the formula, current-choice semantics, history, completion shape, and UI rendering. They do not establish a validated psychometric instrument, grade, classroom outcome, or transfer effect. Primary/Secondary educators or representative learners must review the learning experience before any effectiveness claim.

See [gameplay](03-GAMEPLAY-CONTENT.md), [privacy](12-PRIVACY-SECURITY.md), and [acceptance](18-VERTICAL-SLICE-ACCEPTANCE.md).
