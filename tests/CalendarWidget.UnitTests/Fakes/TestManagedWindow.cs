using System.Windows;
using CalendarWidget.App.Services;

namespace CalendarWidget.UnitTests.Fakes;

public sealed class TestManagedWindow : IManagedWindow
{
    public bool IsVisible { get; set; }

    public WindowState WindowState { get; set; } = WindowState.Normal;
    public bool Topmost { get; set; }
    public double Opacity { get; set; } = 1.0;

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
