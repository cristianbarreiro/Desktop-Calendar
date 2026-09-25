# Current Sprint

## Sprint Objective

Transition from Phase 7 (Persistence & SQLite Repositories) into Phase 8 (Events Management).

---

## Status

- **Engineering Foundation (Phases 0–3)**: COMPLETE
- **Phase 4 — Application Shell**: COMPLETE
- **Phase 5 — Widget UI**: COMPLETE
- **Phase 6 — Calendar Grid & Navigation**: COMPLETE
- **Phase 7 — Persistence & SQLite Repositories**: COMPLETE
- **Next Target — Phase 8 (Events Management)**: READY TO START

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
  - Contextual keyboard navigation (Left, Right, Up, Down) with month/year crossing scoped to `CalendarGrid`.
  - Coherent focus model with visible keyboard focus indicators on buttons and day cells.
  - Accessibility: `AutomationProperties.Name` and `ToolTip` on all buttons and calendar day cells.
  - Comprehensive test suite expanded to 100 automated tests (99 unit tests + 1 integration test, 0 failures).
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
  - Total automated tests: 132 (99 unit + 33 integration, 0 failures).

---

## Next Implementation Target

### Phase 8 — Events Management

- Event CRUD UI in `CalendarView` (create, edit, delete dialogs or inline forms).
- Event indicators on calendar day cells.
- Day detail panel with event list.
- Validation rules (`EndTime >= StartTime`, mandatory title).
- Event persistence via `ICalendarEventRepository`.

---

## Explicitly Out of Scope for Phase 8

Do NOT implement during Phase 8:
- Notes editor UI (Planned Phase 9)
- Settings persistence UI (Planned Phase 10)
- System tray icon docking (Planned Phase 11)
- Windows startup registration (Planned Phase 11)
- Packaging, installers, and release automation (Planned Phase 13)
