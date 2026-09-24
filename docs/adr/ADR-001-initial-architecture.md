# ADR-001: Initial Architecture and Technology Stack

## Status

Accepted

## Context

The Desktop Calendar Widget is a greenfield Windows desktop utility requiring:
1. Fast startup and low memory usage.
2. Two connected experiences: a compact desktop widget and a full calendar/notes management application.
3. Clean, testable architecture friendly to multi-agent AI development.
4. Local-first, private data storage.

## Decision

1. **Framework & Language**: .NET 10 with C# 14 and WPF (XAML).
2. **Architecture**: 4-tier Layered / Clean Architecture:
   - `CalendarWidget.Core`: Pure domain logic, zero external dependencies.
   - `CalendarWidget.Infrastructure`: EF Core, SQLite, Windows OS integrations.
   - `CalendarWidget.Presentation`: MVVM via `CommunityToolkit.Mvvm`, XAML views and custom controls.
   - `CalendarWidget.App`: Composition root, host lifecycle (`Microsoft.Extensions.Hosting`).
3. **Database**: SQLite via `Microsoft.EntityFrameworkCore.Sqlite`.
4. **Testing**: `xUnit` with `FluentAssertions` across unit and integration test assemblies.
5. **AI Development Infrastructure**: Canonical contract in `AGENTS.md`, progressive disclosure docs, Cursor rules, GitHub Copilot instructions, and standardized skills.

## Consequences

### Positive
- Strict dependency rules prevent UI/database coupling.
- Pure domain layer simplifies automated testing without mocks for UI or EF Core.
- Canonical agent contracts prevent AI hallucination and inconsistent coding styles.
- SQLite provides zero-configuration local-first reliability.

### Negative / Trade-offs
- WPF limits cross-platform support strictly to Windows (acceptable as Windows is the explicit target platform).
- Requires clear boundary enforcement so developers don't inadvertently bleed WPF types into Core.

## Alternatives Considered
- **WinUI 3 / Windows App SDK**: Modern, but heavier runtime footprint, complex deployment dependencies, and higher fragility compared to mature WPF on .NET 10.
- **Avalonia / .NET MAUI**: Cross-platform benefits are unnecessary given Windows-first focus; WPF offers native Windows integration with minimal overhead.
- **LiteDB**: Embedded NoSQL; rejected in favor of SQLite due to standard SQL migration support in EF Core.

## Date

2026-09-24
