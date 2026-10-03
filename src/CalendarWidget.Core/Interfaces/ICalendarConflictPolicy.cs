using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Models;

namespace CalendarWidget.Core.Interfaces;

public interface ICalendarConflictPolicy
{
    CalendarConflictResolution Resolve(CalendarEvent localEvent, CalendarProviderChange remoteChange);
}
