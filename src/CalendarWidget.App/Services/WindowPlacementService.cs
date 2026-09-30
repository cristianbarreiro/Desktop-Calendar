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
    private readonly Lock _lock = new();

    private double? _pendingMainLeft;
    private double? _pendingMainTop;
    private double? _pendingMainWidth;
    private double? _pendingMainHeight;

    private double? _pendingWidgetLeft;
    private double? _pendingWidgetTop;

    private CancellationTokenSource? _debounceCts;
    private Task? _debounceTask;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowPlacementService"/> class.
    /// </summary>
    /// <param name="settingsService">Settings persistence service.</param>
    /// <param name="displayProvider">Display monitor provider.</param>
    /// <param name="debounceDelay">Optional debounce delay override for testing.</param>
    public WindowPlacementService(
        ISettingsService settingsService,
        IDisplayMonitorProvider displayProvider,
        TimeSpan? debounceDelay = null)
    {
        _settingsService = settingsService;
        _displayProvider = displayProvider;
        _debounceDelay = debounceDelay ?? DefaultDebounceDelay;
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

            ScheduleDebouncedSaveLocked();
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

            ScheduleDebouncedSaveLocked();
        }
    }

    /// <inheritdoc />
    public async Task FlushPendingSaveAsync()
    {
        CancellationTokenSource? oldCts;
        UserSettings? toSave = null;

        lock (_lock)
        {
            oldCts = _debounceCts;
            _debounceCts = null;

            if (HasPendingChangesLocked())
            {
                toSave = BuildUpdatedSettingsLocked();
                ClearPendingChangesLocked();
            }
        }

        if (oldCts is not null)
        {
            try
            {
                await oldCts.CancelAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                oldCts.Dispose();
            }
        }

        if (toSave is not null)
        {
            await _settingsService.SaveSettingsAsync(toSave).ConfigureAwait(false);
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
            try
            {
                _debounceCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                _debounceCts?.Dispose();
                _debounceCts = null;
            }
        }
    }

    private void ScheduleDebouncedSaveLocked()
    {
        try
        {
            _debounceCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            _debounceCts?.Dispose();
        }

        _debounceCts = new CancellationTokenSource();
        CancellationToken token = _debounceCts.Token;

        _debounceTask = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_debounceDelay, token).ConfigureAwait(false);

                UserSettings? toSave = null;
                lock (_lock)
                {
                    try
                    {
                        if (token.IsCancellationRequested || _disposed)
                        {
                            return;
                        }
                    }
                    catch (ObjectDisposedException)
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
                    await _settingsService.SaveSettingsAsync(toSave, token).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
            {
                // Expected when new changes coalesce or during disposal
            }
        }, token);
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
