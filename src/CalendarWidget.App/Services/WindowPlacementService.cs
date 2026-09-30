using System.Windows;
using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Interfaces;

namespace CalendarWidget.App.Services;

/// <summary>
/// Implements window position restoration with multi-monitor off-screen recovery
/// and coalesced asynchronous debounced persistence of window bounds.
/// </summary>
public sealed class WindowPlacementService : IWindowPlacementService
{
    private static readonly TimeSpan DefaultDebounceDelay = TimeSpan.FromMilliseconds(400);

    private readonly ISettingsService _settingsService;
    private readonly IDisplayMonitorProvider _displayProvider;
    private readonly TimeSpan _debounceDelay;
    private readonly TimeProvider _timeProvider;
    private readonly ITimer _debounceTimer;
    private readonly Lock _lock = new();

    private double? _pendingMainLeft;
    private double? _pendingMainTop;
    private double? _pendingMainWidth;
    private double? _pendingMainHeight;

    private double? _pendingWidgetLeft;
    private double? _pendingWidgetTop;

    private Task? _currentSaveTask;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowPlacementService"/> class.
    /// </summary>
    /// <param name="settingsService">Settings persistence service.</param>
    /// <param name="displayProvider">Display monitor provider.</param>
    /// <param name="debounceDelay">Optional debounce delay override for testing.</param>
    /// <param name="timeProvider">Optional time provider for deterministic testing.</param>
    public WindowPlacementService(
        ISettingsService settingsService,
        IDisplayMonitorProvider displayProvider,
        TimeSpan? debounceDelay = null,
        TimeProvider? timeProvider = null)
    {
        _settingsService = settingsService;
        _displayProvider = displayProvider;
        _debounceDelay = debounceDelay ?? DefaultDebounceDelay;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _debounceTimer = _timeProvider.CreateTimer(
            OnDebounceTimerElapsed,
            null,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan);
    }

    /// <inheritdoc />
    public void ApplyMainWindowBounds(IManagedWindow window)
    {
        UserSettings settings = _settingsService.CurrentSettings;
        IReadOnlyList<DisplayArea> displayAreas = _displayProvider.GetDisplayAreas();
        DisplayArea primary = _displayProvider.GetPrimaryDisplayArea();

        if (settings.MainWindowLeft.HasValue &&
            settings.MainWindowTop.HasValue &&
            settings.MainWindowWidth.HasValue &&
            settings.MainWindowHeight.HasValue)
        {
            WindowBounds requested = new(
                settings.MainWindowLeft.Value,
                settings.MainWindowTop.Value,
                settings.MainWindowWidth.Value,
                settings.MainWindowHeight.Value);

            WindowBounds recovered = WindowBoundsHelper.EnsureVisible(requested, displayAreas, primary);

            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = recovered.Left;
            window.Top = recovered.Top;
            window.Width = recovered.Width;
            window.Height = recovered.Height;
        }
        else
        {
            double width = window.Width > 0 ? window.Width : 960;
            double height = window.Height > 0 ? window.Height : 620;
            WindowBounds centered = WindowBoundsHelper.CenterOnDisplay(width, height, primary);

            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = centered.Left;
            window.Top = centered.Top;
            window.Width = centered.Width;
            window.Height = centered.Height;
        }
    }

    /// <inheritdoc />
    public void ApplyWidgetWindowBounds(IManagedWindow window)
    {
        UserSettings settings = _settingsService.CurrentSettings;
        IReadOnlyList<DisplayArea> displayAreas = _displayProvider.GetDisplayAreas();
        DisplayArea primary = _displayProvider.GetPrimaryDisplayArea();

        double defaultWidth = window.Width > 0 ? window.Width : 288;
        double defaultHeight = window.Height > 0 ? window.Height : 240;

        if (settings.WidgetWindowLeft.HasValue && settings.WidgetWindowTop.HasValue)
        {
            WindowBounds requested = new(
                settings.WidgetWindowLeft.Value,
                settings.WidgetWindowTop.Value,
                defaultWidth,
                defaultHeight);

            WindowBounds recovered = WindowBoundsHelper.EnsureVisible(requested, displayAreas, primary);

            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = recovered.Left;
            window.Top = recovered.Top;
        }
        else
        {
            WindowBounds centered = WindowBoundsHelper.CenterOnDisplay(defaultWidth, defaultHeight, primary);

            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = centered.Left;
            window.Top = centered.Top;
        }
    }

    /// <inheritdoc />
    public void OnMainWindowBoundsChanged(double left, double top, double width, double height)
    {
        if (double.IsNaN(left) || double.IsNaN(top) || double.IsNaN(width) || double.IsNaN(height) ||
            double.IsInfinity(left) || double.IsInfinity(top) || width < 200 || height < 200)
        {
            return;
        }

        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _pendingMainLeft = left;
            _pendingMainTop = top;
            _pendingMainWidth = width;
            _pendingMainHeight = height;

            _debounceTimer.Change(_debounceDelay, Timeout.InfiniteTimeSpan);
        }
    }

    /// <inheritdoc />
    public void OnWidgetWindowBoundsChanged(double left, double top)
    {
        if (double.IsNaN(left) || double.IsNaN(top) || double.IsInfinity(left) || double.IsInfinity(top))
        {
            return;
        }

        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _pendingWidgetLeft = left;
            _pendingWidgetTop = top;

            _debounceTimer.Change(_debounceDelay, Timeout.InfiniteTimeSpan);
        }
    }

    /// <inheritdoc />
    public async Task FlushPendingSaveAsync()
    {
        Task? activeSave;

        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _debounceTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            activeSave = _currentSaveTask;
        }

        if (activeSave is not null)
        {
            try
            {
                await activeSave.ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Background save failure logged/handled within active task
            }
        }

        UserSettings? toSave = null;
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            if (HasPendingChangesLocked())
            {
                toSave = BuildUpdatedSettingsLocked();
                ClearPendingChangesLocked();
            }
        }

        if (toSave is not null)
        {
            Task flushTask = ExecuteSaveAsync(toSave);
            lock (_lock)
            {
                _currentSaveTask = flushTask;
            }

            await flushTask.ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _debounceTimer.Dispose();
            ClearPendingChangesLocked();
        }
    }

    /// <summary>
    /// Gets the current active save task for test synchronization.
    /// </summary>
    internal Task? CurrentSaveTaskForTesting
    {
        get
        {
            lock (_lock)
            {
                return _currentSaveTask;
            }
        }
    }

    private void OnDebounceTimerElapsed(object? state)
    {
        UserSettings? toSave = null;
        lock (_lock)
        {
            if (_disposed || !HasPendingChangesLocked())
            {
                return;
            }

            toSave = BuildUpdatedSettingsLocked();
            ClearPendingChangesLocked();
        }

        if (toSave is not null)
        {
            Task saveTask = ExecuteSaveAsync(toSave);
            lock (_lock)
            {
                _currentSaveTask = saveTask;
            }
        }
    }

    private async Task ExecuteSaveAsync(UserSettings toSave)
    {
        try
        {
            await _settingsService.SaveSettingsAsync(toSave).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Best-effort debounced persistence
        }
    }

    private bool HasPendingChangesLocked()
    {
        return _pendingMainLeft.HasValue ||
               _pendingMainTop.HasValue ||
               _pendingMainWidth.HasValue ||
               _pendingMainHeight.HasValue ||
               _pendingWidgetLeft.HasValue ||
               _pendingWidgetTop.HasValue;
    }

    private UserSettings BuildUpdatedSettingsLocked()
    {
        UserSettings updated = _settingsService.CurrentSettings.Clone();

        if (_pendingMainLeft.HasValue) updated.MainWindowLeft = _pendingMainLeft.Value;
        if (_pendingMainTop.HasValue) updated.MainWindowTop = _pendingMainTop.Value;
        if (_pendingMainWidth.HasValue) updated.MainWindowWidth = _pendingMainWidth.Value;
        if (_pendingMainHeight.HasValue) updated.MainWindowHeight = _pendingMainHeight.Value;

        if (_pendingWidgetLeft.HasValue) updated.WidgetWindowLeft = _pendingWidgetLeft.Value;
        if (_pendingWidgetTop.HasValue) updated.WidgetWindowTop = _pendingWidgetTop.Value;

        return updated;
    }

    private void ClearPendingChangesLocked()
    {
        _pendingMainLeft = null;
        _pendingMainTop = null;
        _pendingMainWidth = null;
        _pendingMainHeight = null;
        _pendingWidgetLeft = null;
        _pendingWidgetTop = null;
    }
}
