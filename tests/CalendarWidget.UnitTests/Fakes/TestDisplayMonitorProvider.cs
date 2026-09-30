using CalendarWidget.App.Services;

namespace CalendarWidget.UnitTests.Fakes;

public sealed class TestDisplayMonitorProvider : IDisplayMonitorProvider
{
    public List<DisplayArea> Displays { get; set; } = [new(0, 0, 1920, 1080)];
    public DisplayArea Primary { get; set; } = new(0, 0, 1920, 1080);

    public IReadOnlyList<DisplayArea> GetDisplayAreas() => Displays;
    public DisplayArea GetPrimaryDisplayArea() => Primary;
}
