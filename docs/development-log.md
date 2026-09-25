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
- Contextual keyboard navigation (Left, Right, Up, Down) with month/year crossing scoped to `CalendarGrid`.
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
- Total automated tests: 132 (99 unit + 33 integration, 0 failures).
- Validated solution build (Debug & Release, 0 errors, 0 warnings), test execution (132/132 passed), and code formatting (`dotnet format --verify-no-changes`).
- Committed as `feat: phase 7` (SHA `992faba`).
