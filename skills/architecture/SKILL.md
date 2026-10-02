---
name: architecture
description: Guides evaluation, boundary validation, and design decisions for layered clean architecture in CalendarWidget.
---

# Architecture Decision Aid

Use when a change crosses projects, adds an abstraction, or affects persistence or UI ownership. Load only the relevant architecture document and project references.

## Decide

- Put domain rules and technology-neutral repository contracts in Core.
- Put view state and UI interaction in Presentation; keep it independent of Infrastructure.
- Put EF Core, files, and OS integrations in Infrastructure.
- Keep startup, dependency wiring, and concrete window ownership in App.
- Add or revise an ADR when a durable boundary or ownership decision changes; routine implementation details do not need one.

## Check

- Trace references toward Core; Core must not depend on outer projects or WPF.
- Prefer an existing owner and pattern over a new layer or service.
- Build affected projects and test behavior at the layer that owns it.
