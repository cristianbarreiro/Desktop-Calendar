using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Interfaces;
using CalendarWidget.Core.Models;

namespace CalendarWidget.Infrastructure.Services;

public sealed class LocalWinsCalendarConflictPolicy : ICalendarConflictPolicy
{
    public CalendarConflictResolution Resolve(CalendarEvent localEvent, CalendarProviderChange remoteChange) =>
        CalendarConflictResolution.PreferLocal;
}
