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
cd "Desktop Calendar"
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

## Roadmap

- [x] Phase 0: Repository + AI context infrastructure
- [x] Phase 1: Solution + projects + dependency boundaries
- [ ] Phase 2: Application shell + window management
- [ ] Phase 3: Widget UI
- [ ] Phase 4: Calendar domain
- [ ] Phase 5: SQLite persistence
- [ ] Phase 6: Events
- [ ] Phase 7: Notes
- [ ] Phase 8: Settings
- [ ] Phase 9: Windows integration
- [ ] Phase 10: Testing + polish
- [ ] Phase 11: Packaging + release

## Contributing

1. Read [AGENTS.md](./AGENTS.md) before making changes
2. Follow Conventional Commits
3. Ensure all tests pass before submitting changes
4. Update documentation when architecture or behavior changes

## License

MIT
