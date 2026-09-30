using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CalendarWidget.Infrastructure.Services;

/// <summary>
/// Singleton service managing in-memory access and runtime notifications for application settings.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private static readonly Action<ILogger, Exception?> LogInitializing =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, "Initializing"), "Initializing application settings from repository.");

    private static readonly Action<ILogger, Exception?> LogSaved =
        LoggerMessage.Define(LogLevel.Information, new EventId(2, "Saved"), "Settings saved successfully. Broadcasting SettingsChanged event.");

    private readonly ISettingsRepository _repository;
    private readonly ILogger<SettingsService> _logger;
    private readonly Lock _lock = new();
    private UserSettings _currentSettings = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsService"/> class.
    /// </summary>
    public SettingsService(ISettingsRepository repository, ILogger<SettingsService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public UserSettings CurrentSettings
    {
        get
        {
            lock (_lock)
            {
                return _currentSettings.Clone();
            }
        }
        private set
        {
            lock (_lock)
            {
                _currentSettings = value;
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<UserSettings>? SettingsChanged;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        LogInitializing(_logger, null);
        UserSettings loaded = await _repository.LoadSettingsAsync(cancellationToken);
        CurrentSettings = loaded;
    }

    /// <inheritdoc />
    public async Task SaveSettingsAsync(UserSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        await _repository.SaveSettingsAsync(settings, cancellationToken);
        CurrentSettings = settings.Clone();

        LogSaved(_logger, null);
        SettingsChanged?.Invoke(this, settings.Clone());
    }
}
