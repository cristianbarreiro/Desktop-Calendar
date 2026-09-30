namespace CalendarWidget.Core.Interfaces;

/// <summary>
/// Abstraction for configuring application startup with Windows logon.
/// </summary>
public interface IWindowsStartupService
{
    /// <summary>
    /// Gets whether the application is currently configured to launch on Windows logon.
    /// </summary>
    bool IsStartupEnabled();

    /// <summary>
    /// Configures or removes the application startup registration with Windows.
    /// </summary>
    /// <param name="enable">True to enable startup, false to disable.</param>
    /// <returns>True if the configuration was successfully applied; otherwise false.</returns>
    bool SetStartup(bool enable);
}
