using System.IO;
using CalendarWidget.Core.Interfaces;
using CalendarWidget.Infrastructure.Persistence;
using CalendarWidget.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalendarWidget.Infrastructure;

/// <summary>
/// Extension methods for registering infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services (persistence, repositories, settings, OS integrations) to the DI container.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string? settingsFilePath = null)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<ICalendarEventRepository, EfCalendarEventRepository>();
        services.AddScoped<INoteRepository, EfNoteRepository>();
        services.AddScoped<DatabaseInitializer>();

        string finalSettingsPath = settingsFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DesktopCalendar",
            "settings.json");

        services.AddSingleton<ISettingsRepository>(sp =>
            new FileSettingsRepository(finalSettingsPath, sp.GetRequiredService<ILogger<FileSettingsRepository>>()));
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IWindowsStartupService, WindowsStartupService>();
        services.AddSingleton<IDataManagementService, DataManagementService>();

        return services;
    }
}
