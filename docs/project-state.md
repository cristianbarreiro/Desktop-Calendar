# Project State

## Overview
- **Project**: Modern Desktop Calendar Widget
- **Engineering Foundation**: Complete (Phases 0–3)
- **Product Implementation**: Not started
- **Next Phase**: Phase 4 — Application Shell
- **Current Date**: 2026-09-24

---

## Phase Status Summary

| Phase | Category | Description | Status |
|---|---|---|---|
| **Phase 0** | Engineering Foundation | Repository Foundation & Tooling | **COMPLETED** |
| **Phase 1** | Engineering Foundation | Architecture, Context & Boundaries | **COMPLETED** |
| **Phase 2** | Engineering Foundation | Read-Only Repository Audit | **COMPLETED** |
| **Phase 3** | Engineering Foundation | Controlled Audit Remediation | **COMPLETED** |
| **Phase 4** | Product Implementation | Application Shell & Window Management | **NEXT** |
| **Phase 5** | Product Implementation | Widget UI | **PLANNED** |
| **Phase 6** | Product Implementation | Calendar Grid & Navigation | **PLANNED** |
| **Phase 7** | Product Implementation | Persistence & SQLite Repositories | **PLANNED** |
| **Phase 8** | Product Implementation | Events Management | **PLANNED** |
| **Phase 9** | Product Implementation | Notes Management | **PLANNED** |
| **Phase 10** | Product Implementation | Settings & Appearance | **PLANNED** |
| **Phase 11** | Product Implementation | Windows OS Integration | **PLANNED** |
| **Phase 12** | Product Implementation | Testing, Accessibility & Polish | **PLANNED** |
| **Phase 13** | Product Implementation | Packaging & Distribution | **PLANNED** |

---

## Implementation Status

### Core Domain
- [x] Initial Entities (`CalendarEvent`, `Note`)
- [x] Initial Interfaces (`ICalendarEventRepository`, `INoteRepository`)
- [x] Initial Value Object (`DateRange`)
- [ ] Value Objects (`TimeRange`, `ColorHex`)
- [x] Domain Exception (`DomainValidationException`)

### Specifications & AI Context
- [x] Full Specification Suite (`specs/calendar/`, `specs/widget/`, `specs/notes/`, `specs/settings/`, `specs/windows/`)
- [x] Canonical Agent Contract (`AGENTS.md`)
- [x] 8 Reusable Agent Skills (`skills/`) with Git tracking verified
- [x] Cursor (`.cursor/rules/`) & GitHub Copilot (`.github/instructions/`) rule suite
- [x] 6 Reusable task prompts (`.github/prompts/`)
- [x] Structured internal knowledge base (`knowledge/`)

### Infrastructure
- [x] EF Core `AppDbContext` configured with entity models
- [x] Infrastructure DI extension method (`AddInfrastructure`)
- [ ] SQLite repository implementations (`EfCalendarEventRepository`, `EfNoteRepository`) — Planned Phase 7
- [ ] Database migration pipeline — Planned Phase 7
- [ ] Windows Shell / Tray / Startup services — Planned Phase 11

### Presentation
- [x] Presentation project created with `CommunityToolkit.Mvvm`
- [x] Foundational `ViewModelBase` created
- [ ] ViewModels (`WidgetViewModel`, `CalendarViewModel`, `NotesViewModel`, `SettingsViewModel`) — Planned Phase 4+
- [ ] XAML Views (`WidgetWindow`, `MainWindow`) — Planned Phase 4+
- [ ] Design System styles, colors, and controls — Planned Phase 4+

### Host Application
- [x] `CalendarWidget.App` setup with `Microsoft.Extensions.Hosting`
- [ ] Host service builder and window lifecycle management — Planned Phase 4

### Testing & QA
- [x] Unit test project configured (`xUnit` + `FluentAssertions`)
- [x] Integration test project configured (`xUnit` + `FluentAssertions` + EF Core InMemory/Sqlite)
- [x] Renamed tests matching class fixtures (`CalendarEventTests.cs`, `AppDbContextTests.cs`)
- [x] 5 automated tests passing (0 failures)
- [x] Solution builds with 0 errors and 0 warnings

---

## Known Technical Debt
- None. Intermediate build files removed, `.gitignore` anchored, and specifications layer established.

---

## Current Priorities
1. **Phase 4 — Application Shell**:
   - Implement `Program.cs` host composition root (`Microsoft.Extensions.Hosting`).
   - Wire dependency injection for windows, view models, and application services.
   - Implement `MainWindow` and `WidgetWindow` shell containers with navigation placeholders.
   - Implement basic `IWindowManager` service for switching between Widget and Main Application modes.
