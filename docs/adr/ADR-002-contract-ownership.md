# ADR-002: Ownership of Application and UI Integration Contracts

## Status

Accepted

## Context

The project uses Clean Architecture with a technology-agnostic Core. Product Phase 4 introduces application-shell concerns such as window switching and lifecycle coordination.

Not every interface belongs in Core. In particular, a window-management abstraction describes application/UI orchestration rather than a domain rule.

## Decision

Contracts are owned by the layer that defines the concept:

- **Core**: domain concepts and technology-agnostic domain persistence contracts.
- **Presentation**: UI state and presentation interaction contracts.
- **Infrastructure**: persistence/OS implementation details and infrastructure-facing contracts.
- **App**: composition-root wiring and concrete application/window lifecycle ownership.

`IWindowManager` and similar window orchestration contracts MUST NOT be placed in `CalendarWidget.Core`.

## Consequences

- Core remains independent from WPF and application lifecycle concepts.
- Presentation can depend on a window-management abstraction without depending on Infrastructure.
- App remains the composition root for concrete window instances and lifecycle coordination.
- Agents must classify an abstraction by responsibility rather than by the fact that it is an interface.

## Date

2026-09-24
