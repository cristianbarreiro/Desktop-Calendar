using CalendarWidget.Core.Exceptions;
using CalendarWidget.Core.ValueObjects;
using FluentAssertions;

namespace CalendarWidget.UnitTests.Core;

public sealed class DateRangeTests
{
    [Fact]
    public void Constructor_WhenEndBeforeStart_ThrowsDomainValidationException()
    {
        // Arrange
        var start = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);
        var end = start.AddHours(-1);

        // Act
        var act = () => new DateRange(start, end);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("*cannot be earlier than start date*");
    }

    [Fact]
    public void Contains_WhenDateInsideRange_ReturnsTrue()
    {
        // Arrange
        var start = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc);
        var range = new DateRange(start, end);
        var target = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        bool contains = range.Contains(target);

        // Assert
        contains.Should().BeTrue();
    }

    [Fact]
    public void Overlaps_WhenRangesIntersect_ReturnsTrue()
    {
        // Arrange
        var range1 = new DateRange(
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc));

        var range2 = new DateRange(
            new DateTime(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc));

        // Act & Assert
        range1.Overlaps(range2).Should().BeTrue();
        range2.Overlaps(range1).Should().BeTrue();
    }
}
