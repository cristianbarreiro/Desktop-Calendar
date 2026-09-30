using CalendarWidget.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CalendarWidget.IntegrationTests.Helpers;

/// <summary>
/// Provides an isolated SQLite database for a single test, cleaned up on disposal.
/// </summary>
public sealed class SqliteTestContext : IAsyncDisposable
{
    private readonly string _dbPath;

    /// <summary>Gets the configured <see cref="AppDbContext"/>.</summary>
    public AppDbContext Context { get; }

    /// <summary>Gets the database file path.</summary>
    public string DbPath => _dbPath;

    /// <summary>
    /// Creates an <see cref="IServiceScopeFactory"/> configured for this test database, optionally with interceptors.
    /// </summary>
    /// <param name="interceptors">Optional EF Core interceptors for fault injection.</param>
    public IServiceScopeFactory CreateScopeFactory(params Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor[] interceptors)
    {
        Microsoft.Extensions.DependencyInjection.ServiceCollection services = new();
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite($"Data Source={_dbPath}");
            if (interceptors.Length > 0)
            {
                options.AddInterceptors(interceptors);
            }
        });
        services.AddScoped<CalendarWidget.Core.Interfaces.ICalendarEventRepository, EfCalendarEventRepository>();
        services.AddScoped<CalendarWidget.Core.Interfaces.INoteRepository, EfNoteRepository>();
        Microsoft.Extensions.DependencyInjection.ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IServiceScopeFactory>();
    }

    private SqliteTestContext(string dbPath, AppDbContext context)
    {
        _dbPath = dbPath;
        Context = context;
    }

    /// <summary>
    /// Creates a new isolated SQLite database with migrations applied.
    /// </summary>
    public static async Task<SqliteTestContext> CreateAsync()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"cw_test_{Guid.NewGuid():N}.db");
        string connectionString = $"Data Source={dbPath}";

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options;

        AppDbContext context = new(options);
        await context.Database.MigrateAsync();

        return new SqliteTestContext(dbPath, context);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();

        // Clear SQLite connection pool so the file handle is released before deletion.
        SqliteConnection.ClearAllPools();

        foreach (string file in new[] { _dbPath, $"{_dbPath}-wal", $"{_dbPath}-shm" })
        {
            if (File.Exists(file))
                File.Delete(file);
        }
    }
}
