# Current Sprint

## Sprint Objective

Transition from Phase 5 (Widget UI) into Phase 6 (Calendar Grid & Navigation) to implement full application calendar grid capabilities.

---

## Status

- **Engineering Foundation (Phases 0–3)**: COMPLETE
- **Phase 4 — Application Shell**: COMPLETE
- **Phase 5 — Widget UI**: COMPLETE
- **Next Target — Phase 6 (Calendar Grid & Navigation)**: READY TO START

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
- [x] **Widget UI (Phase 5)**:
  - Visual polish: distinct day cell states (normal, other month, today, hover, pressed, selected, has events).
  - Selected day multi-value converter (`IsSelectedDayConverter`) preserving immutable record architecture.
  - Interactive selection: click to select and open tray, click selected day again to collapse tray.
  - Keyboard navigation: arrow keys (Left/Right/Up/Down) with automatic month/year boundary crossing, Escape to collapse.
  - Smooth animation: XAML Storyboard vertical expand/collapse and opacity transitions for day detail tray.
  - Design tokens: added `SurfacePressedBrush` and `TodayBackgroundBrush` color tokens, keyboard focus indicators on action buttons.
  - Accessibility: `AutomationProperties.Name` and tooltips on all widget buttons and day cells.
  - Comprehensive unit test suite: 72 unit tests + 1 integration test (73 total passing).

---

## Next Implementation Target

### Phase 6 — Calendar Grid & Navigation

- Full application calendar grid view (`CalendarView`).
- Navigation controls (previous/next month, jump to today, year selection).
- Multi-view presentation (month view, week view preparation).
- Configurable first day of week.
- Selected date synchronization.

---

## Explicitly Out of Scope for Phase 6

Do NOT implement during Phase 6:
- SQLite database CRUD operations (Planned Phase 7)
- EF Core migrations (Planned Phase 7)
- Event persistence and validation forms (Planned Phase 8)
- Notes persistence and text editor (Planned Phase 9)
- Settings persistence (Planned Phase 10)
- System tray icon docking (Planned Phase 11)
- Windows startup registration (Planned Phase 11)
- Packaging, installers, and release automation (Planned Phase 13)
