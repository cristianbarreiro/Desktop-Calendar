using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Interfaces;
using CalendarWidget.Core.Models;
using CalendarWidget.Infrastructure.Persistence;
using CalendarWidget.Infrastructure.Services.Google;
using CalendarWidget.IntegrationTests.Helpers;
using FluentAssertions;

namespace CalendarWidget.IntegrationTests.Providers;

public sealed class GoogleCalendarConnectionServiceTests
{
    [Fact]
    public async Task ConnectAndDiscover_PersistsAccountsAndDisabledCalendarChoicesPerAccount()
    {
        await using SqliteTestContext database = await SqliteTestContext.CreateAsync();
        EfCalendarCatalogRepository catalogs = new(database.Context);
        FakeGoogleOAuthClient oauth = new();
        FakeCredentialStore credentials = new();
        GoogleCalendarProviderStub provider = new();
        GoogleCalendarConnectionService service = new(oauth, credentials, catalogs, new ProviderRegistry(provider));

        CalendarAccount firstAccount = await service.ConnectGoogleAccountAsync();
        CalendarAccount secondAccount = await service.ConnectGoogleAccountAsync();
        IReadOnlyList<Calendar> firstCalendars = await service.DiscoverGoogleCalendarsAsync(firstAccount.Id);
        IReadOnlyList<Calendar> secondCalendars = await service.DiscoverGoogleCalendarsAsync(secondAccount.Id);

        (await service.GetGoogleAccountsAsync()).Should().HaveCount(2);
        firstCalendars.Should().ContainSingle().Which.IsEnabled.Should().BeFalse();
        secondCalendars.Should().ContainSingle().Which.IsEnabled.Should().BeFalse();
        firstCalendars.Single().Id.Should().NotBe(secondCalendars.Single().Id);
        firstCalendars.Single().ExternalId.Should().Be(secondCalendars.Single().ExternalId);
        await service.SetCalendarEnabledAsync(firstCalendars.Single().Id, true);
        (await catalogs.GetCalendarsAsync()).Single(calendar => calendar.Id == firstCalendars.Single().Id)
            .IsEnabled.Should().BeTrue();
        (await catalogs.GetCalendarsAsync()).Single(calendar => calendar.Id == secondCalendars.Single().Id)
            .IsEnabled.Should().BeFalse();
        credentials.Values.Keys.Should().Contain(firstAccount.ProviderAccountId).And.Contain(secondAccount.ProviderAccountId);
    }

    private sealed class ProviderRegistry(ICalendarProvider provider) : ICalendarProviderRegistry
    {
        public ICalendarProvider GetProvider(CalendarProvider type) =>
            type == provider.Provider ? provider : throw new NotSupportedException();
    }

    private sealed class FakeGoogleOAuthClient : IGoogleOAuthClient
    {
        private int nextAccount;

        public Task<(GoogleAuthenticatedUser User, GoogleOAuthTokenSet Tokens)> AuthorizeAsync(CancellationToken cancellationToken = default)
        {
            int current = ++nextAccount;
            return Task.FromResult((
                new GoogleAuthenticatedUser($"subject-{current}", $"user-{current}@example.test", $"User {current}"),
                new GoogleOAuthTokenSet($"access-{current}", $"refresh-{current}", DateTime.UtcNow.AddHours(1))));
        }

        public Task<GoogleOAuthTokenSet> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default) =>
            Task.FromResult(new GoogleOAuthTokenSet("refreshed-access", refreshToken, DateTime.UtcNow.AddHours(1)));
    }

    private sealed class FakeCredentialStore : IGoogleCredentialStore
    {
        public Dictionary<string, string> Values { get; } = new(StringComparer.Ordinal);
        public Task<string?> ReadAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(Values.GetValueOrDefault(key));
        public Task WriteAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            Values[key] = value;
            return Task.CompletedTask;
        }
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            Values.Remove(key);
            return Task.CompletedTask;
        }
    }

    private sealed class GoogleCalendarProviderStub : ICalendarProvider
    {
        public CalendarProvider Provider => CalendarProvider.Google;
        public bool SupportsIncrementalChanges => true;
        public bool SupportsEventWrites => false;
        public Task<IReadOnlyList<CalendarProviderCalendar>> GetCalendarsAsync(Guid? accountId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CalendarProviderCalendar>>([new CalendarProviderCalendar("primary", "Primary")]);
        public Task<IReadOnlyList<CalendarProviderEvent>> GetEventsForRangeAsync(Guid? accountId, string externalCalendarId, DateTime rangeStartUtc, DateTime rangeEndUtc, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<CalendarProviderChangePage> GetChangesAsync(Guid? accountId, string externalCalendarId, string? cursor, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<CalendarProviderEventIdentity> CreateEventAsync(Guid? accountId, string externalCalendarId, CalendarEvent calendarEvent, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<CalendarProviderEventIdentity> UpdateEventAsync(Guid? accountId, string externalCalendarId, string externalEventId, string? expectedVersion, CalendarEvent calendarEvent, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task DeleteEventAsync(Guid? accountId, string externalCalendarId, string externalEventId, string? expectedVersion, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
