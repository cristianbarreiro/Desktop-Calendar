# Agent Contract — Desktop Calendar

Read this router before changing the repository. Load only the linked source needed for the task; do not copy its details here.

## Repository Identity

- Windows desktop calendar: C# / .NET 10, WPF, MVVM, SQLite/EF Core.
- `src/CalendarWidget.Core`: domain and contracts.
- `src/CalendarWidget.Infrastructure`: persistence and operating-system integrations.
- `src/CalendarWidget.Presentation`: views, view models, and presentation services.
- `src/CalendarWidget.App`: startup, dependency wiring, and application lifecycle.
- `tests/CalendarWidget.UnitTests` and `tests/CalendarWidget.IntegrationTests`: behavioral and integration coverage.

Dependency direction: `Presentation → Core`; `Infrastructure → Core`; `App → Core, Infrastructure, Presentation`; tests may reference all projects. Core remains technology- and UI-independent; Presentation must not depend on Infrastructure; persistence stays behind domain contracts.

## Working Rules

- Preserve layer ownership and user data. Do not change production behavior to accommodate a test-harness failure.
- Prefer deterministic regression tests for behavior and concurrency; coordinate with explicit gates instead of sleeps or scheduler timing.
- Do not hide failures by skipping tests, weakening assertions, retrying blindly, or disabling parallel execution.
- Keep changes scoped, use existing dependencies, and distinguish observations from hypotheses and confirmed causes.
- Do not treat a successful local build as a published release. Do not change release metadata without an explicit request.

## Validation

Choose validation proportional to the change using [`skills/testing/SKILL.md`](skills/testing/SKILL.md). Project test counts and the latest verified implementation, validation, release, and next-work states belong in [`docs/project-state.md`](docs/project-state.md), not in this file.

## Context Router

| Need | Load |
|---|---|
| Layer boundaries | [`knowledge/architecture.md`](knowledge/architecture.md), [`docs/architecture/`](docs/architecture/), [`skills/architecture/SKILL.md`](skills/architecture/SKILL.md) |
| Product requirements | [`docs/product/`](docs/product/) |
| Current implementation / validation / release / next work | [`docs/project-state.md`](docs/project-state.md) |
| Historical incidents and confidence | [`knowledge/failure-memory.md`](knowledge/failure-memory.md), [`skills/failure-investigation/SKILL.md`](skills/failure-investigation/SKILL.md) |
| Test selection, regressions, hang diagnosis | [`skills/testing/SKILL.md`](skills/testing/SKILL.md) |
| Persistence / concurrency invariants | [`docs/architecture/persistence.md`](docs/architecture/persistence.md), [`skills/database/SKILL.md`](skills/database/SKILL.md) |
| WPF behavior or test STA lifecycle | [`docs/architecture/windows-integration.md`](docs/architecture/windows-integration.md), [`skills/wpf-ui/SKILL.md`](skills/wpf-ui/SKILL.md) |
| Durable domain and engineering knowledge | [`knowledge/index.md`](knowledge/index.md); load only the relevant topic |
| Data and security boundaries | [`knowledge/constraints.md`](knowledge/constraints.md) |
| C# and XAML conventions | [`knowledge/conventions.md`](knowledge/conventions.md) |
| Packaging and release process | [`skills/release/SKILL.md`](skills/release/SKILL.md), [`README.md`](README.md), `scripts/build_installer.ps1` |
| Updating docs, AGENTS, skills, or knowledge | [`skills/documentation/SKILL.md`](skills/documentation/SKILL.md) |

Skills are workflows and decision aids, not mandatory command sequences. Code, tests, and the designated source documents remain authoritative for implementation facts.
