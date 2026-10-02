using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Threading;

namespace CalendarWidget.IntegrationTests.Helpers;

/// <summary>
/// Lifecycle states for the dedicated WPF test STA thread and dispatcher.
/// </summary>
public enum WpfLifecycleState
{
    /// <summary>No dispatcher or STA thread has been initialized.</summary>
    NotStarted,

    /// <summary>STA thread has been launched and is preparing the dispatcher.</summary>
    Starting,

    /// <summary>Dispatcher instance has been acquired on the STA thread but message pumping has not started.</summary>
    DispatcherCreatedNotPumping,

    /// <summary><see cref="Application.Current"/> has been attached on the STA thread but message pumping has not started.</summary>
    ApplicationAttached,

    /// <summary>Dispatcher is actively executing its message pump via <see cref="Dispatcher.Run()"/>.</summary>
    Pumping,

    /// <summary>Terminal cleanup has been requested and is actively stopping the dispatcher and STA thread.</summary>
    Stopping,

    /// <summary>Dispatcher and STA thread have stopped completely and resources are cleared.</summary>
    Stopped,

    /// <summary>An unhandled error occurred during STA initialization before normal pumping was reached.</summary>
    InitializationFailed
}

/// <summary>
/// Provides a dedicated, deterministic STA thread and actively pumped WPF <see cref="Dispatcher"/>
/// for integration tests requiring UI element instantiation, theme resolution, and Dispatcher execution.
/// Ensures complete lifecycle closure by scoping <see cref="Application.Current"/> strictly to active context ownership,
/// deterministically shutting down the Dispatcher and terminating the STA thread upon disposal of the final owner.
/// </summary>
public sealed class WpfTestContext : IDisposable, IAsyncDisposable
{
    private static readonly object SyncRoot = new();

    private static int s_activeOwnerCount;
    private static Thread? s_staThread;
    private static Dispatcher? s_dispatcher;
    private static Application? s_application;
    private static WpfLifecycleState s_lifecycleState = WpfLifecycleState.NotStarted;
    private static volatile bool s_isPumping;

    // WPF System.Windows.Application has no public setter for Application.Current and internally records
    // _appCreatedInThisAppDomain = true, which throws InvalidOperationException if new Application() is called
    // again in the same AppDomain/process. Furthermore, Application.Shutdown() does not reset Application.Current
    // to null. Reflection is strictly isolated to this test helper to reset these static fields upon final disposal,
    // ensuring clean headless execution for subsequent tests and allowing clean context recreation.
    private static readonly FieldInfo? AppInstanceField =
        typeof(Application).GetField("_appInstance", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly FieldInfo? AppCreatedField =
        typeof(Application).GetField("_appCreatedInThisAppDomain", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly FieldInfo? IsShuttingDownField =
        typeof(Application).GetField("_isShuttingDown", BindingFlags.Static | BindingFlags.NonPublic);

    private readonly Thread _staThread;
    private readonly Dispatcher _dispatcher;
    private int _disposeState;

    /// <summary>
    /// Initializes a new instance of the <see cref="WpfTestContext"/> class, ensuring
    /// the STA test dispatcher is actively pumping and attaching <see cref="Application.Current"/>.
    /// </summary>
    public WpfTestContext()
    {
        lock (SyncRoot)
        {
            while (s_lifecycleState is WpfLifecycleState.Stopping
                || s_lifecycleState is WpfLifecycleState.Starting
                || s_lifecycleState is WpfLifecycleState.DispatcherCreatedNotPumping
                || s_lifecycleState is WpfLifecycleState.ApplicationAttached)
            {
                Monitor.Wait(SyncRoot);
            }

            if (s_lifecycleState == WpfLifecycleState.Pumping
                && s_staThread is not null
                && s_dispatcher is not null
                && s_application is not null
                && s_staThread.IsAlive)
            {
                s_activeOwnerCount++;
                _staThread = s_staThread;
                _dispatcher = s_dispatcher;
                return;
            }

            s_lifecycleState = WpfLifecycleState.Starting;
        }

        try
        {
            EnsureStaThreadStarted();
            lock (SyncRoot)
            {
                s_activeOwnerCount++;
                _staThread = s_staThread!;
                _dispatcher = s_dispatcher!;
                Monitor.PulseAll(SyncRoot);
            }
        }
        catch
        {
            lock (SyncRoot)
            {
                s_lifecycleState = WpfLifecycleState.Stopped;
                Monitor.PulseAll(SyncRoot);
            }

            throw;
        }
    }

    /// <summary>
    /// Gets or sets a test seam allowing tests to simulate an exception before Dispatcher creation on the STA thread.
    /// </summary>
    internal static Action? SeamBeforeDispatcherCreationForTesting { get; set; }

    /// <summary>
    /// Gets or sets a test seam allowing tests to simulate an exception during STA initialization after Dispatcher creation.
    /// </summary>
    internal static Action? InitializationSeamForTesting { get; set; }

    /// <summary>
    /// Gets or sets a test seam allowing tests to simulate an exception after Application creation on the STA thread.
    /// </summary>
    internal static Action? SeamAfterApplicationCreationForTesting { get; set; }

    /// <summary>
    /// Gets the most recently created STA thread instance for lifecycle verification in tests.
    /// </summary>
    internal static Thread? LastStaThreadForTesting { get; private set; }

    /// <summary>
    /// Gets the most recently created Dispatcher instance for lifecycle verification in tests.
    /// </summary>
    internal static Dispatcher? LastDispatcherForTesting { get; private set; }

    /// <summary>
    /// Gets the current lifecycle state of the WPF test infrastructure.
    /// </summary>
    internal static WpfLifecycleState LifecycleState
    {
        get
        {
            lock (SyncRoot)
            {
                return s_lifecycleState;
            }
        }
    }

    /// <summary>
    /// Gets the number of currently active owners sharing the test dispatcher.
    /// </summary>
    internal static int ActiveOwnerCount
    {
        get
        {
            lock (SyncRoot)
            {
                return s_activeOwnerCount;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether an STA worker thread is currently running.
    /// </summary>
    internal static bool HasActiveStaThread
    {
        get
        {
            lock (SyncRoot)
            {
                return s_staThread is not null && s_staThread.IsAlive;
            }
        }
    }

    /// <summary>
    /// Gets the active test <see cref="Dispatcher"/>.
    /// </summary>
    public Dispatcher Dispatcher
    {
        get
        {
            ThrowIfDisposed();
            return _dispatcher;
        }
    }

    /// <summary>
    /// Gets the dedicated STA <see cref="Thread"/>.
    /// </summary>
    public Thread StaThread
    {
        get
        {
            ThrowIfDisposed();
            return _staThread;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the dispatcher thread is alive and pumping.
    /// </summary>
    public bool IsRunning => _disposeState == 0
        && _staThread.IsAlive
        && !_dispatcher.HasShutdownStarted
        && s_isPumping;

    /// <summary>
    /// Executes a synchronous action on the STA Dispatcher thread.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    public void Invoke(Action action)
    {
        ThrowIfDisposed();
        if (!_staThread.IsAlive || _dispatcher.HasShutdownStarted)
        {
            throw new InvalidOperationException("WPF test context dispatcher is not actively pumping.");
        }

        if (_dispatcher.CheckAccess())
        {
            action();
            return;
        }

        _dispatcher.Invoke(action);
    }

    /// <summary>
    /// Executes a synchronous function on the STA Dispatcher thread and returns its result.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <returns>The result returned by <paramref name="func"/>.</returns>
    public T Invoke<T>(Func<T> func)
    {
        ThrowIfDisposed();
        if (!_staThread.IsAlive || _dispatcher.HasShutdownStarted)
        {
            throw new InvalidOperationException("WPF test context dispatcher is not actively pumping.");
        }

        if (_dispatcher.CheckAccess())
        {
            return func();
        }

        return _dispatcher.Invoke(func);
    }

    /// <summary>
    /// Asynchronously executes an action on the STA Dispatcher thread.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>A task representing the completion of the action.</returns>
    public async Task InvokeAsync(Action action)
    {
        ThrowIfDisposed();
        if (!_staThread.IsAlive || _dispatcher.HasShutdownStarted)
        {
            throw new InvalidOperationException("WPF test context dispatcher is not actively pumping.");
        }

        if (_dispatcher.CheckAccess())
        {
            action();
            return;
        }

        await _dispatcher.InvokeAsync(action);
    }

    /// <summary>
    /// Asynchronously executes a function on the STA Dispatcher thread and returns its result.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <returns>A task containing the result returned by <paramref name="func"/>.</returns>
    public async Task<T> InvokeAsync<T>(Func<T> func)
    {
        ThrowIfDisposed();
        if (!_staThread.IsAlive || _dispatcher.HasShutdownStarted)
        {
            throw new InvalidOperationException("WPF test context dispatcher is not actively pumping.");
        }

        if (_dispatcher.CheckAccess())
        {
            return func();
        }

        return await _dispatcher.InvokeAsync(func);
    }

    /// <summary>
    /// Asynchronously executes an asynchronous operation on the STA Dispatcher thread.
    /// </summary>
    /// <param name="asyncAction">The asynchronous action to execute.</param>
    /// <returns>A task representing the completion of the operation.</returns>
    public async Task InvokeAsync(Func<Task> asyncAction)
    {
        ThrowIfDisposed();
        if (!_staThread.IsAlive || _dispatcher.HasShutdownStarted)
        {
            throw new InvalidOperationException("WPF test context dispatcher is not actively pumping.");
        }

        if (_dispatcher.CheckAccess())
        {
            await asyncAction();
            return;
        }

        Task task = await _dispatcher.InvokeAsync(asyncAction);
        await task;
    }

    /// <summary>
    /// Asynchronously executes an asynchronous function on the STA Dispatcher thread and returns its result.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="asyncFunc">The asynchronous function to execute.</param>
    /// <returns>A task containing the result returned by <paramref name="asyncFunc"/>.</returns>
    public async Task<T> InvokeAsync<T>(Func<Task<T>> asyncFunc)
    {
        ThrowIfDisposed();
        if (!_staThread.IsAlive || _dispatcher.HasShutdownStarted)
        {
            throw new InvalidOperationException("WPF test context dispatcher is not actively pumping.");
        }

        if (_dispatcher.CheckAccess())
        {
            return await asyncFunc();
        }

        Task<T> task = await _dispatcher.InvokeAsync(asyncFunc);
        return await task;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
        {
            return;
        }

        bool shouldCleanup = false;
        lock (SyncRoot)
        {
            s_activeOwnerCount--;
            if (s_activeOwnerCount <= 0)
            {
                s_activeOwnerCount = 0;
                if (s_lifecycleState is WpfLifecycleState.Pumping
                    or WpfLifecycleState.Starting
                    or WpfLifecycleState.DispatcherCreatedNotPumping
                    or WpfLifecycleState.ApplicationAttached)
                {
                    s_lifecycleState = WpfLifecycleState.Stopping;
                    shouldCleanup = true;
                }
            }
        }

        if (shouldCleanup)
        {
            try
            {
                PerformTerminalCleanup(isInitializationFailure: false);
            }
            finally
            {
                lock (SyncRoot)
                {
                    s_lifecycleState = WpfLifecycleState.Stopped;
                    Monitor.PulseAll(SyncRoot);
                }
            }
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private static void EnsureStaThreadStarted()
    {
        using ManualResetEventSlim initialized = new();
        Exception? initException = null;

        Thread thread = new(() =>
        {
            try
            {
                SeamBeforeDispatcherCreationForTesting?.Invoke();

                s_dispatcher = Dispatcher.CurrentDispatcher;
                lock (SyncRoot)
                {
                    s_lifecycleState = WpfLifecycleState.DispatcherCreatedNotPumping;
                }

                SynchronizationContext.SetSynchronizationContext(
                    new DispatcherSynchronizationContext(s_dispatcher));

                InitializationSeamForTesting?.Invoke();

                s_application = new Application
                {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };
                lock (SyncRoot)
                {
                    s_lifecycleState = WpfLifecycleState.ApplicationAttached;
                }

                Uri themeUri = new("pack://application:,,,/CalendarWidget.Presentation;component/Resources/Theme.xaml", UriKind.Absolute);
                if (!s_application.Resources.MergedDictionaries.Any(d => d.Source == themeUri))
                {
                    s_application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
                }

                SeamAfterApplicationCreationForTesting?.Invoke();

                lock (SyncRoot)
                {
                    s_lifecycleState = WpfLifecycleState.Pumping;
                }

                s_isPumping = true;
                initialized.Set();
                Dispatcher.Run();
            }
            catch (Exception ex)
            {
                initException = ex;
                lock (SyncRoot)
                {
                    s_lifecycleState = WpfLifecycleState.InitializationFailed;
                }

                CleanupOnStaThread();
                initialized.Set();
            }
            finally
            {
                s_isPumping = false;
                lock (SyncRoot)
                {
                    s_lifecycleState = WpfLifecycleState.Stopped;
                }
            }
        })
        {
            IsBackground = true,
            Name = "WpfTestContext.STA"
        };

        s_staThread = thread;
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        bool ready = initialized.Wait(TimeSpan.FromSeconds(10));
        bool isPumping;
        lock (SyncRoot)
        {
            isPumping = s_lifecycleState == WpfLifecycleState.Pumping;
        }

        if (!ready || initException is not null || s_dispatcher is null || s_application is null || !isPumping)
        {
            PerformTerminalCleanup(isInitializationFailure: true);

            if (initException is not null)
            {
                ExceptionDispatchInfo.Capture(initException).Throw();
            }

            throw new TimeoutException("WPF test STA thread failed to initialize within 10 seconds.");
        }
    }

    private static void CleanupOnStaThread()
    {
        s_isPumping = false;

        try
        {
            if (s_application is not null)
            {
                foreach (Window window in s_application.Windows.OfType<Window>().ToList())
                {
                    try
                    {
                        window.Close();
                    }
                    catch
                    {
                        // Best-effort window closure on STA thread
                    }
                }
            }
        }
        catch
        {
        }

        try
        {
            if (s_dispatcher is not null && !s_dispatcher.HasShutdownStarted)
            {
                s_dispatcher.InvokeShutdown();
            }
        }
        catch
        {
        }
    }

    private static void PerformTerminalCleanup(bool isInitializationFailure)
    {
        List<Exception> cleanupExceptions = new();

        // 1. Close open windows only if dispatcher was actively pumping and STA thread is alive
        if (!isInitializationFailure)
        {
            try
            {
                CloseOpenWindows();
            }
            catch (Exception ex)
            {
                cleanupExceptions.Add(ex);
            }
        }

        // 2. Request dispatcher shutdown if active
        try
        {
            if (s_dispatcher is not null && !s_dispatcher.HasShutdownStarted)
            {
                if (s_staThread is not null && s_staThread.IsAlive)
                {
                    s_dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
                }
            }
        }
        catch (Exception ex)
        {
            cleanupExceptions.Add(ex);
        }

        // 3. Wait for STA thread termination
        try
        {
            if (s_staThread is not null && s_staThread.IsAlive && Thread.CurrentThread != s_staThread)
            {
                bool terminated = s_staThread.Join(TimeSpan.FromSeconds(10));
                if (!terminated)
                {
                    cleanupExceptions.Add(new TimeoutException("WPF test STA thread failed to terminate within 10 seconds."));
                }
            }
        }
        catch (Exception ex)
        {
            cleanupExceptions.Add(ex);
        }

        // 4. Detach Application.Current and reset internal WPF flags via reflection
        try
        {
            AppInstanceField?.SetValue(null, null);
            AppCreatedField?.SetValue(null, false);
            IsShuttingDownField?.SetValue(null, false);
        }
        catch (Exception ex)
        {
            cleanupExceptions.Add(ex);
        }

        // 5. Record last references for test assertions before clearing static fields
        LastStaThreadForTesting = s_staThread;
        LastDispatcherForTesting = s_dispatcher;

        s_staThread = null;
        s_dispatcher = null;
        s_application = null;

        if (cleanupExceptions.Count > 0 && !isInitializationFailure)
        {
            if (cleanupExceptions.Count == 1)
            {
                ExceptionDispatchInfo.Capture(cleanupExceptions[0]).Throw();
            }
            else
            {
                throw new AggregateException("One or more errors occurred during WPF test teardown.", cleanupExceptions);
            }
        }
    }

    private static void CloseOpenWindows()
    {
        if (!s_isPumping
            || s_dispatcher is null
            || s_dispatcher.HasShutdownStarted
            || s_staThread is null
            || !s_staThread.IsAlive)
        {
            return;
        }

        Action closeAction = () =>
        {
            if (Application.Current is not null)
            {
                foreach (Window window in Application.Current.Windows.OfType<Window>().ToList())
                {
                    try
                    {
                        window.Close();
                    }
                    catch
                    {
                        // Best-effort window closure
                    }
                }
            }
        };

        if (s_dispatcher.CheckAccess())
        {
            closeAction();
        }
        else
        {
            s_dispatcher.Invoke(closeAction);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposeState != 0, this);
    }
}
