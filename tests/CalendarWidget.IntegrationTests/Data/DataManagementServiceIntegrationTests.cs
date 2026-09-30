using System.Text.Json;
using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Interfaces;
using CalendarWidget.Core.Models;
using CalendarWidget.Infrastructure.Persistence;
using CalendarWidget.Infrastructure.Services;
using CalendarWidget.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CalendarWidget.IntegrationTests.Data;

/// <summary>
/// Integration tests for <see cref="DataManagementService"/> against a real SQLite database.
/// </summary>
public sealed class DataManagementServiceIntegrationTests
{
    private sealed class StubSettingsService : ISettingsService
    {
        public UserSettings CurrentSettings { get; set; } = new();

        public event EventHandler<UserSettings>? SettingsChanged;

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SaveSettingsAsync(UserSettings settings, CancellationToken cancellationToken = default)
        {
            CurrentSettings = settings.Clone();
            SettingsChanged?.Invoke(this, CurrentSettings);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task ExportDataJsonAsync_ExportsEventsNotesAndSettings_ToJson()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfCalendarEventRepository eventRepo = new(db.Context);
        EfNoteRepository noteRepo = new(db.Context);

        CalendarEvent ev = new()
        {
            Id = Guid.NewGuid(),
            Title = "Team Standup",
            StartTime = new DateTime(2026, 9, 30, 9, 0, 0),
            EndTime = new DateTime(2026, 9, 30, 9, 30, 0),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        await eventRepo.AddAsync(ev);

        Note note = new()
        {
            Id = Guid.NewGuid(),
            Title = "Meeting Summary",
            Content = "All tasks on schedule.",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        await noteRepo.AddAsync(note);

        StubSettingsService settingsService = new();
        settingsService.CurrentSettings.Theme = AppThemeMode.Light;

        DataManagementService service = new(
            db.CreateScopeFactory(),
            settingsService,
            NullLogger<DataManagementService>.Instance);

        string json = await service.ExportDataJsonAsync();

        json.Should().NotBeNullOrWhiteSpace();
        using JsonDocument doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("version").GetInt32().Should().Be(1);

        AppBackupData? backup = JsonSerializer.Deserialize<AppBackupData>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
        });

        backup.Should().NotBeNull();
        backup!.Events.Should().ContainSingle(e => e.Id == ev.Id && e.Title == "Team Standup");
        backup.Notes.Should().ContainSingle(n => n.Id == note.Id && n.Title == "Meeting Summary");
    }

    [Fact]
    public async Task ImportDataJsonAsync_ImportsEventsAndNotes_IntoEmptyDatabase()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        StubSettingsService settingsService = new();

        DataManagementService service = new(
            db.CreateScopeFactory(),
            settingsService,
            NullLogger<DataManagementService>.Instance);

        Guid eventId = Guid.NewGuid();
        Guid noteId = Guid.NewGuid();

        AppBackupData backup = new()
        {
            Version = 1,
            ExportedAt = DateTime.UtcNow,
            Events =
            [
                new CalendarEventBackupDto
                {
                    Id = eventId,
                    Title = "Imported Event",
                    StartTime = new DateTime(2026, 10, 1, 10, 0, 0),
                    EndTime = new DateTime(2026, 10, 1, 11, 0, 0),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            ],
            Notes =
            [
                new NoteBackupDto
                {
                    Id = noteId,
                    Title = "Imported Note",
                    Content = "Imported content",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            ]
        };

        string json = JsonSerializer.Serialize(backup);
        DataImportResult result = await service.ImportDataJsonAsync(json);

        result.Success.Should().BeTrue();
        result.EventsImported.Should().Be(1);
        result.NotesImported.Should().Be(1);
        result.EventsSkipped.Should().Be(0);
        result.NotesSkipped.Should().Be(0);

        // Verify data in database
        CalendarEvent? storedEvent = await db.Context.CalendarEvents.FindAsync(eventId);
        storedEvent.Should().NotBeNull();
        storedEvent!.Title.Should().Be("Imported Event");

        Note? storedNote = await db.Context.Notes.FindAsync(noteId);
        storedNote.Should().NotBeNull();
        storedNote!.Title.Should().Be("Imported Note");
    }

    [Fact]
    public async Task ImportDataJsonAsync_SkipsExistingDuplicates_WithoutOverwriting()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfCalendarEventRepository eventRepo = new(db.Context);

        Guid existingId = Guid.NewGuid();
        CalendarEvent existing = new()
        {
            Id = existingId,
            Title = "Original Event Title",
            StartTime = new DateTime(2026, 9, 30, 9, 0, 0),
            EndTime = new DateTime(2026, 9, 30, 10, 0, 0),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        await eventRepo.AddAsync(existing);

        StubSettingsService settingsService = new();
        DataManagementService service = new(
            db.CreateScopeFactory(),
            settingsService,
            NullLogger<DataManagementService>.Instance);

        AppBackupData backup = new()
        {
            Version = 1,
            ExportedAt = DateTime.UtcNow,
            Events =
            [
                new CalendarEventBackupDto
                {
                    Id = existingId,
                    Title = "Modified Should Be Skipped",
                    StartTime = new DateTime(2026, 9, 30, 9, 0, 0),
                    EndTime = new DateTime(2026, 9, 30, 10, 0, 0),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                },
                new CalendarEventBackupDto
                {
                    Id = Guid.NewGuid(),
                    Title = "Brand New Event",
                    StartTime = new DateTime(2026, 10, 5, 9, 0, 0),
                    EndTime = new DateTime(2026, 10, 5, 10, 0, 0),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            ]
        };

        string json = JsonSerializer.Serialize(backup);
        DataImportResult result = await service.ImportDataJsonAsync(json);

        result.Success.Should().BeTrue();
        result.EventsImported.Should().Be(1);
        result.EventsSkipped.Should().Be(1);

        // Check original was not modified
        CalendarEvent? reloaded = await db.Context.CalendarEvents.AsNoTracking().FirstOrDefaultAsync(e => e.Id == existingId);
        reloaded.Should().NotBeNull();
        reloaded!.Title.Should().Be("Original Event Title");
    }

    [Fact]
    public async Task ImportDataJsonAsync_RollsBack_WhenAnEntityFailsDomainValidation()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        StubSettingsService settingsService = new();

        DataManagementService service = new(
            db.CreateScopeFactory(),
            settingsService,
            NullLogger<DataManagementService>.Instance);

        AppBackupData backup = new()
        {
            Version = 1,
            ExportedAt = DateTime.UtcNow,
            Events =
            [
                new CalendarEventBackupDto
                {
                    Id = Guid.NewGuid(),
                    Title = "Valid Event",
                    StartTime = new DateTime(2026, 9, 30, 9, 0, 0),
                    EndTime = new DateTime(2026, 9, 30, 10, 0, 0),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                },
                new CalendarEventBackupDto
                {
                    Id = Guid.NewGuid(),
                    Title = "", // Invalid: Empty title violates domain rule!
                    StartTime = new DateTime(2026, 9, 30, 9, 0, 0),
                    EndTime = new DateTime(2026, 9, 30, 10, 0, 0),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            ]
        };

        string json = JsonSerializer.Serialize(backup);
        DataImportResult result = await service.ImportDataJsonAsync(json);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("title");

        // Verify total rollback: zero events added
        int count = await db.Context.CalendarEvents.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task ImportDataJsonAsync_WhenJsonMalformed_ReturnsFailureResult()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        StubSettingsService settingsService = new();

        DataManagementService service = new(
            db.CreateScopeFactory(),
            settingsService,
            NullLogger<DataManagementService>.Instance);

        DataImportResult result = await service.ImportDataJsonAsync("This is not valid JSON.");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ResetAllDataAsync_RemovesAllEventsAndNotes_LeavesDatabaseEmpty()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfCalendarEventRepository eventRepo = new(db.Context);
        EfNoteRepository noteRepo = new(db.Context);

        await eventRepo.AddAsync(new CalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = "Event 1",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        await noteRepo.AddAsync(new Note
        {
            Id = Guid.NewGuid(),
            Title = "Note 1",
            Content = "Content 1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        StubSettingsService settingsService = new();
        DataManagementService service = new(
            db.CreateScopeFactory(),
            settingsService,
            NullLogger<DataManagementService>.Instance);

        bool eventFired = false;
        service.DataChanged += (_, _) => eventFired = true;

        await service.ResetAllDataAsync();

        eventFired.Should().BeTrue();
        int eventCount = await db.Context.CalendarEvents.CountAsync();
        int noteCount = await db.Context.Notes.CountAsync();

        eventCount.Should().Be(0);
        noteCount.Should().Be(0);
    }
}
