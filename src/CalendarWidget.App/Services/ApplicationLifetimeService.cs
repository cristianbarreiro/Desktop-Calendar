using System.Windows;
using Microsoft.Extensions.Hosting;

namespace CalendarWidget.App.Services;

/// <summary>
/// Coordinates application startup and shutdown lifecycle with the Generic Host.
/// </summary>
public sealed class ApplicationLifetimeService
{
    private readonly IHostApplicationLifetime _hostLifetime;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationLifetimeService"/> class.
    /// </summary>
    /// <param name="hostLifetime">The generic host application lifetime.</param>
    public ApplicationLifetimeService(IHostApplicationLifetime hostLifetime)
    {
        _hostLifetime = hostLifetime;
    }

    /// <summary>
    /// Gracefully initiates application shutdown.
    /// </summary>
    /// <param name="exitCode">The exit code to return to the operating system.</param>
    public void Shutdown(int exitCode = 0)
    {
        if (Application.Current is not null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Application.Current.Shutdown(exitCode);
            });
        }

        _hostLifetime.StopApplication();
    }
}
