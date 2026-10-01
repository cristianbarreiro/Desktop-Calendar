using CalendarWidget.Core.Entities;

namespace CalendarWidget.Core.Interfaces;

/// <summary>
/// Service managing runtime access, synchronization, and notification of user preferences.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the current in-memory settings snapshot.
    /// </summary>
    UserSettings CurrentSettings { get; }

    /// <summary>
    /// Initializes settings from storage and prepares defaults.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves updated settings, synchronizes runtime state, and notifies observers.
    /// </summary>
    /// <param name="settings">The updated settings.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task SaveSettingsAsync(UserSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically mutates current settings under the persistence lock and saves them.
    /// </summary>
    /// <param name="mutator">Action modifying the latest settings snapshot.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task MutateSettingsAsync(Action<UserSettings> mutator, CancellationToken cancellationToken = default);

    /// <summary>
    /// Occurs when any application settings value changes.
    /// </summary>
    event EventHandler<UserSettings>? SettingsChanged;
}
