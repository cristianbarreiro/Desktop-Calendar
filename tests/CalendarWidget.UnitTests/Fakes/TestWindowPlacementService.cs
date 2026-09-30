using CalendarWidget.App.Services;

namespace CalendarWidget.UnitTests.Fakes;

public sealed class TestWindowPlacementService : IWindowPlacementService
{
    public int ApplyMainWindowBoundsCallCount { get; private set; }
    public int ApplyWidgetWindowBoundsCallCount { get; private set; }
    public int OnMainWindowBoundsChangedCallCount { get; private set; }
    public int OnWidgetWindowBoundsChangedCallCount { get; private set; }
    public int FlushPendingSaveCallCount { get; private set; }

    public void ApplyMainWindowBounds(IManagedWindow window)
    {
        ApplyMainWindowBoundsCallCount++;
    }

    public void ApplyWidgetWindowBounds(IManagedWindow window)
    {
        ApplyWidgetWindowBoundsCallCount++;
    }

    public void OnMainWindowBoundsChanged(double left, double top, double width, double height)
    {
        OnMainWindowBoundsChangedCallCount++;
    }

    public void OnWidgetWindowBoundsChanged(double left, double top)
    {
        OnWidgetWindowBoundsChangedCallCount++;
    }

    public Task FlushPendingSaveAsync()
    {
        FlushPendingSaveCallCount++;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}
