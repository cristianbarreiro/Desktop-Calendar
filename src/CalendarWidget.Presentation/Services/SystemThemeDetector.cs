using System.Runtime.Versioning;
using Microsoft.Win32;

namespace CalendarWidget.Presentation.Services;

/// <summary>
/// Detects Windows system theme setting using Current User registry.
/// </summary>
public sealed class SystemThemeDetector : ISystemThemeDetector
{
    private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string ValueName = "AppsUseLightTheme";

    /// <inheritdoc />
    public bool IsSystemInDarkTheme()
    {
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                return true;
            }

            return CheckRegistry();
        }
        catch
        {
            return true;
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool CheckRegistry()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath, writable: false);
        object? val = key?.GetValue(ValueName);
        if (val is int intVal)
        {
            return intVal == 0;
        }

        return true;
    }
}
