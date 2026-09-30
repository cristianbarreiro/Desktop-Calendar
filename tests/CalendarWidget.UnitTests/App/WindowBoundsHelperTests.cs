using CalendarWidget.App.Services;
using FluentAssertions;

namespace CalendarWidget.UnitTests.App;

public sealed class WindowBoundsHelperTests
{
    private readonly DisplayArea _primaryDisplay = new(0, 0, 1920, 1080);

    [Fact]
    public void EnsureVisible_WhenBoundsInsideSingleMonitor_PreservesRequestedBounds()
    {
        // Arrange
        WindowBounds requested = new(100, 100, 960, 620);
        DisplayArea[] displays = [_primaryDisplay];

        // Act
        WindowBounds actual = WindowBoundsHelper.EnsureVisible(requested, displays, _primaryDisplay);

        // Assert
        actual.Left.Should().Be(100);
        actual.Top.Should().Be(100);
        actual.Width.Should().Be(960);
        actual.Height.Should().Be(620);
    }

    [Fact]
    public void EnsureVisible_WhenBoundsCompletelyOffScreenNegativeCoordinates_RelocatesToDefaultCenter()
    {
        // Canonical specification test scenario: (-5000, -5000)
        // Arrange
        WindowBounds requested = new(-5000, -5000, 960, 620);
        DisplayArea[] displays = [_primaryDisplay];

        // Act
        WindowBounds actual = WindowBoundsHelper.EnsureVisible(requested, displays, _primaryDisplay);

        // Assert
        actual.Left.Should().Be((1920 - 960) / 2);
        actual.Top.Should().Be((1080 - 620) / 2);
        actual.Width.Should().Be(960);
        actual.Height.Should().Be(620);
    }

    [Fact]
    public void EnsureVisible_WhenBoundsCompletelyOffScreenPositiveCoordinates_RelocatesToDefaultCenter()
    {
        // Arrange
        WindowBounds requested = new(5000, 5000, 960, 620);
        DisplayArea[] displays = [_primaryDisplay];

        // Act
        WindowBounds actual = WindowBoundsHelper.EnsureVisible(requested, displays, _primaryDisplay);

        // Assert
        actual.Left.Should().Be((1920 - 960) / 2);
        actual.Top.Should().Be((1080 - 620) / 2);
        actual.Width.Should().Be(960);
        actual.Height.Should().Be(620);
    }

    [Fact]
    public void EnsureVisible_WhenSecondaryMonitorWithNegativeCoordinatesConnected_PreservesBounds()
    {
        // Multi-monitor arrangement: secondary monitor to the left with negative X coordinates
        // Arrange
        DisplayArea secondaryLeft = new(-1920, 0, 1920, 1080);
        DisplayArea[] displays = [secondaryLeft, _primaryDisplay];
        WindowBounds requested = new(-1500, 150, 960, 620);

        // Act
        WindowBounds actual = WindowBoundsHelper.EnsureVisible(requested, displays, _primaryDisplay);

        // Assert: valid multi-monitor position preserved without relocation
        actual.Left.Should().Be(-1500);
        actual.Top.Should().Be(150);
        actual.Width.Should().Be(960);
        actual.Height.Should().Be(620);
    }

    [Fact]
    public void EnsureVisible_WhenSecondaryMonitorDisconnected_RecoversToPrimaryCenter()
    {
        // Scenario: window was saved on external monitor (-1920, 0), but cable was disconnected so only primary remains
        // Arrange
        DisplayArea[] displays = [_primaryDisplay];
        WindowBounds requested = new(-1500, 150, 960, 620);

        // Act
        WindowBounds actual = WindowBoundsHelper.EnsureVisible(requested, displays, _primaryDisplay);

        // Assert: recovered onto primary monitor
        actual.Left.Should().Be((1920 - 960) / 2);
        actual.Top.Should().Be((1080 - 620) / 2);
        actual.Width.Should().Be(960);
        actual.Height.Should().Be(620);
    }

    [Fact]
    public void EnsureVisible_WhenPartiallyOffscreenWithSufficientOverlap_PreservesBounds()
    {
        // Arrange: window extends 200px off the right edge, but 760px is still visible (>= 50px threshold)
        WindowBounds requested = new(1160, 100, 960, 620);
        DisplayArea[] displays = [_primaryDisplay];

        // Act
        WindowBounds actual = WindowBoundsHelper.EnsureVisible(requested, displays, _primaryDisplay);

        // Assert
        actual.Left.Should().Be(1160);
        actual.Top.Should().Be(100);
        actual.Width.Should().Be(960);
        actual.Height.Should().Be(620);
    }

    [Fact]
    public void EnsureVisible_WhenTopEdgeAboveScreen_ClampsTopToScreenTopSoTitleBarIsGrabable()
    {
        // Arrange: window is positioned at Top = -50, hiding title bar off the top edge
        WindowBounds requested = new(100, -50, 960, 620);
        DisplayArea[] displays = [_primaryDisplay];

        // Act
        WindowBounds actual = WindowBoundsHelper.EnsureVisible(requested, displays, _primaryDisplay);

        // Assert: top is clamped to 0 so the user can reach the caption bar
        actual.Top.Should().Be(0);
        actual.Left.Should().Be(100);
    }

    [Fact]
    public void EnsureVisible_WhenDisplayAreasEmpty_ReturnsCenteredOnDefaultDisplay()
    {
        // Arrange
        WindowBounds requested = new(200, 200, 800, 600);
        DisplayArea[] displays = [];

        // Act
        WindowBounds actual = WindowBoundsHelper.EnsureVisible(requested, displays, _primaryDisplay);

        // Assert
        actual.Left.Should().Be((1920 - 800) / 2);
        actual.Top.Should().Be((1080 - 600) / 2);
    }

    [Fact]
    public void CenterOnDisplay_WhenWindowLargerThanDisplay_ClampsToDisplaySize()
    {
        // Arrange
        DisplayArea smallDisplay = new(0, 0, 800, 600);

        // Act
        WindowBounds actual = WindowBoundsHelper.CenterOnDisplay(1200, 900, smallDisplay);

        // Assert
        actual.Width.Should().Be(800);
        actual.Height.Should().Be(600);
        actual.Left.Should().Be(0);
        actual.Top.Should().Be(0);
    }

    [Fact]
    public void CenterOnDisplay_NormalDimensions_CalculatesCorrectCenter()
    {
        // Arrange
        DisplayArea display = new(100, 50, 1000, 800);

        // Act
        WindowBounds actual = WindowBoundsHelper.CenterOnDisplay(600, 400, display);

        // Assert
        actual.Left.Should().Be(100 + (1000 - 600) / 2);
        actual.Top.Should().Be(50 + (800 - 400) / 2);
        actual.Width.Should().Be(600);
        actual.Height.Should().Be(400);
    }

    [Theory]
    [InlineData(double.NaN, 100, 800, 600)]
    [InlineData(100, double.NaN, 800, 600)]
    [InlineData(double.PositiveInfinity, 100, 800, 600)]
    [InlineData(100, double.NegativeInfinity, 800, 600)]
    [InlineData(100, 100, double.NaN, 600)]
    [InlineData(100, 100, 800, double.NaN)]
    public void EnsureVisible_WhenBoundsContainNaNOrInfinity_RecoversToDefaultCenter(double left, double top, double width, double height)
    {
        // Arrange
        WindowBounds requested = new(left, top, width, height);
        DisplayArea[] displays = [_primaryDisplay];

        // Act
        WindowBounds actual = WindowBoundsHelper.EnsureVisible(requested, displays, _primaryDisplay);

        // Assert: safely sanitized and centered without NaN or Infinity
        double.IsNaN(actual.Left).Should().BeFalse();
        double.IsNaN(actual.Top).Should().BeFalse();
        double.IsNaN(actual.Width).Should().BeFalse();
        double.IsNaN(actual.Height).Should().BeFalse();
        double.IsInfinity(actual.Left).Should().BeFalse();
        double.IsInfinity(actual.Top).Should().BeFalse();
        actual.Width.Should().BeGreaterThan(0);
        actual.Height.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData(0, 0, 800, 600)]
    [InlineData(-100, 500, 800, 500)]
    [InlineData(800, -200, 800, 600)]
    public void EnsureVisible_WhenDimensionsAreZeroOrNegative_ResetsToSensibleDefaults(double width, double height, double expectedWidth, double expectedHeight)
    {
        // Arrange
        WindowBounds requested = new(100, 100, width, height);
        DisplayArea[] displays = [_primaryDisplay];

        // Act
        WindowBounds actual = WindowBoundsHelper.EnsureVisible(requested, displays, _primaryDisplay);

        // Assert
        actual.Width.Should().Be(expectedWidth);
        actual.Height.Should().Be(expectedHeight);
    }

    [Fact]
    public void EnsureVisible_WhenTopPositionedBelowMonitorBottom_ClampsTopSafely()
    {
        // Arrange: window top is placed near the bottom edge (1070) such that title bar is practically pushed off screen
        WindowBounds requested = new(100, 1070, 800, 600);
        DisplayArea[] displays = [_primaryDisplay];

        // Act
        WindowBounds actual = WindowBoundsHelper.EnsureVisible(requested, displays, _primaryDisplay);

        // Assert: top is clamped so at least 32 DIPs of title bar remain visible
        actual.Top.Should().BeLessThanOrEqualTo(_primaryDisplay.Height - 32);
    }

    [Theory]
    [InlineData(0, 0, 800, 600)]
    [InlineData(-500, 400, 800, 400)]
    [InlineData(double.NaN, double.NaN, 800, 600)]
    public void CenterOnDisplay_WhenRequestedSizeIsInvalidOrZero_UsesSafeFallbackDimensions(double width, double height, double expectedWidth, double expectedHeight)
    {
        // Act
        WindowBounds actual = WindowBoundsHelper.CenterOnDisplay(width, height, _primaryDisplay);

        // Assert
        actual.Width.Should().Be(expectedWidth);
        actual.Height.Should().Be(expectedHeight);
        actual.Left.Should().Be((1920 - expectedWidth) / 2);
        actual.Top.Should().Be((1080 - expectedHeight) / 2);
    }
}
