using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Interfaces;

namespace CalendarWidget.UnitTests.Fakes;

/// <summary>
/// In-memory fake implementation of <see cref="ISettingsRepository"/> for unit tests.
/// </summary>
public sealed class TestSettingsRepository : ISettingsRepository
{
    private UserSettings _settings;

    public TestSettingsRepository(UserSettings? initialSettings = null)
    {
        _settings = initialSettings?.Clone() ?? new UserSettings();
    }

    public int SaveCount { get; private set; }
    public int LoadCount { get; private set; }
    public bool ThrowOnLoad { get; set; }
    public bool ThrowOnSave { get; set; }

    public Task<UserSettings> LoadSettingsAsync(CancellationToken cancellationToken = default)
    {
        LoadCount++;
        if (ThrowOnLoad)
            throw new InvalidOperationException("Simulated load failure.");

        return Task.FromResult(_settings.Clone());
    }

    public Task SaveSettingsAsync(UserSettings settings, CancellationToken cancellationToken = default)
    {
        SaveCount++;
        if (ThrowOnSave)
            throw new InvalidOperationException("Simulated save failure.");

        _settings = settings.Clone();
        return Task.CompletedTask;
    }
}
