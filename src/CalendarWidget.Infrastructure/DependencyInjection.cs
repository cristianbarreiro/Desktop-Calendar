using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CalendarWidget.Infrastructure;

/// <summary>
/// Extension methods for registering infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services (persistence, OS integrations) to the DI container.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<Persistence.AppDbContext>(options =>
            options.UseSqlite(connectionString));

        // Repository registrations will be added as implementations are created.

        return services;
    }
}
