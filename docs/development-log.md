# Development Log

## 2026-09-24 — Repository Initialization (Phase 0 & 1)

### Activities
- Inspected repository workspace (clean git state).
- Established multi-agent context files (`AGENTS.md`, `CLAUDE.md`, `GEMINI.md`, `.cursor/rules/`, `.github/`).
- Scaffolded .NET 10 solution `CalendarWidget.slnx` with 6 projects:
  - `CalendarWidget.App` (WPF Executable)
  - `CalendarWidget.Core` (Class library)
  - `CalendarWidget.Infrastructure` (Class library)
  - `CalendarWidget.Presentation` (WPF Class library)
  - `CalendarWidget.UnitTests` (xUnit)
  - `CalendarWidget.IntegrationTests` (xUnit)
- Configured project dependencies enforcing unidirectional Clean Architecture.
- Added foundational packages: `CommunityToolkit.Mvvm`, `Microsoft.Extensions.Hosting`, `Microsoft.EntityFrameworkCore.Sqlite`, `FluentAssertions`.
- Added `.editorconfig`, `Directory.Build.props`, and test-specific rule overrides.
- Implemented initial domain entities (`CalendarEvent`, `Note`) and repositories (`ICalendarEventRepository`, `INoteRepository`).
- Configured `AppDbContext` and initial passing unit/integration smoke tests.
- Authored product vision, requirements, user flows, architecture documents, UI design system, ADR-001, and knowledge base.

## 2026-09-24 — Phase 3: Controlled Remediation

### Activities
- Unanchored `.gitignore` pattern `[Rr]elease*/` corrected and `!skills/**` added, ensuring `skills/release/SKILL.md` is tracked in git.
- Untracked temporary MSBuild artifact `src/CalendarWidget.App/CalendarWidget.App_krlchx0l_wpftmp.csproj` and ignored via `*_wpftmp.csproj`.
- Renamed test files `UnitTest1.cs` to match class fixtures (`Core/CalendarEventTests.cs`, `Persistence/AppDbContextTests.cs`).
- Established complete `/specs/` hierarchy with 5 behavioral specifications (`calendar`, `widget`, `notes`, `settings`, `windows`).
- Harmonized `docs/architecture/application-structure.md` by annotating planned product implementation components.
- Validated solution build, test suite execution (5/5 passed), and formatting verification.

## 2026-09-24 — Phase Roadmap Normalization

### Activities
- Established Canonical Project Roadmap distinguishing Engineering Foundation (Phases 0–3) from Product Implementation (Phases 4–13).
- Updated `README.md`, `knowledge/roadmap.md`, `docs/project-state.md`, `docs/current-sprint.md`, and `docs/product/feature-map.md`.
- Normalized phase numbers across architecture documentation to designate Phase 4 (Application Shell) as the immediate next target.
- Verified global consistency across all agent instructions, rules, and documentation files.

## 2026-09-24 — Phase 4: Application Shell

### Activities
- Implemented Generic Host composition root in `CalendarWidget.App/Program.cs` (`Microsoft.Extensions.Hosting`).
- Wired Dependency Injection in `Program.cs` for views, view models, lifecycle services, and window orchestration.
- Created `IWindowManager` in `CalendarWidget.Presentation.Services` and concrete `WindowManager` in `CalendarWidget.App.Services` (per ADR-002).
- Implemented `MainWindow.xaml` (full application) and `WidgetWindow.xaml` (compact desktop widget) with bidirectional window switching (`[APP]` ↔ `[WIDGET]`).
- Implemented sidebar navigation with `NavigationTab` switching between `CalendarView`, `NotesView`, and `SettingsView` using MVVM `ContentControl` DataTemplates.
- Implemented deterministic 42-cell calendar grid generator (`CalendarGridService`) and unit tests covering month navigation, leading/trailing days, leap years, and first-day-of-week settings.
- Implemented `IClockService` and `SystemClockService` powering real-time digital clock display in the widget.
- Created initial Fluent dark Design System resource dictionaries (`Colors.xaml`, `Typography.xaml`, `Spacing.xaml`, `Controls.xaml`, `Theme.xaml`).
- Expanded automated test suite from 5 to 40 passing tests (39 unit tests, 1 integration test).
- Validated solution build (0 errors, 0 warnings), test execution (40/40 passed), and code formatting (`dotnet format --verify-no-changes`).

## 2026-09-24 — Phase 4.1: Application Shell Lifecycle & Tests Remediation

### Activities
- Connected explicit application shutdown path via `ApplicationLifetimeService` with thread-safe `_isShuttingDown` guard, Dispatcher deadlock prevention (`CheckAccess`), and Generic Host termination (`StopApplication`).
- Established `IManagedWindow` abstraction in `CalendarWidget.App.Services` implemented by `MainWindow` and `WidgetWindow`, enabling deterministic, headless lifecycle and orchestration unit testing.
- Updated `WindowManager` to coordinate with `ApplicationLifetimeService`: handles `Closed` events, nulls closed/disposed references, allows recreation, distinguishes window switching and minimization from application close, and triggers host shutdown on real window close.
- Added `[✕]` close button to `WidgetWindow.xaml` alongside `[─]` minimize, providing distinct controls for window switching (`[APP]`), minimization (`[─]`), and full application shutdown (`[✕]`).
- Updated `App.xaml.cs` to inject `ApplicationLifetimeService` and delegate `OnExit` to ensure host termination.
- Expanded automated test suite from 40 to 54 passing tests (53 unit tests, 1 integration test), adding full coverage for `ApplicationLifetimeService` and 12 state/lifecycle test scenarios in `WindowManagerTests`.
- Harmonized documentation in `docs/product/feature-map.md`, `docs/architecture/application-structure.md`, `docs/project-state.md`, and `docs/current-sprint.md`.
- Validated solution build (Debug & Release), test execution (54/54 passed in 299ms), and code formatting (`dotnet format --verify-no-changes`).



## 2026-09-24 — Phase 5: Widget UI & Remediation

### Activities
- Implemented visual polish for widget day cells: distinct states (normal, other-month, today, hover, pressed, selected, has-events).
- Implemented `IsSelectedDayConverter` (multi-value converter) preserving immutable record architecture; 100% unit test coverage.
- Coexistence of `Today + Selected` visual signals via MultiDataTrigger (accent fill + high-contrast white border + bold text).
- High-contrast `Selected + HasEvents` via `SelectedEventDotBrush` design token.
- Interactive selection: click to select/open tray, click again to collapse.
- Contextual keyboard navigation: arrow keys scoped to `CalendarGrid` with automatic month/year boundary crossing; Escape collapses from anywhere.
- Focus model: day cells focusable with visible accent border focus indicator.
- Stale selection prevention on month navigation.
- Zero-footprint collapsed detail tray via `ThicknessAnimation` Storyboard.
- Design tokens added: `SurfacePressedBrush`, `TodayBackgroundBrush`, `TodaySelectedBorderBrush`, `SelectedEventDotBrush`.
- Accessibility: `AutomationProperties.Name` and tooltips on all widget buttons and day cells.
- Expanded automated test suite to 83 tests (82 unit + 1 integration, 0 failures).
- Validated solution build (0 errors, 0 warnings), test execution, and code formatting.

## 2026-09-25 — Phase 6: Calendar Grid & Navigation

### Activities
- Implemented full application calendar view (`CalendarView.xaml`) and view model (`CalendarViewModel`).
- Deterministic 42-cell calendar grid displaying current, trailing, and leading days.
- Month navigation with robust year boundary transitions (Jan ↔ Dec).
- Jump to today (`Today` / `GoToToday`) resetting grid and selecting current date.
- Configurable `FirstDayOfWeek` with automatic header and grid synchronization.
- Selection handling via `IsSelectedDayConverter` with full coexistence of `Today + Selected`.
- Stale selection prevention: selection cleared if date leaves 42-cell grid, preserved if still visible.
- Contextual keyboard navigation (Left, Right, Up, Down, PageUp, PageDown, Home, End) with month/year crossing scoped to `CalendarGrid`.
- PageUp/PageDown navigate to previous/next month preserving day-of-month selection (clamped to last valid day of target month).
- Home/End navigate to first/last day of the currently displayed month.
- Coherent focus model with visible keyboard focus indicators on buttons and day cells.
- Accessibility: `AutomationProperties.Name` and `ToolTip` on all buttons and calendar day cells.
- Expanded automated test suite to 100 tests (99 unit + 1 integration, 0 failures).
- Validated solution build (Debug & Release, 0 errors, 0 warnings), test execution, and code formatting.

## 2026-09-25 — Phase 7: Persistence & SQLite Repositories

### Activities
- Added `Microsoft.EntityFrameworkCore.Design` (v10.0.12, PrivateAssets=all) to Infrastructure project.
- Updated `AppDbContext` Note entity config: `Content.HasMaxLength(50_000)`; added XML doc comments.
- Implemented `EfCalendarEventRepository`: `AsNoTracking` reads, overlap semantics (`StartTime < end AND EndTime > start`), ordered by `StartTime`.
- Implemented `EfNoteRepository`: `AsNoTracking` reads, `EF.Functions.Like` for case-insensitive search, ordered by `CreatedAt`.
- Implemented `DatabaseInitializer`: runs `MigrateAsync()` then `PRAGMA journal_mode=WAL` via direct `SqliteConnection`; uses `LoggerMessage.Define` (CA1848).
- Implemented `AppDbContextFactory` (`IDesignTimeDbContextFactory<AppDbContext>`) for `dotnet ef` tooling without WPF startup project.
- Generated migration `20260925191413_InitialCreate`: `CalendarEvents` and `Notes` tables with all constraints; composite index `IX_CalendarEvents_StartTime_EndTime`.
- Updated `DependencyInjection.cs`: registered `EfCalendarEventRepository`, `EfNoteRepository`, and `DatabaseInitializer` as scoped.
- Updated `Program.cs`: DB path `%LOCALAPPDATA%\DesktopCalendar\calendar.db`; calls `AddInfrastructure(connectionString)`; runs `DatabaseInitializer.InitializeAsync()` in a scoped service scope before `app.Run()`.
- Implemented `SqliteTestContext` test helper: isolated SQLite DB per test using migrations; `SqliteConnection.ClearAllPools()` before file deletion.
- Added 33 integration tests: 11 event repository tests (CRUD + 7 date-range overlap scenarios), 17 note repository tests (CRUD + 7 search scenarios), 5 migration/schema tests.
- Total automated tests at Phase 7 completion: 132 (99 unit + 33 integration, 0 failures); subsequently expanded to 147 (114 unit + 33 integration) by Phase 6 keyboard navigation remediation.
- Validated solution build (Debug & Release, 0 errors, 0 warnings), test execution (132/132 passed), and code formatting (`dotnet format --verify-no-changes`).
- Committed as `feat: phase 7` (SHA `992faba`).

## 2026-09-29 — Phase 8: Events Management

### Activities
- Enhanced domain entity `CalendarEvent` with `Validate()` enforcing non-empty title (<= 200 chars), description <= 2000 chars, and `EndTime >= StartTime` (for non-all-day events).
- Added `Microsoft.Extensions.DependencyInjection.Abstractions` to `CalendarWidget.Presentation` to resolve `ICalendarEventRepository` via `IServiceScopeFactory`, respecting layered architecture without direct EF Core/Infrastructure references.
- Added `InternalsVisibleTo` in `CalendarWidget.Presentation` for unit test assemblies.
- Implemented `EventListItemModel` presentation record `(Guid Id, string Title, string TimeLabel, string? Description, bool IsAllDay)` with UTC-to-local conversion.
- Implemented `EventFormViewModel` supporting create and edit workflows, reactive `FormTitle` and `IsEditing`, date/time picking, all-day toggle, inline validation errors, and `IsValid()` check.
- Extended `CalendarViewModel` with event CRUD commands (`OpenAddEventCommand`, `OpenEditEventCommand`, `SaveEventCommand`, `CancelEventFormCommand`, `RequestDeleteEventCommand`, `ConfirmDeleteEventCommand`, `CancelDeleteEventCommand`).
- Implemented 42-cell visible date range querying (`RefreshGridWithEventsAsync`) populating `HasEvents` day cell indicator in a single repository query.
- Implemented day selection event loading (`LoadSelectedDayEventsAsync`) with chronological ordering and empty state signaling (`HasSelectedDayEvents`, `HasNoSelectedDayEvents`, `HasSelectedDay`).
- Enhanced `CalendarView.xaml` with real Day Detail panel (add event button, empty states, scrollable event cards with Edit/Delete buttons) and modal overlay dialogs for Event Form and Delete Confirmation.
- Added comprehensive unit tests:
  - `CalendarEventValidationTests`: 11 tests verifying domain validation rules.
  - `EventFormViewModelTests`: 16 tests covering form initialization, validation, all-day toggle, edit population, and property notifications.
  - `CalendarViewModelEventTests`: 27 tests covering event loading, visible grid range indicators, midnight boundaries, multi-day overlap, CRUD actions, and dialog state transitions.
- Added integration tests in `CalendarEventRepositoryTests` verifying `Validate()` behavior on persistence and preservation of `CreatedAt` with updated `UpdatedAt`.
- Total automated tests expanded from 147 to 201 (166 unit + 35 integration, 0 failures).
- Validated solution build (0 errors, 0 warnings), test execution (201/201 passed), and code formatting (`dotnet format --verify-no-changes`).
- Committed as `feat: phase 8` (SHA `5ca3eed27fe225980dda747a2b6d96f4b3553fa0`).

## 2026-09-29 — Phase 9: Notes Management

### Activities
- Enhanced domain entity `Note` with `Validate()` enforcing non-empty title (<= 200 chars) and content length <= 50,000 chars.
- Implemented `NoteListItemModel` presentation record with culture-aware date formatting and single-line content preview snippet.
- Implemented `NotesViewModel` managing note listing, CRUD workflows, inline/domain validation error display, character counting, delete confirmation modal, substring search filtering, and selection transitions.
- Designed and implemented modern master-detail `NotesView.xaml` with search box, responsive master list, view/editor panels, delete confirmation modal overlay, accessible names, and keyboard shortcuts (`Ctrl+N`, `Escape`).
- Updated `TestScopeFactory` and added `TestNoteRepository` in unit test fakes.
- Added 32 unit tests:
  - `NoteValidationTests`: 8 tests covering domain validation rules.
  - `NotesViewModelTests`: 24 tests covering empty state, listing, create, edit, cancel, validation, persistence, delete modal, adjacent selection maintenance, search filtering, search+edit/delete dynamics, and error handling.
- Added 2 integration tests in `NoteRepositoryTests` validating `Note.Validate()` persistence and preservation of `CreatedAt` during `UpdateAsync`.
- Total automated tests expanded from 201 to 235 (198 unit + 37 integration, 0 failures).
- Validated solution build (0 errors, 0 warnings), test execution (235/235 passed), and code formatting (`dotnet format --verify-no-changes`).
- Committed as `feat: phase 9` (SHA `a660270280cff819470277b342f16a37c6087327`).

## 2026-09-30 — Phase 10: Settings & Appearance

### Activities
- Created strongly-typed domain model `UserSettings` with domain validation invariants, cloning support, and enums `AppThemeMode` (Dark, Light, System) and `TimeFormatOption` (24-hour, 12-hour).
- Implemented `ISettingsRepository` and `FileSettingsRepository` persisting settings to `%LocalAppData%\DesktopCalendar\settings.json` with thread synchronization (`SemaphoreSlim`), atomic write semantics via temporary files, fallback defaults, and automatic corrupted-file backup recovery.
- Implemented `ISettingsService` and `SettingsService` maintaining an in-memory snapshot and broadcasting runtime changes via `SettingsChanged`.
- Implemented `IWindowsStartupService` and `WindowsStartupService` managing Windows user logon startup configuration via `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, operating without administrator privileges and failing gracefully.
- Implemented `IDataManagementService` and `DataManagementService` executing:
  - Backup export to formatted JSON (`AppBackupData`) with versioning, timestamp, settings, events, and notes.
  - Safe transactional backup restore with domain validation, atomic rollback, and skipping of duplicate IDs without overwriting existing data.
  - Factory reset clearing all user calendar events and notes while preserving database schema and broadcasting `DataChanged`.
- Implemented presentation services:
  - `IThemeService` and `WpfThemeService` managing dynamic runtime theme resource dictionary replacement (`Themes/Dark.xaml`, `Themes/Light.xaml`) without application restart.
  - `ISystemThemeDetector` and `SystemThemeDetector` reading Windows personalize registry settings (`AppsUseLightTheme`).
  - `IDateTimeFormatService` and `DateTimeFormatService` providing unified clock, event range, and date formatting based on user preferences.
  - `IFileDialogService` and `WpfFileDialogService` providing testable abstractions over Windows Open and Save file dialogs.
- Created Fluent light and dark theme dictionaries (`Resources/Themes/Dark.xaml`, `Resources/Themes/Light.xaml`) ensuring high contrast, WCAG AA/AAA compliance, and consistent tokens.
- Implemented `SettingsViewModel` managing:
  - Theme selection (Dark / Light / System) with immediate visual application and persistence.
  - Always-on-top widget toggle dynamically bound to `WidgetWindow.Topmost`.
  - Desktop widget opacity slider (50%–100%) dynamically bound to `WidgetWindow.Opacity`.
  - Windows startup toggle with graceful error handling and auto-reversion on failure.
  - First day of week and 12/24-hour time format and short date format preferences.
  - Export, Import, and Reset data maintenance commands with feedback banners and confirmation modal overlay.
- Designed accessible `SettingsView.xaml` with categorized cards (Appearance, Calendar, Windows, Data), accessible names, and keyboard navigation.
- Updated `CalendarViewModel`, `NotesViewModel`, and `WidgetViewModel` to integrate with `ISettingsService`, `IDataManagementService`, and `IDateTimeFormatService`.
- Enhanced host composition root `Program.cs` to load user settings and apply visual theme before UI launch, eliminating startup theme flashes.
- Added comprehensive unit and integration tests:
  - `UserSettingsTests`: 10 unit tests for domain invariants, validation rules, boundaries, and cloning.
  - `DateTimeFormatServiceTests`: 8 unit tests covering 12/24-hour, custom date patterns, event ranges, and change notification.
  - `SettingsViewModelTests`: 22 unit tests covering all properties, runtime theme switching, opacity clamping, startup error handling, export/import/reset workflows, and banners.
  - `FileSettingsRepositoryTests`: 4 integration tests covering defaults, roundtrip persistence, validation rejection, and corrupted backup recovery.
  - `DataManagementServiceIntegrationTests`: 6 integration tests against isolated SQLite databases covering JSON export, transactional import, duplicate skipping, atomic rollback on validation failure, malformed JSON recovery, and factory reset.
- Total automated tests expanded from 235 to 301 (254 unit + 47 integration, 0 failures).
- Validated solution build (0 errors, 0 warnings in Debug and Release), test execution (301/301 passed in Debug and Release), and formatting (`dotnet format --verify-no-changes`).

## 2026-09-30 — Phase 10.1: Remediation

### Activities
- **Import Atomicity Correction (`DataManagementService`)**:
  - Implemented upfront pre-mutation validation of the entire backup payload: all settings, calendar events, and notes are validated before modifying persistent storage or opening database transactions.
  - Wrapped database mutations and settings synchronization in an explicit SQLite transaction (`BeginTransactionAsync`).
  - Added full rollback handling: if database save, settings persistence, or transaction commit fails, rolls back SQLite transaction, restores original settings snapshot, and reports failure without leaving partially imported records.
- **Settings Save Ordering Correction (`SettingsViewModel` & `SettingsService`)**:
  - In `SettingsViewModel`, coalesced rapid consecutive user input using cumulative state and a sequential asynchronous worker loop (`ProcessPendingSavesAsync`) under lock, ensuring intermediate changes are aggregated and the latest requested state always wins without blocking the UI thread.
  - In `SettingsService`, implemented monotonic version tracking (`Interlocked.Increment`) and lock synchronization (`SemaphoreSlim`), discarding stale out-of-order snapshots so an earlier save cannot overwrite newer state.
  - Handled save failures gracefully with observable error messaging (`ErrorMessage`) without crashing the WPF UI thread, automatically clearing the error when subsequent valid saves succeed.
  - Added `WaitForPendingSavesAsync` to `SettingsViewModel` and test synchronization hooks to eliminate test race conditions.
- **Automated Test Suite Expansion**:
  - Added 5 integration tests in `DataManagementServiceIntegrationTests`:
    - `ImportDataJsonAsync_InvalidSettings_LeavesEventsNotesAndSettingsUnchanged`
    - `ImportDataJsonAsync_InvalidEvent_PersistsNothing`
    - `ImportDataJsonAsync_InvalidNote_PersistsNothing`
    - `ImportDataJsonAsync_ValidBackup_RestoresAllDataSuccessfully`
    - `ImportDataJsonAsync_PersistenceFailure_DoesNotLeavePartialImportedState`
  - Added 3 integration/concurrency tests in `SettingsServiceOrderingTests`:
    - `SaveSettingsAsync_RapidSequentialSaves_PersistsLatestSettings`
    - `SaveSettingsAsync_OutOfOrderCompletion_DropsStaleSnapshot`
    - `SaveSettingsAsync_WhenRepositoryThrows_PreservesStateAndSubsequentSaveSucceeds`
  - Added 3 unit tests in `SettingsViewModelTests`:
    - `RapidSequentialChanges_CoalescesAndPersistsLatestState`
    - `OutOfOrderCompletion_ControlledByFake_AlwaysPersistsLatestState`
    - `SaveFailure_DoesNotCrashUI_AndAllowsNewerValidStateToBePersisted`
  - Total automated tests expanded from 301 to 312 (257 unit + 55 integration, 0 failures).
- **Validation**:
  - Build: 0 errors, 0 warnings.
  - Test suite: 312/312 passed.
  - Formatting: `dotnet format --verify-no-changes` passed.
  - CI workflow verification succeeded (run 36665461368). Phase 11 remains NEXT.

## 2026-09-30 — Phase 10.2: Import/Restore Atomicity Remediation

### Activities
- **Import/Restore Failure Recovery Hardening (`DataManagementService`)**:
  - Refined transaction and compensation lifecycle across SQLite and Settings persistence boundaries.
  - Implemented uncancelled token (`CancellationToken.None`) execution for rollback and settings compensation, guaranteeing mandatory cleanup completes even if the import operation is cancelled.
  - Added explicit detection and handling for database rollback failures (`LogRollbackFailed`) and settings restoration failures (`LogSettingsRestoreFailed`), returning distinguished diagnostic error messages and logging incomplete recovery states without falsely claiming successful recovery.
  - Ensured `DataChanged` is strictly not raised on any failure path (validation, persistence, commit, rollback, compensation, or cancellation).
- **Test Infrastructure & Interceptor Seams**:
  - Implemented `TestSaveChangesInterceptor` and `TestTransactionInterceptor` using EF Core `IInterceptor` diagnostics to deterministically simulate database persistence errors, transaction commit failures, and rollback failures against real SQLite instances.
  - Enhanced `StubSettingsService` with `ThrowOnRestore` to simulate compensation failure scenarios.
  - Added 6 integration tests in `DataManagementServiceIntegrationTests`:
    - `ImportDataJsonAsync_DatabasePersistenceFailure_RollsBackAndLeavesStateUnchanged`
    - `ImportDataJsonAsync_CommitFailure_RollsBackDatabaseAndRestoresSettings`
    - `ImportDataJsonAsync_RollbackFailure_DetectsFailureAndReportsDiagnosticMessage`
    - `ImportDataJsonAsync_SettingsRestorationFailure_DetectsFailureAndReportsDiagnosticMessage`
    - `ImportDataJsonAsync_WhenCancelled_CleanlyRollsBackAndRestoresSettings`
    - `ImportDataJsonAsync_SuccessfulImport_RaisesDataChangedExactlyOnce`
  - Total automated tests expanded from 312 to 318 (257 unit + 61 integration, 0 failures).
- **Validation**:
  - Build: 0 errors, 0 warnings.
  - Test suite: 318/318 passed.
  - Formatting: `dotnet format --verify-no-changes` passed.
  - Phase 10 stabilized; Phase 11 remains NEXT.

## 2026-09-30 — Phase 11: Windows Integration

### Activities
- **Single-Instance Enforcement & Secondary-Instance Activation (`SingleInstanceCoordinator`)**:
  - Enforced single-instance application execution using named global OS Mutex (`DesktopCalendarWidget_SingleInstance`) in `Program.cs` before Generic Host construction, preventing secondary processes from initializing duplicate state.
  - Implemented asynchronous local Named Pipe IPC (`DesktopCalendarWidget_SingleInstance_Pipe`) over same-user security boundary.
  - Secondary instance detects existing instance, transmits `"ACTIVATE"` signal via named pipe client, and exits immediately.
  - Primary instance runs an asynchronous server loop listening for secondary launches, and dispatches `ActivateCurrentWindow()` on the WPF UI dispatcher, restoring minimized windows and bringing the primary window to the foreground.
- **Dual-Window Lifecycle Coordination (`WindowManager`)**:
  - Refined window switching and lifecycle between `MainWindow` and `WidgetWindow`.
  - Added `ActivateCurrentWindow()` activating the active window mode or defaulting to widget mode.
  - Ensured switching windows does not trigger application shutdown (`_isSwitching` guard flag).
  - Maintained single authoritative shutdown path through `ApplicationLifetimeService.Shutdown()`.
- **Window Position & Size Persistence (`WindowPlacementService`)**:
  - Extended `UserSettings` entity with `MainWindowLeft`, `MainWindowTop`, `MainWindowWidth`, `MainWindowHeight`, `WidgetWindowLeft`, `WidgetWindowTop`, and `MinimizeToTray`.
  - Implemented domain validation for bounds: minimum dimensions (>= 200px), finite values, rejecting NaN/Infinity.
  - Implemented asynchronous coalesced debounced saving of window moves and resizes to `%LocalAppData%\DesktopCalendar\settings.json`, preventing excessive disk I/O during window dragging.
  - Added `FlushPendingSaveAsync()` ensuring bounds are flushed to disk before window close or shutdown.
- **Multi-Monitor Display Awareness & Off-Screen Recovery (`WindowBoundsHelper`, `WpfDisplayMonitorProvider`)**:
  - Created pure geometry abstractions `DisplayArea` and `WindowBounds`.
  - Implemented `WindowBoundsHelper.EnsureVisible()` calculating display intersection against all active monitor working areas.
  - Supported negative monitor coordinates on mixed multi-monitor layouts without accidental repositioning.
  - Handled disconnected monitors and out-of-bounds coordinates (e.g. `-5000, -5000`) by relocating windows safely to the primary monitor center.
- **Per-Monitor V2 DPI Awareness (`app.manifest`)**:
  - Created application manifest `app.manifest` configuring `<dpiAwareness>PerMonitorV2</dpiAwareness>` and Windows 10/11 compatibility (`supportedOS`).
  - Configured project properties `<ApplicationManifest>app.manifest</ApplicationManifest>` and `<ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>`.
  - Ensured crisp rendering and correct hit-testing on mixed-DPI displays with 0 compiler warnings.
- **System Tray Integration (`SystemTrayService`, `ITrayService`)**:
  - Implemented shell notification area integration using `NotifyIcon` with a custom calendar icon.
  - Added context menu: "Open Application", "Open Widget", separator, and "Exit".
  - Implemented minimize-to-tray: minimizing hides window while keeping process alive under `ShutdownMode.OnExplicitShutdown`.
  - Single-click and double-click restore/activate the preferred window.
  - Coordinated clean shutdown from tray and deterministic disposal on exit, preventing ghost tray icons.
- **Automated Test Suite Expansion**:
  - Added `WindowBoundsHelperTests`: 10 unit tests for single monitor, multi-monitor with negative coordinates, disconnected monitor recovery, title bar clamping, and display centering.
  - Added `UserSettingsBoundsTests`: 5 unit tests verifying bounds validation, invalid dimensions, non-finite coordinates, and faithful cloning.
  - Added `SingleInstanceCoordinatorTests`: 4 unit tests covering mutex acquisition, second-instance rejection, Named Pipe IPC activation signaling, and mutex release on disposal.
  - Added `WindowPlacementServiceTests`: 6 unit tests covering default display centering, saved bounds restoration, off-screen recovery, rapid drag-resize debouncing, and immediate flush.
  - Added `SystemTrayServiceTests`: 3 unit tests verifying initialization, visibility, and safe disposal.
  - Added 5 unit tests to `WindowManagerTests` covering `ActivateCurrentWindow()` for visible, minimized, and hidden states, plus bounds application hooks.
  - Total automated tests expanded from 318 to 363 (302 unit + 61 integration, 0 failures).
- **Validation**:
  - Build: 0 errors, 0 warnings (Debug & Release).
  - Test suite: 363/363 passed (Debug & Release).
  - Formatting: `dotnet format --verify-no-changes` passed.
  - Git diff check: passed.
  - Phase 11 complete; Phase 12 is NEXT.
