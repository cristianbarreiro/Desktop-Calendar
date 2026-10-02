# Historical Sprint — Phase 13

## Sprint Objective

Complete Phase 13 (Packaging & Release) to establish reproducible, installable, versioned desktop release packages (`v1.0.0`).

---

## Status

- **Engineering Foundation (Phases 0–3)**: COMPLETE
- **Phase 4 — Application Shell**: COMPLETE
- **Phase 5 — Widget UI**: COMPLETE
- **Phase 6 — Calendar Grid & Navigation**: COMPLETE
- **Phase 7 — Persistence & SQLite Repositories**: COMPLETE
- **Phase 8 — Events Management**: COMPLETE
- **Phase 9 — Notes Management**: COMPLETE
- **Phase 10 — Settings & Appearance**: COMPLETE
- **Phase 11 — Windows Integration**: COMPLETE
- **Phase 12 — Testing, Accessibility, Performance & Hardening**: COMPLETE
- **Phase 13 — Packaging & Release**: COMPLETE

---

## Completed

- [x] **Repository Foundation (Phase 0)**: Solution setup, 6 projects, Directory.Build.props, .editorconfig, git hygiene.
- [x] **Architecture & Context (Phase 1)**: Clean Architecture boundaries, zero-outward Core, AGENTS.md canonical contract, multi-agent context files, skills suite, internal knowledge base.
- [x] **Specifications (Phase 1/3)**: Full behavioral specifications in `/specs/`.
- [x] **Repository Audit (Phase 2)**: Comprehensive read-only audit across architecture, testing, CI, and AI context.
- [x] **Audit Remediation (Phase 3)**: Fixed `.gitignore` release rule, untracked intermediate artifacts, renamed test fixtures, harmonized documentation.
- [x] **Application Shell (Phase 4)**:
  - Generic Host composition root (`Program.cs`) via `Microsoft.Extensions.Hosting`.
  - Dependency Injection configured for windows, view models, and services.
  - Windows: `MainWindow` (full application) and `WidgetWindow` (compact widget).
  - Window switching & lifecycle: `IWindowManager` / `WindowManager` (`[APP]` ↔ `[WIDGET]`) with reference cleanup and coordinated host shutdown via `ApplicationLifetimeService`.
  - Navigation: Calendar, Notes, Settings view switching with MVVM DataTemplates.
  - Calendar shell: Deterministic 42-cell calendar grid generator (`CalendarGridService`).
  - Widget shell: Month navigation, date selection with detail tray toggle, digital clock via `IClockService`, minimize and close actions.
  - Initial Design System resources (`Colors.xaml`, `Typography.xaml`, `Spacing.xaml`, `Controls.xaml`, `Theme.xaml`).
  - Test suite with automated tests covering grid, view models, window orchestration, and application shutdown lifecycle.
  - Zero build warnings/errors, clean code formatting.
- [x] **Widget UI (Phase 5 & Phase 5 Remediation)**:
  - Visual polish: distinct day cell states (normal, other month, today, hover, pressed, selected, has events).
  - Selected day multi-value converter (`IsSelectedDayConverter`) preserving immutable record architecture, with 100% unit test coverage.
  - Coexistence of `Today + Selected` visual signals via MultiDataTrigger (blue accent background + high-contrast white border + bold text).
  - High contrast for `Selected + HasEvents` via `SelectedEventDotBrush`.
  - Interactive selection: click to select and open tray, click selected day again to collapse tray.
  - Contextual keyboard navigation: arrow keys (Left/Right/Up/Down) scoped to `CalendarGrid` with automatic month/year boundary crossing, Escape to collapse from anywhere in window.
  - Focus model: day cells are focusable with visible accent border focus indicator.
  - Stale selection prevention: `SelectedDay = null`, `SelectedDayHeader = string.Empty`, and `IsExpanded = false` when date leaves visible 42-cell grid upon month navigation.
  - Clean collapsed state: detail tray leaves zero layout footprint when collapsed, with smooth Storyboard `ThicknessAnimation` for margin/padding/borders alongside height and opacity.
  - Design tokens: added `SurfacePressedBrush`, `TodayBackgroundBrush`, `TodaySelectedBorderBrush`, and `SelectedEventDotBrush`.
  - Accessibility: `AutomationProperties.Name` and tooltips on all widget buttons and day cells.
  - Comprehensive unit test suite expanded to 83 automated tests (82 unit tests + 1 integration test, 0 failures).
- [x] **Calendar Grid & Navigation (Phase 6)**:
  - Full application calendar view (`CalendarView`) and view model (`CalendarViewModel`).
  - Deterministic 42-cell calendar grid displaying current, trailing, and leading days.
  - Month navigation with robust year boundary transitions (Jan ↔ Dec).
  - Jump to today (`Today`, `GoToToday`) resetting grid and selecting current date.
  - Configurable `FirstDayOfWeek` with automatic headers and grid synchronization.
  - Selection handling via `IsSelectedDayConverter` with full coexistence of `Today + Selected`.
  - Stale selection prevention: selection cleared if date leaves 42-cell grid, preserved if still visible.
  - Contextual keyboard navigation (Left, Right, Up, Down, PageUp, PageDown, Home, End) with month/year crossing scoped to `CalendarGrid`.
  - PageUp/PageDown navigate to previous/next month preserving day-of-month selection (clamped to last valid day of target month).
  - Home/End navigate to first/last day of the currently displayed month.
  - Coherent focus model with visible keyboard focus indicators on buttons and day cells.
  - Accessibility: `AutomationProperties.Name` and `ToolTip` on all buttons and calendar day cells.
  - Expanded the Phase 6 calendar navigation test coverage to 100 tests (99 unit + 1 integration, 0 failures).
- [x] **Persistence & SQLite Repositories (Phase 7)**:
  - EF Core SQLite repository implementations (`EfCalendarEventRepository`, `EfNoteRepository`).
  - `AppDbContext` entity configuration with max-length constraints.
  - `DatabaseInitializer` running `MigrateAsync()` and enabling WAL mode via `PRAGMA journal_mode=WAL`.
  - `AppDbContextFactory` (`IDesignTimeDbContextFactory`) for `dotnet ef` tooling without WPF startup project.
  - Initial migration `20260925191413_InitialCreate` creating `CalendarEvents` and `Notes` tables with composite index.
  - DB path: `%LOCALAPPDATA%\DesktopCalendar\calendar.db`; initialized in a scoped service scope before `app.Run()`.
  - Infrastructure DI extension (`AddInfrastructure`) registering repositories and `DatabaseInitializer` as scoped.
  - Isolated SQLite test helper (`SqliteTestContext`) with `SqliteConnection.ClearAllPools()` for safe temp-file cleanup.
  - 33 integration tests: 11 event repository tests, 17 note repository tests, 5 migration/schema tests.
  - Total automated tests: 147 (114 unit + 33 integration, 0 failures).
- [x] **Events Management (Phase 8)**:
  - Domain validation (`Validate()`) on `CalendarEvent`: required non-empty title (<= 200 chars), description <= 2000 chars, `EndTime >= StartTime` (for non-all-day events).
  - Event CRUD UI: Add event button, event creation/editing modal form, delete confirmation modal.
  - Event list item model (`EventListItemModel`) with localized time display (`HH:mm` or `All day`).
  - Day detail panel: shows selected day header, empty states ("Select a day", "No events scheduled"), and chronological event cards with Edit/Delete buttons.
  - Event indicators: single query across visible 42-cell grid range populates `HasEvents` indicator on day cells.
  - Architecture integrity: presentation resolves `ICalendarEventRepository` via `IServiceScopeFactory`, zero direct EF Core / Infrastructure dependencies.
  - Test suite expanded to 201 tests (166 unit + 35 integration, 0 failures).
- [x] **Notes Management (Phase 9)**:
  - Domain validation (`Validate()`) on `Note`: required non-empty title (<= 200 chars), content <= 50,000 chars.
  - Note presentation model (`NoteListItemModel`) with culture-aware date formatting and content preview snippet.
  - Master-detail UI (`NotesView.xaml`): search box, clear action, master list of note cards, view mode, editor mode with char count, and delete confirmation modal.
  - Note CRUD and Search ViewModel (`NotesViewModel`): Create, Edit, Cancel, Save, RequestDelete, ConfirmDelete, Search, and ClearSearch commands.
  - Case-insensitive substring search matching across Title and Content via `INoteRepository.SearchAsync`.
  - Timestamp integrity: UTC persistence, local display, `CreatedAt` strictly preserved on note update.
  - Architecture integrity: presentation resolves `INoteRepository` via `IServiceScopeFactory`, zero direct EF Core / Infrastructure dependencies.
  - Test suite expanded to 235 tests (198 unit + 37 integration, 0 failures).
- [x] **Settings & Appearance (Phase 10)**:
  - Strongly-typed `UserSettings` domain entity with domain validation, cloning, and `AppThemeMode` / `TimeFormatOption` enums.
  - JSON file settings persistence in `%LocalAppData%\DesktopCalendar\settings.json` via `FileSettingsRepository` with atomic writing, corrupt backup recovery, and concurrency locking.
  - `ISettingsService` snapshot provider and event broadcaster for immediate runtime updates.
  - Dynamic runtime theme switching (Dark, Light, System) via `WpfThemeService` and `SystemThemeDetector` applying instantly across all windows and controls without application restart.
  - Early settings loading in host `Program.cs` before UI initialization, eliminating startup theme flashes.
  - Desktop widget opacity slider (50%–100%) dynamically bound to `WidgetWindow.Opacity` with persisted state.
  - Always-on-top window toggle dynamically bound to `WidgetWindow.Topmost` with persisted state.
  - Start with Windows configuration using `IWindowsStartupService` via current user registry run key (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`), requiring no administrator privileges and handling errors gracefully.
  - Calendar first day of week preference (Monday / Sunday) synchronizing calendar headers and grid dynamically.
  - Time format (12-hour AM/PM vs. 24-hour) and short date format preferences applied dynamically via `IDateTimeFormatService`.
  - Application backup export generating deterministic JSON payload (`AppBackupData`) with version, timestamp, settings, events, and notes.
  - Safe transactional backup restore with domain validation, atomic rollback, and skipping of existing duplicates to prevent accidental overwrites.
  - Factory reset workflow with destructive confirmation modal dialog, resetting all calendar events and notes while keeping database schema intact.
  - Settings UI (`SettingsView.xaml`, `SettingsViewModel.cs`) organized into Appearance, Calendar, Windows, and Data sections with accessible names, error/success banners, and keyboard navigation.
  - Test suite expanded to 318 tests (257 unit + 61 integration, 0 failures) including Phase 10.1 & Phase 10.2 import atomicity, failure recovery, and settings save serialization tests.
- [x] **Windows Integration (Phase 11)**:
  - Single-instance application enforcement via named global OS Mutex (`DesktopCalendarWidget_SingleInstance`) preventing duplicate processes from initializing.
  - Secondary-instance activation and focus handoff using local Named Pipe IPC (`SingleInstanceCoordinator`), gracefully restoring minimized windows and bringing the primary instance to foreground.
  - Dual-window lifecycle coordination (`MainWindow` ↔ `WidgetWindow`) with explicit shutdown semantics and protection against termination during window switching.
  - Window position and size persistence across restarts with asynchronous coalesced debounced writing to `%LocalAppData%\DesktopCalendar\settings.json`.
  - Multi-monitor off-screen bounds recovery (`WindowBoundsHelper`, `IDisplayMonitorProvider`, `WpfDisplayMonitorProvider`) handling negative monitor coordinates, monitor disconnections, and out-of-bounds coordinates (e.g. -5000, -5000) by restoring windows to active monitor visible areas.
  - Per-Monitor V2 DPI awareness configured cleanly via `app.manifest` and project configuration for crisp rendering on mixed-DPI displays.
  - Notification area system tray icon (`SystemTrayService`, `ITrayService`) with context menu ("Open Application", "Open Widget", "Exit"), left-click / double-click activation, clean minimize-to-tray handling, and deterministic disposal without ghost tray icons.
  - Test suite expanded to 363 tests (302 unit + 61 integration, 0 failures) with comprehensive coverage for geometry calculations, single-instance coordination, IPC signaling, window bounds debouncing, and tray service lifecycle.
- [x] **Testing, Accessibility, Performance & Hardening (Phase 12)**:
  - Single-instance & IPC hardening: safe Named Pipe server cancellation, graceful disposal order, malformed payload resilience, timeout handling, and multiple sequential activations.
  - Geometry and bounds hardening: robust handling of NaN, Infinity, negative/zero dimensions, and title-bar vertical clamping to active monitor work areas.
  - Placement persistence & exit flushing: debounced window movement flushes on window close and application exit, preventing dropped placements when exiting quickly after moving.
  - Resource cleanup: unsubscribed window LocationChanged/SizeChanged events on window close, added IDisposable to `MainWindowViewModel` to clean up child ViewModel event subscriptions, and made `SystemTrayService` disposal strictly idempotent with rollback on partial initialization failures.
  - Accessibility & UI Automation: added `AutomationProperties.Name`, `AutomationProperties.HelpText`, and `AutomationProperties.AutomationId` across all views (`MainWindow`, `WidgetWindow`, `CalendarView`, `NotesView`, `SettingsView`).
  - Added computed `AccessibleDescription` on `CalendarDayModel` providing rich date, today, and event status without relying exclusively on color.
  - Added visible high-contrast keyboard focus indicators (`IsKeyboardFocused`) to sidebar buttons, header caption buttons, and danger action buttons.
  - Added keyboard default/cancel actions (`IsDefault="True"`, `IsCancel="True"`) to modal confirmation dialogs in Calendar, Notes, and Settings.
  - Reduced-motion path implemented in `WidgetWindow` DetailTray using `SystemParameters.ClientAreaAnimation`, bypassing animations when OS animations are turned off.
  - Concurrency & lifecycle integration test suite: 74 integration tests + 326 unit tests (400 total, 0 failures), covering multi-monitor geometries, disconnected monitor fallback, restart persistence, IPC errors, and concurrent settings/placement updates.
  - Build verified with 0 warnings, 0 errors, and formatting verification passes.
- [x] **Packaging & Release (Phase 13)**:
  - Single source of truth versioning (`1.0.0`) in `Directory.Build.props` propagating to all build targets and assembly metadata.
  - Application identity harmonized: `DesktopCalendar.exe`, `Desktop Calendar`, `Desktop Calendar Contributors`, icon, copyright.
  - Runtime targeting explicitly declared for Windows `win-x64`.
  - Multi-resolution Windows application icon (`src/CalendarWidget.App/app.ico`) and manifest identity.
  - Deterministic build artifact directories (`artifacts/publish/`, `artifacts/installer/`, `artifacts/release/`).
  - Framework-dependent publish distribution (`DesktopCalendar-1.0.0-win-x64-framework-dependent.zip`).
  - Self-contained publish distribution (`DesktopCalendar-1.0.0-win-x64-self-contained.zip`).
  - Inno Setup installer (`installer/setup.iss` producing `DesktopCalendar-1.0.0-win-x64-setup.exe`).
  - User data preservation on upgrade and normal uninstall (`%LOCALAPPDATA%\DesktopCalendar\calendar.db` and `settings.json` preserved).
  - SHA-256 checksums generation (`artifacts/release/SHA256SUMS.txt`).
  - Automated packaging pipeline script (`scripts/package.ps1`).
  - Dedicated GitHub Actions release workflow (`.github/workflows/release.yml`) with automated tag and workflow_dispatch triggers.
  - Clean separation between CI and Release workflows.

---

## Next Implementation Target

- This Phase 13 sprint is complete; the existing `v1.0.0` release is unchanged.
- Current implementation, validation, release, and next-work state is maintained in `project-state.md`.

---

## Explicitly Out of Scope for Phase 13

Do NOT implement:
- Cloud synchronization, networking, telemetry (Prohibited by Architecture)
- Unrelated feature changes or architecture rewrites
