using System.Globalization;
using CalendarWidget.Presentation.Converters;
using CalendarWidget.Presentation.Models;
using FluentAssertions;

namespace CalendarWidget.UnitTests.Presentation;

/// <summary>
/// Unit tests for <see cref="IsSelectedDayConverter"/>.
/// </summary>
public sealed class IsSelectedDayConverterTests
{
    private readonly IsSelectedDayConverter _sut = new();

    [Fact]
    public void Convert_WhenSameDate_ReturnsTrue()
    {
        // Arrange
        DateOnly testDate = new(2026, 9, 25);
        CalendarDayModel selectedDay = new(testDate, 25, true, false, false);
        CalendarDayModel currentDay = new(testDate, 25, true, false, true);

        // Act
        object result = _sut.Convert([selectedDay, currentDay], typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void Convert_WhenDifferentDate_ReturnsFalse()
    {
        // Arrange
        CalendarDayModel selectedDay = new(new DateOnly(2026, 9, 25), 25, true, false, false);
        CalendarDayModel currentDay = new(new DateOnly(2026, 9, 26), 26, true, false, false);

        // Act
        object result = _sut.Convert([selectedDay, currentDay], typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void Convert_WhenSelectedDayIsNull_ReturnsFalse()
    {
        // Arrange
        CalendarDayModel currentDay = new(new DateOnly(2026, 9, 25), 25, true, false, false);

        // Act
        object result = _sut.Convert([null, currentDay], typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void Convert_WhenCurrentDayIsNull_ReturnsFalse()
    {
        // Arrange
        CalendarDayModel selectedDay = new(new DateOnly(2026, 9, 25), 25, true, false, false);

        // Act
        object result = _sut.Convert([selectedDay, null], typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void Convert_WhenValuesLengthIsNotTwo_ReturnsFalse()
    {
        // Arrange
        CalendarDayModel day = new(new DateOnly(2026, 9, 25), 25, true, false, false);

        // Act & Assert
        _sut.Convert([], typeof(bool), null, CultureInfo.InvariantCulture).Should().Be(false);
        _sut.Convert([day], typeof(bool), null, CultureInfo.InvariantCulture).Should().Be(false);
        _sut.Convert([day, day, day], typeof(bool), null, CultureInfo.InvariantCulture).Should().Be(false);
    }

    [Fact]
    public void Convert_WhenValuesAreNotCalendarDayModel_ReturnsFalse()
    {
        // Arrange
        object value1 = "NotADayModel";
        object value2 = 42;

        // Act
        object result = _sut.Convert([value1, value2], typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void ConvertBack_Always_ThrowsNotSupportedException()
    {
        // Act
        Action act = () => _sut.ConvertBack(true, [typeof(object)], null, CultureInfo.InvariantCulture);

        // Assert
        act.Should().Throw<NotSupportedException>();
    }
}
