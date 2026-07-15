# Guided Pitch Builder Design

**Date:** 2026-07-15

**Status:** Approved

**Repository boundary:** Implement only in the pitch-simulator worktree. The AGROVATOR LMS repository is read-only and must not be modified.

## Purpose

The simulator will move from primarily testing recognition through multiple-choice dialogue to teaching learners how to assemble a clear pitch. Success means that a learner can build a four-part pitch for a team project:

1. Problem
2. Evidence
3. Solution
4. Value

For younger learners, the same framework is reinforced with the plain-language prompts **Spot it, Prove it, Solve it, Show why it matters**.

The primary audience is Primary and Secondary school students in the AGROVATOR programme. The simulator is individual practice that prepares each learner to contribute to their team's real pitch.

## Core Learning Flow

1. **Briefing**
   - Judge Aya introduces the Smart School Garden project.
   - The learner chooses Primary or Secondary mode when no reviewed LMS category is available.
   - The learner is told that the activity will help them build a pitch for their team.

2. **Learn**
   - A short visual example shows an incomplete pitch.
   - Aya introduces Problem, Evidence, Solution, and Value.
   - Each part receives a persistent icon and colour used throughout the experience.

3. **Build**
   - The learner completes four focused rounds, one per pitch part.
   - Aya asks a focused question and presents three respectful sentence cards.
   - The chosen sentence is added visibly to the learner's Pitch Board.
   - Immediate feedback names what worked, what is missing, and how to improve.

4. **Improve**
   - The four selected cards appear together.
   - Developing and Needs Practice sections are labelled constructively as opportunities to strengthen the pitch.
   - The learner may replace weak cards after reading a focused hint.

5. **Present**
   - The four cards combine into one readable final pitch.
   - Aya asks one follow-up question about cost, feasibility, or audience needs to preserve adaptability practice.

6. **Reflect**
   - Results show whether each part is Clear, Developing, or Needs Practice.
   - One concrete transfer prompt tells the learner how to use the framework in their team's real pitch.

Pitch construction is untimed. The target duration is approximately 8–10 minutes.

## Primary and Secondary Modes

Both modes use the same mechanics, framework, and visual system. They differ in reading level and expected reasoning, not in a childish versus advanced theme.

### Primary

- Short, concrete sentences with one idea per card.
- Familiar school-based observations and examples.
- Larger visual cues and direct prompts.
- Typical sentence-card target of roughly 12–16 words.
- Evidence may be a realistic observation rather than formal data.
- Feedback uses direct coaching, such as: "Good problem. Now add something you saw or measured."

### Secondary

- More precise explanation and audience reasoning.
- Clear distinction between observations, assumptions, and unsupported claims.
- Simple measurements, trial results, qualified claims, and honest uncertainty.
- Concise spoken language, with moderately longer cards where needed.
- Feedback explains why a statement is or is not credible and relevant.

The AGROVATOR category named `Elementary` maps conceptually to the learner-facing `Primary` mode. Results and comparisons remain within existing programme categories rather than comparing Primary and Secondary learners directly.

## Feedback and Assessment

First choices are diagnostic rather than permanent mistakes. After each selection, Aya explains:

1. What worked.
2. What is missing.
3. How to improve it.

The final result primarily assesses the revised pitch and explicitly recognizes improvement. Completion requires assembling and reviewing all four parts, not reaching a high score.

The learner-facing dimensions are:

- Problem clarity
- Evidence quality
- Solution fit
- Audience value

Existing Clear Explanation and Communication categories contribute across the four dimensions. Time Management does not affect the untimed construction exercise.

The current Confidence meter is replaced in the learner-facing experience by **Pitch Readiness**. It represents the completeness and support of the assembled pitch, not a measurement of the learner's personal confidence. Aya reacts to the pitch statement and never judges the learner.

## Visual Direction

The pixel-art garden remains, but the UI must render crisply and must not be browser-stretched. The WebGL canvas should render at its displayed resolution, account for device pixel ratio, and use controlled letterboxing when the viewport does not match the intended aspect ratio. Pixel-art textures retain point filtering while text and controls render at full UI resolution.

At the 1280x720 reference, gameplay uses a centred 960–1000 pixel panel with an opaque deep-navy surface. The garden remains visible without competing with the lesson.

The panel contains:

- A compact four-part progress rail.
- Aya and an integrated dark dialogue card with light text and a gold speaker accent.
- A four-slot Pitch Board that fills as choices are made.
- Three contained cream sentence cards with clear hover, focus, selected, and disabled states.
- A responsive stacked arrangement on constrained viewports.

The current floating cream prompt strip is removed. Cream is reserved for selectable pitch cards so it has a consistent meaning. Score and Confidence bars are absent during construction; the four Pitch Board slots communicate readiness directly. Results reuse the same four-part visual language.

## Technical Design and Boundary

All implementation remains inside the pitch-simulator worktree.

A simulator-local learner-mode value controls Primary or Secondary content. The current `LmsLaunchConfig` does not contain learner category, so automatic LMS mapping is not part of this work. The learner selects the mode during Briefing until a future reviewed contract adds an appropriate field.

An engine-independent Pitch Draft model stores the four sections. Each section records its initial response ID, current response ID, revision state, and mastery state. Unity presenters render the model; assembly, scoring, and revision rules remain testable without scenes.

Existing scenario and response IDs are preserved when their semantics remain unchanged. New age-specific content receives new stable IDs and a content-version increment.

The completion boundary remains compatible with the existing payload: selected response IDs, overall score, and competency scores continue through the current fields. The legacy `FinalConfidence` field is not renamed or assigned a new production contract meaning without formal LMS, privacy, and learning-product review.

No learner free text, audio recording, AI scoring, personal details, or new LMS transport is introduced.

## Error Handling

- Missing learner mode routes to the explicit mode selector.
- Missing or invalid content blocks the attempt with a localized, non-sensitive recovery message.
- Incomplete drafts cannot enter Present until all four parts are populated.
- Retry clears the draft, revision history, score state, and local learner-mode flow as defined by the final implementation plan.
- Submission failure preserves the completed result for resubmission, matching the existing session contract.

## Verification

- EditMode tests for four-part assembly, revision, scoring, reset, and both learner modes.
- Content tests for every route, localization key, stable response ID, and reading-length limit.
- PlayMode tests for mode selection, learning flow, building, revising, presenting, Results, and Retry.
- Generated layout tests at the 1280x720 reference and constrained viewport sizes.
- WebGL checks for device-pixel sharpness, controlled aspect handling, keyboard navigation, focus visibility, and text truncation.
- Chrome and Edge smoke coverage equivalent to the existing vertical-slice gate.
- Human review by Primary and Secondary educators or representative learners before any claim of pedagogical effectiveness.

## Non-Goals

- Free-form pitch writing or generative-AI assessment.
- Voice recording, speech recognition, or automated delivery scoring.
- Multiplayer or synchronous team construction inside the simulator.
- Changes to the AGROVATOR LMS repository or its production contract.
- Claims that automated tests prove classroom learning effectiveness.
