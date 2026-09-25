using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CalendarWidget.Infrastructure.Persistence;

/// <summary>
/// Initializes the SQLite database by applying pending EF Core migrations and enabling WAL mode.
/// </summary>
public sealed class DatabaseInitializer(AppDbContext context, ILogger<DatabaseInitializer> logger)
{
    private static readonly Action<ILogger, Exception?> LogApplyingMigrations =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, "ApplyingMigrations"), "Applying database migrations.");

    private static readonly Action<ILogger, Exception?> LogInitializationComplete =
        LoggerMessage.Define(LogLevel.Information, new EventId(2, "InitializationComplete"), "Database initialization complete.");

    /// <summary>
    /// Applies pending migrations and configures WAL journal mode.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        LogApplyingMigrations(logger, null);
        await context.Database.MigrateAsync(cancellationToken);

        await EnableWalModeAsync(cancellationToken);
        LogInitializationComplete(logger, null);
    }

    private async Task EnableWalModeAsync(CancellationToken cancellationToken)
    {
        string? connectionString = context.Database.GetConnectionString();
        if (connectionString is null)
            return;

        await using SqliteConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
