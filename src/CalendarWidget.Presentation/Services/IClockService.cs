namespace CalendarWidget.Presentation.Services;

/// <summary>
/// Provides local date and time information and periodic tick updates for UI clocks.
/// </summary>
public interface IClockService
{
    /// <summary>
    /// Gets the current local date and time.
    /// </summary>
    DateTime Now { get; }

    /// <summary>
    /// Gets the current local date.
    /// </summary>
    DateOnly Today => DateOnly.FromDateTime(Now);

    /// <summary>
    /// Raised periodically (e.g. every second) when the clock advances.
    /// </summary>
    event EventHandler<DateTime>? TimeChanged;
}
