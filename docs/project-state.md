# Project State

## Overview
- **Project**: Modern Desktop Calendar Widget
- **Engineering Foundation**: Complete (Phases 0–3)
- **Product Implementation**: In Progress (Phase 5 complete)
- **Next Phase**: Phase 6 — Calendar Grid & Navigation
- **Current Date**: 2026-09-25

---

## Phase Status Summary

| Phase | Category | Description | Status |
|---|---|---|---|
| **Phase 0** | Engineering Foundation | Repository Foundation & Tooling | **COMPLETED** |
| **Phase 1** | Engineering Foundation | Architecture, Context & Boundaries | **COMPLETED** |
| **Phase 2** | Engineering Foundation | Read-Only Repository Audit | **COMPLETED** |
| **Phase 3** | Engineering Foundation | Controlled Audit Remediation | **COMPLETED** |
| **Phase 4** | Product Implementation | Application Shell & Window Management | **COMPLETED** |
| **Phase 5** | Product Implementation | Widget UI | **COMPLETED** |
| **Phase 6** | Product Implementation | Calendar Grid & Navigation | **NEXT** |
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
- [x] Design System (`Colors.xaml`, `Typography.xaml`, `Spacing.xaml`, `Controls.xaml`, `Theme.xaml`)
  - Added `SurfacePressedBrush` and `TodayBackgroundBrush` color tokens
  - Added visible keyboard focus indicators and pressed states to action buttons
- [x] Multi-value converter `IsSelectedDayConverter` for MVVM date selection state binding
- [x] Widget interactive refinement:
  - Day selection interaction model with toggle expand/collapse behavior
  - Keyboard navigation (Left/Right/Up/Down arrow keys) with month/year boundary crossing
  - Escape shortcut for tray collapse
  - Storyboard-driven expand/collapse animation for detail tray (height and opacity transitions)
  - Accessibility labels and tooltips on widget header and calendar controls
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
- [x] Comprehensive test suite covering grid calculations, view models, window orchestration, lifecycle, keyboard navigation, and selection edge cases
- [x] 73 automated tests passing (72 unit tests, 1 integration test, 0 failures)
- [x] Solution builds with 0 errors and 0 warnings in Debug and Release configurations
- [x] Code formatting verification passes (`dotnet format --verify-no-changes`)

---

## Known Technical Debt
- None.

---

## Current Priorities
1. **Phase 6 — Calendar Grid & Navigation**:
   - Comprehensive full-application calendar grid view (`CalendarView`).
   - Month/week navigation and view modes.
   - Configurable first day of week.
   - Selected date sync across views.
