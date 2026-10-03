using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Interfaces;
using CalendarWidget.Core.Models;
using CalendarWidget.Infrastructure.Persistence;
using CalendarWidget.Infrastructure.Services.Outlook;
using CalendarWidget.IntegrationTests.Helpers;
using FluentAssertions;

namespace CalendarWidget.IntegrationTests.Providers;

public sealed class MicrosoftCalendarConnectionServiceTests
{
    [Fact]
    public async Task AccessTokenProvider_WhenAccountIsDisconnected_RejectsProviderRequests()
    {
        await using SqliteTestContext database = await SqliteTestContext.CreateAsync();
        MicrosoftAccessTokenProvider tokens = new(new EfCalendarCatalogRepository(database.Context), new FakeMicrosoftOAuthClient());

        Func<Task> getToken = () => tokens.GetAccessTokenAsync(Guid.NewGuid());

        await getToken.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Connected Microsoft account was not found.");
    }

    [Fact]
    public async Task ConnectAndDiscover_PersistsMicrosoftIdentityAndIndependentCalendarSelection()
    {
        await using SqliteTestContext database = await SqliteTestContext.CreateAsync();
        EfCalendarCatalogRepository catalogs = new(database.Context);
        CalendarAccount googleAccount = new()
        {
            Id = Guid.NewGuid(),
            Provider = CalendarProvider.Google,
            ProviderAccountId = "google-user",
            DisplayName = "Google user",
            CreatedAt = DateTime.UtcNow,
        };
        await catalogs.SaveAccountAsync(googleAccount);
        FakeMicrosoftOAuthClient oauth = new();
        MicrosoftCalendarProviderStub provider = new();
        MicrosoftCalendarConnectionService service = new(oauth, catalogs, new ProviderRegistry(provider));

        CalendarAccount account = await service.ConnectMicrosoftAccountAsync();
        IReadOnlyList<Calendar> calendars = await service.DiscoverMicrosoftCalendarsAsync(account.Id);
        await service.SetMicrosoftCalendarEnabledAsync(calendars[0].Id, true);

        account.Provider.Should().Be(CalendarProvider.Microsoft);
        account.ProviderAccountId.Should().Be("home-id.tenant-id");
        (await service.GetMicrosoftAccountsAsync()).Should().ContainSingle();
        calendars.Should().HaveCount(2).And.OnlyContain(calendar =>
            calendar.Provider == CalendarProvider.Microsoft && calendar.AccountId == account.Id && !calendar.IsEnabled);
        (await catalogs.GetCalendarsAsync()).Single(calendar => calendar.Id == calendars[0].Id).IsEnabled.Should().BeTrue();
        (await catalogs.GetCalendarsAsync()).Single(calendar => calendar.Id == calendars[1].Id).IsEnabled.Should().BeFalse();
        (await catalogs.GetAccountsAsync()).Should().ContainSingle(candidate => candidate.Provider == CalendarProvider.Google);
    }

    private sealed class FakeMicrosoftOAuthClient : IMicrosoftOAuthClient
    {
        public Task<MicrosoftAuthenticatedAccount> ConnectAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new MicrosoftAuthenticatedAccount("home-id.tenant-id", "Microsoft user"));

        public Task<string> GetAccessTokenAsync(string accountId, CancellationToken cancellationToken = default) =>
            Task.FromResult("fake-access-token");

        public Task DisconnectAsync(string accountId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ProviderRegistry(ICalendarProvider provider) : ICalendarProviderRegistry
    {
        public ICalendarProvider GetProvider(CalendarProvider type) =>
            type == provider.Provider ? provider : throw new NotSupportedException();
    }

    private sealed class MicrosoftCalendarProviderStub : ICalendarProvider
    {
        public CalendarProvider Provider => CalendarProvider.Microsoft;
        public bool SupportsIncrementalChanges => true;
        public bool SupportsEventWrites => false;

        public Task<IReadOnlyList<CalendarProviderCalendar>> GetCalendarsAsync(
            Guid? accountId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CalendarProviderCalendar>>(
            [new CalendarProviderCalendar("primary", "Primary"), new CalendarProviderCalendar("work", "Work")]);

        public Task<IReadOnlyList<CalendarProviderEvent>> GetEventsForRangeAsync(
            Guid? accountId, string externalCalendarId, DateTime rangeStartUtc, DateTime rangeEndUtc,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<CalendarProviderChangePage> GetChangesAsync(
            Guid? accountId, string externalCalendarId, string? cursor, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<CalendarProviderEventIdentity> CreateEventAsync(
            Guid? accountId, string externalCalendarId, CalendarEvent calendarEvent,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<CalendarProviderEventIdentity> UpdateEventAsync(
            Guid? accountId, string externalCalendarId, string externalEventId, string? expectedVersion,
            CalendarEvent calendarEvent, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task DeleteEventAsync(
            Guid? accountId, string externalCalendarId, string externalEventId, string? expectedVersion,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
