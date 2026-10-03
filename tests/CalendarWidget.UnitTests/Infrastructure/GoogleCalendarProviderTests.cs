using System.Net;
using System.Net.Http;
using System.Text;
using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Models;
using CalendarWidget.Infrastructure.Services.Google;
using FluentAssertions;

namespace CalendarWidget.UnitTests.Infrastructure;

public sealed class GoogleCalendarProviderTests
{
    [Fact]
    public async Task GetChanges_MapsGoogleEventFieldsAndUsesAccountScopedAccessToken()
    {
        Guid accountId = Guid.NewGuid();
        Guid? usedAccount = null;
        HttpClient httpClient = new(new SingleResponseHandler("""
            {"items":[{"id":"event-1","etag":"\"rev-1\"","summary":"Appointment","description":"Bring documents","location":"Office","start":{"dateTime":"2026-01-12T09:00:00-03:00"},"end":{"dateTime":"2026-01-12T10:00:00-03:00"}}],"nextSyncToken":"opaque/google/cursor"}
            """));
        GoogleCalendarProvider provider = new(httpClient, new FakeTokenProvider(id =>
        {
            usedAccount = id;
            return "fake-access-token";
        }));

        CalendarProviderChangePage page = await provider.GetChangesAsync(accountId, "calendar-id", null);

        usedAccount.Should().Be(accountId);
        page.Cursor.Should().Be("opaque/google/cursor");
        page.Changes.Should().ContainSingle();
        CalendarEvent imported = page.Changes.Single().Event!;
        imported.Title.Should().Be("Appointment");
        imported.Description.Should().Be("Bring documents");
        imported.Location.Should().Be("Office");
        imported.StartTime.Should().Be(new DateTime(2026, 1, 12, 12, 0, 0, DateTimeKind.Utc));
        page.Changes.Single().ExternalVersion.Should().Be("\"rev-1\"");
    }

    [Fact]
    public async Task GetCalendars_MapsGoogleCalendarIdentityAndName()
    {
        Guid accountId = Guid.NewGuid();
        GoogleCalendarProvider provider = new(
            new HttpClient(new SingleResponseHandler("""{"items":[{"id":"primary@example.com","summary":"Personal"}]}""")),
            new FakeTokenProvider(_ => "token"));

        IReadOnlyList<CalendarProviderCalendar> calendars = await provider.GetCalendarsAsync(accountId);

        calendars.Should().ContainSingle().Which.Should().Be(new CalendarProviderCalendar("primary@example.com", "Personal"));
    }

    [Fact]
    public async Task GetEventsForRange_MapsEventAndAppliesUtcBounds()
    {
        FakeTokenProvider tokenProvider = new(_ => "token");
        SingleResponseHandler handler = new("""
            {"items":[{"id":"event-2","etag":"v2","summary":"Range event","start":{"dateTime":"2026-03-01T10:00:00Z"},"end":{"dateTime":"2026-03-01T11:00:00Z"}}]}
            """);
        GoogleCalendarProvider provider = new(new HttpClient(handler), tokenProvider);

        IReadOnlyList<CalendarProviderEvent> events = await provider.GetEventsForRangeAsync(
            Guid.NewGuid(), "calendar-id", new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc));

        events.Should().ContainSingle().Which.Event.Title.Should().Be("Range event");
        handler.RequestUri!.Query.Should().Contain("timeMin=2026-03-01T00%3A00%3A00Z");
        handler.RequestUri.Query.Should().Contain("timeMax=2026-03-02T00%3A00%3A00Z");
    }

    private sealed class FakeTokenProvider(Func<Guid, string> tokenForAccount) : IGoogleAccessTokenProvider
    {
        public Task<string> GetAccessTokenAsync(Guid accountId, CancellationToken cancellationToken = default) =>
            Task.FromResult(tokenForAccount(accountId));
    }

    private sealed class SingleResponseHandler(string json) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            RecordAndRespond(request);

        private Task<HttpResponseMessage> RecordAndRespond(HttpRequestMessage request)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        }
    }
}
