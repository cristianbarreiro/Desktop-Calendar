using System.IO;
using System.IO.Pipes;
using System.Text;

namespace CalendarWidget.App.Services;

/// <summary>
/// Enforces single application instance execution via a named OS Mutex
/// and handles secondary instance activation signaling through a local asynchronous Named Pipe.
/// </summary>
public sealed class SingleInstanceCoordinator : ISingleInstanceCoordinator
{
    /// <summary>
    /// Canonical system-wide Mutex identifier.
    /// </summary>
    public const string DefaultMutexName = "DesktopCalendarWidget_SingleInstance";

    /// <summary>
    /// Canonical local machine Named Pipe identifier.
    /// </summary>
    public const string DefaultPipeName = "DesktopCalendarWidget_SingleInstance_Pipe";

    private const string ActivationMessage = "ACTIVATE";

    private readonly string _mutexName;
    private readonly string _pipeName;
    private readonly Lock _lock = new();

    private Action? _onActivateRequested;
    private Mutex? _mutex;
    private bool _isPrimary;
    private CancellationTokenSource? _cts;
    private Task? _listenerTask;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleInstanceCoordinator"/> class and attempts to acquire the mutex.
    /// </summary>
    /// <param name="onActivateRequested">Action invoked when an activation request is received from a secondary instance.</param>
    /// <param name="mutexName">The name of the system mutex.</param>
    /// <param name="pipeName">The name of the local named pipe.</param>
    public SingleInstanceCoordinator(
        Action? onActivateRequested = null,
        string mutexName = DefaultMutexName,
        string pipeName = DefaultPipeName)
    {
        _onActivateRequested = onActivateRequested;
        _mutexName = mutexName;
        _pipeName = pipeName;

        TryAcquireMutex();
    }

    /// <inheritdoc />
    public bool IsPrimary => _isPrimary;

    /// <inheritdoc />
    public void SetActivationHandler(Action onActivateRequested)
    {
        lock (_lock)
        {
            _onActivateRequested = onActivateRequested;
        }
    }

    /// <inheritdoc />
    public bool SignalPrimary(int timeoutMs = 1500)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out);
            client.Connect(timeoutMs);
            using var writer = new StreamWriter(client, Encoding.UTF8);
            writer.WriteLine(ActivationMessage);
            writer.Flush();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public void StartListening()
    {
        lock (_lock)
        {
            if (!_isPrimary || _disposed || _listenerTask is not null)
            {
                return;
            }

            _cts = new CancellationTokenSource();
            _listenerTask = Task.Run(() => RunServerLoopAsync(_cts.Token));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Task? taskToAwait = null;
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_cts is not null)
            {
                try
                {
                    _cts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
            }

            taskToAwait = _listenerTask;

            if (_isPrimary && _mutex is not null)
            {
                try
                {
                    _mutex.ReleaseMutex();
                }
                catch
                {
                    // Ignore if already released or not owned
                }

                _mutex.Dispose();
                _mutex = null;
            }
        }

        if (taskToAwait is not null)
        {
            try
            {
                taskToAwait.Wait(TimeSpan.FromMilliseconds(500));
            }
            catch
            {
                // Ignore task faults or cancellation during shutdown
            }
        }

        lock (_lock)
        {
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void TryAcquireMutex()
    {
        try
        {
            _mutex = new Mutex(true, _mutexName, out bool createdNew);
            _isPrimary = createdNew;
        }
        catch (AbandonedMutexException)
        {
            // The previous holding process terminated abnormally. The current instance acquires ownership.
            _isPrimary = true;
        }
        catch
        {
            _isPrimary = false;
        }
    }

    private async Task RunServerLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            NamedPipeServerStream? server = null;
            try
            {
                server = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.In,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(token).ConfigureAwait(false);

                using (server)
                {
                    using var reader = new StreamReader(server, Encoding.UTF8);
                    string? line = await reader.ReadLineAsync(token).ConfigureAwait(false);
                    if (string.Equals(line?.Trim(), ActivationMessage, StringComparison.OrdinalIgnoreCase))
                    {
                        Action? handler;
                        lock (_lock)
                        {
                            handler = _onActivateRequested;
                        }

                        handler?.Invoke();
                    }
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
            {
                server?.Dispose();
                break;
            }
            catch
            {
                server?.Dispose();
                try
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }
    }
}
