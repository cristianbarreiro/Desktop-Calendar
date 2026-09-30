using CalendarWidget.App.Services;
using CalendarWidget.UnitTests.Fakes;
using FluentAssertions;

namespace CalendarWidget.UnitTests.App;

public sealed class SystemTrayServiceTests
{
    private readonly TestWindowManager _windowManager = new();
    private readonly TestHostApplicationLifetime _hostLifetime = new();
    private readonly ApplicationLifetimeService _lifetimeService;

    public SystemTrayServiceTests()
    {
        _lifetimeService = new ApplicationLifetimeService(_hostLifetime);
    }

    [Fact]
    public void Initialize_CreatesTrayIconAndSetsVisibleTrue()
    {
        // Arrange
        using SystemTrayService sut = new(_windowManager, _lifetimeService);

        // Act
        sut.Initialize();

        // Assert
        sut.IsVisible.Should().BeTrue();
    }

    [Fact]
    public void Dispose_HidesTrayIconAndCleansUp()
    {
        // Arrange
        SystemTrayService sut = new(_windowManager, _lifetimeService);
        sut.Initialize();
        sut.IsVisible.Should().BeTrue();

        // Act
        sut.Dispose();

        // Assert
        sut.IsVisible.Should().BeFalse();
    }

    [Fact]
    public void DoubleDispose_IsSafeAndDoesNotThrow()
    {
        // Arrange
        SystemTrayService sut = new(_windowManager, _lifetimeService);
        sut.Initialize();

        // Act & Assert
        sut.Dispose();
        sut.Invoking(s => s.Dispose()).Should().NotThrow();
    }

    [Fact]
    public void Initialize_CalledMultipleTimes_IsIdempotentAndDoesNotThrow()
    {
        // Arrange
        using SystemTrayService sut = new(_windowManager, _lifetimeService);

        // Act & Assert
        sut.Initialize();
        sut.Invoking(s => s.Initialize()).Should().NotThrow();
        sut.IsVisible.Should().BeTrue();
    }

    [Fact]
    public void Dispose_WhenNeverInitialized_DoesNotThrow()
    {
        // Arrange
        SystemTrayService sut = new(_windowManager, _lifetimeService);

        // Act & Assert
        sut.Invoking(s => s.Dispose()).Should().NotThrow();
        sut.IsVisible.Should().BeFalse();
    }
}
