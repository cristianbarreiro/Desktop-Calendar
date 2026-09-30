using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Interfaces;

namespace CalendarWidget.UnitTests.Fakes;

/// <summary>
/// Fake implementation of <see cref="ISettingsService"/> for unit tests.
/// </summary>
public sealed class TestSettingsService : ISettingsService
{
    private UserSettings _settings;

    public TestSettingsService(UserSettings? initialSettings = null)
    {
        _settings = initialSettings?.Clone() ?? new UserSettings();
    }

    public UserSettings CurrentSettings => _settings;

    public event EventHandler<UserSettings>? SettingsChanged;

    public int SaveCount { get; private set; }
    public bool ThrowOnSave { get; set; }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SaveSettingsAsync(UserSettings settings, CancellationToken cancellationToken = default)
    {
        SaveCount++;
        if (ThrowOnSave)
            throw new InvalidOperationException("Simulated save failure.");

        _settings = settings.Clone();
        SettingsChanged?.Invoke(this, _settings);
        return Task.CompletedTask;
    }
}
