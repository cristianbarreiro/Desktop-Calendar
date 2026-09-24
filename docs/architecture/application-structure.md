# Application Structure

## Project Layout

```
Desktop Calendar/
├── src/
│   ├── CalendarWidget.App/
│   │   ├── App.xaml / App.xaml.cs      # Host bootstrap and lifecycle
│   │   ├── MainWindow.xaml / cs        # Host shell / navigation container
│   │   └── Program.cs                  # Host builder entry point
│   ├── CalendarWidget.Core/
│   │   ├── Entities/                   # CalendarEvent, Note, CalendarSettings
│   │   ├── ValueObjects/               # DateRange, TimeRange, ColorHex
│   │   ├── Interfaces/                 # ICalendarEventRepository, INoteRepository
│   │   └── Exceptions/                 # DomainValidationException
│   ├── CalendarWidget.Infrastructure/
│   │   ├── Persistence/                # AppDbContext, Configurations, Migrations
│   │   ├── Repositories/               # EfCalendarEventRepository, EfNoteRepository
│   │   ├── Services/                   # WindowsStartupService, NotificationService
│   │   └── DependencyInjection.cs      # IServiceCollection extensions
│   └── CalendarWidget.Presentation/
│       ├── ViewModels/                 # WidgetViewModel, CalendarViewModel, NotesViewModel
│       ├── Views/                      # WidgetView, CalendarView, NotesView, SettingsView
│       ├── Controls/                   # CalendarGridControl, DayCellControl
│       ├── Converters/                 # BoolToVisibilityConverter, DateFormatConverter
│       └── Themes/                     # Light.xaml, Dark.xaml, Generic.xaml
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
