using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using CalendarWidget.Core.Enums;
using CalendarWidget.Infrastructure.Services.Outlook;
using FluentAssertions;

namespace CalendarWidget.IntegrationTests.Providers;

public sealed class MicrosoftCalendarProviderTests
{
    [Fact]
    public async Task GetChanges_MapsEventsDeletionsAndPreservesOpaqueDeltaLinkAcrossPages()
    {
        Guid accountId = Guid.NewGuid();
        int responseIndex = 0;
        FakeHttpHandler handler = new(_ => ++responseIndex switch
        {
            1 => JsonResponse("""
                {
                  "value": [{
                    "id":"timed-event","changeKey":"version-1","subject":"Review",
                    "body":{"contentType":"text","content":"Agenda"},
                    "location":{"displayName":"Room 2"},"isAllDay":false,
                    "start":{"dateTime":"2026-01-03T10:00:00.0000000","timeZone":"Pacific Standard Time"},
                    "end":{"dateTime":"2026-01-03T11:00:00.0000000","timeZone":"Pacific Standard Time"}
                  }],
                  "@odata.nextLink":"https://graph.microsoft.com/v1.0/me/calendars/calendar-id/calendarView/delta?$skiptoken=opaque-page"
                }
                """),
            2 => JsonResponse("""
                {
                  "value": [
                    {"id":"all-day-event","changeKey":"version-2","subject":"Holiday","isAllDay":true,
                     "start":{"dateTime":"2026-04-02T00:00:00.0000000","timeZone":"Eastern Standard Time"},
                     "end":{"dateTime":"2026-04-03T00:00:00.0000000","timeZone":"Eastern Standard Time"}},
                    {"id":"deleted-event","@removed":{"reason":"deleted"}}
                  ],
                  "@odata.deltaLink":"https://graph.microsoft.com/v1.0/me/calendars/calendar-id/calendarView/delta?$deltatoken=opaque%2B%2F%3D"
                }
                """),
            _ => JsonResponse("""{"value":[],"@odata.deltaLink":"https://graph.microsoft.com/v1.0/delta?token=next"}"""),
        });
        MicrosoftCalendarProvider provider = new(new HttpClient(handler), new FakeAccessTokenProvider());

        var initial = await provider.GetChangesAsync(accountId, "calendar-id", null);

        provider.Provider.Should().Be(CalendarProvider.Microsoft);
        provider.SupportsIncrementalChanges.Should().BeTrue();
        provider.SupportsEventWrites.Should().BeTrue();
        initial.IsFullSnapshot.Should().BeTrue();
        initial.Changes.Should().HaveCount(3);
        initial.Changes[0].Event!.Title.Should().Be("Review");
        initial.Changes[0].Event!.Description.Should().Be("Agenda");
        initial.Changes[0].Event!.Location.Should().Be("Room 2");
        initial.Changes[0].Event!.StartTime.Should().Be(new DateTime(2026, 1, 3, 18, 0, 0, DateTimeKind.Utc));
        initial.Changes[0].ExternalVersion.Should().Be("version-1");
        initial.Changes[1].Event!.IsAllDay.Should().BeTrue();
        initial.Changes[1].Event!.StartTime.Should().Be(new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc));
        initial.Changes[2].Type.Should().Be(CalendarWidget.Core.Models.CalendarProviderChangeType.Delete);
        initial.Cursor.Should().Be("https://graph.microsoft.com/v1.0/me/calendars/calendar-id/calendarView/delta?$deltatoken=opaque%2B%2F%3D");
        handler.Requests.Should().HaveCount(2);
        handler.Requests[0].Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.Requests[0].Headers.GetValues("Prefer").Should().Contain("IdType=\"ImmutableId\"");

        var next = await provider.GetChangesAsync(accountId, "calendar-id", initial.Cursor);

        next.IsFullSnapshot.Should().BeFalse();
        handler.Requests[2].RequestUri!.AbsoluteUri.Should().Be(initial.Cursor);
    }

    [Fact]
    public async Task EventWrites_CreateUpdateAndDeleteUsingMappedMicrosoftFields()
    {
        FakeHttpHandler handler = new(request => request.Method switch
        {
            var method when method == HttpMethod.Post => JsonResponse("""{"id":"created-id","changeKey":"v1"}""", HttpStatusCode.Created),
            var method when method.Method == "PATCH" => JsonResponse("""{"id":"created-id","changeKey":"v2"}"""),
            _ => new HttpResponseMessage(HttpStatusCode.NoContent),
        });
        MicrosoftCalendarProvider provider = new(new HttpClient(handler), new FakeAccessTokenProvider());
        DateTime start = new(2026, 5, 4, 9, 30, 0, DateTimeKind.Utc);
        CalendarWidget.Core.Entities.CalendarEvent calendarEvent = new()
        {
            Id = Guid.NewGuid(),
            Title = "Planning",
            Description = "Agenda",
            Location = "Room 4",
            StartTime = start,
            EndTime = start.AddHours(1),
            CreatedAt = start,
            UpdatedAt = start,
        };

        var created = await provider.CreateEventAsync(Guid.NewGuid(), "calendar/one", calendarEvent);
        calendarEvent.Title = "Updated planning";
        var updated = await provider.UpdateEventAsync(
            Guid.NewGuid(), "calendar/one", created.ExternalEventId, created.ExternalVersion, calendarEvent);
        await provider.DeleteEventAsync(Guid.NewGuid(), "calendar/one", updated.ExternalEventId, updated.ExternalVersion);

        created.Should().Be(new CalendarWidget.Core.Models.CalendarProviderEventIdentity("created-id", "v1"));
        updated.Should().Be(new CalendarWidget.Core.Models.CalendarProviderEventIdentity("created-id", "v2"));
        handler.CapturedRequests.Select(request => request.Method.Method).Should().Equal("POST", "PATCH", "DELETE");
        handler.CapturedRequests[1].Headers.Should().ContainKey("If-Match").WhoseValue.Should().Contain("v1");
        handler.CapturedRequests[2].Headers.Should().ContainKey("If-Match").WhoseValue.Should().Contain("v2");
        handler.CapturedRequests.Select(request => request.Uri.AbsolutePath).Should().Equal(
            "/v1.0/me/calendars/calendar%2Fone/events",
            "/v1.0/me/calendars/calendar%2Fone/events/created-id",
            "/v1.0/me/calendars/calendar%2Fone/events/created-id");
        using JsonDocument payload = JsonDocument.Parse(handler.CapturedRequests[1].Body!);
        payload.RootElement.GetProperty("subject").GetString().Should().Be("Updated planning");
        payload.RootElement.GetProperty("body").GetProperty("content").GetString().Should().Be("Agenda");
        payload.RootElement.GetProperty("start").GetProperty("dateTime").GetString().Should().Be("2026-05-04T09:30:00");
        payload.RootElement.GetProperty("start").GetProperty("timeZone").GetString().Should().Be("UTC");
        payload.RootElement.GetProperty("location").GetProperty("displayName").GetString().Should().Be("Room 4");
        using JsonDocument createPayload = JsonDocument.Parse(handler.CapturedRequests[0].Body!);
        createPayload.RootElement.GetProperty("transactionId").GetString().Should().Be(calendarEvent.Id.ToString("N"));
    }

    [Fact]
    public async Task DeleteEvent_WhenExternalEventIsAlreadyMissing_IsIdempotent()
    {
        FakeHttpHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        MicrosoftCalendarProvider provider = new(new HttpClient(handler), new FakeAccessTokenProvider());

        Func<Task> delete = () => provider.DeleteEventAsync(Guid.NewGuid(), "calendar-id", "missing-event", null);

        await delete.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EventWrite_WhenGraphFails_PropagatesApiFailure()
    {
        FakeHttpHandler handler = new(_ => JsonResponse("""{"error":{"code":"ServiceUnavailable"}}""", HttpStatusCode.ServiceUnavailable));
        MicrosoftCalendarProvider provider = new(new HttpClient(handler), new FakeAccessTokenProvider());
        CalendarWidget.Core.Entities.CalendarEvent calendarEvent = new()
        {
            Title = "Must not be lost",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1),
        };

        Func<Task> create = () => provider.CreateEventAsync(Guid.NewGuid(), "calendar-id", calendarEvent);

        await create.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task UpdateEvent_WhenExternalEventIsMissing_PropagatesNotFound()
    {
        FakeHttpHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        MicrosoftCalendarProvider provider = new(new HttpClient(handler), new FakeAccessTokenProvider());
        CalendarWidget.Core.Entities.CalendarEvent calendarEvent = new()
        {
            Title = "Local change",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1),
        };

        Func<Task> update = () => provider.UpdateEventAsync(
            Guid.NewGuid(), "calendar-id", "missing-event", "version", calendarEvent);

        await update.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetChanges_WhenDeltaLinkExpires_RestartsCalendarViewDeltaSnapshot()
    {
        int requestCount = 0;
        FakeHttpHandler handler = new(_ => ++requestCount == 1
            ? new HttpResponseMessage(HttpStatusCode.Gone)
            : JsonResponse("""{"value":[],"@odata.deltaLink":"https://graph.microsoft.com/v1.0/fresh-delta"}"""));
        MicrosoftCalendarProvider provider = new(new HttpClient(handler), new FakeAccessTokenProvider());

        var page = await provider.GetChangesAsync(Guid.NewGuid(), "calendar-id", "https://graph.microsoft.com/v1.0/expired-delta");

        page.IsFullSnapshot.Should().BeTrue();
        page.Cursor.Should().Be("https://graph.microsoft.com/v1.0/fresh-delta");
        handler.Requests.Should().HaveCount(2);
        handler.Requests[0].RequestUri!.AbsoluteUri.Should().Be("https://graph.microsoft.com/v1.0/expired-delta");
        handler.Requests[1].RequestUri!.AbsolutePath.Should().Contain("calendarView/delta");
        handler.Requests[1].RequestUri!.Query.Should().Contain("startDateTime=").And.Contain("endDateTime=");
    }

    private static HttpResponseMessage JsonResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK) => new(statusCode)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private sealed class FakeAccessTokenProvider : IMicrosoftAccessTokenProvider
    {
        public Task<string> GetAccessTokenAsync(Guid accountId, CancellationToken cancellationToken = default) =>
            Task.FromResult("fake-access-token");
    }

    private sealed class FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];
        public List<CapturedRequest> CapturedRequests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            CapturedRequests.Add(new CapturedRequest(request.Method, request.RequestUri!,
                request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken),
                request.Headers.ToDictionary(header => header.Key, header => header.Value.ToArray(), StringComparer.OrdinalIgnoreCase)));
            return responseFactory(request);
        }
    }

    private sealed record CapturedRequest(HttpMethod Method, Uri Uri, string? Body, IReadOnlyDictionary<string, string[]> Headers);
}
