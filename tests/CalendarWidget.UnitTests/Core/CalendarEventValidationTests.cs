using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Exceptions;
using FluentAssertions;

namespace CalendarWidget.UnitTests.Core;

public sealed class CalendarEventValidationTests
{
    private static CalendarEvent ValidEvent() => new()
    {
        Id = Guid.NewGuid(),
        Title = "Meeting",
        StartTime = new DateTime(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc),
        EndTime = new DateTime(2026, 9, 15, 11, 0, 0, DateTimeKind.Utc),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    // ── Title ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyTitle_ThrowsDomainValidationException()
    {
        CalendarEvent ev = ValidEvent();
        ev.Title = string.Empty;
        Action act = () => ev.Validate();
        act.Should().Throw<DomainValidationException>().WithMessage("*title*");
    }

    [Fact]
    public void Validate_WhitespaceTitleOnly_ThrowsDomainValidationException()
    {
        CalendarEvent ev = ValidEvent();
        ev.Title = "   ";
        Action act = () => ev.Validate();
        act.Should().Throw<DomainValidationException>().WithMessage("*title*");
    }

    [Fact]
    public void Validate_TitleExactly200Chars_Succeeds()
    {
        CalendarEvent ev = ValidEvent();
        ev.Title = new string('A', 200);
        Action act = () => ev.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_TitleOver200Chars_ThrowsDomainValidationException()
    {
        CalendarEvent ev = ValidEvent();
        ev.Title = new string('A', 201);
        Action act = () => ev.Validate();
        act.Should().Throw<DomainValidationException>().WithMessage("*200*");
    }

    // ── Description ───────────────────────────────────────────────────────────

    [Fact]
    public void Validate_DescriptionExactly2000Chars_Succeeds()
    {
        CalendarEvent ev = ValidEvent();
        ev.Description = new string('B', 2000);
        Action act = () => ev.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_DescriptionOver2000Chars_ThrowsDomainValidationException()
    {
        CalendarEvent ev = ValidEvent();
        ev.Description = new string('B', 2001);
        Action act = () => ev.Validate();
        act.Should().Throw<DomainValidationException>().WithMessage("*2000*");
    }

    [Fact]
    public void Validate_NullDescription_Succeeds()
    {
        CalendarEvent ev = ValidEvent();
        ev.Description = null;
        Action act = () => ev.Validate();
        act.Should().NotThrow();
    }

    // ── Time range ────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_EndTimeEqualToStartTime_Succeeds()
    {
        CalendarEvent ev = ValidEvent();
        ev.EndTime = ev.StartTime;
        Action act = () => ev.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_EndTimeBeforeStartTime_ThrowsDomainValidationException()
    {
        CalendarEvent ev = ValidEvent();
        ev.EndTime = ev.StartTime.AddMinutes(-1);
        Action act = () => ev.Validate();
        act.Should().Throw<DomainValidationException>().WithMessage("*end time*");
    }

    [Fact]
    public void Validate_ValidEvent_DoesNotThrow()
    {
        CalendarEvent ev = ValidEvent();
        Action act = () => ev.Validate();
        act.Should().NotThrow();
    }
}
