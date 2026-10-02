---
name: documentation
description: Standards and guidelines for keeping project documentation synchronized with code changes.
---

# Documentation Decision Aid

Use when behavior, ownership, requirements, or project status changes. Update the source that owns the fact; avoid copying detailed rules into indexes or agent adapters.

## Choose the Source

- Requirements and user flows: `docs/product/`.
- Architecture and durable trade-offs: `docs/architecture/` and `docs/adr/`.
- UI behavior and accessibility: `docs/ui/`.
- Domain rules and shared concepts: `knowledge/`.
- Implementation, validation, release, and next work: `docs/project-state.md`.
- Dated history: `docs/development-log.md`.

Update indexes or adapter files only when links or routing change. Check links, dates, counts, and status claims against repository and test evidence.
