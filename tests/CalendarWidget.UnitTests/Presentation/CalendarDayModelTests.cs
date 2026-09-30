using CalendarWidget.Presentation.Models;
using FluentAssertions;

namespace CalendarWidget.UnitTests.Presentation;

public sealed class CalendarDayModelTests
{
    [Fact]
    public void AccessibleDescription_WhenTodayWithEvents_IncludesTodayAndEventsDescription()
    {
        // Arrange
        DateOnly date = new(2026, 9, 30);
        CalendarDayModel day = new(date, 30, IsCurrentMonth: true, IsToday: true, HasEvents: true);

        // Act
        string description = day.AccessibleDescription;

        // Assert
        description.Should().Contain("Today");
        description.Should().Contain("has events");
        description.Should().Contain(date.Day.ToString(System.Globalization.CultureInfo.CurrentCulture));
    }

    [Fact]
    public void AccessibleDescription_WhenTodayWithoutEvents_IncludesTodayOnly()
    {
        // Arrange
        DateOnly date = new(2026, 9, 30);
        CalendarDayModel day = new(date, 30, IsCurrentMonth: true, IsToday: true, HasEvents: false);

        // Act
        string description = day.AccessibleDescription;

        // Assert
        description.Should().Contain("Today");
        description.Should().NotContain("has events");
    }

    [Fact]
    public void AccessibleDescription_WhenNotTodayWithEvents_IncludesEventsOnly()
    {
        // Arrange
        DateOnly date = new(2026, 9, 25);
        CalendarDayModel day = new(date, 25, IsCurrentMonth: true, IsToday: false, HasEvents: true);

        // Act
        string description = day.AccessibleDescription;

        // Assert
        description.Should().NotContain("Today");
        description.Should().Contain("has events");
    }

    [Fact]
    public void AccessibleDescription_WhenNotTodayWithoutEvents_ReturnsDateStringOnly()
    {
        // Arrange
        DateOnly date = new(2026, 9, 25);
        CalendarDayModel day = new(date, 25, IsCurrentMonth: true, IsToday: false, HasEvents: false);

        // Act
        string description = day.AccessibleDescription;

        // Assert
        description.Should().NotContain("Today");
        description.Should().NotContain("has events");
    }
}
