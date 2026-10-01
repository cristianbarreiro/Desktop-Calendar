using System.Windows;
using CalendarWidget.App.Services;
using CalendarWidget.IntegrationTests.Helpers;
using CalendarWidget.Presentation.Views;
using FluentAssertions;

namespace CalendarWidget.IntegrationTests.Windows;

/// <summary>
/// Regression tests verifying that WPF test dispatcher lifecycle isolation prevents
/// cross-test global state contamination, deadlocks, and abandoned dispatchers.
/// </summary>
[Collection(WpfTestCollection.Name)]
public sealed class WpfDispatcherIsolationRegressionTests
{
    [Fact]
    public void WpfDispatcherIsolation_SequentialContexts_AllowsSubsequentWindowManagerDispatcherExecutionWithoutDeadlock()
    {
        // 1. First WPF test dispatcher/application is created.
        using (WpfTestContext context1 = new())
        {
            // 2. WPF work executes successfully on pumped dispatcher.
            bool wpfWorkCompleted = context1.Invoke(() =>
            {
                NotesView view = new();
                return view is not null;
            });

            wpfWorkCompleted.Should().BeTrue("WPF view instantiation should succeed on pumped dispatcher");
            context1.IsRunning.Should().BeTrue();
            Application.Current.Should().NotBeNull();
            Application.Current.Dispatcher.Should().BeSameAs(context1.Dispatcher);
        }

        // 3. Test infrastructure resets Application.Current deterministically after disposal.
        Application.Current.Should().BeNull("Application.Current must not leak across tests");

        // 4. Another WPF integration test can subsequently execute WindowManager dispatcher-dependent logic.
        using (WpfTestContext context2 = new())
        {
            context2.IsRunning.Should().BeTrue();
            Application.Current.Should().NotBeNull();
            Application.Current.Dispatcher.Should().BeSameAs(context2.Dispatcher);

            TestHostApplicationLifetime hostLifetime = new();
            ApplicationLifetimeService lifetimeService = new(hostLifetime);
            TestManagedWindow mainWindow = new();
            TestManagedWindow widgetWindow = new();

            WindowManager windowManager = new(
                () => mainWindow,
                () => widgetWindow,
                lifetimeService);

            // Execute WindowManager logic from current test runner thread (dispatches cross-thread to context2)
            windowManager.ShowWidget();

            // 5. No deadlock occurs.
            windowManager.IsWidgetVisible.Should().BeTrue();
            widgetWindow.IsVisible.Should().BeTrue();

            // 6. The test confirms that the dispatcher used by the later test is valid and pumped.
            context2.Dispatcher.HasShutdownStarted.Should().BeFalse("Subsequent test dispatcher must still be active");
            context2.Dispatcher.Thread.IsAlive.Should().BeTrue("Subsequent dispatcher thread must be alive");

            bool canPumpWork = context2.Invoke(() => true);
            canPumpWork.Should().BeTrue("Subsequent dispatcher must be actively pumping messages");
        }

        // Verify post-disposal cleanup of second context
        Application.Current.Should().BeNull("Application.Current must be reset after second context disposal");

        // Verify headless WindowManager execution works deterministically when Application.Current is null
        TestHostApplicationLifetime headlessHostLifetime = new();
        ApplicationLifetimeService headlessLifetime = new(headlessHostLifetime);
        TestManagedWindow headlessMainWindow = new();
        TestManagedWindow headlessWidgetWindow = new();

        WindowManager headlessWindowManager = new(
            () => headlessMainWindow,
            () => headlessWidgetWindow,
            headlessLifetime);

        headlessWindowManager.ShowWidget();
        headlessWindowManager.IsWidgetVisible.Should().BeTrue();
        headlessWidgetWindow.IsVisible.Should().BeTrue();
    }

    [Fact]
    public void WpfDispatcherIsolation_HeadlessTests_ExecuteWithoutWpfContamination()
    {
        // Confirm no leftover Application.Current exists
        Application.Current.Should().BeNull();

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

        // Trigger window close which triggers lifetimeService.Shutdown()
        widgetWindow.SimulateClose();

        lifetimeService.IsShuttingDown.Should().BeTrue();
        hostLifetime.StopApplicationCallCount.Should().Be(1);
        Application.Current.Should().BeNull();
    }
}
