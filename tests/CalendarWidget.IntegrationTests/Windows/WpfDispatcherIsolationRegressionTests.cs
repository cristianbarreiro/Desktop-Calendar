using System.Windows;
using System.Windows.Threading;
using CalendarWidget.App.Services;
using CalendarWidget.IntegrationTests.Helpers;
using CalendarWidget.Presentation.Views;
using FluentAssertions;

namespace CalendarWidget.IntegrationTests.Windows;

/// <summary>
/// Regression tests verifying that WPF test dispatcher lifecycle isolation prevents
/// cross-test global state contamination, deadlocks, and abandoned dispatchers,
/// and enforces deterministic lifecycle closure, reference counting, and Application cleanup.
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

    [Fact]
    public void WpfTestContext_DispatcherTerminatesAfterFinalDisposal()
    {
        Thread staThread;
        Dispatcher dispatcher;

        using (WpfTestContext context = new())
        {
            context.IsRunning.Should().BeTrue();
            staThread = context.StaThread;
            dispatcher = context.Dispatcher;

            staThread.IsAlive.Should().BeTrue("STA worker thread must be alive while owned");
            dispatcher.HasShutdownStarted.Should().BeFalse("Dispatcher must not be shutting down while owned");
            WpfTestContext.HasActiveStaThread.Should().BeTrue();
            Application.Current.Should().NotBeNull();
        }

        // After final disposal:
        staThread.IsAlive.Should().BeFalse("STA worker thread must terminate upon final disposal");
        dispatcher.HasShutdownStarted.Should().BeTrue("Dispatcher shutdown must have been requested");
        WpfTestContext.HasActiveStaThread.Should().BeFalse("No active STA worker thread should remain");
        Application.Current.Should().BeNull("Application.Current must be reset to null");
    }

    [Fact]
    public void WpfTestContext_ApplicationCurrentCleanup_AttachesDuringOwnershipAndCleansUpAfterDisposal()
    {
        Application.Current.Should().BeNull("Application.Current must initially be null in headless environment");

        using (WpfTestContext context = new())
        {
            Application.Current.Should().NotBeNull("Application.Current must be attached during context lifetime");
            Application.Current.Dispatcher.Should().BeSameAs(context.Dispatcher);
        }

        Application.Current.Should().BeNull("Application.Current must be cleaned up immediately after disposal");

        // Verify headless test runs without contamination
        TestHostApplicationLifetime hostLifetime = new();
        ApplicationLifetimeService lifetime = new(hostLifetime);
        TestManagedWindow mainWindow = new();
        TestManagedWindow widgetWindow = new();

        WindowManager wm = new(() => mainWindow, () => widgetWindow, lifetime);
        wm.ShowWidget();
        widgetWindow.SimulateClose();

        lifetime.IsShuttingDown.Should().BeTrue();
        Application.Current.Should().BeNull();
    }

    [Fact]
    public void WpfTestContext_ContextRecreation_SequentialContextsCreateFreshIndependentLifecycles()
    {
        Thread firstThread;
        using (WpfTestContext context1 = new())
        {
            firstThread = context1.StaThread;
            bool ran1 = context1.Invoke(() => new NotesView() is not null);
            ran1.Should().BeTrue("First context must execute WPF work");
        }

        firstThread.IsAlive.Should().BeFalse("First STA thread must terminate");
        Application.Current.Should().BeNull();

        Thread secondThread;
        using (WpfTestContext context2 = new())
        {
            secondThread = context2.StaThread;
            secondThread.Should().NotBeSameAs(firstThread, "Second context must instantiate a fresh STA thread");
            bool ran2 = context2.Invoke(() => new NotesView() is not null);
            ran2.Should().BeTrue("Second context must execute WPF work");
        }

        secondThread.IsAlive.Should().BeFalse("Second STA thread must terminate");
        Application.Current.Should().BeNull();
    }

    [Fact]
    public void WpfTestContext_MultipleOwners_FinalOwnerPerformsShutdown()
    {
        using (WpfTestContext outer = new())
        {
            WpfTestContext.ActiveOwnerCount.Should().Be(1);
            Thread staThread = outer.StaThread;

            using (WpfTestContext inner = new())
            {
                WpfTestContext.ActiveOwnerCount.Should().Be(2);
                inner.StaThread.Should().BeSameAs(staThread, "Inner context should share existing active STA thread");
                inner.Dispatcher.Should().BeSameAs(outer.Dispatcher, "Inner context should share active Dispatcher");
            }

            // Inner disposed: outer still owns the dispatcher
            WpfTestContext.ActiveOwnerCount.Should().Be(1);
            staThread.IsAlive.Should().BeTrue("STA thread must remain alive while outer owner is still active");
            outer.Dispatcher.HasShutdownStarted.Should().BeFalse();
            bool workCompleted = outer.Invoke(() => true);
            workCompleted.Should().BeTrue("Dispatcher must still be active and pumping");
        }

        // Both disposed: final owner triggered complete shutdown
        WpfTestContext.ActiveOwnerCount.Should().Be(0);
        WpfTestContext.HasActiveStaThread.Should().BeFalse("STA worker thread must be shut down and joined");
        Application.Current.Should().BeNull();
    }

    [Fact]
    public async Task WpfTestContext_IdempotentDisposal_MultipleCallsAreSafeAndDoNotCorruptState()
    {
        WpfTestContext context = new();
        context.IsRunning.Should().BeTrue();

        Action act1 = () => context.Dispose();
        Action act2 = () => context.Dispose();
        Func<Task> act3 = async () => await context.DisposeAsync();

        act1.Should().NotThrow();
        act2.Should().NotThrow();
        await act3.Should().NotThrowAsync();

        context.IsRunning.Should().BeFalse();
        WpfTestContext.ActiveOwnerCount.Should().Be(0);
        WpfTestContext.HasActiveStaThread.Should().BeFalse();
        Application.Current.Should().BeNull();
    }

    [Fact]
    public void WpfTestContext_InitializationFailure_CleansUpLeakedThreadAndAllowsSubsequentContext()
    {
        try
        {
            WpfTestContext.InitializationSeamForTesting = () => throw new InvalidOperationException("Simulated init failure");

            Action act = () =>
            {
                using WpfTestContext broken = new();
            };

            act.Should().Throw<InvalidOperationException>().WithMessage("Simulated init failure");
        }
        finally
        {
            WpfTestContext.InitializationSeamForTesting = null;
        }

        // Verify no leaked thread or stale state
        WpfTestContext.ActiveOwnerCount.Should().Be(0);
        WpfTestContext.HasActiveStaThread.Should().BeFalse("Failed initialization must not leak a running STA thread");
        Application.Current.Should().BeNull("Failed initialization must not leave Application.Current set");

        // Verify subsequent healthy context initializes and works properly
        using (WpfTestContext healthy = new())
        {
            healthy.IsRunning.Should().BeTrue();
            bool ran = healthy.Invoke(() => new NotesView() is not null);
            ran.Should().BeTrue("Subsequent context must initialize cleanly and execute WPF operations");
        }

        WpfTestContext.ActiveOwnerCount.Should().Be(0);
        WpfTestContext.HasActiveStaThread.Should().BeFalse();
        Application.Current.Should().BeNull();
    }

    [Fact]
    public void WpfTestContext_Dispose_WithQueuedDispatcherWork_TerminatesCleanlyWithoutDeadlock()
    {
        using (WpfTestContext context = new())
        {
            // Queue low-priority background work on the dispatcher
            context.Dispatcher.InvokeAsync(async () =>
            {
                await Task.Yield();
            }, DispatcherPriority.Background);
        }

        // Disposal should complete normally without deadlocking on queued work
        WpfTestContext.HasActiveStaThread.Should().BeFalse();
        Application.Current.Should().BeNull();
    }
}
