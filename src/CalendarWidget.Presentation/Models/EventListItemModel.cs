namespace CalendarWidget.Presentation.Models;

/// <summary>
/// Immutable presentation model for a single event in the day detail list.
/// </summary>
/// <param name="Id">The event identifier.</param>
/// <param name="Title">The event title.</param>
/// <param name="TimeLabel">Formatted time string (e.g. "10:00 – 11:30" or "All day").</param>
/// <param name="Description">Optional description preview.</param>
/// <param name="IsAllDay">Whether the event is an all-day event.</param>
public sealed record EventListItemModel(
    Guid Id,
    string Title,
    string TimeLabel,
    string? Description,
    bool IsAllDay,
    string CalendarName = "Local Calendar",
    string SyncStatus = "Local");
