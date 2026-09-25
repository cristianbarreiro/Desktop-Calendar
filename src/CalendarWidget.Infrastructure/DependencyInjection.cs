using CalendarWidget.Core.Interfaces;
using CalendarWidget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CalendarWidget.Infrastructure;

/// <summary>
/// Extension methods for registering infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services (persistence, repositories) to the DI container.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<ICalendarEventRepository, EfCalendarEventRepository>();
        services.AddScoped<INoteRepository, EfNoteRepository>();
        services.AddScoped<DatabaseInitializer>();

        return services;
    }
}
