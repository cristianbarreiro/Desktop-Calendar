# Key Technical Decisions

## Architectural Decisions Log

1. **Framework**: .NET 10 + C# 14 + WPF. Selected for optimal performance, mature Windows API integration, and low memory usage.
2. **MVVM Library**: `CommunityToolkit.Mvvm` (official Microsoft toolkit) using source-generated observable properties and relay commands.
3. **Database**: SQLite with Entity Framework Core. Ensures local-first privacy, zero server dependencies, and deterministic schema migrations.
4. **Dependency Injection**: `Microsoft.Extensions.Hosting` + `Microsoft.Extensions.DependencyInjection` in the App composition root.
5. **No Event Aggregator / MediatR**: Avoids indirect magic and excessive indirection; standard C# events, interfaces, and MVVM Messenger are sufficient.
