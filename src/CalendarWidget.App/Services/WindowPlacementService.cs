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

    private double? _lastAcceptedMainLeft;
    private double? _lastAcceptedMainTop;
    private double? _lastAcceptedMainWidth;
    private double? _lastAcceptedMainHeight;

    private double? _lastAcceptedWidgetLeft;
    private double? _lastAcceptedWidgetTop;

    private Task _activeSaveTask = Task.CompletedTask;
    private bool _isSaveLoopRunning;
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

            _lastAcceptedMainLeft = left;
            _lastAcceptedMainTop = top;
            _lastAcceptedMainWidth = width;
            _lastAcceptedMainHeight = height;

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

            _lastAcceptedWidgetLeft = left;
            _lastAcceptedWidgetTop = top;

            _pendingWidgetLeft = left;
            _pendingWidgetTop = top;

            _debounceTimer.Change(_debounceDelay, Timeout.InfiniteTimeSpan);
        }
    }

    /// <inheritdoc />
    public async Task FlushPendingSaveAsync()
    {
        while (true)
        {
            Task saveTaskToAwait;
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                _debounceTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                EnsureSaveLoopRunningLocked();
                saveTaskToAwait = _activeSaveTask;
            }

            try
            {
                await saveTaskToAwait.ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Handled in SaveLoopAsync
            }

            lock (_lock)
            {
                if (_disposed || (!HasPendingChangesLocked() && !_isSaveLoopRunning))
                {
                    return;
                }
            }
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
                return _activeSaveTask;
            }
        }
    }

    /// <summary>
    /// Hook invoked before persisting placement, for deterministic concurrency testing.
    /// </summary>
    internal Func<Task>? BeforePersistHookForTesting { get; set; }

    /// <summary>
    /// Hook invoked after persisting placement, for deterministic concurrency testing.
    /// </summary>
    internal Func<Task>? AfterPersistHookForTesting { get; set; }

    private void OnDebounceTimerElapsed(object? state)
    {
        lock (_lock)
        {
            if (_disposed || !HasPendingChangesLocked())
            {
                return;
            }

            EnsureSaveLoopRunningLocked();
        }
    }

    private void EnsureSaveLoopRunningLocked()
    {
        if (_disposed || !HasPendingChangesLocked())
        {
            return;
        }

        if (!_isSaveLoopRunning)
        {
            _isSaveLoopRunning = true;
            _activeSaveTask = Task.Run(SaveLoopAsync);
        }
    }

    private async Task SaveLoopAsync()
    {
        try
        {
            while (true)
            {
                PendingPlacement toSave;
                lock (_lock)
                {
                    if (_disposed || !HasPendingChangesLocked())
                    {
                        _isSaveLoopRunning = false;
                        return;
                    }

                    toSave = ExtractPendingChangesLocked();
                }

                try
                {
                    if (BeforePersistHookForTesting is not null)
                    {
                        await BeforePersistHookForTesting().ConfigureAwait(false);
                    }

                    await PersistPlacementAsync(toSave).ConfigureAwait(false);

                    if (AfterPersistHookForTesting is not null)
                    {
                        await AfterPersistHookForTesting().ConfigureAwait(false);
                    }
                }
                catch (Exception)
                {
                    // Best-effort debounced persistence
                }
            }
        }
        finally
        {
            lock (_lock)
            {
                _isSaveLoopRunning = false;
            }
        }
    }

    private async Task PersistPlacementAsync(PendingPlacement toSave)
    {
        await _settingsService.MutateSettingsAsync(settings =>
        {
            if (toSave.MainLeft.HasValue) settings.MainWindowLeft = toSave.MainLeft.Value;
            if (toSave.MainTop.HasValue) settings.MainWindowTop = toSave.MainTop.Value;
            if (toSave.MainWidth.HasValue) settings.MainWindowWidth = toSave.MainWidth.Value;
            if (toSave.MainHeight.HasValue) settings.MainWindowHeight = toSave.MainHeight.Value;

            if (toSave.WidgetLeft.HasValue) settings.WidgetWindowLeft = toSave.WidgetLeft.Value;
            if (toSave.WidgetTop.HasValue) settings.WidgetWindowTop = toSave.WidgetTop.Value;
        }).ConfigureAwait(false);
    }

    private bool HasPendingChangesLocked()
    {
        if (_pendingMainLeft.HasValue ||
            _pendingMainTop.HasValue ||
            _pendingMainWidth.HasValue ||
            _pendingMainHeight.HasValue ||
            _pendingWidgetLeft.HasValue ||
            _pendingWidgetTop.HasValue)
        {
            return true;
        }

        UserSettings current = _settingsService.CurrentSettings;
        if (_lastAcceptedMainLeft.HasValue && current.MainWindowLeft != _lastAcceptedMainLeft.Value) return true;
        if (_lastAcceptedMainTop.HasValue && current.MainWindowTop != _lastAcceptedMainTop.Value) return true;
        if (_lastAcceptedMainWidth.HasValue && current.MainWindowWidth != _lastAcceptedMainWidth.Value) return true;
        if (_lastAcceptedMainHeight.HasValue && current.MainWindowHeight != _lastAcceptedMainHeight.Value) return true;
        if (_lastAcceptedWidgetLeft.HasValue && current.WidgetWindowLeft != _lastAcceptedWidgetLeft.Value) return true;
        if (_lastAcceptedWidgetTop.HasValue && current.WidgetWindowTop != _lastAcceptedWidgetTop.Value) return true;

        return false;
    }

    private PendingPlacement ExtractPendingChangesLocked()
    {
        UserSettings current = _settingsService.CurrentSettings;

        double? mainLeft = _pendingMainLeft ?? (_lastAcceptedMainLeft.HasValue && current.MainWindowLeft != _lastAcceptedMainLeft.Value ? _lastAcceptedMainLeft : null);
        double? mainTop = _pendingMainTop ?? (_lastAcceptedMainTop.HasValue && current.MainWindowTop != _lastAcceptedMainTop.Value ? _lastAcceptedMainTop : null);
        double? mainWidth = _pendingMainWidth ?? (_lastAcceptedMainWidth.HasValue && current.MainWindowWidth != _lastAcceptedMainWidth.Value ? _lastAcceptedMainWidth : null);
        double? mainHeight = _pendingMainHeight ?? (_lastAcceptedMainHeight.HasValue && current.MainWindowHeight != _lastAcceptedMainHeight.Value ? _lastAcceptedMainHeight : null);

        double? widgetLeft = _pendingWidgetLeft ?? (_lastAcceptedWidgetLeft.HasValue && current.WidgetWindowLeft != _lastAcceptedWidgetLeft.Value ? _lastAcceptedWidgetLeft : null);
        double? widgetTop = _pendingWidgetTop ?? (_lastAcceptedWidgetTop.HasValue && current.WidgetWindowTop != _lastAcceptedWidgetTop.Value ? _lastAcceptedWidgetTop : null);

        ClearPendingChangesLocked();
        return new PendingPlacement(mainLeft, mainTop, mainWidth, mainHeight, widgetLeft, widgetTop);
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

    private sealed record PendingPlacement(
        double? MainLeft,
        double? MainTop,
        double? MainWidth,
        double? MainHeight,
        double? WidgetLeft,
        double? WidgetTop);
}
