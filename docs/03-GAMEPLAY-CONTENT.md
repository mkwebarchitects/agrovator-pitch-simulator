# Maintain the Smart School Garden Experience

## Confirmed learning design

The scenario asks learners to explain a problem, connect the solution to an audience, use evidence, communicate uncertainty honestly, and summarize value. The authored graph is `Assets/Content/Scenarios/smart-school-garden.en.json`. Its opening `tutorial` node leads to `introduction`; an unsupported claim sets `weak_claim_made` and routes through `audience-recovery` and `evidence-recovery`. Correcting the record can set `recovered_after_weak_answer`.

Responses carry stable IDs, localization keys, a quality tier, score/confidence deltas, competency tags, a reaction cue, feedback/explanation keys, next node, and required/blocked/set flags. Runtime identity comparisons are ordinal and case-sensitive.

## Content rules

- Keep IDs stable once released because completion payloads store selected response IDs.
- Keep scores within validator bounds and confidence within 0-100 after accumulation.
- Every destination must exist; every nonterminal path must reach the terminal node.
- Preserve three or fewer visible choices for the current UI pool.
- Pair each learner-facing key with reviewed English. Malay parity is mechanical, but translation approval is human work.
- Avoid trick wording and answer-length cues. The three quality tiers should not be inferable from placement or verbosity.

## Validation

`ScenarioJsonLoader`, `ScenarioValidator`, `SmartSchoolGardenContentTests`, and `ScenarioAssetTests` check parsing, graph structure, key coverage, path behavior, and Unity import. Do not treat authored duration or pedagogical intent as measured learning efficacy.

See [content authoring](04-CONTENT-AUTHORING.md), [scoring](07-SCORING-RESULTS.md), and [QA](13-QA-PLAN.md).
