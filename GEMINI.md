# GEMINI.md — Desktop Calendar Widget

> Gemini-specific context. Canonical rules are in [AGENTS.md](./AGENTS.md).

## Instructions

1. **Always read [AGENTS.md](./AGENTS.md) first** — it is the canonical contract.
2. Follow all rules, conventions, and constraints defined there.
3. This file contains only Gemini/Antigravity-specific guidance.

## Gemini Behavior

- Use `/skills/` directories when specialized workflows are needed.
- Respect layered architecture boundaries — check `/docs/architecture/overview.md`.
- For domain changes, read `/knowledge/domain.md` first.
- For UI changes, read `/docs/ui/design-system.md` first.
- Keep changes minimal and coherent.

## Context Loading

- Load AGENTS.md on every task.
- Load additional context only when relevant to the specific task.
- Prefer `/knowledge/index.md` as an entry point to the knowledge base.

## Project Documentation Map

| Need | Read |
|------|------|
| Architecture overview | `/docs/architecture/overview.md` |
| Domain model | `/knowledge/domain.md` |
| Current status | `/docs/project-state.md` |
| UI rules | `/docs/ui/design-system.md` |
| Coding style | `AGENTS.md` → Coding Conventions |
| Testing approach | `/skills/testing/SKILL.md` |
