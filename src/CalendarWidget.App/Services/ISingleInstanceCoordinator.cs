namespace CalendarWidget.App.Services;

/// <summary>
/// Coordinates single-instance lifecycle enforcement and secondary-instance activation signaling.
/// </summary>
public interface ISingleInstanceCoordinator : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether this process successfully acquired the primary instance mutex.
    /// </summary>
    bool IsPrimary { get; }

    /// <summary>
    /// Signals the active primary instance to activate its user interface.
    /// </summary>
    /// <param name="timeoutMs">Connection timeout in milliseconds.</param>
    /// <returns><c>true</c> if the signal was delivered; otherwise, <c>false</c>.</returns>
    bool SignalPrimary(int timeoutMs = 1500);

    /// <summary>
    /// Sets the action to invoke when an activation request is received from a secondary instance.
    /// </summary>
    /// <param name="onActivateRequested">Action to run upon activation signal.</param>
    void SetActivationHandler(Action onActivateRequested);

    /// <summary>
    /// Starts the background IPC listener to receive activation signals from secondary instances.
    /// </summary>
    void StartListening();
}
