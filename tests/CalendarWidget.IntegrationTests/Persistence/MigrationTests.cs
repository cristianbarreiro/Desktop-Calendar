using CalendarWidget.Core.Entities;
using CalendarWidget.Infrastructure.Persistence;
using CalendarWidget.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CalendarWidget.IntegrationTests.Persistence;

/// <summary>
/// Integration tests verifying EF Core migration pipeline and SQLite initialization.
/// </summary>
public sealed class MigrationTests
{
    [Fact]
    public async Task MigrateAsync_OnCleanDatabase_CreatesSchema()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();

        // Verify tables exist by querying sqlite_master
        IReadOnlyList<string> tables = await GetTableNamesAsync(db.Context);

        tables.Should().Contain("CalendarEvents");
        tables.Should().Contain("Notes");
    }

    [Fact]
    public async Task MigrateAsync_CalendarEventsIndex_Exists()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();

        IReadOnlyList<string> indexes = await GetIndexNamesAsync(db.Context, "CalendarEvents");

        indexes.Should().Contain("IX_CalendarEvents_StartTime_EndTime");
    }

    [Fact]
    public async Task MigrateAsync_RunTwice_IsIdempotent()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();

        // Running migrations again on an already-migrated database must not throw
        Func<Task> act = () => db.Context.Database.MigrateAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task MigrateAsync_ExistingData_SurvivesSubsequentMigration()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfNoteRepository repo = new(db.Context);

        Note note = new()
        {
            Id = Guid.NewGuid(),
            Title = "Surviving Note",
            Content = "Must survive",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        await repo.AddAsync(note);

        // Re-run migrations (no-op on current schema)
        await db.Context.Database.MigrateAsync();

        Note? retrieved = await repo.GetByIdAsync(note.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Title.Should().Be("Surviving Note");
    }

    [Fact]
    public async Task WalMode_IsEnabled_AfterInitialization()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"cw_wal_test_{Guid.NewGuid():N}.db");
        string connectionString = $"Data Source={dbPath}";

        try
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connectionString)
                .Options;

            await using AppDbContext context = new(options);
            DatabaseInitializer initializer = new(context, Microsoft.Extensions.Logging.Abstractions.NullLogger<DatabaseInitializer>.Instance);
            await initializer.InitializeAsync();

            // Verify WAL mode via PRAGMA
            await using SqliteConnection connection = new(connectionString);
            await connection.OpenAsync();
            await using SqliteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA journal_mode;";
            object? result = await cmd.ExecuteScalarAsync();

            result.Should().Be("wal");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            foreach (string file in new[] { dbPath, $"{dbPath}-wal", $"{dbPath}-shm" })
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
        }
    }

    private static async Task<IReadOnlyList<string>> GetTableNamesAsync(AppDbContext context)
    {
        List<string> tables = [];
        string? connectionString = context.Database.GetConnectionString();
        await using SqliteConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqliteCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '__EF%';";
        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            tables.Add(reader.GetString(0));
        return tables;
    }

    private static async Task<IReadOnlyList<string>> GetIndexNamesAsync(AppDbContext context, string tableName)
    {
        List<string> indexes = [];
        string? connectionString = context.Database.GetConnectionString();
        await using SqliteConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqliteCommand cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT name FROM sqlite_master WHERE type='index' AND tbl_name='{tableName}';";
        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            indexes.Add(reader.GetString(0));
        return indexes;
    }
}
