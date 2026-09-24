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
- Refactoring core models or data flow boundaries.

## Prerequisites
- Read `AGENTS.md` and `docs/architecture/overview.md`.
- Verify existing dependency graph via project files.

## Workflow
1. Check source and target layers for proposed changes.
2. Confirm dependency direction: `App -> Presentation/Infrastructure -> Core`.
3. If new abstractions are needed, verify they live in `CalendarWidget.Core.Interfaces`.
4. Ensure no UI/WPF types leak into `Core` or `Infrastructure`.
5. If architectural boundaries change materially, create an ADR in `docs/adr/`.

## Constraints
- Never allow `CalendarWidget.Core` to reference other solution projects.
- Never allow `CalendarWidget.Presentation` to reference `CalendarWidget.Infrastructure`.

## Validation
- Run `dotnet build` to ensure compiler enforces dependency references.
- Verify no circular dependencies exist.
