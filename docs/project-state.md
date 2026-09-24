# Project State

## Overview
- **Project**: Modern Desktop Calendar Widget
- **Engineering Foundation**: Complete (Phases 0–3)
- **Product Implementation**: In Progress (Phase 4 complete)
- **Next Phase**: Phase 5 — Widget UI
- **Current Date**: 2026-09-24

---

## Phase Status Summary

| Phase | Category | Description | Status |
|---|---|---|---|
| **Phase 0** | Engineering Foundation | Repository Foundation & Tooling | **COMPLETED** |
| **Phase 1** | Engineering Foundation | Architecture, Context & Boundaries | **COMPLETED** |
| **Phase 2** | Engineering Foundation | Read-Only Repository Audit | **COMPLETED** |
| **Phase 3** | Engineering Foundation | Controlled Audit Remediation | **COMPLETED** |
| **Phase 4** | Product Implementation | Application Shell & Window Management | **COMPLETED** |
| **Phase 5** | Product Implementation | Widget UI | **NEXT** |
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
- [ ] Value Objects (`TimeRange`, `ColorHex`) — Planned Phase 6/8
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
- [x] Presentation project configured with `CommunityToolkit.Mvvm`
- [x] Foundational `ViewModelBase` created
- [x] ViewModels (`MainWindowViewModel`, `WidgetViewModel`, `CalendarViewModel`, `NotesViewModel`, `SettingsViewModel`)
- [x] XAML Views (`CalendarView`, `NotesView`, `SettingsView`)
- [x] Initial Design System (`Colors.xaml`, `Typography.xaml`, `Spacing.xaml`, `Controls.xaml`, `Theme.xaml`)
- [x] Presentation services: `ICalendarGridService`, `CalendarGridService`, `IClockService`, `SystemClockService`, `IWindowManager`

### Host Application
- [x] `CalendarWidget.App` setup with `Microsoft.Extensions.Hosting`
- [x] `Program.cs` composition root with Generic Host and DI container
- [x] `App.xaml` and `App.xaml.cs` configured with explicit lifecycle
- [x] Windows: `MainWindow` and `WidgetWindow`
- [x] Window orchestration: `WindowManager` implementing `IWindowManager`
- [x] Application lifetime service (`ApplicationLifetimeService`)

### Testing & QA
- [x] Unit test project configured (`xUnit` + `FluentAssertions`)
- [x] Integration test project configured (`xUnit` + `FluentAssertions` + EF Core InMemory/Sqlite)
- [x] Comprehensive test suite covering grid calculations, view models, and window orchestration
- [x] 40 automated tests passing (39 unit tests, 1 integration test, 0 failures)
- [x] Solution builds with 0 errors and 0 warnings
- [x] Code formatting verification passes (`dotnet format --verify-no-changes`)

---

## Known Technical Debt
- None.

---

## Current Priorities
1. **Phase 5 — Widget UI**:
   - Deepen widget interaction model and smooth expand/collapse transitions.
   - Refine compact day cell typography, states, and hit targets.
   - Connect live events indicator display when Phase 8 is reached.
