# Claude Working Agreement

Follow `AGENTS.md` and the approved design and implementation plan under `docs/`.

- Make changes test-first and retain RED/GREEN verification evidence.
- Run focused tests and all relevant Unity tests before claiming completion.
- Inspect test XML and scan Unity logs for compilation failures and unhandled exceptions.
- Update `TASKS.md` with evidence and the next unchecked action before committing.
- Do not access or modify the AGROVATOR LMS repository from this project.
- Do not expose or commit tokens, secrets, private learner data, raw LMS payloads, names, emails, or school identifiers.
- Prefer pure-C# domain code, thin Unity presentation bridges, explicit dependencies, and one future bootstrap composition root. Do not add unnecessary frameworks or packages.
