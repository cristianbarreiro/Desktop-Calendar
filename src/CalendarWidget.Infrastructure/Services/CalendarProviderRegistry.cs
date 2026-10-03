using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Interfaces;

namespace CalendarWidget.Infrastructure.Services;

public sealed class CalendarProviderRegistry(IEnumerable<ICalendarProvider> providers) : ICalendarProviderRegistry
{
    private readonly Dictionary<CalendarProvider, ICalendarProvider> providersByType =
        providers.ToDictionary(provider => provider.Provider);

    public ICalendarProvider GetProvider(CalendarProvider provider) =>
        providersByType.TryGetValue(provider, out ICalendarProvider? calendarProvider)
            ? calendarProvider
            : throw new NotSupportedException($"Calendar provider '{provider}' is not registered.");
}
