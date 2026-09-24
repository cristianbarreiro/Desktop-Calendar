using CalendarWidget.Presentation.Models;
using CalendarWidget.Presentation.Services;
using FluentAssertions;

namespace CalendarWidget.UnitTests.Presentation;

public sealed class CalendarGridServiceTests
{
    private readonly CalendarGridService _sut = new();

    [Theory]
    [InlineData(2026, 1)]
    [InlineData(2026, 2)]
    [InlineData(2026, 9)]
    [InlineData(2024, 2)] // leap year
    [InlineData(2025, 12)]
    public void GenerateGrid_Always_ProducesExactly42Cells(int year, int month)
    {
        // Act
        IReadOnlyList<CalendarDayModel> cells = _sut.GenerateGrid(year, month, DayOfWeek.Monday, new DateOnly(2026, 9, 24));

        // Assert
        cells.Should().HaveCount(42);
    }

    [Fact]
    public void GenerateGrid_September2026_CorrectlyIdentifiesCurrentMonthDays()
    {
        // Arrange: September 2026 has 30 days
        int year = 2026;
        int month = 9;

        // Act
        IReadOnlyList<CalendarDayModel> cells = _sut.GenerateGrid(year, month, DayOfWeek.Monday, new DateOnly(2026, 9, 24));

        // Assert
        cells.Count(c => c.IsCurrentMonth).Should().Be(30);
        cells.Where(c => c.IsCurrentMonth).Select(c => c.DayNumber).Should().BeEquivalentTo(Enumerable.Range(1, 30));
    }

    [Fact]
    public void GenerateGrid_September2026_IncludesTrailingAndLeadingDays()
    {
        // Arrange: Sept 1, 2026 is Tuesday. With Monday as first day of week, 1 preceding day from August (Aug 31)
        // 1 trailing + 30 current = 31. Total 42 cells means 11 leading days from October (Oct 1..11).
        int year = 2026;
        int month = 9;

        // Act
        IReadOnlyList<CalendarDayModel> cells = _sut.GenerateGrid(year, month, DayOfWeek.Monday, new DateOnly(2026, 9, 24));

        // Assert
        cells[0].Date.Should().Be(new DateOnly(2026, 8, 31));
        cells[0].IsCurrentMonth.Should().BeFalse();

        cells[1].Date.Should().Be(new DateOnly(2026, 9, 1));
        cells[1].IsCurrentMonth.Should().BeTrue();

        cells[30].Date.Should().Be(new DateOnly(2026, 9, 30));
        cells[30].IsCurrentMonth.Should().BeTrue();

        cells[31].Date.Should().Be(new DateOnly(2026, 10, 1));
        cells[31].IsCurrentMonth.Should().BeFalse();

        cells[41].Date.Should().Be(new DateOnly(2026, 10, 11));
        cells[41].IsCurrentMonth.Should().BeFalse();
    }

    [Fact]
    public void GenerateGrid_MondayFirstDayOfWeek_StartsFirstColumnOnMonday()
    {
        // Act
        IReadOnlyList<CalendarDayModel> cells = _sut.GenerateGrid(2026, 9, DayOfWeek.Monday, new DateOnly(2026, 9, 24));

        // Assert
        cells[0].Date.DayOfWeek.Should().Be(DayOfWeek.Monday);
        cells[7].Date.DayOfWeek.Should().Be(DayOfWeek.Monday);
    }

    [Fact]
    public void GenerateGrid_SundayFirstDayOfWeek_StartsFirstColumnOnSunday()
    {
        // Act
        IReadOnlyList<CalendarDayModel> cells = _sut.GenerateGrid(2026, 9, DayOfWeek.Sunday, new DateOnly(2026, 9, 24));

        // Assert
        cells[0].Date.DayOfWeek.Should().Be(DayOfWeek.Sunday);
        cells[7].Date.DayOfWeek.Should().Be(DayOfWeek.Sunday);
    }

    [Fact]
    public void GenerateGrid_LeapYearFebruary_Contains29CurrentMonthDays()
    {
        // Act
        IReadOnlyList<CalendarDayModel> cells = _sut.GenerateGrid(2024, 2, DayOfWeek.Monday, new DateOnly(2024, 2, 1));

        // Assert
        cells.Count(c => c.IsCurrentMonth).Should().Be(29);
    }

    [Fact]
    public void GenerateGrid_NonLeapYearFebruary_Contains28CurrentMonthDays()
    {
        // Act
        IReadOnlyList<CalendarDayModel> cells = _sut.GenerateGrid(2025, 2, DayOfWeek.Monday, new DateOnly(2025, 2, 1));

        // Assert
        cells.Count(c => c.IsCurrentMonth).Should().Be(28);
    }

    [Fact]
    public void GenerateGrid_DateMatchesToday_MarksIsTodayTrue()
    {
        // Arrange
        DateOnly today = new(2026, 9, 24);

        // Act
        IReadOnlyList<CalendarDayModel> cells = _sut.GenerateGrid(2026, 9, DayOfWeek.Monday, today);

        // Assert
        CalendarDayModel todayCell = cells.Single(c => c.IsToday);
        todayCell.Date.Should().Be(today);
        todayCell.DayNumber.Should().Be(24);
        todayCell.IsCurrentMonth.Should().BeTrue();
    }

    [Fact]
    public void GetDayHeaders_MondayFirst_StartsOnMonday()
    {
        // Act
        IReadOnlyList<string> headers = _sut.GetDayHeaders(DayOfWeek.Monday);

        // Assert
        headers.Should().HaveCount(7);
        headers[0].Should().Be("Mo");
        headers[6].Should().Be("Su");
    }

    [Fact]
    public void GetDayHeaders_SundayFirst_StartsOnSunday()
    {
        // Act
        IReadOnlyList<string> headers = _sut.GetDayHeaders(DayOfWeek.Sunday);

        // Assert
        headers.Should().HaveCount(7);
        headers[0].Should().Be("Su");
        headers[6].Should().Be("Sa");
    }
}
