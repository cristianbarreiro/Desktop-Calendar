# Project State

## Overview
- **Project**: Modern Desktop Calendar Widget
- **Engineering Foundation**: Complete (Phases 0–3)
- **Product Implementation**: In Progress (Phase 6 complete)
- **Next Phase**: Phase 8 — Events Management
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
| **Phase 5** | Product Implementation | Widget UI (Completed & Remediated) | **COMPLETED** |
| **Phase 6** | Product Implementation | Calendar Grid & Navigation | **COMPLETED** |
| **Phase 7** | Product Implementation | Persistence & SQLite Repositories | **COMPLETED** |
| **Phase 8** | Product Implementation | Events Management | **NEXT** |
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
- [ ] Value Objects (`TimeRange`, `ColorHex`) — Planned Phase 8
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
- [x] SQLite repository implementations (`EfCalendarEventRepository`, `EfNoteRepository`) — Phase 7
- [x] Database migration pipeline — Phase 7
- [ ] Windows Shell / Tray / Startup services — Planned Phase 11

### Presentation
- [x] Presentation project configured with `CommunityToolkit.Mvvm`
- [x] Foundational `ViewModelBase` created
- [x] ViewModels (`MainWindowViewModel`, `WidgetViewModel`, `CalendarViewModel`, `NotesViewModel`, `SettingsViewModel`)
- [x] XAML Views (`CalendarView`, `NotesView`, `SettingsView`)
- [x] Design System (`Colors.xaml`, `Typography.xaml`, `Spacing.xaml`, `Controls.xaml`, `Theme.xaml`)
  - Added `SurfacePressedBrush`, `TodayBackgroundBrush`, `TodaySelectedBorderBrush`, and `SelectedEventDotBrush` color tokens
  - Added visible keyboard focus indicators and pressed states to action buttons
- [x] Multi-value converter `IsSelectedDayConverter` for MVVM date selection state binding (fully unit-tested)
- [x] Widget interactive refinement & remediation (Phase 5):
  - Coexistence of `Today + Selected` visual states via MultiDataTrigger (accent fill + high-contrast white border)
  - Contrast preservation for `Selected + HasEvents` via `SelectedEventDotBrush`
  - Contextual keyboard navigation: arrow keys scoped to `CalendarGrid`, `Escape` available at window level
  - Coherent focus model on day cells with visible focus borders
  - Selection consistency on month navigation: stale selection cleared when date leaves visible 42-cell grid
  - Zero-footprint collapsed detail tray using `ThicknessAnimation` for margin, padding, and border thickness alongside height and opacity
  - Accessibility labels and tooltips on widget header and calendar controls
- [x] Full application calendar grid & navigation (Phase 6):
  - Full application calendar grid view (`CalendarView`) and view model (`CalendarViewModel`)
  - Deterministic 42-cell calendar grid displaying current, trailing, and leading days
  - Month navigation with robust year boundary transitions (Jan ↔ Dec)
  - Jump to today (`Today`, `GoToToday`) resetting grid and selecting current date
  - Configurable `FirstDayOfWeek` with automatic headers and grid synchronization
  - Selection handling via `IsSelectedDayConverter` with full coexistence of `Today + Selected`
  - Stale selection prevention: selection cleared if date leaves 42-cell grid, preserved if still visible
  - Contextual keyboard navigation (Left, Right, Up, Down) with month/year crossing scoped to `CalendarGrid`
  - Coherent focus model with visible keyboard focus indicators on buttons and day cells
  - Accessibility: `AutomationProperties.Name` and `ToolTip` on all buttons and calendar day cells
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
- [x] Comprehensive test suite covering grid calculations, view models, window orchestration, lifecycle, keyboard navigation, selection edge cases, and converter logic
- [x] 132 automated tests passing (99 unit tests, 33 integration tests, 0 failures)
- [x] Solution builds with 0 errors and 0 warnings in Debug and Release configurations
- [x] Code formatting verification passes (`dotnet format --verify-no-changes`)

---

## Known Technical Debt
- None.

---

## Current Priorities
1. **Phase 8 — Events Management**:
   - Event CRUD UI in CalendarView.
   - Event indicators on calendar day cells.
   - Day detail panel with event list.
