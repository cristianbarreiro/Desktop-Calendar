# Application Structure

## Target Project Layout (Phased Architecture)

```
Desktop Calendar/
├── src/
│   ├── CalendarWidget.App/
│   │   ├── App.xaml / App.xaml.cs      # Host bootstrap and lifecycle
│   │   ├── MainWindow.xaml / cs        # Host shell / navigation container
│   │   └── Program.cs                  # [Planned Phase 2] Host builder entry point
│   ├── CalendarWidget.Core/
│   │   ├── Entities/                   # CalendarEvent, Note, CalendarSettings (planned)
│   │   ├── ValueObjects/               # DateRange, TimeRange (planned), ColorHex (planned)
│   │   ├── Interfaces/                 # ICalendarEventRepository, INoteRepository
│   │   └── Exceptions/                 # DomainValidationException
│   ├── CalendarWidget.Infrastructure/
│   │   ├── Persistence/                # AppDbContext, Configurations, Migrations (Phase 5)
│   │   ├── Repositories/               # [Planned Phase 5] EfCalendarEventRepository, EfNoteRepository
│   │   ├── Services/                   # [Planned Phase 9] WindowsStartupService, NotificationService
│   │   └── DependencyInjection.cs      # IServiceCollection extensions
│   └── CalendarWidget.Presentation/
│       ├── ViewModels/                 # ViewModelBase; [Planned Phase 2+] WidgetViewModel, CalendarViewModel
│       ├── Views/                      # [Planned Phase 2+] WidgetView, CalendarView, NotesView
│       ├── Controls/                   # [Planned Phase 3] CalendarGridControl, DayCellControl
│       ├── Converters/                 # [Planned Phase 3] BoolToVisibilityConverter, DateFormatConverter
│       └── Themes/                     # [Planned Phase 3] Light.xaml, Dark.xaml
└── tests/
    ├── CalendarWidget.UnitTests/       # Domain logic, ViewModels, validators
    └── CalendarWidget.IntegrationTests/# EF Core SQLite, database migrations
```

## Assembly & Package Manifest

| Project | Target Framework | Output Type | Key Packages |
|---------|-----------------|-------------|--------------|
| `CalendarWidget.Core` | `net10.0` | Class Library | None (zero dependency) |
| `CalendarWidget.Infrastructure` | `net10.0` | Class Library | `Microsoft.EntityFrameworkCore.Sqlite` |
| `CalendarWidget.Presentation` | `net10.0-windows` | WPF Class Library | `CommunityToolkit.Mvvm` |
| `CalendarWidget.App` | `net10.0-windows` | WinExe | `Microsoft.Extensions.Hosting` |
| `CalendarWidget.UnitTests` | `net10.0-windows` | Unit Tests | `xUnit`, `FluentAssertions` |
| `CalendarWidget.IntegrationTests` | `net10.0-windows` | Unit Tests | `xUnit`, `FluentAssertions`, `EF Core InMemory/Sqlite` |

## Enforcement Rules
1. Never link `CalendarWidget.Infrastructure` into `CalendarWidget.Presentation`.
2. Do not introduce cyclical dependencies.
3. Every public interface in `Core` must be technology-agnostic.
