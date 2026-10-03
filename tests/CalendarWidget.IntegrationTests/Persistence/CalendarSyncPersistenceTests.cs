using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Infrastructure.Persistence;
using CalendarWidget.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CalendarWidget.IntegrationTests.Persistence;

public sealed class CalendarSyncPersistenceTests
{
    [Fact]
    public async Task Repositories_SaveAndUpdateCalendarMappingsAndSyncState()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        EfCalendarCatalogRepository catalog = new(db.Context);
        EfCalendarSyncRepository sync = new(db.Context);
        CalendarAccount account = Account(CalendarProvider.Microsoft, "work-account");
        Calendar calendar = CalendarFor(account, "work-calendar");
        CalendarEvent ev = Event(calendar.Id);

        await catalog.SaveAccountAsync(account);
        await catalog.SaveCalendarAsync(calendar);
        db.Context.CalendarEvents.Add(ev);
        await db.Context.SaveChangesAsync();
        await sync.SaveMappingAsync(Mapping(ev, account, calendar, "event-id", "version-1", null));
        await sync.SaveMappingAsync(Mapping(ev, account, calendar, "event-id", "version-2", DateTime.UtcNow));
        await sync.SaveStateAsync(new CalendarSyncState { CalendarId = calendar.Id, Cursor = "cursor-v1" });
        await sync.SaveStateAsync(new CalendarSyncState { CalendarId = calendar.Id, Cursor = "cursor-v2" });

        (await catalog.GetAccountsAsync()).Should().ContainSingle(accountItem => accountItem.Id == account.Id);
        (await catalog.GetCalendarsAsync()).Should().Contain(calendarItem => calendarItem.Id == calendar.Id);
        (await sync.GetMappingsAsync(ev.Id)).Should().ContainSingle().Which.ExternalVersion.Should().Be("version-2");
        (await sync.FindMappingAsync(account.Provider, account.Id, calendar.Id, "event-id"))!
            .InternalEventId.Should().Be(ev.Id);
        (await sync.GetStateAsync(calendar.Id))!.Cursor.Should().Be("cursor-v2");
    }

    [Fact]
    public async Task SaveChanges_PersistsMultipleAccountsCalendarsMappingsAndOpaqueStates()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        DateTime now = DateTime.UtcNow;
        CalendarAccount google = Account(CalendarProvider.Google, "google-user");
        CalendarAccount microsoft = Account(CalendarProvider.Microsoft, "microsoft-user");
        Calendar googleCalendar = CalendarFor(google, "google-calendar");
        Calendar microsoftCalendar = CalendarFor(microsoft, "microsoft-calendar");
        CalendarEvent calendarEvent = Event(googleCalendar.Id);

        db.Context.CalendarAccounts.AddRange(google, microsoft);
        db.Context.Calendars.AddRange(googleCalendar, microsoftCalendar);
        db.Context.CalendarEvents.Add(calendarEvent);
        db.Context.CalendarEventMappings.AddRange(
            Mapping(calendarEvent, google, googleCalendar, "g-event", "g-v1", now),
            Mapping(calendarEvent, microsoft, microsoftCalendar, "m-event", "m-v2", now));
        db.Context.CalendarSyncStates.AddRange(
            new CalendarSyncState { CalendarId = googleCalendar.Id, Cursor = "opaque-google-cursor", LastSyncedAt = now },
            new CalendarSyncState { CalendarId = microsoftCalendar.Id, Cursor = "opaque-ms-delta-link", LastSyncedAt = now });

        await db.Context.SaveChangesAsync();
        db.Context.ChangeTracker.Clear();

        (await db.Context.CalendarAccounts.AsNoTracking().CountAsync()).Should().Be(2);
        (await db.Context.Calendars.AsNoTracking().CountAsync()).Should().Be(3);
        CalendarEvent restoredEvent = await db.Context.CalendarEvents.AsNoTracking().SingleAsync();
        restoredEvent.CalendarId.Should().Be(googleCalendar.Id);
        restoredEvent.Location.Should().BeNull();
        (await db.Context.CalendarEventMappings.AsNoTracking().CountAsync()).Should().Be(2);
        (await db.Context.CalendarSyncStates.AsNoTracking().Select(state => state.Cursor).ToListAsync())
            .Should().BeEquivalentTo("opaque-google-cursor", "opaque-ms-delta-link");
    }

    [Fact]
    public async Task EventMapping_RejectsDuplicateProviderEventIdentity()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        CalendarAccount account = Account(CalendarProvider.Google, "user");
        Calendar calendar = CalendarFor(account, "calendar");
        CalendarEvent first = Event(calendar.Id);
        CalendarEvent second = Event(calendar.Id);
        db.Context.CalendarAccounts.Add(account);
        db.Context.Calendars.Add(calendar);
        db.Context.CalendarEvents.AddRange(first, second);
        await db.Context.SaveChangesAsync();
        db.Context.CalendarEventMappings.Add(Mapping(first, account, calendar, "same-remote-id", "v1", DateTime.UtcNow));
        await db.Context.SaveChangesAsync();
        db.Context.ChangeTracker.Clear();

        db.Context.CalendarEventMappings.Add(Mapping(second, account, calendar, "same-remote-id", "v1", DateTime.UtcNow));
        Func<Task> act = () => db.Context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task EventMapping_AllowsSameRemoteIdInDifferentCalendarsOrAccounts()
    {
        await using SqliteTestContext db = await SqliteTestContext.CreateAsync();
        CalendarAccount firstAccount = Account(CalendarProvider.Google, "user-1");
        CalendarAccount secondAccount = Account(CalendarProvider.Google, "user-2");
        Calendar firstCalendar = CalendarFor(firstAccount, "calendar-1");
        Calendar secondCalendar = CalendarFor(secondAccount, "calendar-2");
        CalendarEvent first = Event(firstCalendar.Id);
        CalendarEvent second = Event(secondCalendar.Id);
        db.Context.AddRange(firstAccount, secondAccount, firstCalendar, secondCalendar, first, second);
        await db.Context.SaveChangesAsync();

        db.Context.CalendarEventMappings.AddRange(
            Mapping(first, firstAccount, firstCalendar, "same-id", null, null),
            Mapping(second, secondAccount, secondCalendar, "same-id", null, null));

        await db.Context.SaveChangesAsync();

        (await db.Context.CalendarEventMappings.CountAsync()).Should().Be(2);
    }

    private static CalendarAccount Account(CalendarProvider provider, string externalId) => new()
    {
        Id = Guid.NewGuid(),
        Provider = provider,
        ProviderAccountId = externalId,
        DisplayName = externalId,
        CreatedAt = DateTime.UtcNow,
    };

    private static Calendar CalendarFor(CalendarAccount account, string externalId) => new()
    {
        Id = Guid.NewGuid(),
        Provider = account.Provider,
        AccountId = account.Id,
        Name = externalId,
        ExternalId = externalId,
        CreatedAt = DateTime.UtcNow,
    };

    private static CalendarEvent Event(Guid calendarId) => new()
    {
        Id = Guid.NewGuid(),
        CalendarId = calendarId,
        Title = "Meeting",
        StartTime = DateTime.UtcNow,
        EndTime = DateTime.UtcNow.AddHours(1),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    private static CalendarEventMapping Mapping(
        CalendarEvent ev,
        CalendarAccount account,
        Calendar calendar,
        string remoteId,
        string? version,
        DateTime? lastSynced) => new()
        {
            InternalEventId = ev.Id,
            Provider = account.Provider,
            AccountId = account.Id,
            CalendarId = calendar.Id,
            ExternalEventId = remoteId,
            ExternalVersion = version,
            LastSyncedAt = lastSynced,
        };
}
