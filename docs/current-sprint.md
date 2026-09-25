# Current Sprint

## Sprint Objective

Transition from Phase 4 (Application Shell) into Phase 5 (Widget UI) to refine the compact desktop widget experience.

---

## Status

- **Engineering Foundation (Phases 0–3)**: COMPLETE
- **Phase 4 — Application Shell**: COMPLETE
- **Next Target — Phase 5 (Widget UI)**: READY TO START

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
  - Test suite expanded to 54 passing automated tests (53 unit, 1 integration) covering grid, view models, window orchestration, and application shutdown lifecycle.
  - Zero build warnings/errors, clean code formatting.

---

## Next Implementation Target

### Phase 5 — Widget UI

- Refine compact widget typography, styling, and transitions.
- Day selection interaction model and smooth vertical tray expand/collapse animation.
- Weekday headers and month navigation styling polish.
- Keyboard navigation (arrow keys) inside the widget calendar grid.

---

## Explicitly Out of Scope for Phase 5

Do NOT implement during Widget UI:
- SQLite database CRUD operations (Planned Phase 7)
- EF Core migrations (Planned Phase 7)
- Event persistence and validation forms (Planned Phase 8)
- Notes persistence and text editor (Planned Phase 9)
- Settings persistence (Planned Phase 10)
- System tray icon docking (Planned Phase 11)
- Windows startup registration (Planned Phase 11)
- Packaging, installers, and release automation (Planned Phase 13)
