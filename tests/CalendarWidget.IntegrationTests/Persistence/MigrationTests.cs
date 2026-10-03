using System.Globalization;
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
    public async Task MigrateAsync_FromCurrentSchema_AssociatesExistingEventsWithLocalCalendar()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"cw_migration_{Guid.NewGuid():N}.db");
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
        Guid eventId = Guid.NewGuid();
        DateTime start = new(2026, 8, 12, 14, 30, 0, DateTimeKind.Utc);
        DateTime end = start.AddHours(1);

        try
        {
            await using AppDbContext context = new(options);
            await context.Database.MigrateAsync("20260925191413_InitialCreate");
            string storedStart = start.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture);
            string storedEnd = end.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture);
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO CalendarEvents (Id, Title, Description, StartTime, EndTime, IsAllDay, CreatedAt, UpdatedAt)
                VALUES ({eventId}, {"Existing event"}, {null}, {storedStart}, {storedEnd}, {false}, {storedStart}, {storedStart})
                """);

            await context.Database.MigrateAsync();

            CalendarEvent migrated = await context.CalendarEvents.AsNoTracking().SingleAsync(eventItem => eventItem.Id == eventId);
            migrated.CalendarId.Should().Be(CalendarIdentity.LocalCalendarId);
            migrated.Location.Should().BeNull();
            migrated.Title.Should().Be("Existing event");
            migrated.StartTime.Should().Be(start);
            (await context.Calendars.AsNoTracking().CountAsync(calendar => calendar.Id == CalendarIdentity.LocalCalendarId))
                .Should().Be(1);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            foreach (string file in new[] { dbPath, $"{dbPath}-wal", $"{dbPath}-shm" })
            {
                if (File.Exists(file)) File.Delete(file);
            }
        }
    }

    [Fact]
    public async Task MigrateAsync_OnCleanDatabase_CreatesSchema()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();

        // Verify tables exist by querying sqlite_master
        IReadOnlyList<string> tables = await GetTableNamesAsync(db.Context);

        tables.Should().Contain("CalendarEvents");
        tables.Should().Contain("Calendars");
        tables.Should().Contain("CalendarAccounts");
        tables.Should().Contain("CalendarEventMappings");
        tables.Should().Contain("CalendarSyncStates");
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
    public async Task MigrateAsync_CalendarSyncUiState_IsPersisted()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        CalendarAccount account = new()
        {
            Id = Guid.NewGuid(),
            Provider = CalendarWidget.Core.Enums.CalendarProvider.Google,
            ProviderAccountId = "ui-test-account",
            DisplayName = "UI Test Account",
            CreatedAt = DateTime.UtcNow,
            IsConnected = false,
        };
        CalendarSyncState state = new()
        {
            CalendarId = CalendarIdentity.LocalCalendarId,
            Cursor = "opaque-cursor",
            LastError = "offline",
        };

        db.Context.CalendarAccounts.Add(account);
        db.Context.CalendarSyncStates.Add(state);
        db.Context.PendingCalendarOperations.Add(new PendingCalendarOperation
        {
            Id = Guid.NewGuid(),
            CalendarId = CalendarIdentity.LocalCalendarId,
            InternalEventId = Guid.NewGuid(),
            Type = PendingCalendarOperationType.Delete,
            CreatedAt = DateTime.UtcNow,
            LastError = "offline",
        });
        await db.Context.SaveChangesAsync();
        db.Context.ChangeTracker.Clear();

        (await db.Context.CalendarAccounts.SingleAsync()).IsConnected.Should().BeFalse();
        (await db.Context.CalendarSyncStates.SingleAsync()).LastError.Should().Be("offline");
        (await db.Context.PendingCalendarOperations.SingleAsync()).LastError.Should().Be("offline");
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
