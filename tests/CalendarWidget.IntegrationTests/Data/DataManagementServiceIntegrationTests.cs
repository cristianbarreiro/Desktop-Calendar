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

        public bool ThrowOnSave { get; set; }

        public bool ThrowOnRestore { get; set; }

        public int SaveCallCount { get; private set; }

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SaveSettingsAsync(UserSettings settings, CancellationToken cancellationToken = default)
        {
            SaveCallCount++;
            if (ThrowOnSave && SaveCallCount == 1)
            {
                throw new IOException("Simulated settings disk failure.");
            }

            if (ThrowOnRestore && SaveCallCount > 1)
            {
                throw new IOException("Simulated settings restoration failure.");
            }

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

    [Fact]
    public async Task ImportDataJsonAsync_InvalidSettings_LeavesEventsNotesAndSettingsUnchanged()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfCalendarEventRepository eventRepo = new(db.Context);
        EfNoteRepository noteRepo = new(db.Context);

        Guid existingEventId = Guid.NewGuid();
        CalendarEvent existingEvent = new()
        {
            Id = existingEventId,
            Title = "Existing Event A",
            StartTime = new DateTime(2026, 9, 30, 9, 0, 0),
            EndTime = new DateTime(2026, 9, 30, 10, 0, 0),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        await eventRepo.AddAsync(existingEvent);

        Guid existingNoteId = Guid.NewGuid();
        Note existingNote = new()
        {
            Id = existingNoteId,
            Title = "Existing Note B",
            Content = "Existing content B",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        await noteRepo.AddAsync(existingNote);

        StubSettingsService settingsService = new();
        settingsService.CurrentSettings.Theme = AppThemeMode.Dark;
        settingsService.CurrentSettings.WidgetOpacity = 0.85;
        settingsService.CurrentSettings.FirstDayOfWeek = DayOfWeek.Wednesday;

        DataManagementService service = new(
            db.CreateScopeFactory(),
            settingsService,
            NullLogger<DataManagementService>.Instance);

        Guid newEventId = Guid.NewGuid();
        Guid newNoteId = Guid.NewGuid();

        AppBackupData backup = new()
        {
            Version = 1,
            ExportedAt = DateTime.UtcNow,
            Events =
            [
                new CalendarEventBackupDto
                {
                    Id = newEventId,
                    Title = "Candidate Event X",
                    StartTime = new DateTime(2026, 10, 1, 9, 0, 0),
                    EndTime = new DateTime(2026, 10, 1, 10, 0, 0),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            ],
            Notes =
            [
                new NoteBackupDto
                {
                    Id = newNoteId,
                    Title = "Candidate Note Y",
                    Content = "Candidate content Y",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            ],
            Settings = new UserSettings
            {
                WidgetOpacity = 0.1 // Invalid: opacity < 0.3 violates domain rule
            }
        };

        string json = JsonSerializer.Serialize(backup);
        DataImportResult result = await service.ImportDataJsonAsync(json);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Widget opacity");

        // Existing data must remain unchanged
        CalendarEvent? storedEvent = await db.Context.CalendarEvents.FindAsync(existingEventId);
        storedEvent.Should().NotBeNull();
        storedEvent!.Title.Should().Be("Existing Event A");

        Note? storedNote = await db.Context.Notes.FindAsync(existingNoteId);
        storedNote.Should().NotBeNull();
        storedNote!.Title.Should().Be("Existing Note B");

        // Candidate data must not be persisted
        (await db.Context.CalendarEvents.FindAsync(newEventId)).Should().BeNull();
        (await db.Context.Notes.FindAsync(newNoteId)).Should().BeNull();
        (await db.Context.CalendarEvents.CountAsync()).Should().Be(1);
        (await db.Context.Notes.CountAsync()).Should().Be(1);

        // Existing settings must remain unchanged
        settingsService.CurrentSettings.Theme.Should().Be(AppThemeMode.Dark);
        settingsService.CurrentSettings.WidgetOpacity.Should().Be(0.85);
        settingsService.CurrentSettings.FirstDayOfWeek.Should().Be(DayOfWeek.Wednesday);
    }

    [Fact]
    public async Task ImportDataJsonAsync_InvalidEvent_PersistsNothing()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfCalendarEventRepository eventRepo = new(db.Context);
        EfNoteRepository noteRepo = new(db.Context);

        Guid existingEventId = Guid.NewGuid();
        await eventRepo.AddAsync(new CalendarEvent
        {
            Id = existingEventId,
            Title = "Event A",
            StartTime = new DateTime(2026, 9, 30, 9, 0, 0),
            EndTime = new DateTime(2026, 9, 30, 10, 0, 0),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        Guid existingNoteId = Guid.NewGuid();
        await noteRepo.AddAsync(new Note
        {
            Id = existingNoteId,
            Title = "Note B",
            Content = "Content B",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        StubSettingsService settingsService = new();
        settingsService.CurrentSettings.WidgetOpacity = 0.9;

        DataManagementService service = new(
            db.CreateScopeFactory(),
            settingsService,
            NullLogger<DataManagementService>.Instance);

        Guid validNoteId = Guid.NewGuid();
        AppBackupData backup = new()
        {
            Version = 1,
            ExportedAt = DateTime.UtcNow,
            Events =
            [
                new CalendarEventBackupDto
                {
                    Id = Guid.NewGuid(),
                    Title = "", // Invalid: empty title
                    StartTime = new DateTime(2026, 10, 1, 9, 0, 0),
                    EndTime = new DateTime(2026, 10, 1, 10, 0, 0),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            ],
            Notes =
            [
                new NoteBackupDto
                {
                    Id = validNoteId,
                    Title = "Valid Candidate Note",
                    Content = "Valid content",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            ],
            Settings = new UserSettings { WidgetOpacity = 0.6 }
        };

        string json = JsonSerializer.Serialize(backup);
        DataImportResult result = await service.ImportDataJsonAsync(json);

        result.Success.Should().BeFalse();
        (await db.Context.CalendarEvents.CountAsync()).Should().Be(1);
        (await db.Context.Notes.CountAsync()).Should().Be(1);
        (await db.Context.Notes.FindAsync(validNoteId)).Should().BeNull();
        settingsService.CurrentSettings.WidgetOpacity.Should().Be(0.9);
    }

    [Fact]
    public async Task ImportDataJsonAsync_InvalidNote_PersistsNothing()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfCalendarEventRepository eventRepo = new(db.Context);
        EfNoteRepository noteRepo = new(db.Context);

        Guid existingEventId = Guid.NewGuid();
        await eventRepo.AddAsync(new CalendarEvent
        {
            Id = existingEventId,
            Title = "Event A",
            StartTime = new DateTime(2026, 9, 30, 9, 0, 0),
            EndTime = new DateTime(2026, 9, 30, 10, 0, 0),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        Guid existingNoteId = Guid.NewGuid();
        await noteRepo.AddAsync(new Note
        {
            Id = existingNoteId,
            Title = "Note B",
            Content = "Content B",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        StubSettingsService settingsService = new();
        settingsService.CurrentSettings.WidgetOpacity = 0.9;

        DataManagementService service = new(
            db.CreateScopeFactory(),
            settingsService,
            NullLogger<DataManagementService>.Instance);

        Guid validEventId = Guid.NewGuid();
        AppBackupData backup = new()
        {
            Version = 1,
            ExportedAt = DateTime.UtcNow,
            Events =
            [
                new CalendarEventBackupDto
                {
                    Id = validEventId,
                    Title = "Valid Candidate Event",
                    StartTime = new DateTime(2026, 10, 1, 9, 0, 0),
                    EndTime = new DateTime(2026, 10, 1, 10, 0, 0),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            ],
            Notes =
            [
                new NoteBackupDto
                {
                    Id = Guid.NewGuid(),
                    Title = "", // Invalid: empty title
                    Content = "Valid content",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            ],
            Settings = new UserSettings { WidgetOpacity = 0.6 }
        };

        string json = JsonSerializer.Serialize(backup);
        DataImportResult result = await service.ImportDataJsonAsync(json);

        result.Success.Should().BeFalse();
        (await db.Context.CalendarEvents.CountAsync()).Should().Be(1);
        (await db.Context.Notes.CountAsync()).Should().Be(1);
        (await db.Context.CalendarEvents.FindAsync(validEventId)).Should().BeNull();
        settingsService.CurrentSettings.WidgetOpacity.Should().Be(0.9);
    }

    [Fact]
    public async Task ImportDataJsonAsync_ValidBackup_RestoresAllDataSuccessfully()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        StubSettingsService settingsService = new();
        settingsService.CurrentSettings.WidgetOpacity = 0.5;
        settingsService.CurrentSettings.Theme = AppThemeMode.Dark;

        DataManagementService service = new(
            db.CreateScopeFactory(),
            settingsService,
            NullLogger<DataManagementService>.Instance);

        Guid event1Id = Guid.NewGuid();
        Guid event2Id = Guid.NewGuid();
        Guid note1Id = Guid.NewGuid();
        Guid note2Id = Guid.NewGuid();

        AppBackupData backup = new()
        {
            Version = 1,
            ExportedAt = DateTime.UtcNow,
            Events =
            [
                new CalendarEventBackupDto
                {
                    Id = event1Id,
                    Title = "Restored Event 1",
                    StartTime = new DateTime(2026, 10, 1, 9, 0, 0),
                    EndTime = new DateTime(2026, 10, 1, 10, 0, 0),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                },
                new CalendarEventBackupDto
                {
                    Id = event2Id,
                    Title = "Restored Event 2",
                    StartTime = new DateTime(2026, 10, 2, 14, 0, 0),
                    EndTime = new DateTime(2026, 10, 2, 15, 0, 0),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            ],
            Notes =
            [
                new NoteBackupDto
                {
                    Id = note1Id,
                    Title = "Restored Note 1",
                    Content = "Note content 1",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                },
                new NoteBackupDto
                {
                    Id = note2Id,
                    Title = "Restored Note 2",
                    Content = "Note content 2",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            ],
            Settings = new UserSettings
            {
                Theme = AppThemeMode.Light,
                WidgetOpacity = 0.8,
                AlwaysOnTop = true,
                FirstDayOfWeek = DayOfWeek.Monday
            }
        };

        string json = JsonSerializer.Serialize(backup);
        DataImportResult result = await service.ImportDataJsonAsync(json);

        result.Success.Should().BeTrue();
        result.EventsImported.Should().Be(2);
        result.NotesImported.Should().Be(2);

        (await db.Context.CalendarEvents.FindAsync(event1Id)).Should().NotBeNull();
        (await db.Context.CalendarEvents.FindAsync(event2Id)).Should().NotBeNull();
        (await db.Context.Notes.FindAsync(note1Id)).Should().NotBeNull();
        (await db.Context.Notes.FindAsync(note2Id)).Should().NotBeNull();

        settingsService.CurrentSettings.Theme.Should().Be(AppThemeMode.Light);
        settingsService.CurrentSettings.WidgetOpacity.Should().Be(0.8);
        settingsService.CurrentSettings.AlwaysOnTop.Should().BeTrue();
        settingsService.CurrentSettings.FirstDayOfWeek.Should().Be(DayOfWeek.Monday);
    }

    [Fact]
    public async Task ImportDataJsonAsync_PersistenceFailure_DoesNotLeavePartialImportedState()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfCalendarEventRepository eventRepo = new(db.Context);
        EfNoteRepository noteRepo = new(db.Context);

        Guid existingEventId = Guid.NewGuid();
        await eventRepo.AddAsync(new CalendarEvent
        {
            Id = existingEventId,
            Title = "Existing Event A",
            StartTime = new DateTime(2026, 9, 30, 9, 0, 0),
            EndTime = new DateTime(2026, 9, 30, 10, 0, 0),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        Guid existingNoteId = Guid.NewGuid();
        await noteRepo.AddAsync(new Note
        {
            Id = existingNoteId,
            Title = "Existing Note B",
            Content = "Existing content B",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        StubSettingsService settingsService = new();
        settingsService.CurrentSettings.WidgetOpacity = 0.75;
        settingsService.ThrowOnSave = true; // Simulates failure during settings persistence

        DataManagementService service = new(
            db.CreateScopeFactory(),
            settingsService,
            NullLogger<DataManagementService>.Instance);

        Guid candidateEventId = Guid.NewGuid();
        Guid candidateNoteId = Guid.NewGuid();

        AppBackupData backup = new()
        {
            Version = 1,
            ExportedAt = DateTime.UtcNow,
            Events =
            [
                new CalendarEventBackupDto
                {
                    Id = candidateEventId,
                    Title = "Candidate Event X",
                    StartTime = new DateTime(2026, 10, 1, 9, 0, 0),
                    EndTime = new DateTime(2026, 10, 1, 10, 0, 0),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            ],
            Notes =
            [
                new NoteBackupDto
                {
                    Id = candidateNoteId,
                    Title = "Candidate Note Y",
                    Content = "Candidate content Y",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            ],
            Settings = new UserSettings { WidgetOpacity = 0.95 }
        };

        string json = JsonSerializer.Serialize(backup);
        DataImportResult result = await service.ImportDataJsonAsync(json);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Simulated settings disk failure");

        // Database transaction was rolled back: only existing records remain
        (await db.Context.CalendarEvents.FindAsync(candidateEventId)).Should().BeNull();
        (await db.Context.Notes.FindAsync(candidateNoteId)).Should().BeNull();
        (await db.Context.CalendarEvents.CountAsync()).Should().Be(1);
        (await db.Context.Notes.CountAsync()).Should().Be(1);

        // Settings remain at original state
        settingsService.CurrentSettings.WidgetOpacity.Should().Be(0.75);
    }

    [Fact]
    public async Task ImportDataJsonAsync_DatabasePersistenceFailure_RollsBackAndLeavesStateUnchanged()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfCalendarEventRepository eventRepo = new(db.Context);
        EfNoteRepository noteRepo = new(db.Context);

        Guid existingEventId = Guid.NewGuid();
        await eventRepo.AddAsync(new CalendarEvent
        {
            Id = existingEventId,
            Title = "Event A",
            StartTime = new DateTime(2026, 9, 30, 9, 0, 0),
            EndTime = new DateTime(2026, 9, 30, 10, 0, 0),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        Guid existingNoteId = Guid.NewGuid();
        await noteRepo.AddAsync(new Note
        {
            Id = existingNoteId,
            Title = "Note B",
            Content = "Content B",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        TestSaveChangesInterceptor interceptor = new() { FailSavingChanges = true };
        StubSettingsService settingsService = new();
        settingsService.CurrentSettings.WidgetOpacity = 0.8;

        DataManagementService service = new(
            db.CreateScopeFactory(interceptor),
            settingsService,
            NullLogger<DataManagementService>.Instance);

        bool dataChangedFired = false;
        service.DataChanged += (_, _) => dataChangedFired = true;

        Guid candidateEventId = Guid.NewGuid();
        Guid candidateNoteId = Guid.NewGuid();
        AppBackupData backup = new()
        {
            Version = 1,
            ExportedAt = DateTime.UtcNow,
            Events =
            [
                new CalendarEventBackupDto
                {
                    Id = candidateEventId,
                    Title = "Candidate Event X",
                    StartTime = new DateTime(2026, 10, 1, 9, 0, 0),
                    EndTime = new DateTime(2026, 10, 1, 10, 0, 0),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            ],
            Notes =
            [
                new NoteBackupDto
                {
                    Id = candidateNoteId,
                    Title = "Candidate Note Y",
                    Content = "Candidate Content Y",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            ],
            Settings = new UserSettings { WidgetOpacity = 0.5 }
        };

        string json = JsonSerializer.Serialize(backup);
        DataImportResult result = await service.ImportDataJsonAsync(json);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Simulated database persistence failure");

        // Database rolled back: only existing records remain
        (await db.Context.CalendarEvents.FindAsync(candidateEventId)).Should().BeNull();
        (await db.Context.Notes.FindAsync(candidateNoteId)).Should().BeNull();
        (await db.Context.CalendarEvents.CountAsync()).Should().Be(1);
        (await db.Context.Notes.CountAsync()).Should().Be(1);

        // Settings remain unchanged
        settingsService.CurrentSettings.WidgetOpacity.Should().Be(0.8);
        dataChangedFired.Should().BeFalse();
    }

    [Fact]
    public async Task ImportDataJsonAsync_CommitFailure_RollsBackDatabaseAndRestoresSettings()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfCalendarEventRepository eventRepo = new(db.Context);
        EfNoteRepository noteRepo = new(db.Context);

        Guid existingEventId = Guid.NewGuid();
        await eventRepo.AddAsync(new CalendarEvent
        {
            Id = existingEventId,
            Title = "Event A",
            StartTime = new DateTime(2026, 9, 30, 9, 0, 0),
            EndTime = new DateTime(2026, 9, 30, 10, 0, 0),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        TestTransactionInterceptor interceptor = new() { FailCommit = true };
        StubSettingsService settingsService = new();
        settingsService.CurrentSettings.WidgetOpacity = 0.85;

        DataManagementService service = new(
            db.CreateScopeFactory(interceptor),
            settingsService,
            NullLogger<DataManagementService>.Instance);

        bool dataChangedFired = false;
        service.DataChanged += (_, _) => dataChangedFired = true;

        Guid candidateEventId = Guid.NewGuid();
        AppBackupData backup = new()
        {
            Version = 1,
            ExportedAt = DateTime.UtcNow,
            Events =
            [
                new CalendarEventBackupDto
                {
                    Id = candidateEventId,
                    Title = "Candidate Event",
                    StartTime = new DateTime(2026, 10, 1, 9, 0, 0),
                    EndTime = new DateTime(2026, 10, 1, 10, 0, 0),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            ],
            Settings = new UserSettings { WidgetOpacity = 0.55 }
        };

        string json = JsonSerializer.Serialize(backup);
        DataImportResult result = await service.ImportDataJsonAsync(json);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Simulated transaction commit failure");

        // Database rolled back
        (await db.Context.CalendarEvents.FindAsync(candidateEventId)).Should().BeNull();
        (await db.Context.CalendarEvents.CountAsync()).Should().Be(1);

        // Settings compensated back to original
        settingsService.CurrentSettings.WidgetOpacity.Should().Be(0.85);
        dataChangedFired.Should().BeFalse();
    }

    [Fact]
    public async Task ImportDataJsonAsync_RollbackFailure_DetectsFailureAndReportsDiagnosticMessage()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();

        TestTransactionInterceptor interceptor = new() { FailCommit = true, FailRollback = true };
        StubSettingsService settingsService = new();

        DataManagementService service = new(
            db.CreateScopeFactory(interceptor),
            settingsService,
            NullLogger<DataManagementService>.Instance);

        bool dataChangedFired = false;
        service.DataChanged += (_, _) => dataChangedFired = true;

        AppBackupData backup = new()
        {
            Version = 1,
            ExportedAt = DateTime.UtcNow,
            Events =
            [
                new CalendarEventBackupDto
                {
                    Id = Guid.NewGuid(),
                    Title = "Candidate Event",
                    StartTime = new DateTime(2026, 10, 1, 9, 0, 0),
                    EndTime = new DateTime(2026, 10, 1, 10, 0, 0),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            ]
        };

        string json = JsonSerializer.Serialize(backup);
        DataImportResult result = await service.ImportDataJsonAsync(json);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Database rollback could not be confirmed");
        dataChangedFired.Should().BeFalse();
    }

    [Fact]
    public async Task ImportDataJsonAsync_SettingsRestorationFailure_DetectsFailureAndReportsDiagnosticMessage()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();

        TestTransactionInterceptor interceptor = new() { FailCommit = true };
        StubSettingsService settingsService = new();
        settingsService.ThrowOnRestore = true;

        DataManagementService service = new(
            db.CreateScopeFactory(interceptor),
            settingsService,
            NullLogger<DataManagementService>.Instance);

        bool dataChangedFired = false;
        service.DataChanged += (_, _) => dataChangedFired = true;

        AppBackupData backup = new()
        {
            Version = 1,
            ExportedAt = DateTime.UtcNow,
            Settings = new UserSettings { WidgetOpacity = 0.6 }
        };

        string json = JsonSerializer.Serialize(backup);
        DataImportResult result = await service.ImportDataJsonAsync(json);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Complete settings restoration could not be confirmed");
        dataChangedFired.Should().BeFalse();
    }

    [Fact]
    public async Task ImportDataJsonAsync_WhenCancelled_CleanlyRollsBackAndRestoresSettings()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfCalendarEventRepository eventRepo = new(db.Context);

        Guid existingEventId = Guid.NewGuid();
        await eventRepo.AddAsync(new CalendarEvent
        {
            Id = existingEventId,
            Title = "Event A",
            StartTime = new DateTime(2026, 9, 30, 9, 0, 0),
            EndTime = new DateTime(2026, 9, 30, 10, 0, 0),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        StubSettingsService settingsService = new();
        settingsService.CurrentSettings.WidgetOpacity = 0.85;

        DataManagementService service = new(
            db.CreateScopeFactory(),
            settingsService,
            NullLogger<DataManagementService>.Instance);

        bool dataChangedFired = false;
        service.DataChanged += (_, _) => dataChangedFired = true;

        using CancellationTokenSource cts = new();
        cts.Cancel(); // Pre-cancelled token

        AppBackupData backup = new()
        {
            Version = 1,
            ExportedAt = DateTime.UtcNow,
            Events =
            [
                new CalendarEventBackupDto
                {
                    Id = Guid.NewGuid(),
                    Title = "Cancelled Candidate Event",
                    StartTime = new DateTime(2026, 10, 1, 9, 0, 0),
                    EndTime = new DateTime(2026, 10, 1, 10, 0, 0),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            ],
            Settings = new UserSettings { WidgetOpacity = 0.5 }
        };

        string json = JsonSerializer.Serialize(backup);
        DataImportResult result = await service.ImportDataJsonAsync(json, cts.Token);

        result.Success.Should().BeFalse();
        (await db.Context.CalendarEvents.CountAsync()).Should().Be(1);
        settingsService.CurrentSettings.WidgetOpacity.Should().Be(0.85);
        dataChangedFired.Should().BeFalse();
    }

    [Fact]
    public async Task ImportDataJsonAsync_SuccessfulImport_RaisesDataChangedExactlyOnce()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        StubSettingsService settingsService = new();

        DataManagementService service = new(
            db.CreateScopeFactory(),
            settingsService,
            NullLogger<DataManagementService>.Instance);

        int dataChangedCount = 0;
        service.DataChanged += (_, _) => dataChangedCount++;

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
                    StartTime = new DateTime(2026, 10, 1, 9, 0, 0),
                    EndTime = new DateTime(2026, 10, 1, 10, 0, 0),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            ]
        };

        string json = JsonSerializer.Serialize(backup);
        DataImportResult result = await service.ImportDataJsonAsync(json);

        result.Success.Should().BeTrue();
        dataChangedCount.Should().Be(1);
    }
}
