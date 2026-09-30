using System.IO;
using CalendarWidget.Core.Interfaces;
using CalendarWidget.Core.Models;

namespace CalendarWidget.UnitTests.Fakes;

/// <summary>
/// Fake implementation of <see cref="IDataManagementService"/> for unit tests.
/// </summary>
public sealed class TestDataManagementService : IDataManagementService
{
    public bool ThrowOnExport { get; set; }
    public bool ThrowOnImport { get; set; }
    public bool ThrowOnReset { get; set; }

    public int ExportCount { get; private set; }
    public int ImportCount { get; private set; }
    public int ResetCount { get; private set; }

    public string? LastImportJson { get; private set; }
    public string ExportJsonToReturn { get; set; } = "{\"version\":1,\"calendarEvents\":[],\"notes\":[]}";

    public DataImportResult ImportResultToReturn { get; set; } = new()
    {
        Success = true,
        EventsImported = 2,
        NotesImported = 3,
        EventsSkipped = 0,
        NotesSkipped = 0
    };

    public event EventHandler? DataChanged;

    public Task<string> ExportDataJsonAsync(CancellationToken cancellationToken = default)
    {
        ExportCount++;
        if (ThrowOnExport)
            throw new IOException("Simulated export I/O failure.");

        return Task.FromResult(ExportJsonToReturn);
    }

    public Task<DataImportResult> ImportDataJsonAsync(string json, CancellationToken cancellationToken = default)
    {
        ImportCount++;
        if (ThrowOnImport)
            throw new FormatException("Simulated corrupt import JSON.");

        LastImportJson = json;
        DataChanged?.Invoke(this, EventArgs.Empty);
        return Task.FromResult(ImportResultToReturn);
    }

    public Task ResetAllDataAsync(CancellationToken cancellationToken = default)
    {
        ResetCount++;
        if (ThrowOnReset)
            throw new InvalidOperationException("Simulated reset failure.");

        DataChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }
}
