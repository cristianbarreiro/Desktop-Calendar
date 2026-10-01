using CalendarWidget.App.Services;
using CalendarWidget.IntegrationTests.Helpers;
using FluentAssertions;

namespace CalendarWidget.IntegrationTests.Windows;

[Collection(WpfTestCollection.Name)]
public sealed class SingleInstanceAndLifecycleIntegrationTests
{
    [Fact]
    public async Task SingleInstance_FirstAcquiresMutex_SecondFailsAndSignalsPrimary()
    {
        string uniqueId = Guid.NewGuid().ToString("N");
        string mutexName = $"Local\\CW_Test_Mutex_{uniqueId}";
        string pipeName = $"CW_Test_Pipe_{uniqueId}";

        TaskCompletionSource<bool> activatedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        // Instance 1: Primary
        using SingleInstanceCoordinator primary = new(
            onActivateRequested: () => activatedTcs.TrySetResult(true),
            mutexName: mutexName,
            pipeName: pipeName);

        primary.IsPrimary.Should().BeTrue();
        primary.StartListening();

        // Instance 2: Secondary launch attempts to start
        using SingleInstanceCoordinator secondary = new(
            mutexName: mutexName,
            pipeName: pipeName);

        secondary.IsPrimary.Should().BeFalse();

        // Act: Secondary signals primary and exits
        bool signalSent = secondary.SignalPrimary(timeoutMs: 3000);
        signalSent.Should().BeTrue();

        // Assert: Primary received activation
        bool activated = await activatedTcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
        activated.Should().BeTrue();
    }

    [Fact]
    public void PrimaryInstance_OnShutdown_ReleasesMutex_AllowingThirdInstanceToStart()
    {
        string uniqueId = Guid.NewGuid().ToString("N");
        string mutexName = $"Local\\CW_Test_Mutex_{uniqueId}";
        string pipeName = $"CW_Test_Pipe_{uniqueId}";

        // Instance 1: Primary starts
        SingleInstanceCoordinator primary = new(
            mutexName: mutexName,
            pipeName: pipeName);

        primary.IsPrimary.Should().BeTrue();

        // Simulate shutdown of primary
        primary.Dispose();

        // Instance 2 launches after primary shutdown
        using SingleInstanceCoordinator nextInstance = new(
            mutexName: mutexName,
            pipeName: pipeName);

        // Assert: Mutex was released and next instance successfully becomes primary
        nextInstance.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void WindowManager_LifecycleSwitching_HidesPreviousAndShowsNext()
    {
        TestHostApplicationLifetime hostLifetime = new();
        ApplicationLifetimeService lifetimeService = new(hostLifetime);

        TestManagedWindow mainWindow = new();
        TestManagedWindow widgetWindow = new();

        WindowManager windowManager = new(
            () => mainWindow,
            () => widgetWindow,
            lifetimeService);

        // Act 1: Initial display shows widget
        windowManager.ShowWidget();
        windowManager.IsWidgetVisible.Should().BeTrue();
        windowManager.IsFullApplicationVisible.Should().BeFalse();
        widgetWindow.IsVisible.Should().BeTrue();

        // Act 2: Switch to full application
        windowManager.ShowFullApplication();
        windowManager.IsFullApplicationVisible.Should().BeTrue();
        windowManager.IsWidgetVisible.Should().BeFalse();
        mainWindow.IsVisible.Should().BeTrue();
        widgetWindow.IsVisible.Should().BeFalse();

        // Act 3: Switch back to widget
        windowManager.ShowWidget();
        windowManager.IsWidgetVisible.Should().BeTrue();
        windowManager.IsFullApplicationVisible.Should().BeFalse();
        widgetWindow.IsVisible.Should().BeTrue();
        mainWindow.IsVisible.Should().BeFalse();

        // Assert: process should not be shutting down during window switches
        lifetimeService.IsShuttingDown.Should().BeFalse();
    }

    [Fact]
    public void WindowManager_MinimizeWidget_HidesWindowForTrayStateWithoutExiting()
    {
        TestHostApplicationLifetime hostLifetime = new();
        ApplicationLifetimeService lifetimeService = new(hostLifetime);

        TestManagedWindow mainWindow = new();
        TestManagedWindow widgetWindow = new();

        WindowManager windowManager = new(
            () => mainWindow,
            () => widgetWindow,
            lifetimeService);

        windowManager.ShowWidget();
        widgetWindow.IsVisible.Should().BeTrue();

        // Act: minimize widget to tray
        windowManager.MinimizeWidget();

        // Assert: window hidden/minimized, but application lifetime is still active (not shutting down)
        widgetWindow.IsVisible.Should().BeFalse();
        widgetWindow.WindowState.Should().Be(System.Windows.WindowState.Minimized);
        lifetimeService.IsShuttingDown.Should().BeFalse();
        hostLifetime.StopApplicationCallCount.Should().Be(0);

        // Act 2: Activate restores widget
        windowManager.ActivateCurrentWindow();
        widgetWindow.IsVisible.Should().BeTrue();
        widgetWindow.WindowState.Should().Be(System.Windows.WindowState.Normal);
    }

    [Fact]
    public void WindowManager_WindowClose_WhenNotSwitching_TriggersShutdown()
    {
        TestHostApplicationLifetime hostLifetime = new();
        ApplicationLifetimeService lifetimeService = new(hostLifetime);

        TestManagedWindow mainWindow = new();
        TestManagedWindow widgetWindow = new();

        WindowManager windowManager = new(
            () => mainWindow,
            () => widgetWindow,
            lifetimeService);

        windowManager.ShowWidget();
        widgetWindow.IsVisible.Should().BeTrue();

        // Act: user closes window
        widgetWindow.SimulateClose();

        // Assert: application lifetime service shutdown triggered
        lifetimeService.IsShuttingDown.Should().BeTrue();
        hostLifetime.StopApplicationCallCount.Should().Be(1);
    }
}
