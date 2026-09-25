using System.Windows;
using Microsoft.Extensions.Hosting;

namespace CalendarWidget.App.Services;

/// <summary>
/// Coordinates application startup and shutdown lifecycle with the Generic Host.
/// </summary>
public sealed class ApplicationLifetimeService
{
    private readonly IHostApplicationLifetime _hostLifetime;
    private int _isShuttingDown;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationLifetimeService"/> class.
    /// </summary>
    /// <param name="hostLifetime">The generic host application lifetime.</param>
    public ApplicationLifetimeService(IHostApplicationLifetime hostLifetime)
    {
        _hostLifetime = hostLifetime;
    }

    /// <summary>
    /// Gets a value indicating whether application shutdown has been initiated.
    /// </summary>
    public bool IsShuttingDown => Volatile.Read(ref _isShuttingDown) != 0;

    /// <summary>
    /// Gracefully initiates application shutdown and stops the generic host.
    /// </summary>
    /// <param name="exitCode">The exit code to return to the operating system.</param>
    public void Shutdown(int exitCode = 0)
    {
        if (Interlocked.Exchange(ref _isShuttingDown, 1) != 0)
        {
            return;
        }

        if (Application.Current is not null)
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Shutdown(exitCode);
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown(exitCode));
            }
        }

        _hostLifetime.StopApplication();
    }
}
