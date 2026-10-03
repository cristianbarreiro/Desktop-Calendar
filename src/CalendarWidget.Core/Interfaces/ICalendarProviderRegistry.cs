using CalendarWidget.Core.Enums;

namespace CalendarWidget.Core.Interfaces;

public interface ICalendarProviderRegistry
{
    ICalendarProvider GetProvider(CalendarProvider provider);
}
