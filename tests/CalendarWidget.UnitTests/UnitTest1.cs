using CalendarWidget.Core.Entities;
using FluentAssertions;

namespace CalendarWidget.UnitTests.Core;

/// <summary>
/// Verifies the architecture is wired correctly with a basic entity test.
/// </summary>
public sealed class CalendarEventTests
{
    [Fact]
    public void CalendarEvent_Creation_SetsRequiredProperties()
    {
        var now = DateTime.UtcNow;
        var calendarEvent = new CalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = "Test Event",
            StartTime = now,
            EndTime = now.AddHours(1),
            CreatedAt = now,
            UpdatedAt = now,
        };

        calendarEvent.Title.Should().Be("Test Event");
        calendarEvent.StartTime.Should().Be(now);
        calendarEvent.EndTime.Should().BeAfter(calendarEvent.StartTime);
        calendarEvent.IsAllDay.Should().BeFalse();
        calendarEvent.Description.Should().BeNull();
    }
}
