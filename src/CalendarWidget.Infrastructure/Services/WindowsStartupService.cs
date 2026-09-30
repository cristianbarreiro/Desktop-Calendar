using System.IO;
using System.Runtime.Versioning;
using CalendarWidget.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace CalendarWidget.Infrastructure.Services;

/// <summary>
/// Configures automatic application launch upon Windows user logon via current user registry.
/// </summary>
public sealed class WindowsStartupService : IWindowsStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "DesktopCalendar";

    private static readonly Action<ILogger, Exception?> LogInspectFailed =
        LoggerMessage.Define(LogLevel.Warning, new EventId(1, "InspectFailed"), "Failed to inspect Windows startup configuration.");

    private static readonly Action<ILogger, Exception?> LogNotSupported =
        LoggerMessage.Define(LogLevel.Warning, new EventId(2, "NotSupported"), "Windows startup configuration is not supported on this platform.");

    private static readonly Action<ILogger, bool, Exception?> LogUpdateFailed =
        LoggerMessage.Define<bool>(LogLevel.Error, new EventId(3, "UpdateFailed"), "Failed to update Windows startup configuration to {Enable}.");

    private static readonly Action<ILogger, Exception?> LogKeyOpenFailed =
        LoggerMessage.Define(LogLevel.Warning, new EventId(4, "KeyOpenFailed"), "Unable to open HKCU Run registry key with write permissions.");

    private static readonly Action<ILogger, Exception?> LogProcessPathUnavailable =
        LoggerMessage.Define(LogLevel.Warning, new EventId(5, "ProcessPathUnavailable"), "Environment.ProcessPath is not available to configure startup.");

    private static readonly Action<ILogger, string, string, Exception?> LogAddedStartup =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(6, "AddedStartup"), "Added {AppName} to Windows startup: {Path}.");

    private static readonly Action<ILogger, string, Exception?> LogRemovedStartup =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(7, "RemovedStartup"), "Removed {AppName} from Windows startup.");

    private readonly ILogger<WindowsStartupService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsStartupService"/> class.
    /// </summary>
    public WindowsStartupService(ILogger<WindowsStartupService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsStartupEnabled()
    {
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                return false;
            }

            return CheckStartupRegistry();
        }
        catch (Exception ex)
        {
            LogInspectFailed(_logger, ex);
            return false;
        }
    }

    /// <inheritdoc />
    public bool SetStartup(bool enable)
    {
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                LogNotSupported(_logger, null);
                return false;
            }

            return UpdateStartupRegistry(enable);
        }
        catch (Exception ex)
        {
            LogUpdateFailed(_logger, enable, ex);
            return false;
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool CheckStartupRegistry()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(AppName) is not null;
    }

    [SupportedOSPlatform("windows")]
    private bool UpdateStartupRegistry(bool enable)
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key is null)
        {
            LogKeyOpenFailed(_logger, null);
            return false;
        }

        if (enable)
        {
            string exePath = Environment.ProcessPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(exePath))
            {
                LogProcessPathUnavailable(_logger, null);
                return false;
            }

            key.SetValue(AppName, $"\"{exePath}\"");
            LogAddedStartup(_logger, AppName, exePath, null);
        }
        else
        {
            if (key.GetValue(AppName) is not null)
            {
                key.DeleteValue(AppName, false);
                LogRemovedStartup(_logger, AppName, null);
            }
        }

        return true;
    }
}
