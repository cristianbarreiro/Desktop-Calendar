using System.Windows;
using System.Windows.Threading;

namespace CalendarWidget.Presentation.Services;

/// <summary>
/// System clock service implementation that raises <see cref="IClockService.TimeChanged"/> every second.
/// </summary>
public sealed class SystemClockService : IClockService, IDisposable
{
    private readonly DispatcherTimer? _dispatcherTimer;
    private readonly Timer? _fallbackTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemClockService"/> class.
    /// </summary>
    public SystemClockService()
    {
        if (Application.Current?.Dispatcher is not null)
        {
            _dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal, Application.Current.Dispatcher)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _dispatcherTimer.Tick += OnDispatcherTimerTick;
            _dispatcherTimer.Start();
        }
        else
        {
            _fallbackTimer = new Timer(OnFallbackTimerTick, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }
    }

    /// <inheritdoc />
    public DateTime Now => DateTime.Now;

    /// <inheritdoc />
    public event EventHandler<DateTime>? TimeChanged;

    private void OnDispatcherTimerTick(object? sender, EventArgs e)
    {
        TimeChanged?.Invoke(this, Now);
    }

    private void OnFallbackTimerTick(object? state)
    {
        TimeChanged?.Invoke(this, Now);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_dispatcherTimer is not null)
        {
            _dispatcherTimer.Stop();
            _dispatcherTimer.Tick -= OnDispatcherTimerTick;
        }

        _fallbackTimer?.Dispose();
    }
}
