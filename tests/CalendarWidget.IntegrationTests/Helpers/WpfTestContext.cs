using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Threading;

namespace CalendarWidget.IntegrationTests.Helpers;

/// <summary>
/// Provides a dedicated, deterministic STA thread and actively pumped WPF <see cref="Dispatcher"/>
/// for integration tests requiring UI element instantiation, theme resolution, and Dispatcher execution.
/// Ensures complete isolation by scoping <see cref="Application.Current"/> strictly to the context lifetime.
/// </summary>
public sealed class WpfTestContext : IDisposable, IAsyncDisposable
{
    private static readonly SemaphoreSlim ContextGate = new(1, 1);
    private static readonly object SyncRoot = new();

    private static Thread? s_staThread;
    private static Dispatcher? s_dispatcher;
    private static Application? s_application;
    private static readonly FieldInfo? AppInstanceField =
        typeof(Application).GetField("_appInstance", BindingFlags.Static | BindingFlags.NonPublic);

    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WpfTestContext"/> class, ensuring
    /// the shared STA test dispatcher is running and attaching <see cref="Application.Current"/>.
    /// </summary>
    public WpfTestContext()
    {
        ContextGate.Wait();

        try
        {
            EnsureInitialized();
            AppInstanceField?.SetValue(null, s_application);
        }
        catch
        {
            ContextGate.Release();
            throw;
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
            return s_dispatcher!;
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
            return s_staThread!;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the dispatcher thread is alive and pumping.
    /// </summary>
    public bool IsRunning => !_isDisposed && s_staThread is not null && s_staThread.IsAlive && s_dispatcher is not null && !s_dispatcher.HasShutdownStarted;

    /// <summary>
    /// Executes a synchronous action on the STA Dispatcher thread.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    public void Invoke(Action action)
    {
        ThrowIfDisposed();
        if (Dispatcher.CheckAccess())
        {
            action();
            return;
        }

        Dispatcher.Invoke(action);
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
        if (Dispatcher.CheckAccess())
        {
            return func();
        }

        return Dispatcher.Invoke(func);
    }

    /// <summary>
    /// Asynchronously executes an action on the STA Dispatcher thread.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>A task representing the completion of the action.</returns>
    public async Task InvokeAsync(Action action)
    {
        ThrowIfDisposed();
        if (Dispatcher.CheckAccess())
        {
            action();
            return;
        }

        await Dispatcher.InvokeAsync(action);
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
        if (Dispatcher.CheckAccess())
        {
            return func();
        }

        return await Dispatcher.InvokeAsync(func);
    }

    /// <summary>
    /// Asynchronously executes an asynchronous operation on the STA Dispatcher thread.
    /// </summary>
    /// <param name="asyncAction">The asynchronous action to execute.</param>
    /// <returns>A task representing the completion of the operation.</returns>
    public async Task InvokeAsync(Func<Task> asyncAction)
    {
        ThrowIfDisposed();
        if (Dispatcher.CheckAccess())
        {
            await asyncAction();
            return;
        }

        Task task = await Dispatcher.InvokeAsync(asyncAction);
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
        if (Dispatcher.CheckAccess())
        {
            return await asyncFunc();
        }

        Task<T> task = await Dispatcher.InvokeAsync(asyncFunc);
        return await task;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        try
        {
            // Close any open windows created during the test
            if (s_dispatcher is not null && !s_dispatcher.HasShutdownStarted)
            {
                try
                {
                    s_dispatcher.Invoke(() =>
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
                                }
                            }
                        }
                    });
                }
                catch
                {
                }
            }

            // Detach Application.Current so non-WPF tests see null
            AppInstanceField?.SetValue(null, null);
        }
        finally
        {
            ContextGate.Release();
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private static void EnsureInitialized()
    {
        if (s_staThread is not null && s_dispatcher is not null && s_application is not null)
        {
            return;
        }

        lock (SyncRoot)
        {
            if (s_staThread is not null && s_dispatcher is not null && s_application is not null)
            {
                return;
            }

            using ManualResetEventSlim initialized = new();
            Exception? initException = null;

            s_staThread = new Thread(() =>
            {
                try
                {
                    s_dispatcher = Dispatcher.CurrentDispatcher;
                    SynchronizationContext.SetSynchronizationContext(
                        new DispatcherSynchronizationContext(s_dispatcher));

                    s_application = Application.Current ?? new Application
                    {
                        ShutdownMode = ShutdownMode.OnExplicitShutdown
                    };

                    Uri themeUri = new("pack://application:,,,/CalendarWidget.Presentation;component/Resources/Theme.xaml", UriKind.Absolute);
                    if (!s_application.Resources.MergedDictionaries.Any(d => d.Source == themeUri))
                    {
                        s_application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
                    }

                    initialized.Set();
                    Dispatcher.Run();
                }
                catch (Exception ex)
                {
                    initException = ex;
                    initialized.Set();
                }
            })
            {
                IsBackground = true,
                Name = "WpfTestContext.STA"
            };

            s_staThread.SetApartmentState(ApartmentState.STA);
            s_staThread.Start();

            bool ready = initialized.Wait(TimeSpan.FromSeconds(10));
            if (!ready || initException is not null || s_dispatcher is null || s_application is null)
            {
                if (initException is not null)
                {
                    ExceptionDispatchInfo.Capture(initException).Throw();
                }

                throw new TimeoutException("WPF test STA thread failed to initialize within 10 seconds.");
            }
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }
}
