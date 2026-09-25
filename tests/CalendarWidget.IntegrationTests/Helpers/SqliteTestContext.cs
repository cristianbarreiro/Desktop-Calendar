using CalendarWidget.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CalendarWidget.IntegrationTests.Helpers;

/// <summary>
/// Provides an isolated SQLite database for a single test, cleaned up on disposal.
/// </summary>
public sealed class SqliteTestContext : IAsyncDisposable
{
    private readonly string _dbPath;

    /// <summary>Gets the configured <see cref="AppDbContext"/>.</summary>
    public AppDbContext Context { get; }

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
