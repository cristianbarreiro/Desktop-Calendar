using CalendarWidget.Core.Entities;

namespace CalendarWidget.Core.Interfaces;

/// <summary>
/// Repository abstraction for persisting and retrieving user preferences and application settings.
/// </summary>
public interface ISettingsRepository
{
    /// <summary>
    /// Loads the stored user settings asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The persisted settings, or default settings if none exist.</returns>
    Task<UserSettings> LoadSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the user settings asynchronously.
    /// </summary>
    /// <param name="settings">The settings to persist.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task SaveSettingsAsync(UserSettings settings, CancellationToken cancellationToken = default);
}
