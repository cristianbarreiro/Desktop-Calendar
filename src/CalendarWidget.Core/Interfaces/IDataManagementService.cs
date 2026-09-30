using CalendarWidget.Core.Models;

namespace CalendarWidget.Core.Interfaces;

/// <summary>
/// Service providing application data maintenance, backup export/import, and factory reset operations.
/// </summary>
public interface IDataManagementService
{
    /// <summary>
    /// Serializes all calendar events, notes, and user settings into a deterministic JSON backup.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Formatted JSON string of the backup.</returns>
    Task<string> ExportDataJsonAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and safely restores application data from an exported JSON backup.
    /// </summary>
    /// <param name="json">The backup JSON payload.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Result report detailing imported and skipped records.</returns>
    Task<DataImportResult> ImportDataJsonAsync(string json, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all user calendar events and notes from the database after explicit confirmation.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task ResetAllDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Occurs when application data has been mutated via import or reset.
    /// </summary>
    event EventHandler? DataChanged;
}
