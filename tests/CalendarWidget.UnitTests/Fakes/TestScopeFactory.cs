using CalendarWidget.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CalendarWidget.UnitTests.Fakes;

/// <summary>
/// Test fake for <see cref="IServiceScopeFactory"/> that returns injected repository implementations.
/// </summary>
public sealed class TestScopeFactory : IServiceScopeFactory
{
    private readonly ICalendarEventRepository? _eventRepository;
    private readonly INoteRepository? _noteRepository;

    /// <summary>Initializes a factory returning an <see cref="ICalendarEventRepository"/>.</summary>
    public TestScopeFactory(ICalendarEventRepository repository)
    {
        _eventRepository = repository;
    }

    /// <summary>Initializes a factory returning an <see cref="INoteRepository"/>.</summary>
    public TestScopeFactory(INoteRepository repository)
    {
        _noteRepository = repository;
    }

    /// <summary>Initializes a factory returning both event and note repositories.</summary>
    public TestScopeFactory(ICalendarEventRepository? eventRepository, INoteRepository? noteRepository)
    {
        _eventRepository = eventRepository;
        _noteRepository = noteRepository;
    }

    /// <inheritdoc />
    public IServiceScope CreateScope() => new TestScope(_eventRepository, _noteRepository);

    private sealed class TestScope(ICalendarEventRepository? eventRepo, INoteRepository? noteRepo) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = new TestServiceProvider(eventRepo, noteRepo);
        public void Dispose() { }
    }

    private sealed class TestServiceProvider(ICalendarEventRepository? eventRepo, INoteRepository? noteRepo) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(ICalendarEventRepository))
                return eventRepo;
            if (serviceType == typeof(INoteRepository))
                return noteRepo;
            return null;
        }
    }
}
