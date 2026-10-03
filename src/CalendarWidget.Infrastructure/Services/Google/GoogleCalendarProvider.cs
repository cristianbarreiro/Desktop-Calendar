using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Exceptions;
using CalendarWidget.Core.Interfaces;
using CalendarWidget.Core.Models;

namespace CalendarWidget.Infrastructure.Services.Google;

/// <summary>Google Calendar API v3 adapter with native sync-token and ETag support.</summary>
public sealed class GoogleCalendarProvider(
    HttpClient httpClient,
    IGoogleAccessTokenProvider accessTokens) : ICalendarProvider
{
    private const string ApiRoot = "https://www.googleapis.com/calendar/v3";
    private const int MaxResults = 2500;
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public CalendarProvider Provider => CalendarProvider.Google;
    public bool SupportsIncrementalChanges => true;
    public bool SupportsEventWrites => true;

    public async Task<IReadOnlyList<CalendarProviderCalendar>> GetCalendarsAsync(
        Guid? accountId,
        CancellationToken cancellationToken = default)
    {
        Guid requiredAccountId = RequireAccount(accountId);
        List<CalendarProviderCalendar> calendars = [];
        string? pageToken = null;
        do
        {
            string query = "maxResults=250";
            if (pageToken is not null)
                query += $"&pageToken={Uri.EscapeDataString(pageToken)}";
            string url = $"{ApiRoot}/users/me/calendarList?{query}";
            using JsonDocument response = await GetJsonAsync(url, requiredAccountId, cancellationToken);
            JsonElement root = response.RootElement;
            if (root.TryGetProperty("items", out JsonElement items))
            {
                foreach (JsonElement item in items.EnumerateArray())
                {
                    string? id = OptionalString(item, "id");
                    if (!string.IsNullOrWhiteSpace(id))
                        calendars.Add(new CalendarProviderCalendar(id, OptionalString(item, "summary") ?? id));
                }
            }
            pageToken = OptionalString(root, "nextPageToken");
        } while (pageToken is not null);
        return calendars;
    }

    public async Task<IReadOnlyList<CalendarProviderEvent>> GetEventsForRangeAsync(
        Guid? accountId,
        string externalCalendarId,
        DateTime rangeStartUtc,
        DateTime rangeEndUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rangeEndUtc, rangeStartUtc);
        Guid requiredAccountId = RequireAccount(accountId);
        string query = $"singleEvents=true&showDeleted=false&maxResults={MaxResults}" +
                       $"&timeMin={Uri.EscapeDataString(ToUtcString(rangeStartUtc))}" +
                       $"&timeMax={Uri.EscapeDataString(ToUtcString(rangeEndUtc))}";
        List<CalendarProviderEvent> events = [];
        string? pageToken = null;
        do
        {
            if (pageToken is not null)
                query += $"&pageToken={Uri.EscapeDataString(pageToken)}";
            string url = $"{EventsUrl(externalCalendarId)}?{query}";
            using JsonDocument response = await GetJsonAsync(url, requiredAccountId, cancellationToken);
            ReadEvents(response.RootElement, events);
            pageToken = OptionalString(response.RootElement, "nextPageToken");
        } while (pageToken is not null);
        return events;
    }

    public async Task<CalendarProviderChangePage> GetChangesAsync(
        Guid? accountId,
        string externalCalendarId,
        string? cursor,
        CancellationToken cancellationToken = default)
    {
        Guid requiredAccountId = RequireAccount(accountId);
        try
        {
            return await FetchChangesAsync(requiredAccountId, externalCalendarId, cursor, cancellationToken);
        }
        catch (GoogleSyncTokenExpiredException) when (!string.IsNullOrEmpty(cursor))
        {
            return await FetchChangesAsync(requiredAccountId, externalCalendarId, null, cancellationToken);
        }
    }

    public async Task<CalendarProviderEventIdentity> CreateEventAsync(
        Guid? accountId, string externalCalendarId, CalendarEvent calendarEvent, CancellationToken cancellationToken = default)
    {
        Guid requiredAccountId = RequireAccount(accountId);
        using HttpRequestMessage request = await CreateWriteRequestAsync(HttpMethod.Post, EventsUrl(externalCalendarId), requiredAccountId, calendarEvent, cancellationToken);
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
            return await GetEventIdentityAsync(requiredAccountId, externalCalendarId, calendarEvent.Id.ToString("N"), cancellationToken);
        response.EnsureSuccessStatusCode();
        using JsonDocument body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        return ReadIdentity(body.RootElement);
    }

    public async Task<CalendarProviderEventIdentity> UpdateEventAsync(
        Guid? accountId, string externalCalendarId, string externalEventId, string? expectedVersion,
        CalendarEvent calendarEvent, CancellationToken cancellationToken = default)
    {
        Guid requiredAccountId = RequireAccount(accountId);
        string url = $"{EventsUrl(externalCalendarId)}/{Uri.EscapeDataString(externalEventId)}";
        using HttpRequestMessage request = await CreateWriteRequestAsync(HttpMethod.Put, url, requiredAccountId, calendarEvent, cancellationToken, externalEventId);
        AddExpectedVersion(request, expectedVersion);
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.PreconditionFailed)
            throw new CalendarProviderConcurrencyException("Google Calendar event changed remotely during update; synchronization will reconcile the newer version before retrying.");
        response.EnsureSuccessStatusCode();
        using JsonDocument body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        return ReadIdentity(body.RootElement);
    }

    public async Task DeleteEventAsync(
        Guid? accountId, string externalCalendarId, string externalEventId, string? expectedVersion,
        CancellationToken cancellationToken = default)
    {
        Guid requiredAccountId = RequireAccount(accountId);
        string url = $"{EventsUrl(externalCalendarId)}/{Uri.EscapeDataString(externalEventId)}";
        string token = await accessTokens.GetAccessTokenAsync(requiredAccountId, cancellationToken);
        using HttpRequestMessage request = new(HttpMethod.Delete, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        AddExpectedVersion(request, expectedVersion);
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return;
        response.EnsureSuccessStatusCode();
    }

    private async Task<HttpRequestMessage> CreateWriteRequestAsync(
        HttpMethod method, string url, Guid accountId, CalendarEvent calendarEvent, CancellationToken cancellationToken,
        string? externalEventId = null)
    {
        string token = await accessTokens.GetAccessTokenAsync(accountId, cancellationToken);
        HttpRequestMessage request = new(method, url)
        {
            Content = JsonContent.Create(CreateEventPayload(calendarEvent, externalEventId)),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static object CreateEventPayload(CalendarEvent calendarEvent, string? externalEventId)
    {
        calendarEvent.Validate();
        object start = calendarEvent.IsAllDay
            ? new { date = calendarEvent.StartTime.ToString("yyyy-MM-dd", Invariant) }
            : new { dateTime = ToUtcString(calendarEvent.StartTime) };
        object end = calendarEvent.IsAllDay
            ? new
            {
                date = (calendarEvent.EndTime.TimeOfDay == TimeSpan.Zero && calendarEvent.EndTime.Date > calendarEvent.StartTime.Date
                ? calendarEvent.EndTime.Date
                : calendarEvent.EndTime.Date.AddDays(1)).ToString("yyyy-MM-dd", Invariant)
            }
            : new { dateTime = ToUtcString(calendarEvent.EndTime) };
        return new
        {
            id = externalEventId ?? calendarEvent.Id.ToString("N"),
            summary = calendarEvent.Title,
            description = calendarEvent.Description,
            location = calendarEvent.Location,
            start,
            end,
        };
    }

    private static void AddExpectedVersion(HttpRequestMessage request, string? expectedVersion)
    {
        if (!string.IsNullOrWhiteSpace(expectedVersion))
            request.Headers.TryAddWithoutValidation("If-Match", expectedVersion);
    }

    private static CalendarProviderEventIdentity ReadIdentity(JsonElement root)
    {
        string? id = OptionalString(root, "id");
        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidDataException("Google Calendar write response did not include an event ID.");
        return new CalendarProviderEventIdentity(id, OptionalString(root, "etag"));
    }

    private async Task<CalendarProviderEventIdentity> GetEventIdentityAsync(
        Guid accountId, string calendarId, string eventId, CancellationToken cancellationToken)
    {
        using JsonDocument response = await GetJsonAsync(
            $"{EventsUrl(calendarId)}/{Uri.EscapeDataString(eventId)}", accountId, cancellationToken);
        return ReadIdentity(response.RootElement);
    }

    private async Task<CalendarProviderChangePage> FetchChangesAsync(
        Guid accountId,
        string externalCalendarId,
        string? cursor,
        CancellationToken cancellationToken)
    {
        string fixedQuery = $"singleEvents=true&showDeleted=true&maxResults={MaxResults}";
        string? pageToken = null;
        string? nextSyncToken = null;
        List<CalendarProviderChange> changes = [];
        do
        {
            string query = fixedQuery;
            if (cursor is not null)
                query += $"&syncToken={Uri.EscapeDataString(cursor)}";
            if (pageToken is not null)
                query += $"&pageToken={Uri.EscapeDataString(pageToken)}";

            using JsonDocument response = await GetJsonAsync(
                $"{EventsUrl(externalCalendarId)}?{query}", accountId, cancellationToken, translateGone: cursor is not null);
            JsonElement root = response.RootElement;
            List<CalendarProviderEvent> pageEvents = [];
            ReadEvents(root, pageEvents);
            foreach (CalendarProviderEvent item in pageEvents)
                changes.Add(new CalendarProviderChange(CalendarProviderChangeType.Upsert, item.ExternalEventId, item.ExternalVersion, item.Event));
            if (root.TryGetProperty("items", out JsonElement items))
            {
                foreach (JsonElement item in items.EnumerateArray())
                {
                    if (OptionalString(item, "status") == "cancelled")
                    {
                        string? id = OptionalString(item, "id");
                        if (!string.IsNullOrWhiteSpace(id))
                            changes.Add(new CalendarProviderChange(CalendarProviderChangeType.Delete, id, OptionalString(item, "etag"), null));
                    }
                }
            }

            pageToken = OptionalString(root, "nextPageToken");
            nextSyncToken = OptionalString(root, "nextSyncToken") ?? nextSyncToken;
        } while (pageToken is not null);

        if (nextSyncToken is null)
            throw new InvalidDataException("Google Calendar response did not provide a nextSyncToken on its final page.");
        return new CalendarProviderChangePage(changes, nextSyncToken, IsFullSnapshot: cursor is null);
    }

    private async Task<JsonDocument> GetJsonAsync(
        string url,
        Guid accountId,
        CancellationToken cancellationToken,
        bool translateGone = false)
    {
        string token = await accessTokens.GetAccessTokenAsync(accountId, cancellationToken);
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        if (translateGone && response.StatusCode == HttpStatusCode.Gone)
            throw new GoogleSyncTokenExpiredException();
        response.EnsureSuccessStatusCode();
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
    }

    private static void ReadEvents(JsonElement root, List<CalendarProviderEvent> events)
    {
        if (!root.TryGetProperty("items", out JsonElement items))
            return;
        foreach (JsonElement item in items.EnumerateArray())
        {
            if (OptionalString(item, "status") == "cancelled")
                continue;
            string? id = OptionalString(item, "id");
            if (string.IsNullOrWhiteSpace(id))
                continue;
            CalendarEvent? mapped = MapEvent(item);
            if (mapped is not null)
                events.Add(new CalendarProviderEvent(id, OptionalString(item, "etag"), mapped));
        }
    }

    private static CalendarEvent? MapEvent(JsonElement item)
    {
        if (!item.TryGetProperty("start", out JsonElement start) || !item.TryGetProperty("end", out JsonElement end))
            return null;
        bool allDay = start.TryGetProperty("date", out JsonElement startDate);
        DateTime startTime = allDay ? ParseDate(startDate.GetString()) : ParseDateTime(start.GetProperty("dateTime").GetString());
        DateTime endTime = allDay
            ? ParseDate(end.GetProperty("date").GetString())
            : ParseDateTime(end.GetProperty("dateTime").GetString());
        DateTime now = DateTime.UtcNow;
        return new CalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = OptionalString(item, "summary") is string title && !string.IsNullOrWhiteSpace(title) ? title : "(No title)",
            Description = OptionalString(item, "description"),
            Location = OptionalString(item, "location"),
            StartTime = startTime,
            EndTime = endTime,
            IsAllDay = allDay,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    private static DateTime ParseDate(string? value) =>
        DateTime.SpecifyKind(DateTime.ParseExact(value!, "yyyy-MM-dd", Invariant), DateTimeKind.Utc);

    private static DateTime ParseDateTime(string? value) =>
        DateTimeOffset.Parse(value!, Invariant, DateTimeStyles.AssumeUniversal).UtcDateTime;

    private static string EventsUrl(string calendarId) =>
        $"{ApiRoot}/calendars/{Uri.EscapeDataString(calendarId)}/events";

    private static Guid RequireAccount(Guid? accountId) =>
        accountId is Guid value && value != Guid.Empty
            ? value
            : throw new ArgumentException("Google Calendar operations require an account identity.", nameof(accountId));

    private static string ToUtcString(DateTime value) =>
        new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", Invariant);

    private static string? OptionalString(JsonElement element, string property) =>
        element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private sealed class GoogleSyncTokenExpiredException : Exception;
}
