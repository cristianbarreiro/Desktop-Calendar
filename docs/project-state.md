# Project State

## Overview
- **Project**: Modern Desktop Calendar Widget
- **Engineering Foundation**: Complete (Phases 0–3)
- **Product Implementation**: Complete (Phases 4–13 complete)
- **Version**: 1.0.0
- **Status**: Production Release Ready
- **Current Date**: 2026-09-30

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
| **Phase 8** | Product Implementation | Events Management | **COMPLETED** |
| **Phase 9** | Product Implementation | Notes Management | **COMPLETED** |
| **Phase 10** | Product Implementation | Settings & Appearance | **COMPLETED** |
| **Phase 11** | Product Implementation | Windows OS Integration | **COMPLETED** |
| **Phase 12** | Product Implementation | Testing, Accessibility & Polish | **COMPLETED** |
| **Phase 13** | Product Implementation | Packaging & Release | **COMPLETED** |

---

## Implementation Status

### Core Domain
- [x] Initial Entities (`CalendarEvent`, `Note`, `UserSettings`)
- [x] Domain Enums (`AppThemeMode`, `TimeFormatOption`)
- [x] Domain Interfaces (`ICalendarEventRepository`, `INoteRepository`, `ISettingsRepository`, `ISettingsService`, `IWindowsStartupService`, `IDataManagementService`)
- [x] Domain Models / DTOs (`DataImportResult`, `AppBackupData`, `CalendarEventBackupDto`, `NoteBackupDto`)
- [x] Initial Value Object (`DateRange`)
- [ ] Value Objects (`TimeRange`, `ColorHex`) — Planned / Future
- [x] Domain Exception (`DomainValidationException`)
- [x] Domain validation (`CalendarEvent.Validate()`, `Note.Validate()`, `UserSettings.Validate()`)

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
- [x] Settings file persistence (`FileSettingsRepository`, `SettingsService`) — Phase 10
- [x] Windows logon startup service (`WindowsStartupService` via HKCU Run registry key) — Phase 10
- [x] Data maintenance service (`DataManagementService` for JSON export, safe atomic import, factory reset) — Phase 10
- [x] Windows Shell / Tray / Single-instance mutex — Phase 11
  - Single-instance global named OS Mutex (`DesktopCalendarWidget_SingleInstance`)
  - Secondary-instance activation and focus handoff via local Named Pipe (`SingleInstanceCoordinator`)
  - Notification area system tray icon with context menu, click-to-activate, and minimize-to-tray (`SystemTrayService`, `ITrayService`)
  - Multi-monitor off-screen bounds recovery (`WindowBoundsHelper`, `IDisplayMonitorProvider`, `WpfDisplayMonitorProvider`)
  - Asynchronous coalesced debounced window bounds persistence (`WindowPlacementService`, `IWindowPlacementService`)
  - Per-Monitor V2 DPI awareness configured via `app.manifest` and project configuration

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
  - Contextual keyboard navigation (Left, Right, Up, Down, PageUp, PageDown, Home, End) with month/year crossing scoped to `CalendarGrid`
  - PageUp/PageDown navigate to previous/next month preserving day-of-month selection (clamped to last valid day)
  - Home/End navigate to first/last day of the currently displayed month
  - Coherent focus model with visible keyboard focus indicators on buttons and day cells
  - Accessibility: `AutomationProperties.Name` and `ToolTip` on all buttons and calendar day cells
- [x] Full application events management (Phase 8):
  - Event CRUD UI integrated into `CalendarView`: Create, Edit, Delete with modal dialogs
  - Day detail panel with chronological event listing, empty state, and event actions (Edit, Delete)
  - Delete confirmation modal dialog with Cancel / Delete actions
  - Event indicators on calendar day cells dynamically loaded for visible 42-cell date range
  - Domain validation (`CalendarEvent.Validate()`): non-empty title <= 200 chars, description <= 2000 chars, `EndTime >= StartTime`
  - UTC persistence via `ICalendarEventRepository` with localized time display
  - Scoped repository resolution via `IServiceScopeFactory` adhering to layered architecture
- [x] Full application notes management (Phase 9):
  - Master-detail notes view (`NotesView.xaml`) with search box, clear action, and master note cards
  - Create note workflow: editor form, character counter, cancel, and save with persistence
  - Edit note workflow: populates editor, modifies fields, preserves `CreatedAt`, and updates `UpdatedAt`
  - Delete note workflow with modal confirmation overlay (`Delete Note`) and adjacent selection preservation
  - Domain validation (`Note.Validate()`): non-empty title <= 200 chars, content <= 50,000 chars
  - Substring search filtering across Title and Content via `INoteRepository.SearchAsync`
  - Empty states for zero notes and zero search results, plus error handling with dismiss action
  - Accessible names and tooltips on all controls, keyboard shortcuts (`Ctrl+N`, `Escape`)
  - Scoped repository resolution via `IServiceScopeFactory` adhering to layered architecture
- [x] Full application settings & appearance (Phase 10):
  - Settings view (`SettingsView.xaml`) and view model (`SettingsViewModel.cs`)
  - Runtime theme switching (Dark, Light, System) via `IThemeService` and `WpfThemeService` without application restart
  - Always-on-top window toggle updating `WidgetWindow` behavior at runtime and surviving restart
  - Start with Windows configuration using HKCU Run registry key via `IWindowsStartupService` without administrator privileges
  - Widget opacity slider (50% to 100%, 0.5–1.0) with real-time application and persistence
  - First day of week preference (Monday / Sunday) updating calendar grid dynamically
  - 12-hour / 24-hour time format and short date format preferences applied dynamically via `IDateTimeFormatService`
  - Backup export to formatted JSON (`AppBackupData`)
  - Safe transactional backup restore with domain validation, atomic rollback, and skipping of existing duplicates
  - Factory reset with modal confirmation dialog and clean database state
  - Accessible names, tooltips, success/error feedback banners, and keyboard navigation
- [x] Presentation services: `ICalendarGridService`, `CalendarGridService`, `IClockService`, `SystemClockService`, `IWindowManager`, `IThemeService`, `WpfThemeService`, `ISystemThemeDetector`, `SystemThemeDetector`, `IDateTimeFormatService`, `DateTimeFormatService`, `IFileDialogService`, `WpfFileDialogService`

### Host Application
- [x] `CalendarWidget.App` setup with `Microsoft.Extensions.Hosting`
- [x] `Program.cs` composition root with Generic Host and DI container
- [x] Single-instance mutex check prior to Host builder, preventing secondary process initialization
- [x] Early settings load and theme application in `Program.cs` before UI display to eliminate wrong-theme flash
- [x] `App.xaml` and `App.xaml.cs` configured with explicit lifecycle and tray initialization/disposal
- [x] Windows: `MainWindow` and `WidgetWindow` with `Topmost`, `Opacity`, bounds restoration, and minimize-to-tray handling
- [x] Window orchestration: `WindowManager` implementing `IWindowManager` with `ActivateCurrentWindow()`
- [x] Application lifetime service (`ApplicationLifetimeService`)
- [x] Windows Shell integration: `SingleInstanceCoordinator`, `SystemTrayService`, `WindowPlacementService`, `WpfDisplayMonitorProvider`

### Testing & QA
- [x] Unit test project configured (`xUnit` + `FluentAssertions`)
- [x] Integration test project configured (`xUnit` + `FluentAssertions` + EF Core Sqlite + WPF application lifecycle)
- [x] Comprehensive test suite covering grid calculations, view models, window orchestration, lifecycle, keyboard navigation, selection edge cases, converter logic, event domain validation, event form view model, calendar event loading/CRUD, note domain validation, note view model CRUD/search/selection, settings validation, settings view model preferences/export/import/reset, date/time formatting, file settings repository persistence and recovery, data management export/import/rollback, off-screen recovery, single-instance coordination and IPC signaling, window placement debouncing, system tray lifecycle, multi-monitor geometry, IPC error handling, malformed payloads, concurrent settings/placement updates, accessible day descriptions, and modal keyboard bindings.
- [x] 409 automated tests passing (326 unit tests, 83 integration tests, 0 failures)
- [x] Solution builds with 0 errors and 0 warnings in Debug and Release configurations
- [x] Code formatting verification passes (`dotnet format --verify-no-changes`)

### Packaging & Release (Phase 13)
- [x] Single source of truth versioning (`1.0.0`) in `Directory.Build.props` propagating to all build targets
- [x] Application identity metadata harmonized: `DesktopCalendar`, `Desktop Calendar Contributors`, icon, copyright
- [x] Target runtime explicitly declared as `win-x64`
- [x] Embedded high-resolution multi-frame application icon (`app.ico`) and manifest identity
- [x] Deterministic build artifact directories (`artifacts/publish/`, `artifacts/installer/`, `artifacts/release/`)
- [x] Framework-dependent publish distribution (`DesktopCalendar-1.0.0-win-x64-framework-dependent.zip`)
- [x] Self-contained publish distribution (`DesktopCalendar-1.0.0-win-x64-self-contained.zip`)
- [x] Inno Setup installer script (`installer/setup.iss`) producing `DesktopCalendar-1.0.0-win-x64-setup.exe`
- [x] Installer upgrade-safe and uninstall-safe user data preservation (`%LOCALAPPDATA%\DesktopCalendar\calendar.db` and `settings.json` preserved)
- [x] SHA-256 checksums generation (`artifacts/release/SHA256SUMS.txt`)
- [x] Complete automated packaging pipeline script (`scripts/package.ps1`)
- [x] Dedicated GitHub Actions release workflow (`.github/workflows/release.yml`) with automated tag and workflow_dispatch triggers
- [x] Clean separation between CI and Release workflows

---

## Known Technical Debt
- None.

---

## Current Priorities
- Phase 13 Packaging & Release is complete and verified. Application is production-ready at `v1.0.0`.
