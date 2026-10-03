using System.IO;
using CalendarWidget.Core.Interfaces;
using CalendarWidget.Infrastructure.Persistence;
using CalendarWidget.Infrastructure.Services;
using CalendarWidget.Infrastructure.Services.Google;
using CalendarWidget.Infrastructure.Services.Outlook;
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
        string? settingsFilePath = null,
        string? googleOAuthClientId = null,
        string? microsoftOAuthClientId = null)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<ICalendarEventRepository, EfCalendarEventRepository>();
        services.AddScoped<ICalendarCatalogRepository, EfCalendarCatalogRepository>();
        services.AddScoped<ICalendarSyncRepository, EfCalendarSyncRepository>();
        services.AddScoped<LocalCalendarProvider>();
        services.AddScoped<ICalendarProvider>(sp => sp.GetRequiredService<LocalCalendarProvider>());
        services.AddScoped<GoogleCalendarProvider>();
        services.AddScoped<ICalendarProvider>(sp => sp.GetRequiredService<GoogleCalendarProvider>());
        services.AddScoped<MicrosoftCalendarProvider>();
        services.AddScoped<ICalendarProvider>(sp => sp.GetRequiredService<MicrosoftCalendarProvider>());
        services.AddScoped<ICalendarProviderRegistry, CalendarProviderRegistry>();
        services.AddSingleton<ICalendarConflictPolicy, LocalWinsCalendarConflictPolicy>();
        services.AddScoped<CalendarSynchronizationService>();
        services.AddSingleton<ICalendarSynchronizationService, ScopedCalendarSynchronizationService>();
        services.AddScoped<GoogleCalendarConnectionService>();
        services.AddSingleton<ICalendarConnectionService, ScopedCalendarConnectionService>();
        services.AddScoped<MicrosoftCalendarConnectionService>();
        services.AddSingleton<IMicrosoftCalendarConnectionService, ScopedMicrosoftCalendarConnectionService>();
        services.AddSingleton(new HttpClient());
        services.AddSingleton<IGoogleCredentialStore, WindowsCredentialManagerStore>();
        services.AddSingleton(new GoogleOAuthOptions(googleOAuthClientId ?? Environment.GetEnvironmentVariable("DESKTOP_CALENDAR_GOOGLE_CLIENT_ID") ?? string.Empty));
        services.AddScoped<IGoogleOAuthClient, GoogleOAuthClient>();
        services.AddScoped<IGoogleAccessTokenProvider, GoogleAccessTokenProvider>();
        services.AddSingleton(new MicrosoftOAuthOptions(microsoftOAuthClientId ??
            Environment.GetEnvironmentVariable("DESKTOP_CALENDAR_MICROSOFT_CLIENT_ID") ?? string.Empty));
        services.AddSingleton<MicrosoftOAuthClient>();
        services.AddSingleton<IMicrosoftOAuthClient>(sp => sp.GetRequiredService<MicrosoftOAuthClient>());
        services.AddScoped<IMicrosoftAccessTokenProvider, MicrosoftAccessTokenProvider>();
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
