---
name: documentation
description: Standards and guidelines for keeping project documentation synchronized with code changes.
---

# Documentation Decision Aid

Use when behavior, ownership, requirements, project status, or agent context changes. Update the source that owns the fact; avoid copying detailed rules into indexes, skills, or agent adapters.

## Choose the Source

- Requirements and user flows: `docs/product/`.
- Architecture and durable trade-offs: `docs/architecture/` and `docs/adr/`.
- UI behavior and accessibility: `docs/ui/`.
- Domain rules and shared concepts: `knowledge/`.
- Implementation, validation, release, and next work: `docs/project-state.md`.
- Reusable failure evidence and uncertainty: `knowledge/failure-memory.md`.
- Dated implementation history: `docs/development-log.md`.
- Context routing: `AGENTS.md`; task workflows: `skills/`; durable curated knowledge/OKF: `knowledge/`.

## Context Maintenance

- Keep `AGENTS.md` a short router and contract; place procedures in skills and durable facts in knowledge.
- A skill should state when to use it, what to inspect, which decisions/invariants matter, what evidence is needed, and when to stop. Treat it as adaptable guidance, not a command checklist.
- Keep project state split into implementation, validation, release, and next work. Do not equate implemented with tested or locally packaged with released.
- Add failure memory only for reusable incidents. Separate observed evidence, reproduction, hypothesis, confirmed cause, fix, validation, and remaining uncertainty; do not archive logs or conversations.
- Use the existing knowledge index as the OKF entry point. Do not create a parallel knowledge system or copy full source documents into it.

Update indexes or adapter files only when links or routing change. Check links, dates, counts, commit references, and status claims against repository, test, and CI evidence.
