using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Exceptions;
using FluentAssertions;

namespace CalendarWidget.UnitTests.Core;

public sealed class CalendarSyncDomainTests
{
    [Fact]
    public void Calendar_ExternalIdentityAndAccount_AreIndependentOfEvents()
    {
        CalendarAccount account = new()
        {
            Id = Guid.NewGuid(),
            Provider = CalendarProvider.Google,
            ProviderAccountId = "account-1",
            DisplayName = "Work account",
            CreatedAt = DateTime.UtcNow,
        };
        Calendar calendar = new()
        {
            Id = Guid.NewGuid(),
            Provider = account.Provider,
            AccountId = account.Id,
            Name = "Primary",
            ExternalId = "calendar-1",
            CreatedAt = DateTime.UtcNow,
        };

        account.Validate();
        calendar.Validate();

        calendar.AccountId.Should().Be(account.Id);
        calendar.Provider.Should().Be(CalendarProvider.Google);
    }

    [Fact]
    public void Calendar_ExternalProviderWithoutAccount_IsRejected()
    {
        Calendar calendar = new()
        {
            Id = Guid.NewGuid(),
            Provider = CalendarProvider.Microsoft,
            Name = "Work",
            ExternalId = "calendar-1",
        };

        Action act = calendar.Validate;

        act.Should().Throw<DomainValidationException>().WithMessage("*account*");
    }

    [Fact]
    public void Event_CanBelongToDifferentCalendarsAndHaveOptionalLocation()
    {
        Guid firstCalendarId = Guid.NewGuid();
        Guid secondCalendarId = Guid.NewGuid();
        CalendarEvent first = ValidEvent(firstCalendarId);
        CalendarEvent second = ValidEvent(secondCalendarId);
        first.Location = "Conference room";

        first.Validate();
        second.Validate();

        first.CalendarId.Should().Be(firstCalendarId);
        first.Location.Should().Be("Conference room");
        second.CalendarId.Should().Be(secondCalendarId);
        second.Location.Should().BeNull();
    }

    [Fact]
    public void EventMapping_RequiresExternalIdentityAndCannotRepresentLocalProvider()
    {
        CalendarEventMapping mapping = new()
        {
            InternalEventId = Guid.NewGuid(),
            Provider = CalendarProvider.Local,
            AccountId = Guid.NewGuid(),
            CalendarId = Guid.NewGuid(),
            ExternalEventId = "event-1",
        };

        Action act = mapping.Validate;

        act.Should().Throw<DomainValidationException>().WithMessage("*Local*");
    }

    [Fact]
    public void SyncState_CursorIsStoredAsOpaqueText()
    {
        const string opaqueCursor = "provider-owned:cursor+/%==";
        CalendarSyncState state = new() { CalendarId = Guid.NewGuid(), Cursor = opaqueCursor };

        state.Validate();

        state.Cursor.Should().Be(opaqueCursor);
    }

    private static CalendarEvent ValidEvent(Guid calendarId) => new()
    {
        Id = Guid.NewGuid(),
        CalendarId = calendarId,
        Title = "Event",
        StartTime = DateTime.UtcNow,
        EndTime = DateTime.UtcNow.AddHours(1),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };
}
