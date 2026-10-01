using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CalendarWidget.Infrastructure.Services;

/// <summary>
/// Singleton service managing in-memory access and runtime notifications for application settings.
/// </summary>
public sealed class SettingsService : ISettingsService, IDisposable
{
    private static readonly Action<ILogger, Exception?> LogInitializing =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, "Initializing"), "Initializing application settings from repository.");

    private static readonly Action<ILogger, Exception?> LogSaved =
        LoggerMessage.Define(LogLevel.Information, new EventId(2, "Saved"), "Settings saved successfully. Broadcasting SettingsChanged event.");

    private readonly ISettingsRepository _repository;
    private readonly ILogger<SettingsService> _logger;
    private readonly Lock _lock = new();
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private long _versionCounter;
    private long _lastSavedVersion;
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

    /// <summary>
    /// Optional hook invoked before acquiring the save lock, used exclusively for concurrency testing.
    /// </summary>
    internal Func<Task>? BeforeAcquireLockForTesting { get; set; }

    /// <inheritdoc />
    public async Task SaveSettingsAsync(UserSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        long version = Interlocked.Increment(ref _versionCounter);

        if (BeforeAcquireLockForTesting is not null)
        {
            await BeforeAcquireLockForTesting().ConfigureAwait(false);
        }

        await _saveLock.WaitAsync(cancellationToken);
        try
        {
            if (version < _lastSavedVersion)
            {
                // A newer version has already been persisted; drop this stale snapshot.
                return;
            }

            await _repository.SaveSettingsAsync(settings, cancellationToken);
            CurrentSettings = settings.Clone();
            _lastSavedVersion = version;

            LogSaved(_logger, null);
            SettingsChanged?.Invoke(this, settings.Clone());
        }
        finally
        {
            _saveLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task MutateSettingsAsync(Action<UserSettings> mutator, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mutator);

        if (BeforeAcquireLockForTesting is not null)
        {
            await BeforeAcquireLockForTesting().ConfigureAwait(false);
        }

        await _saveLock.WaitAsync(cancellationToken);
        try
        {
            UserSettings updated = CurrentSettings;
            mutator(updated);
            updated.Validate();

            long version = Interlocked.Increment(ref _versionCounter);
            await _repository.SaveSettingsAsync(updated, cancellationToken);
            CurrentSettings = updated.Clone();
            _lastSavedVersion = version;

            LogSaved(_logger, null);
            SettingsChanged?.Invoke(this, updated.Clone());
        }
        finally
        {
            _saveLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _saveLock.Dispose();
    }
}
