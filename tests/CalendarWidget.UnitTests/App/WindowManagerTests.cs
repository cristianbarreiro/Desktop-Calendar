using System.Windows;
using CalendarWidget.App.Services;
using CalendarWidget.UnitTests.Fakes;
using FluentAssertions;

namespace CalendarWidget.UnitTests.App;

public sealed class WindowManagerTests
{
    private readonly TestHostApplicationLifetime _hostLifetime = new();
    private readonly ApplicationLifetimeService _lifetimeService;
    private readonly TestManagedWindow _mainWindow = new();
    private readonly TestManagedWindow _widgetWindow = new();
    private int _mainWindowFactoryCalls;
    private int _widgetWindowFactoryCalls;
    private readonly WindowManager _sut;

    public WindowManagerTests()
    {
        _lifetimeService = new ApplicationLifetimeService(_hostLifetime);
        _sut = new WindowManager(
            mainWindowFactory: () =>
            {
                _mainWindowFactoryCalls++;
                return _mainWindow;
            },
            widgetWindowFactory: () =>
            {
                _widgetWindowFactoryCalls++;
                return _widgetWindow;
            },
            lifetimeService: _lifetimeService);
    }

    [Fact]
    public void WindowManager_InitialVisibilityStates_AreFalse()
    {
        // Assert
        _sut.IsFullApplicationVisible.Should().BeFalse();
        _sut.IsWidgetVisible.Should().BeFalse();
    }

    [Fact]
    public void ShowWidget_WhenCalled_CreatesAndShowsWidgetAndActivatesIt()
    {
        // Act
        _sut.ShowWidget();

        // Assert
        _sut.IsWidgetVisible.Should().BeTrue();
        _sut.IsFullApplicationVisible.Should().BeFalse();
        _widgetWindow.ShowCallCount.Should().Be(1);
        _widgetWindow.ActivateCallCount.Should().Be(1);
        _widgetWindowFactoryCalls.Should().Be(1);
    }

    [Fact]
    public void ShowFullApplication_WhenCalled_CreatesAndShowsMainWindowAndActivatesIt()
    {
        // Act
        _sut.ShowFullApplication();

        // Assert
        _sut.IsFullApplicationVisible.Should().BeTrue();
        _sut.IsWidgetVisible.Should().BeFalse();
        _mainWindow.ShowCallCount.Should().Be(1);
        _mainWindow.ActivateCallCount.Should().Be(1);
        _mainWindowFactoryCalls.Should().Be(1);
    }

    [Fact]
    public void ShowFullApplication_WhenWidgetVisible_HidesWidgetAndShowsMainWindow()
    {
        // Arrange
        _sut.ShowWidget();
        _sut.IsWidgetVisible.Should().BeTrue();

        // Act
        _sut.ShowFullApplication();

        // Assert
        _sut.IsWidgetVisible.Should().BeFalse();
        _sut.IsFullApplicationVisible.Should().BeTrue();
        _widgetWindow.HideCallCount.Should().Be(1);
        _mainWindow.ShowCallCount.Should().Be(1);
        _lifetimeService.IsShuttingDown.Should().BeFalse();
    }

    [Fact]
    public void ShowWidget_WhenFullApplicationVisible_HidesMainWindowAndShowsWidget()
    {
        // Arrange
        _sut.ShowFullApplication();
        _sut.IsFullApplicationVisible.Should().BeTrue();

        // Act
        _sut.ShowWidget();

        // Assert
        _sut.IsFullApplicationVisible.Should().BeFalse();
        _sut.IsWidgetVisible.Should().BeTrue();
        _mainWindow.HideCallCount.Should().Be(1);
        _widgetWindow.ShowCallCount.Should().Be(1);
        _lifetimeService.IsShuttingDown.Should().BeFalse();
    }

    [Fact]
    public void MinimizeWidget_WhenInvoked_SetsWidgetWindowStateToMinimizedWithoutShutdown()
    {
        // Arrange
        _sut.ShowWidget();

        // Act
        _sut.MinimizeWidget();

        // Assert
        _widgetWindow.WindowState.Should().Be(WindowState.Minimized);
        _lifetimeService.IsShuttingDown.Should().BeFalse();
    }

    [Fact]
    public void RepeatedShowWidget_ReusesExistingWindow_DoesNotCallFactoryAgain()
    {
        // Act
        _sut.ShowWidget();
        _sut.ShowWidget();
        _sut.ShowWidget();

        // Assert
        _widgetWindowFactoryCalls.Should().Be(1);
        _sut.IsWidgetVisible.Should().BeTrue();
    }

    [Fact]
    public void RepeatedShowFullApplication_ReusesExistingWindow_DoesNotCallFactoryAgain()
    {
        // Act
        _sut.ShowFullApplication();
        _sut.ShowFullApplication();

        // Assert
        _mainWindowFactoryCalls.Should().Be(1);
        _sut.IsFullApplicationVisible.Should().BeTrue();
    }

    [Fact]
    public void RepeatedSwitching_DoesNotCreateDuplicateActiveStateOrShutdown()
    {
        // Act: switch back and forth multiple times
        _sut.ShowWidget();
        _sut.ShowFullApplication();
        _sut.ShowWidget();
        _sut.ShowFullApplication();

        // Assert
        _widgetWindowFactoryCalls.Should().Be(1);
        _mainWindowFactoryCalls.Should().Be(1);
        _sut.IsFullApplicationVisible.Should().BeTrue();
        _sut.IsWidgetVisible.Should().BeFalse();
        _lifetimeService.IsShuttingDown.Should().BeFalse();
    }

    [Fact]
    public void CloseMainWindow_WhenNotSwitching_TriggersApplicationShutdownAndCleansUpReference()
    {
        // Arrange
        _sut.ShowFullApplication();

        // Act: simulate real window close
        _mainWindow.SimulateClose();

        // Assert
        _sut.IsFullApplicationVisible.Should().BeFalse();
        _lifetimeService.IsShuttingDown.Should().BeTrue();
        _hostLifetime.StopApplicationCallCount.Should().Be(1);
    }

    [Fact]
    public void CloseWidgetWindow_WhenNotSwitching_TriggersApplicationShutdownAndCleansUpReference()
    {
        // Arrange
        _sut.ShowWidget();

        // Act: simulate real widget window close
        _widgetWindow.SimulateClose();

        // Assert
        _sut.IsWidgetVisible.Should().BeFalse();
        _lifetimeService.IsShuttingDown.Should().BeTrue();
        _hostLifetime.StopApplicationCallCount.Should().Be(1);
    }

    [Fact]
    public void CloseMainWindow_AllowsRecreationOnSubsequentShow()
    {
        // Arrange
        _sut.ShowFullApplication();
        _mainWindow.SimulateClose();
        _mainWindowFactoryCalls.Should().Be(1);

        // Act: if shown again, a new window instance is created via factory
        _sut.ShowFullApplication();

        // Assert
        _mainWindowFactoryCalls.Should().Be(2);
    }
}
