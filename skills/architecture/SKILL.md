---
name: architecture
description: Guides evaluation, boundary validation, and design decisions for layered clean architecture in CalendarWidget.
---

# Architecture Skill

## Purpose
Ensure all changes adhere to Clean Architecture, unidirectional dependency rules, and domain-driven design principles.

## When to Use
- Proposing or introducing new projects or library dependencies.
- Adding new cross-layer interfaces or services.
- Refactoring core models or application boundaries.

## Prerequisites
- Read `AGENTS.md` and `docs/architecture/overview.md`.
- Verify existing dependency graph via project files.

## Workflow
1. Check source and target layers for proposed changes.
2. Confirm dependency direction: `App -> Presentation/Infrastructure -> Core`.
3. Classify each new abstraction by ownership before adding it:
   - **Core**: domain concepts and technology-agnostic domain/repository contracts.
   - **Presentation**: UI state and presentation/application interaction contracts.
   - **Infrastructure**: OS/persistence implementation details and infrastructure-private contracts.
   - **App**: composition-root concerns and concrete window ownership/lifecycle wiring.
4. Do not place UI/window-management contracts in Core merely because they are interfaces.
5. If architectural boundaries change materially, create or update an ADR in `/docs/adr/`.

## Constraints
- Never allow `CalendarWidget.Core` to reference other solution projects.
- Never allow `CalendarWidget.Presentation` to reference `CalendarWidget.Infrastructure`.
- Keep WPF/window-management concerns outside Core.
- Repository interfaces belong in Core only when they represent domain persistence contracts.

## Validation
- Run `dotnet build` to ensure compiler enforces dependency references.
- Verify no circular dependencies exist.
