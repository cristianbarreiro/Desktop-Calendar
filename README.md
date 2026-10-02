# Desktop Calendar Widget

A modern, lightweight Windows desktop calendar application built with C#, .NET 10, and WPF.

## Overview

Desktop Calendar Widget provides two connected experiences:

- **Widget** — A compact, always-accessible desktop calendar for quick date checking and event viewing
- **Full Application** — A management experience for calendar events, notes, and settings

## Features (MVP)

### Calendar
- Month view with navigation
- Current date and selected date indication
- Event creation, editing, and deletion
- Event indicators on calendar days
- Day detail panel

### Notes
- Create, edit, delete notes
- List and search notes
- Timestamps and basic organization

### Settings
- Appearance (Dark / Light / System)
- Widget behavior (Always on top, Start with Windows, Opacity)
- Calendar preferences (First day of week, Date/Time format)
- Data management (Export / Import / Reset)

## Screenshots

*Coming soon — application is in early development.*

## Architecture

```
src/
├── CalendarWidget.App/            # Application host, DI, startup
├── CalendarWidget.Core/           # Domain entities, interfaces, rules
├── CalendarWidget.Infrastructure/ # EF Core, SQLite, persistence
└── CalendarWidget.Presentation/   # ViewModels, Views, XAML, themes

tests/
├── CalendarWidget.UnitTests/      # Unit tests (xUnit)
└── CalendarWidget.IntegrationTests/ # Integration tests
```

### Dependency Direction

```
App → Core, Infrastructure, Presentation
Presentation → Core
Infrastructure → Core
```

Core has zero outward dependencies.

## Technology Stack

| Component | Technology |
|-----------|-----------|
| Language | C# 14 |
| Runtime | .NET 10 |
| UI Framework | WPF (XAML) |
| MVVM | CommunityToolkit.Mvvm |
| DI | Microsoft.Extensions.DependencyInjection |
| ORM | Entity Framework Core |
| Database | SQLite |
| Testing | xUnit, FluentAssertions |

## Requirements

- Windows 10 (1809+) or Windows 11
- .NET 10 SDK

## Setup

```bash
git clone <repository-url>
cd Desktop-Calendar
dotnet restore
dotnet build
```

## Run

```bash
# Run the application
dotnet run --project src/CalendarWidget.App
```

## Test

```bash
dotnet test
```

## Format

```bash
# Check formatting
dotnet format --verify-no-changes

# Fix formatting
dotnet format
```

## Project Documentation

| Document | Description |
|----------|-------------|
| [AGENTS.md](./AGENTS.md) | AI agent contract |
| [docs/architecture/](./docs/architecture/) | Architecture documentation |
| [docs/product/](./docs/product/) | Product requirements |
| [docs/ui/](./docs/ui/) | UI/UX design system |
| [docs/adr/](./docs/adr/) | Architecture Decision Records |
| [knowledge/](./knowledge/) | Project knowledge base |

## Packaging & Distribution

```powershell
# Full clean build, tests, format check, distributions, installer, hashes, and manifest
.\scripts\build_installer.ps1 -Clean

# Regenerate distributions without compiling an installer
.\scripts\build_installer.ps1 -Clean -SkipInstaller

# Explicitly skip tests when needed (restore and build still run)
.\scripts\build_installer.ps1 -SkipTests
```

The pipeline can be invoked from any working directory. It writes publish outputs to `artifacts/publish/`, the Inno Setup output to `artifacts/installer/`, and release ZIPs, the setup executable, `SHA256SUMS.txt`, and `BUILD-MANIFEST.json` to `artifacts/release/`. The framework-dependent ZIP requires the .NET 10 Desktop Runtime; the self-contained ZIP and installer include the runtime. Inno Setup 6 must be installed for the default full build; use `-SkipInstaller` only when intentionally omitting it. Optional `-SkipFormat`, `-SkipFrameworkDependent`, `-SkipSelfContained`, and `-SkipChecksums` omit only their named stages/artifacts.

`-Clean` removes generated `bin/`, `obj/`, and pipeline artifact outputs only. Previous pipeline artifacts are also replaced on every run to prevent stale files from being mistaken for fresh results. It never removes or writes application data under `%LOCALAPPDATA%\DesktopCalendar\` (including `calendar.db` and `settings.json`); that directory is independent of build state and remains available for install/upgrade persistence checks. The pipeline intentionally does not launch the application or installer, because application startup initializes and may migrate the real user database.

## Project Status

### Engineering Foundation
Complete (Phases 0–3: Repository setup, clean architecture, audit, and remediation).

### Product Development
Complete (Phases 4–13: Feature implementation, Windows integration, accessibility, test expansion, and release packaging).

- **Phase 4 — Application Shell** (Completed)
- **Phase 5 — Widget UI** (Completed)
- **Phase 6 — Calendar Grid & Navigation** (Completed)
- **Phase 7 — Persistence** (Completed)
- **Phase 8 — Events Management** (Completed)
- **Phase 9 — Notes Management** (Completed)
- **Phase 10 — Settings** (Completed)
- **Phase 11 — Windows Integration** (Completed)
- **Phase 12 — Testing, Accessibility, Performance & Hardening** (Completed)
- **Phase 13 — Packaging & Release** (Completed)

## Roadmap

### Engineering Foundation
- [x] **Phase 0 — Repository Foundation**: Initial repository structure, solution setup, project organization, baseline tooling.
- [x] **Phase 1 — Architecture & Context**: Architecture definition, project boundaries, development conventions, AI context, documentation structure, specifications, engineering rules.
- [x] **Phase 2 — Repository Audit**: Read-only comprehensive audit of architecture, implementation, documentation, testing, CI/CD, AI context, and project consistency.
- [x] **Phase 3 — Audit Remediation**: Controlled remediation of verified audit findings without introducing unrelated product functionality.

### Product Implementation
- [x] **Phase 4 — Application Shell**: WPF application shell, host startup, DI integration, window lifecycle management, MainWindow, WidgetWindow, basic switching.
- [x] **Phase 5 — Widget UI**: Compact calendar widget, time display, month navigation, selected date, expand/collapse detail tray, event indicators, keyboard navigation, and transitions.
- [x] **Phase 6 — Calendar**: 42-cell calendar grid, date selection, current date, keyboard navigation (Arrow keys, PageUp/PageDown, Home/End), configurable first day of week.
- [x] **Phase 7 — Persistence**: EF Core SQLite integration, migrations, repositories, database initialization.
- [x] **Phase 8 — Events**: Event CRUD, validation, event indicators, day details, event persistence.
- [x] **Phase 9 — Notes**: Notes CRUD, list, timestamps, search, persistence integration.
- [x] **Phase 10 — Settings**: Theme switching (Dark/Light/System), always-on-top, Windows startup, opacity, first day of week, date/time format, export/import, factory reset.
- [x] **Phase 11 — Windows Integration**: Single-instance global mutex, secondary-instance activation, position persistence, multi-monitor off-screen recovery, Per-Monitor V2 DPI awareness, system tray integration.
- [x] **Phase 12 — Testing, Accessibility, Performance & Hardening**: Comprehensive test suite (400 tests, 0 failures), UI automation accessibility, visible focus indicators, reduced motion, debounced placement persistence.
- [x] **Phase 13 — Packaging & Release**: Production release configuration, single source of truth versioning (`1.0.0`), framework-dependent & self-contained packages, Inno Setup Windows installer, upgrade/uninstall data safety, SHA-256 checksums, and GitHub Actions release workflow.

## Contributing

1. Read [AGENTS.md](./AGENTS.md) before making changes
2. Follow Conventional Commits
3. Ensure all tests pass before submitting changes
4. Update documentation when architecture or behavior changes

## License

MIT
