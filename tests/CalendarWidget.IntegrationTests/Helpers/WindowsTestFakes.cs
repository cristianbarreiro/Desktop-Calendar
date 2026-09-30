using System.Windows;
using CalendarWidget.App.Services;
using Microsoft.Extensions.Hosting;

namespace CalendarWidget.IntegrationTests.Helpers;

public sealed class TestManagedWindow : IManagedWindow
{
    public bool IsVisible { get; set; }
    public WindowState WindowState { get; set; } = WindowState.Normal;
    public bool Topmost { get; set; }
    public double Opacity { get; set; } = 1.0;
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public WindowStartupLocation WindowStartupLocation { get; set; } = WindowStartupLocation.Manual;

    public int ShowCallCount { get; private set; }
    public int HideCallCount { get; private set; }
    public int ActivateCallCount { get; private set; }

    public event EventHandler? Closed;

    public void Show()
    {
        IsVisible = true;
        ShowCallCount++;
    }

    public void Hide()
    {
        IsVisible = false;
        HideCallCount++;
    }

    public bool Activate()
    {
        ActivateCallCount++;
        return true;
    }

    public void SimulateClose()
    {
        IsVisible = false;
        Closed?.Invoke(this, EventArgs.Empty);
    }
}

public sealed class TestDisplayMonitorProvider : IDisplayMonitorProvider
{
    public List<DisplayArea> Displays { get; set; } = [new(0, 0, 1920, 1080)];
    public DisplayArea Primary { get; set; } = new(0, 0, 1920, 1080);

    public IReadOnlyList<DisplayArea> GetDisplayAreas() => Displays;
    public DisplayArea GetPrimaryDisplayArea() => Primary;
}

public sealed class TestHostApplicationLifetime : IHostApplicationLifetime
{
    private readonly CancellationTokenSource _startedCts = new();
    private readonly CancellationTokenSource _stoppingCts = new();
    private readonly CancellationTokenSource _stoppedCts = new();

    public int StopApplicationCallCount { get; private set; }

    public CancellationToken ApplicationStarted => _startedCts.Token;
    public CancellationToken ApplicationStopping => _stoppingCts.Token;
    public CancellationToken ApplicationStopped => _stoppedCts.Token;

    public void StopApplication()
    {
        StopApplicationCallCount++;
        _stoppingCts.Cancel();
    }
}
