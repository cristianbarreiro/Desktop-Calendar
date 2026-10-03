using CalendarWidget.Core.Enums;

namespace CalendarWidget.Core.Entities;

public enum PendingCalendarOperationType
{
    Upsert,
    Delete,
}

public sealed class PendingCalendarOperation
{
    public Guid Id { get; set; }
    public Guid CalendarId { get; set; }
    public Guid InternalEventId { get; set; }
    public PendingCalendarOperationType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? LastError { get; set; }
}
