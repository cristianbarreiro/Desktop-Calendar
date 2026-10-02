using CalendarWidget.App.Services;
using FluentAssertions;

namespace CalendarWidget.UnitTests.App;

public sealed class DisplayAreaDpiTests
{
    [Theory]
    [InlineData(96, 1920, 1080)]
    [InlineData(120, 2400, 1350)]
    [InlineData(144, 2880, 1620)]
    public void FromDevicePixels_WhenDpiScaleChanges_ReturnsWpfDeviceIndependentUnits(
        double dpi,
        double deviceWidth,
        double deviceHeight)
    {
        DisplayArea actual = DisplayArea.FromDevicePixels(
            -deviceWidth,
            0,
            deviceWidth,
            deviceHeight,
            dpi,
            dpi);

        actual.Left.Should().Be(-1920);
        actual.Top.Should().Be(0);
        actual.Width.Should().Be(1920);
        actual.Height.Should().Be(1080);
    }
}
