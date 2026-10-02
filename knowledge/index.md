# Knowledge Base Index

Welcome to the internal project knowledge base for the Modern Desktop Calendar Widget.

This folder is the repository's curated knowledge base/OKF for durable domain and engineering facts. It complements task workflows in `skills/` and current status in `docs/project-state.md`; it is not a second archive of project documentation.

## Loading Guidance

Start with `AGENTS.md`, then open only the topic needed. `docs/project-state.md` owns current status; `failure-memory.md` owns reusable incident evidence; this index routes to durable topics. Skills provide task-specific decisions, not duplicate facts or mandatory command sequences.

## Knowledge Topics

| Document | Description |
|----------|-------------|
| [architecture.md](./architecture.md) | High-level architectural patterns, layers, and dependency invariants |
| [constraints.md](./constraints.md) | Durable technical, data-safety, and security boundaries |
| [domain.md](./domain.md) | Domain models, event boundaries, notes, and business rules |
| [failure-memory.md](./failure-memory.md) | Evidence, confirmed causes, fixes, validation, and uncertainty for reusable incidents |
| [ui.md](./ui.md) | User experience patterns, widget ergonomics, and design tokens |
| [decisions.md](./decisions.md) | Summary of key technical and architectural decisions |
| [conventions.md](./conventions.md) | C#, XAML, and testing conventions |
| [glossary.md](./glossary.md) | Domain terminology and concepts |
| [roadmap.md](./roadmap.md) | Stable project phase scope; not a live work queue |

## Source Ownership

- Code and tests own implementation behavior; `docs/architecture/` owns detailed system and persistence design.
- `docs/project-state.md` owns implementation, validation, release, and next-work snapshots; `README.md` owns user-facing build/packaging instructions.
- `scripts/build_installer.ps1` is the build entry point; `scripts/package.ps1` owns packaging implementation. `skills/release/` routes release decisions without copying command details.
- Skills explain how to investigate or change an area; they point to the facts rather than restating them.
- Add knowledge only when a fact is durable and helps a future decision. Do not store logs, full conversations, temporary commands, or status snapshots here.
