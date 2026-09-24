# CLAUDE.md — Desktop Calendar Widget

> Claude-specific context. Canonical rules are in [AGENTS.md](./AGENTS.md).

## Instructions

1. **Always read [AGENTS.md](./AGENTS.md) first** — it is the canonical contract.
2. Follow all rules, conventions, and constraints defined there.
3. This file contains only Claude-specific behavioral guidance.

## Claude Behavior

- When working on this project, prefer concise explanations over verbose ones.
- When making architectural decisions, reference or create ADRs in `/docs/adr/`.
- When uncertain about a requirement, check `/docs/product/requirements.md` before asking.
- Use the skills in `/skills/` for specialized workflows.
- Respect the dependency direction strictly — Core has zero outward dependencies.

## Context Loading

- For large tasks, load relevant `/docs/` and `/knowledge/` files incrementally.
- Do not attempt to load the entire repository into context at once.
- Prefer reading specific files over broad directory listings.

## Project Documentation Map

| Need | Read |
|------|------|
| Architecture overview | `/docs/architecture/overview.md` |
| Domain model | `/knowledge/domain.md` |
| Current status | `/docs/project-state.md` |
| UI rules | `/docs/ui/design-system.md` |
| Coding style | `AGENTS.md` → Coding Conventions |
| Testing approach | `/skills/testing/SKILL.md` |
