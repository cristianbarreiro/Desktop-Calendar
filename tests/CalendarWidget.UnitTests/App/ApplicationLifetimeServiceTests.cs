using CalendarWidget.App.Services;
using CalendarWidget.UnitTests.Fakes;
using FluentAssertions;

namespace CalendarWidget.UnitTests.App;

public sealed class ApplicationLifetimeServiceTests
{
    private readonly TestHostApplicationLifetime _hostLifetime = new();
    private readonly ApplicationLifetimeService _sut;

    public ApplicationLifetimeServiceTests()
    {
        _sut = new ApplicationLifetimeService(_hostLifetime);
    }

    [Fact]
    public void IsShuttingDown_Initially_IsFalse()
    {
        // Assert
        _sut.IsShuttingDown.Should().BeFalse();
        _hostLifetime.StopApplicationCallCount.Should().Be(0);
    }

    [Fact]
    public void Shutdown_WhenInvoked_StopsGenericHostAndSetsIsShuttingDown()
    {
        // Act
        _sut.Shutdown();

        // Assert
        _sut.IsShuttingDown.Should().BeTrue();
        _hostLifetime.StopApplicationCallCount.Should().Be(1);
    }

    [Fact]
    public void Shutdown_WhenCalledMultipleTimes_OnlyStopsHostOnce()
    {
        // Act
        _sut.Shutdown();
        _sut.Shutdown();
        _sut.Shutdown();

        // Assert
        _sut.IsShuttingDown.Should().BeTrue();
        _hostLifetime.StopApplicationCallCount.Should().Be(1);
    }
}
