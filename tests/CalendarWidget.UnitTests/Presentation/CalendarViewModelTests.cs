using CalendarWidget.Presentation.Models;
using CalendarWidget.Presentation.Services;
using CalendarWidget.Presentation.ViewModels;
using CalendarWidget.UnitTests.Fakes;
using FluentAssertions;

namespace CalendarWidget.UnitTests.Presentation;

public sealed class CalendarViewModelTests
{
    private readonly CalendarGridService _gridService = new();
    private readonly TestClockService _clockService = new(new DateTime(2026, 9, 24, 10, 0, 0));

    // === Initialization ===

    [Fact]
    public void Constructor_InitializesWithCurrentMonthAnd42Cells()
    {
        // Act
        CalendarViewModel vm = new(_gridService, _clockService);

        // Assert
        vm.CurrentYear.Should().Be(2026);
        vm.CurrentMonth.Should().Be(9);
        vm.MonthYearText.Should().Be("September 2026");
        vm.MonthYearTitle.Should().Be("SEPTEMBER 2026");
        vm.Days.Should().HaveCount(42);
        vm.DayHeaders.Should().HaveCount(7);
        vm.DayHeaders[0].Should().Be("Mo");
        vm.SelectedDay.Should().NotBeNull();
        vm.SelectedDay!.DayNumber.Should().Be(24);
        vm.SelectedDateFormatted.Should().Contain("September 24, 2026");
        vm.SelectedDayHeader.Should().Be("TODAY");
        vm.SelectedDateHeader.Should().Be("SEPTEMBER 24, 2026");
    }

    [Fact]
    public void FirstDayOfWeek_WhenChanged_RegeneratesHeadersAndGrid()
    {
        // Arrange
        CalendarViewModel vm = new(_gridService, _clockService);
        vm.DayHeaders[0].Should().Be("Mo");
        vm.Days[0].Date.DayOfWeek.Should().Be(DayOfWeek.Monday);

        // Act
        vm.FirstDayOfWeek = DayOfWeek.Sunday;

        // Assert
        vm.DayHeaders[0].Should().Be("Su");
        vm.Days[0].Date.DayOfWeek.Should().Be(DayOfWeek.Sunday);
        vm.Days.Should().HaveCount(42);
    }

    // === Month Navigation ===

    [Fact]
    public void PreviousMonth_September2026_NavigatesToAugust2026()
    {
        // Arrange
        CalendarViewModel vm = new(_gridService, _clockService);

        // Act
        vm.PreviousMonth();

        // Assert
        vm.CurrentYear.Should().Be(2026);
        vm.CurrentMonth.Should().Be(8);
        vm.MonthYearText.Should().Be("August 2026");
        vm.Days.Should().HaveCount(42);
    }

    [Fact]
    public void PreviousMonth_January_NavigatesToDecemberOfPreviousYear()
    {
        // Arrange
        TestClockService clock = new(new DateTime(2026, 1, 15));
        CalendarViewModel vm = new(_gridService, clock);

        // Act
        vm.PreviousMonth();

        // Assert
        vm.CurrentYear.Should().Be(2025);
        vm.CurrentMonth.Should().Be(12);
        vm.MonthYearText.Should().Be("December 2025");
        vm.Days.Should().HaveCount(42);
    }

    [Fact]
    public void NextMonth_December_NavigatesToJanuaryOfNextYear()
    {
        // Arrange
        TestClockService clock = new(new DateTime(2026, 12, 15));
        CalendarViewModel vm = new(_gridService, clock);

        // Act
        vm.NextMonth();

        // Assert
        vm.CurrentYear.Should().Be(2027);
        vm.CurrentMonth.Should().Be(1);
        vm.MonthYearText.Should().Be("January 2027");
        vm.Days.Should().HaveCount(42);
    }

    // === Today & GoToToday Commands ===

    [Fact]
    public void Today_WhenNavigatedAway_NavigatesBackToCurrentDateAndSelectsToday()
    {
        // Arrange
        CalendarViewModel vm = new(_gridService, _clockService);
        vm.NextMonth();
        vm.NextMonth();
        vm.CurrentMonth.Should().Be(11);

        // Act
        vm.Today();

        // Assert
        vm.CurrentYear.Should().Be(2026);
        vm.CurrentMonth.Should().Be(9);
        vm.MonthYearText.Should().Be("September 2026");
        vm.SelectedDay.Should().NotBeNull();
        vm.SelectedDay!.DayNumber.Should().Be(24);
        vm.SelectedDay!.IsToday.Should().BeTrue();
    }

    [Fact]
    public void GoToToday_WhenNavigatedAway_NavigatesBackToCurrentDateAndSelectsToday()
    {
        // Arrange
        CalendarViewModel vm = new(_gridService, _clockService);
        vm.PreviousMonth();
        vm.PreviousMonth();
        vm.CurrentMonth.Should().Be(7);

        // Act
        vm.GoToToday();

        // Assert
        vm.CurrentYear.Should().Be(2026);
        vm.CurrentMonth.Should().Be(9);
        vm.MonthYearText.Should().Be("September 2026");
        vm.SelectedDay.Should().NotBeNull();
        vm.SelectedDay!.DayNumber.Should().Be(24);
    }

    // === Selection & Stale Selection Preservation ===

    [Fact]
    public void SelectDay_ValidDay_UpdatesSelectedDayAndFormattedTexts()
    {
        // Arrange
        CalendarViewModel vm = new(_gridService, _clockService);
        CalendarDayModel targetDay = vm.Days.First(d => d.DayNumber == 15 && d.IsCurrentMonth);

        // Act
        vm.SelectDay(targetDay);

        // Assert
        vm.SelectedDay.Should().Be(targetDay);
        vm.SelectedDateFormatted.Should().Be("Tuesday, September 15, 2026");
        vm.SelectedDayHeader.Should().Be("SEPTEMBER 15, 2026");
        vm.SelectedDateHeader.Should().Be("SEPTEMBER 15, 2026");
    }

    [Fact]
    public void SelectDay_Null_ClearsSelectedDayAndFormattedTexts()
    {
        // Arrange
        CalendarViewModel vm = new(_gridService, _clockService);
        vm.SelectedDay.Should().NotBeNull();

        // Act
        vm.SelectDay(null);

        // Assert
        vm.SelectedDay.Should().BeNull();
        vm.SelectedDateFormatted.Should().BeEmpty();
        vm.SelectedDayHeader.Should().BeEmpty();
        vm.SelectedDateHeader.Should().BeEmpty();
    }

    [Fact]
    public void NextMonth_WhenSelectedDayStillInNewGrid_PreservesSelectedDay()
    {
        // Arrange: In August 2026, select August 31.
        // For September 2026, August 31 is the first cell (index 0) of the 42-cell grid.
        TestClockService clock = new(new DateTime(2026, 8, 10));
        CalendarViewModel vm = new(_gridService, clock);
        CalendarDayModel aug31 = vm.Days.First(d => d.Date == new DateOnly(2026, 8, 31));
        vm.SelectDay(aug31);

        // Act: Navigate to September 2026
        vm.NextMonth();

        // Assert: August 31 is still in the 42-cell grid, so selection is preserved
        vm.CurrentMonth.Should().Be(9);
        vm.SelectedDay.Should().NotBeNull();
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 8, 31));
        vm.SelectedDateFormatted.Should().Contain("August 31, 2026");
    }

    [Fact]
    public void NextMonth_WhenSelectedDayNotInNewGrid_ClearsSelectedDayAndFormattedTexts()
    {
        // Arrange: In September 2026, select September 15.
        CalendarViewModel vm = new(_gridService, _clockService);
        CalendarDayModel sept15 = vm.Days.First(d => d.DayNumber == 15 && d.IsCurrentMonth);
        vm.SelectDay(sept15);

        // Act: Navigate to October 2026 (September 15 is NOT in October's 42-cell grid)
        vm.NextMonth();

        // Assert: Stale selection cleared, never leaving null SelectedDay with non-empty header
        vm.CurrentMonth.Should().Be(10);
        vm.SelectedDay.Should().BeNull();
        vm.SelectedDateFormatted.Should().BeEmpty();
        vm.SelectedDayHeader.Should().BeEmpty();
        vm.SelectedDateHeader.Should().BeEmpty();
    }

    [Fact]
    public void PreviousMonth_WhenSelectedDayStillInNewGrid_PreservesSelectedDay()
    {
        // Arrange: In September 2026, select August 31 (visible in September's leading cells).
        CalendarViewModel vm = new(_gridService, _clockService);
        CalendarDayModel aug31 = vm.Days.First(d => d.Date == new DateOnly(2026, 8, 31));
        vm.SelectDay(aug31);

        // Act: Navigate back to August 2026
        vm.PreviousMonth();

        // Assert: August 31 is still in August 2026
        vm.CurrentMonth.Should().Be(8);
        vm.SelectedDay.Should().NotBeNull();
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 8, 31));
        vm.SelectedDateFormatted.Should().Contain("August 31, 2026");
    }

    // === Keyboard Navigation ===

    [Fact]
    public void NavigateRight_MovesOneDay()
    {
        // Arrange - default selected day is 24
        CalendarViewModel vm = new(_gridService, _clockService);
        vm.SelectedDay!.DayNumber.Should().Be(24);

        // Act
        vm.NavigateRight();

        // Assert
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 9, 25));
    }

    [Fact]
    public void NavigateLeft_MovesOneDay()
    {
        // Arrange - default selected day is 24
        CalendarViewModel vm = new(_gridService, _clockService);
        vm.SelectedDay!.DayNumber.Should().Be(24);

        // Act
        vm.NavigateLeft();

        // Assert
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 9, 23));
    }

    [Fact]
    public void NavigateDown_MovesSevenDays()
    {
        // Arrange - default selected day is 24
        CalendarViewModel vm = new(_gridService, _clockService);
        vm.SelectedDay!.DayNumber.Should().Be(24);

        // Act
        vm.NavigateDown();

        // Assert
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 10, 1));
        vm.CurrentMonth.Should().Be(10);
    }

    [Fact]
    public void NavigateUp_MovesSevenDays()
    {
        // Arrange - default selected day is 24
        CalendarViewModel vm = new(_gridService, _clockService);
        vm.SelectedDay!.DayNumber.Should().Be(24);

        // Act
        vm.NavigateUp();

        // Assert
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 9, 17));
    }

    [Fact]
    public void NavigateRight_CrossesMonthBoundary_UpdatesMonthAndYear()
    {
        // Arrange - select September 30
        CalendarViewModel vm = new(_gridService, _clockService);
        CalendarDayModel day30 = vm.Days.First(d => d.DayNumber == 30 && d.IsCurrentMonth);
        vm.SelectDay(day30);

        // Act
        vm.NavigateRight();

        // Assert
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 10, 1));
        vm.CurrentMonth.Should().Be(10);
        vm.MonthYearText.Should().Be("October 2026");
    }

    [Fact]
    public void NavigateLeft_CrossesMonthBoundary_UpdatesMonthAndYear()
    {
        // Arrange - select September 1
        CalendarViewModel vm = new(_gridService, _clockService);
        CalendarDayModel day1 = vm.Days.First(d => d.DayNumber == 1 && d.IsCurrentMonth);
        vm.SelectDay(day1);

        // Act
        vm.NavigateLeft();

        // Assert
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 8, 31));
        vm.CurrentMonth.Should().Be(8);
        vm.MonthYearText.Should().Be("August 2026");
    }

    [Fact]
    public void NavigateDown_CrossesMonthBoundary_UpdatesMonthAndYear()
    {
        // Arrange - select September 28 (28 + 7 = Oct 5)
        CalendarViewModel vm = new(_gridService, _clockService);
        CalendarDayModel day28 = vm.Days.First(d => d.DayNumber == 28 && d.IsCurrentMonth);
        vm.SelectDay(day28);

        // Act
        vm.NavigateDown();

        // Assert
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 10, 5));
        vm.CurrentMonth.Should().Be(10);
        vm.MonthYearText.Should().Be("October 2026");
    }

    [Fact]
    public void NavigateUp_CrossesMonthBoundary_UpdatesMonthAndYear()
    {
        // Arrange - select September 3 (3 - 7 = Aug 27)
        CalendarViewModel vm = new(_gridService, _clockService);
        CalendarDayModel day3 = vm.Days.First(d => d.DayNumber == 3 && d.IsCurrentMonth);
        vm.SelectDay(day3);

        // Act
        vm.NavigateUp();

        // Assert
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 8, 27));
        vm.CurrentMonth.Should().Be(8);
        vm.MonthYearText.Should().Be("August 2026");
    }

    [Fact]
    public void NavigateRight_CrossesYearBoundary_December31ToJanuary1()
    {
        // Arrange - December 31, 2026
        TestClockService clock = new(new DateTime(2026, 12, 31));
        CalendarViewModel vm = new(_gridService, clock);
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 12, 31));

        // Act
        vm.NavigateRight();

        // Assert
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2027, 1, 1));
        vm.CurrentYear.Should().Be(2027);
        vm.CurrentMonth.Should().Be(1);
        vm.MonthYearText.Should().Be("January 2027");
    }

    [Fact]
    public void NavigateLeft_CrossesYearBoundary_January1ToDecember31()
    {
        // Arrange - January 1, 2026
        TestClockService clock = new(new DateTime(2026, 1, 1));
        CalendarViewModel vm = new(_gridService, clock);
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 1, 1));

        // Act
        vm.NavigateLeft();

        // Assert
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2025, 12, 31));
        vm.CurrentYear.Should().Be(2025);
        vm.CurrentMonth.Should().Be(12);
        vm.MonthYearText.Should().Be("December 2025");
    }

    [Fact]
    public void NavigateByDays_WhenSelectedDayIsNull_UsesFirstOfMonthAsBase()
    {
        // Arrange
        CalendarViewModel vm = new(_gridService, _clockService);
        vm.SelectDay(null);
        vm.SelectedDay.Should().BeNull();

        // Act - from September 1 (+1 day = Sept 2)
        vm.NavigateByDays(1);

        // Assert
        vm.SelectedDay.Should().NotBeNull();
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 9, 2));
    }
}
