using System.Net;
using System.Net.Http;
using System.Text;
using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Exceptions;
using CalendarWidget.Infrastructure.Services.Google;
using FluentAssertions;

namespace CalendarWidget.IntegrationTests.Providers;

public sealed class GoogleCalendarProviderTests
{
    [Fact]
    public async Task GetChanges_MapsEventFieldsAndPreservesGoogleSyncToken()
    {
        Guid accountId = Guid.NewGuid();
        Guid? requestedAccount = null;
        FakeHttpHandler handler = new(_ => JsonResponse("""
            {
              "items": [
                {"id":"timed","etag":"\"v1\"","summary":"Design review","description":"Bring mockups","location":"Room 4","start":{"dateTime":"2026-05-01T10:00:00-03:00"},"end":{"dateTime":"2026-05-01T11:30:00-03:00"}},
                {"id":"all-day","etag":"\"v2\"","summary":"Holiday","start":{"date":"2026-05-02"},"end":{"date":"2026-05-03"}}
              ],
              "nextSyncToken":"google-cursor/opaque"
            }
            """));
        GoogleCalendarProvider provider = new(new HttpClient(handler), new FakeAccessTokenProvider(id =>
        {
            requestedAccount = id;
            return "access-token";
        }));

        var page = await provider.GetChangesAsync(accountId, "calendar@example.com", null);

        requestedAccount.Should().Be(accountId);
        provider.Provider.Should().Be(CalendarProvider.Google);
        provider.SupportsIncrementalChanges.Should().BeTrue();
        provider.SupportsEventWrites.Should().BeTrue();
        page.IsFullSnapshot.Should().BeTrue();
        page.Cursor.Should().Be("google-cursor/opaque");
        page.Changes.Should().HaveCount(2);
        page.Changes[0].Event!.Title.Should().Be("Design review");
        page.Changes[0].Event!.Description.Should().Be("Bring mockups");
        page.Changes[0].Event!.Location.Should().Be("Room 4");
        page.Changes[0].Event!.StartTime.Should().Be(new DateTime(2026, 5, 1, 13, 0, 0, DateTimeKind.Utc));
        page.Changes[1].Event!.IsAllDay.Should().BeTrue();
        page.Changes[1].Event!.StartTime.Should().Be(new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc));
        page.Changes[1].Event!.EndTime.Should().Be(new DateTime(2026, 5, 3, 0, 0, 0, DateTimeKind.Utc));
        handler.Requests.Should().ContainSingle().Which.Headers.Authorization!.Parameter.Should().Be("access-token");
    }

    [Fact]
    public async Task EventWrites_UseStableIdentityAndConditionalVersions()
    {
        List<HttpRequestMessage> requests = [];
        FakeHttpHandler handler = new(request =>
        {
            requests.Add(request);
            return request.Method == HttpMethod.Delete
                ? new HttpResponseMessage(HttpStatusCode.NoContent)
                : JsonResponse("""{"id":"0123456789abcdef0123456789abcdef","etag":"\"v2\""}""");
        });
        GoogleCalendarProvider provider = new(new HttpClient(handler), new FakeAccessTokenProvider(_ => "token"));
        DateTime now = DateTime.UtcNow;
        CalendarWidget.Core.Entities.CalendarEvent calendarEvent = new()
        {
            Id = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"),
            Title = "Sync me",
            StartTime = now,
            EndTime = now.AddHours(1),
            CreatedAt = now,
            UpdatedAt = now,
        };

        var created = await provider.CreateEventAsync(Guid.NewGuid(), "calendar", calendarEvent);
        var updated = await provider.UpdateEventAsync(Guid.NewGuid(), "calendar", created.ExternalEventId, created.ExternalVersion, calendarEvent);
        await provider.DeleteEventAsync(Guid.NewGuid(), "calendar", updated.ExternalEventId, updated.ExternalVersion);

        created.ExternalEventId.Should().Be("0123456789abcdef0123456789abcdef");
        requests[0].Method.Should().Be(HttpMethod.Post);
        handler.RequestBodies[0].Should().Contain("\"id\":\"0123456789abcdef0123456789abcdef\"");
        requests[1].Method.Should().Be(HttpMethod.Put);
        requests[1].Headers.GetValues("If-Match").Should().Contain("\"v2\"");
        requests[2].Method.Should().Be(HttpMethod.Delete);
        requests[2].Headers.GetValues("If-Match").Should().Contain("\"v2\"");
    }

    [Fact]
    public async Task Update_WhenEtagChanged_RejectsStaleWriteForConflictReconciliation()
    {
        int updateAttempts = 0;
        FakeHttpHandler handler = new(request => request.Method.Method switch
        {
            "PUT" when ++updateAttempts == 1 => new HttpResponseMessage(HttpStatusCode.PreconditionFailed),
            "GET" => JsonResponse("""{"id":"external-id","etag":"\"latest\""}"""),
            "PUT" => JsonResponse("""{"id":"external-id","etag":"\"updated\""}"""),
            _ => JsonResponse("""{"id":"external-id","etag":"\"initial\""}"""),
        });
        GoogleCalendarProvider provider = new(new HttpClient(handler), new FakeAccessTokenProvider(_ => "token"));
        DateTime now = DateTime.UtcNow;
        CalendarWidget.Core.Entities.CalendarEvent calendarEvent = new()
        {
            Id = Guid.NewGuid(),
            Title = "Local wins",
            StartTime = now,
            EndTime = now.AddHours(1),
            CreatedAt = now,
            UpdatedAt = now,
        };

        Func<Task> update = () => provider.UpdateEventAsync(Guid.NewGuid(), "calendar", "external-id", "stale", calendarEvent);

        await update.Should().ThrowAsync<CalendarProviderConcurrencyException>();
        handler.Requests.Select(request => request.Method.Method).Should().Equal("PUT");
        handler.Requests[0].Headers.GetValues("If-Match").Should().Contain("stale");
    }

    [Fact]
    public async Task Create_WhenStableIdAlreadyExists_ReturnsExistingIdentityInsteadOfCreatingDuplicate()
    {
        FakeHttpHandler handler = new(request => request.Method.Method == "POST"
            ? new HttpResponseMessage(HttpStatusCode.Conflict)
            : JsonResponse("""{"id":"0123456789abcdef0123456789abcdef","etag":"existing-version"}"""));
        GoogleCalendarProvider provider = new(new HttpClient(handler), new FakeAccessTokenProvider(_ => "token"));
        DateTime now = DateTime.UtcNow;
        CalendarWidget.Core.Entities.CalendarEvent calendarEvent = new()
        {
            Id = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"),
            Title = "Idempotent",
            StartTime = now,
            EndTime = now.AddHours(1),
            CreatedAt = now,
            UpdatedAt = now,
        };

        var result = await provider.CreateEventAsync(Guid.NewGuid(), "calendar", calendarEvent);

        result.ExternalEventId.Should().Be(calendarEvent.Id.ToString("N"));
        result.ExternalVersion.Should().Be("existing-version");
        handler.Requests.Select(request => request.Method.Method).Should().Equal("POST", "GET");
    }

    [Fact]
    public async Task GetChanges_MapsDeletionAndUsesOfficialIncrementalCursor()
    {
        FakeHttpHandler handler = new(_ => JsonResponse("""
            {"items":[{"id":"deleted-event","status":"cancelled","etag":"\"v3\""}],"nextSyncToken":"next-cursor"}
            """));
        GoogleCalendarProvider provider = new(new HttpClient(handler), new FakeAccessTokenProvider(_ => "token"));

        var page = await provider.GetChangesAsync(Guid.NewGuid(), "calendar-id", "old-cursor/+==");

        page.IsFullSnapshot.Should().BeFalse();
        page.Changes.Should().ContainSingle(change => change.Type == CalendarWidget.Core.Models.CalendarProviderChangeType.Delete
            && change.ExternalEventId == "deleted-event");
        handler.Requests.Single().RequestUri!.Query.Should().Contain("syncToken=old-cursor%2F%2B%3D%3D");
    }

    [Fact]
    public async Task GetChanges_WhenGoogleInvalidatesCursor_RetriesAsFullSnapshot()
    {
        int requestCount = 0;
        FakeHttpHandler handler = new(_ => ++requestCount == 1
            ? new HttpResponseMessage(HttpStatusCode.Gone)
            : JsonResponse("""{"items":[],"nextSyncToken":"replacement-cursor"}"""));
        GoogleCalendarProvider provider = new(new HttpClient(handler), new FakeAccessTokenProvider(_ => "token"));

        var page = await provider.GetChangesAsync(Guid.NewGuid(), "calendar-id", "expired-cursor");

        page.IsFullSnapshot.Should().BeTrue();
        page.Cursor.Should().Be("replacement-cursor");
        handler.Requests.Should().HaveCount(2);
        handler.Requests[0].RequestUri!.Query.Should().Contain("syncToken=expired-cursor");
        handler.Requests[1].RequestUri!.Query.Should().NotContain("syncToken");
    }

    [Fact]
    public async Task GetChanges_PaginatesBeforeReturningNextSyncToken()
    {
        int responseIndex = 0;
        FakeHttpHandler handler = new(_ => ++responseIndex == 1
            ? JsonResponse("""{"items":[],"nextPageToken":"page-2"}""")
            : JsonResponse("""{"items":[],"nextSyncToken":"last-page-cursor"}"""));
        GoogleCalendarProvider provider = new(new HttpClient(handler), new FakeAccessTokenProvider(_ => "token"));

        var page = await provider.GetChangesAsync(Guid.NewGuid(), "calendar-id", "starting-cursor");

        page.Cursor.Should().Be("last-page-cursor");
        handler.Requests.Should().HaveCount(2);
        handler.Requests[0].RequestUri!.Query.Should().Contain("syncToken=starting-cursor");
        handler.Requests[1].RequestUri!.Query.Should().Contain("syncToken=starting-cursor");
        handler.Requests[1].RequestUri!.Query.Should().Contain("pageToken=page-2");
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private sealed class FakeAccessTokenProvider(Func<Guid, string> getToken) : IGoogleAccessTokenProvider
    {
        public Task<string> GetAccessTokenAsync(Guid accountId, CancellationToken cancellationToken = default) =>
            Task.FromResult(getToken(accountId));
    }

    private sealed class FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string?> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            RequestBodies.Add(request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken));
            return responseFactory(request);
        }
    }
}
