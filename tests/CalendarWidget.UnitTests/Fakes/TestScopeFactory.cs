using CalendarWidget.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CalendarWidget.UnitTests.Fakes;

/// <summary>
/// Test fake for <see cref="IServiceScopeFactory"/> that returns a fixed <see cref="ICalendarEventRepository"/>.
/// </summary>
public sealed class TestScopeFactory(ICalendarEventRepository repository) : IServiceScopeFactory
{
    public IServiceScope CreateScope() => new TestScope(repository);

    private sealed class TestScope(ICalendarEventRepository repository) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = new TestServiceProvider(repository);
        public void Dispose() { }
    }

    private sealed class TestServiceProvider(ICalendarEventRepository repository) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(ICalendarEventRepository))
                return repository;
            return null;
        }
    }
}
