# Application Structure

## Target Project Layout (Phased Architecture)

```
Desktop Calendar/
├── src/
│   ├── CalendarWidget.App/
│   │   ├── App.xaml / App.xaml.cs            # Host bootstrap and lifecycle
│   │   ├── Program.cs                        # Generic Host builder entry point and DI root
│   │   ├── Windows/
│   │   │   ├── MainWindow.xaml / cs          # Full application shell / navigation container
│   │   │   └── WidgetWindow.xaml / cs        # Compact desktop widget window
│   │   └── Services/
│   │       ├── WindowManager.cs              # Window orchestration and lifecycle management
│   │       ├── ApplicationLifetimeService.cs # Application shutdown and Host lifetime coordination
│   │       └── IManagedWindow.cs             # Window abstraction for testable orchestration
│   ├── CalendarWidget.Core/
│   │   ├── Entities/                         # CalendarEvent, Note, CalendarSettings (planned Phase 10)
│   │   ├── ValueObjects/                     # DateRange, TimeRange (planned Phase 6), ColorHex (planned Phase 8)
│   │   ├── Interfaces/                       # ICalendarEventRepository, INoteRepository
│   │   └── Exceptions/                       # DomainValidationException
│   ├── CalendarWidget.Infrastructure/
│   │   ├── Persistence/                      # AppDbContext, Configurations, Migrations, DatabaseInitializer, AppDbContextFactory
│   │   ├── Repositories/                     # EfCalendarEventRepository, EfNoteRepository (Phase 7)
│   │   ├── Services/                         # [Planned Phase 11] WindowsStartupService; notifications are Future scope
│   │   └── DependencyInjection.cs            # IServiceCollection extensions
│   └── CalendarWidget.Presentation/
│       ├── ViewModels/                       # ViewModelBase, MainWindowViewModel, WidgetViewModel, CalendarViewModel, NotesViewModel, SettingsViewModel, NavigationTab
│       ├── Views/                            # CalendarView, NotesView, SettingsView
│       ├── Services/                         # IWindowManager, IClockService, SystemClockService, ICalendarGridService, CalendarGridService
│       ├── Resources/                        # Colors.xaml, Typography.xaml, Spacing.xaml, Controls.xaml, Theme.xaml
│       ├── Controls/                         # [Planned Phase 8] CalendarGridControl, DayCellControl
│       └── Converters/                       # IsSelectedDayConverter; [Planned Phase 8] DateFormatConverter
└── tests/
    ├── CalendarWidget.UnitTests/             # Domain logic, ViewModels, Grid, WindowManager, Lifetime tests
    └── CalendarWidget.IntegrationTests/      # EF Core SQLite, database migrations
```

## Assembly & Package Manifest

| Project | Target Framework | Output Type | Key Packages |
|---|---|---|---|
| `CalendarWidget.Core` | `net10.0` | Class Library | None (zero dependency) |
| `CalendarWidget.Infrastructure` | `net10.0` | Class Library | `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design` |
| `CalendarWidget.Presentation` | `net10.0-windows` | WPF Class Library | `CommunityToolkit.Mvvm` |
| `CalendarWidget.App` | `net10.0-windows` | WinExe | `Microsoft.Extensions.Hosting` |
| `CalendarWidget.UnitTests` | `net10.0-windows` | Unit Tests | `xUnit`, `FluentAssertions` |
| `CalendarWidget.IntegrationTests` | `net10.0-windows` | Unit Tests | `xUnit`, `FluentAssertions`, `EF Core Sqlite` |

## Enforcement Rules
1. Never link `CalendarWidget.Infrastructure` into `CalendarWidget.Presentation`.
2. Do not introduce cyclical dependencies.
3. Every public interface in `Core` must be technology-agnostic.
