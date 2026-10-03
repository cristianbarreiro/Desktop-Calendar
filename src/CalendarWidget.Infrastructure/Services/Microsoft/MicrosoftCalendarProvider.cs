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

namespace CalendarWidget.Infrastructure.Services.Outlook;

public sealed class MicrosoftCalendarProvider(
    HttpClient httpClient,
    IMicrosoftAccessTokenProvider accessTokens) : ICalendarProvider
{
    private const string GraphRoot = "https://graph.microsoft.com/v1.0";
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public CalendarProvider Provider => CalendarProvider.Microsoft;
    public bool SupportsIncrementalChanges => true;
    public bool SupportsEventWrites => true;

    public async Task<IReadOnlyList<CalendarProviderCalendar>> GetCalendarsAsync(
        Guid? accountId,
        CancellationToken cancellationToken = default)
    {
        Guid requiredAccount = RequireAccount(accountId);
        List<CalendarProviderCalendar> calendars = [];
        string? url = $"{GraphRoot}/me/calendars?$select=id,name&$top=100";
        while (url is not null)
        {
            using JsonDocument response = await GetJsonAsync(url, requiredAccount, cancellationToken);
            if (response.RootElement.TryGetProperty("value", out JsonElement values))
            {
                foreach (JsonElement item in values.EnumerateArray())
                {
                    string? id = OptionalString(item, "id");
                    if (!string.IsNullOrWhiteSpace(id))
                        calendars.Add(new CalendarProviderCalendar(id, OptionalString(item, "name") ?? id));
                }
            }
            url = OptionalString(response.RootElement, "@odata.nextLink");
        }
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
        Guid requiredAccount = RequireAccount(accountId);
        string start = Uri.EscapeDataString(ToUtcString(rangeStartUtc));
        string end = Uri.EscapeDataString(ToUtcString(rangeEndUtc));
        string? url = $"{CalendarUrl(externalCalendarId)}/calendarView?startDateTime={start}&endDateTime={end}&$top=100";
        List<CalendarProviderEvent> events = [];
        while (url is not null)
        {
            using JsonDocument response = await GetJsonAsync(url, requiredAccount, cancellationToken);
            ReadEvents(response.RootElement, events);
            url = OptionalString(response.RootElement, "@odata.nextLink");
        }
        return events;
    }

    public async Task<CalendarProviderChangePage> GetChangesAsync(
        Guid? accountId,
        string externalCalendarId,
        string? cursor,
        CancellationToken cancellationToken = default)
    {
        Guid requiredAccount = RequireAccount(accountId);
        try
        {
            return await FetchDeltaAsync(requiredAccount, externalCalendarId, cursor, cancellationToken);
        }
        catch (MicrosoftDeltaExpiredException) when (!string.IsNullOrWhiteSpace(cursor))
        {
            return await FetchDeltaAsync(requiredAccount, externalCalendarId, null, cancellationToken);
        }
    }

    public Task<CalendarProviderEventIdentity> CreateEventAsync(
        Guid? accountId,
        string externalCalendarId,
        CalendarEvent calendarEvent,
        CancellationToken cancellationToken = default) =>
        WriteEventAsync(HttpMethod.Post, $"{CalendarUrl(externalCalendarId)}/events", accountId,
            calendarEvent, null, calendarEvent.Id.ToString("N"), null, cancellationToken);

    public async Task<CalendarProviderEventIdentity> UpdateEventAsync(
        Guid? accountId,
        string externalCalendarId,
        string externalEventId,
        string? expectedVersion,
        CalendarEvent calendarEvent,
        CancellationToken cancellationToken = default)
    {
        return await WriteEventAsync(new HttpMethod("PATCH"),
            $"{CalendarUrl(externalCalendarId)}/events/{Uri.EscapeDataString(externalEventId)}",
            accountId, calendarEvent, externalEventId, null, expectedVersion, cancellationToken);
    }

    public async Task DeleteEventAsync(
        Guid? accountId,
        string externalCalendarId,
        string externalEventId,
        string? expectedVersion,
        CancellationToken cancellationToken = default)
    {
        Guid requiredAccount = RequireAccount(accountId);
        using HttpRequestMessage request = await CreateRequestAsync(
            HttpMethod.Delete,
            $"{CalendarUrl(externalCalendarId)}/events/{Uri.EscapeDataString(externalEventId)}",
            requiredAccount,
            cancellationToken);
        AddExpectedVersion(request, expectedVersion);
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.PreconditionFailed)
            throw new CalendarProviderConcurrencyException("Microsoft event changed remotely during deletion; synchronization will reconcile the newer version before retrying.");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return;
        response.EnsureSuccessStatusCode();
    }

    private async Task<CalendarProviderEventIdentity> WriteEventAsync(
        HttpMethod method,
        string url,
        Guid? accountId,
        CalendarEvent calendarEvent,
        string? existingEventId,
        string? transactionId,
        string? expectedVersion,
        CancellationToken cancellationToken)
    {
        Guid requiredAccount = RequireAccount(accountId);
        using HttpRequestMessage request = await CreateRequestAsync(method, url, requiredAccount, cancellationToken);
        request.Content = JsonContent.Create(CreateEventPayload(calendarEvent, transactionId));
        AddExpectedVersion(request, expectedVersion);
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.PreconditionFailed)
            throw new CalendarProviderConcurrencyException("Microsoft event changed remotely during update; synchronization will reconcile the newer version before retrying.");
        response.EnsureSuccessStatusCode();
        using JsonDocument result = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        string externalEventId = OptionalString(result.RootElement, "id") ?? existingEventId
            ?? throw new InvalidDataException("Microsoft Graph did not return the created event identity.");
        return new CalendarProviderEventIdentity(externalEventId,
            OptionalString(result.RootElement, "@odata.etag") ?? OptionalString(result.RootElement, "changeKey"));
    }

    private static void AddExpectedVersion(HttpRequestMessage request, string? expectedVersion)
    {
        if (!string.IsNullOrWhiteSpace(expectedVersion))
            request.Headers.TryAddWithoutValidation("If-Match", expectedVersion);
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(
        HttpMethod method,
        string url,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        Uri uri = ValidateGraphUrl(url);
        string token = await accessTokens.GetAccessTokenAsync(accountId, cancellationToken);
        HttpRequestMessage request = new(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.TryAddWithoutValidation("Prefer", "IdType=\"ImmutableId\"");
        return request;
    }

    private static Dictionary<string, object?> CreateEventPayload(CalendarEvent calendarEvent, string? transactionId)
    {
        Dictionary<string, object?> payload = new()
        {
            ["subject"] = calendarEvent.Title,
            ["body"] = new { contentType = "text", content = calendarEvent.Description ?? string.Empty },
            ["start"] = CreateGraphDateTime(calendarEvent.StartTime),
            ["end"] = CreateGraphDateTime(calendarEvent.EndTime),
            ["isAllDay"] = calendarEvent.IsAllDay,
            ["location"] = new { displayName = calendarEvent.Location ?? string.Empty },
        };
        if (transactionId is not null)
            payload["transactionId"] = transactionId;
        return payload;
    }

    private static object CreateGraphDateTime(DateTime value) => new
    {
        dateTime = DateTime.SpecifyKind(value, DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss", Invariant),
        timeZone = "UTC",
    };

    private async Task<CalendarProviderChangePage> FetchDeltaAsync(
        Guid accountId,
        string externalCalendarId,
        string? cursor,
        CancellationToken cancellationToken)
    {
        string? url = cursor ?? CreateInitialDeltaUrl(externalCalendarId);
        List<CalendarProviderChange> changes = [];
        string? deltaLink = null;
        while (url is not null)
        {
            using JsonDocument response = await GetJsonAsync(
                url, accountId, cancellationToken, translateGone: !string.IsNullOrEmpty(cursor));
            JsonElement root = response.RootElement;
            ReadChanges(root, changes);
            url = OptionalString(root, "@odata.nextLink");
            deltaLink = OptionalString(root, "@odata.deltaLink") ?? deltaLink;
        }
        if (string.IsNullOrWhiteSpace(deltaLink))
            throw new InvalidDataException("Microsoft Graph delta response did not include an @odata.deltaLink.");
        return new CalendarProviderChangePage(changes, deltaLink, IsFullSnapshot: cursor is null);
    }

    private static string CreateInitialDeltaUrl(string externalCalendarId)
    {
        DateTimeOffset today = new(DateTime.UtcNow.Date, TimeSpan.Zero);
        string start = Uri.EscapeDataString(ToUtcString(today.AddDays(-365).UtcDateTime));
        string end = Uri.EscapeDataString(ToUtcString(today.AddDays(730).UtcDateTime));
        return $"{CalendarUrl(externalCalendarId)}/calendarView/delta?startDateTime={start}&endDateTime={end}&$top=100";
    }

    private async Task<JsonDocument> GetJsonAsync(
        string url,
        Guid accountId,
        CancellationToken cancellationToken,
        bool translateGone = false)
    {
        Uri uri = ValidateGraphUrl(url);
        using HttpRequestMessage request = await CreateRequestAsync(HttpMethod.Get, uri.AbsoluteUri, accountId, cancellationToken);
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        if (translateGone && response.StatusCode == HttpStatusCode.Gone)
            throw new MicrosoftDeltaExpiredException();
        response.EnsureSuccessStatusCode();
        return await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
    }

    private static void ReadChanges(JsonElement root, List<CalendarProviderChange> changes)
    {
        if (!root.TryGetProperty("value", out JsonElement values))
            return;
        foreach (JsonElement item in values.EnumerateArray())
        {
            string? id = OptionalString(item, "id");
            if (string.IsNullOrWhiteSpace(id))
                continue;
            string? version = OptionalString(item, "@odata.etag") ?? OptionalString(item, "changeKey");
            if (item.TryGetProperty("@removed", out _))
            {
                changes.Add(new CalendarProviderChange(CalendarProviderChangeType.Delete, id, version, null));
                continue;
            }
            CalendarEvent? calendarEvent = MapEvent(item);
            if (calendarEvent is not null)
                changes.Add(new CalendarProviderChange(CalendarProviderChangeType.Upsert, id, version, calendarEvent));
        }
    }

    private static void ReadEvents(JsonElement root, List<CalendarProviderEvent> events)
    {
        if (!root.TryGetProperty("value", out JsonElement values))
            return;
        foreach (JsonElement item in values.EnumerateArray())
        {
            string? id = OptionalString(item, "id");
            if (string.IsNullOrWhiteSpace(id))
                continue;
            CalendarEvent? calendarEvent = MapEvent(item);
            if (calendarEvent is not null)
                events.Add(new CalendarProviderEvent(id,
                    OptionalString(item, "@odata.etag") ?? OptionalString(item, "changeKey"), calendarEvent));
        }
    }

    private static CalendarEvent? MapEvent(JsonElement item)
    {
        if (!item.TryGetProperty("start", out JsonElement start) || !item.TryGetProperty("end", out JsonElement end))
            return null;
        bool allDay = item.TryGetProperty("isAllDay", out JsonElement allDayElement) && allDayElement.GetBoolean();
        DateTime startTime = ParseDateTime(start, allDay);
        DateTime endTime = ParseDateTime(end, allDay);
        string title = OptionalString(item, "subject") ?? "(No title)";
        string? description = item.TryGetProperty("body", out JsonElement body)
            ? OptionalString(body, "content")
            : null;
        string? location = item.TryGetProperty("location", out JsonElement locationObject)
            ? OptionalString(locationObject, "displayName")
            : null;
        DateTime now = DateTime.UtcNow;
        return new CalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Location = location,
            StartTime = startTime,
            EndTime = endTime,
            IsAllDay = allDay,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    private static DateTime ParseDateTime(JsonElement value, bool allDay)
    {
        string raw = value.GetProperty("dateTime").GetString()
            ?? throw new InvalidDataException("Microsoft Graph event dateTime was empty.");
        if (allDay)
            return DateTime.SpecifyKind(DateTime.Parse(raw, Invariant).Date, DateTimeKind.Utc);

        bool includesOffset = raw.EndsWith('Z') || raw.LastIndexOf('+') > 10 || raw.LastIndexOf('-') > 10;
        if (includesOffset)
            return DateTimeOffset.Parse(raw, Invariant, DateTimeStyles.AssumeUniversal).UtcDateTime;

        string timeZoneId = OptionalString(value, "timeZone")
            ?? throw new InvalidDataException("Microsoft Graph event did not include a time zone.");
        DateTime localTime = DateTime.SpecifyKind(DateTime.Parse(raw, Invariant), DateTimeKind.Unspecified);
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        return TimeZoneInfo.ConvertTimeToUtc(localTime, timeZone);
    }

    private static Uri ValidateGraphUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(uri.Host, "graph.microsoft.com", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Microsoft Graph returned an invalid continuation URL.");
        return uri;
    }

    private static string CalendarUrl(string calendarId) =>
        $"{GraphRoot}/me/calendars/{Uri.EscapeDataString(calendarId)}";

    private static Guid RequireAccount(Guid? accountId) => accountId is Guid id && id != Guid.Empty
        ? id
        : throw new ArgumentException("Microsoft Calendar operations require an account identity.", nameof(accountId));

    private static string ToUtcString(DateTime value) =>
        new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", Invariant);

    private static string? OptionalString(JsonElement element, string property) =>
        element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private sealed class MicrosoftDeltaExpiredException : Exception;
}
