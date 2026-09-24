using CalendarWidget.Presentation.Services;

namespace CalendarWidget.UnitTests.Fakes;

public sealed class TestWindowManager : IWindowManager
{
    public int ShowFullApplicationCallCount { get; private set; }
    public int ShowWidgetCallCount { get; private set; }
    public int MinimizeWidgetCallCount { get; private set; }

    public bool IsFullApplicationVisible { get; set; }
    public bool IsWidgetVisible { get; set; }

    public void ShowFullApplication()
    {
        ShowFullApplicationCallCount++;
        IsFullApplicationVisible = true;
        IsWidgetVisible = false;
    }

    public void ShowWidget()
    {
        ShowWidgetCallCount++;
        IsWidgetVisible = true;
        IsFullApplicationVisible = false;
    }

    public void MinimizeWidget()
    {
        MinimizeWidgetCallCount++;
    }
}
