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

    // === PageUp / PageDown ===

    [Fact]
    public void NavigatePreviousMonthKeepingSelection_OnMiddleOfMonth_SelectsSameDayPreviousMonth()
    {
        // Arrange: September 15, 2026
        CalendarViewModel vm = new(_gridService, _clockService);
        CalendarDayModel sept15 = vm.Days.First(d => d.DayNumber == 15 && d.IsCurrentMonth);
        vm.SelectDay(sept15);

        // Act
        vm.NavigatePreviousMonthKeepingSelection();

        // Assert
        vm.CurrentYear.Should().Be(2026);
        vm.CurrentMonth.Should().Be(8);
        vm.SelectedDay.Should().NotBeNull();
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 8, 15));
        vm.SelectedDateFormatted.Should().Contain("August 15, 2026");
    }

    [Fact]
    public void NavigatePreviousMonthKeepingSelection_FromJanuary_CrossesIntoPreviousYear()
    {
        // Arrange: January 15, 2026
        TestClockService clock = new(new DateTime(2026, 1, 15));
        CalendarViewModel vm = new(_gridService, clock);
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 1, 15));

        // Act
        vm.NavigatePreviousMonthKeepingSelection();

        // Assert
        vm.CurrentYear.Should().Be(2025);
        vm.CurrentMonth.Should().Be(12);
        vm.SelectedDay.Should().NotBeNull();
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2025, 12, 15));
    }

    [Fact]
    public void NavigatePreviousMonthKeepingSelection_FromDay31_ClampsToPreviousShortMonth()
    {
        // Arrange: March 31, 2026 → February has 28 days (non-leap)
        TestClockService clock = new(new DateTime(2026, 3, 31));
        CalendarViewModel vm = new(_gridService, clock);
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 3, 31));

        // Act
        vm.NavigatePreviousMonthKeepingSelection();

        // Assert
        vm.CurrentYear.Should().Be(2026);
        vm.CurrentMonth.Should().Be(2);
        vm.SelectedDay.Should().NotBeNull();
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 2, 28));
    }

    [Fact]
    public void NavigateNextMonthKeepingSelection_OnMiddleOfMonth_SelectsSameDayNextMonth()
    {
        // Arrange: September 15, 2026
        CalendarViewModel vm = new(_gridService, _clockService);
        CalendarDayModel sept15 = vm.Days.First(d => d.DayNumber == 15 && d.IsCurrentMonth);
        vm.SelectDay(sept15);

        // Act
        vm.NavigateNextMonthKeepingSelection();

        // Assert
        vm.CurrentYear.Should().Be(2026);
        vm.CurrentMonth.Should().Be(10);
        vm.SelectedDay.Should().NotBeNull();
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 10, 15));
        vm.SelectedDateFormatted.Should().Contain("October 15, 2026");
    }

    [Fact]
    public void NavigateNextMonthKeepingSelection_FromDecember_CrossesIntoNextYear()
    {
        // Arrange: December 15, 2026
        TestClockService clock = new(new DateTime(2026, 12, 15));
        CalendarViewModel vm = new(_gridService, clock);
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 12, 15));

        // Act
        vm.NavigateNextMonthKeepingSelection();

        // Assert
        vm.CurrentYear.Should().Be(2027);
        vm.CurrentMonth.Should().Be(1);
        vm.SelectedDay.Should().NotBeNull();
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2027, 1, 15));
    }

    [Fact]
    public void NavigateNextMonthKeepingSelection_FromDay31_ClampsToNextShortMonth()
    {
        // Arrange: January 31, 2026 → February has 28 days (non-leap)
        TestClockService clock = new(new DateTime(2026, 1, 31));
        CalendarViewModel vm = new(_gridService, clock);
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 1, 31));

        // Act
        vm.NavigateNextMonthKeepingSelection();

        // Assert
        vm.CurrentYear.Should().Be(2026);
        vm.CurrentMonth.Should().Be(2);
        vm.SelectedDay.Should().NotBeNull();
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 2, 28));
    }

    // === Home / End ===

    [Fact]
    public void NavigateToMonthStart_OnSelectedDate_SelectsFirstDayOfCurrentMonth()
    {
        // Arrange: September 18, 2026
        CalendarViewModel vm = new(_gridService, _clockService);
        CalendarDayModel sept18 = vm.Days.First(d => d.DayNumber == 18 && d.IsCurrentMonth);
        vm.SelectDay(sept18);

        // Act
        vm.NavigateToMonthStart();

        // Assert
        vm.CurrentYear.Should().Be(2026);
        vm.CurrentMonth.Should().Be(9);
        vm.SelectedDay.Should().NotBeNull();
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 9, 1));
        vm.SelectedDateFormatted.Should().Contain("September 1, 2026");
    }

    [Fact]
    public void NavigateToMonthStart_OnFebruary_SelectsFebruaryFirst()
    {
        // Arrange: February 2026
        TestClockService clock = new(new DateTime(2026, 2, 14));
        CalendarViewModel vm = new(_gridService, clock);

        // Act
        vm.NavigateToMonthStart();

        // Assert
        vm.SelectedDay.Should().NotBeNull();
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 2, 1));
        vm.CurrentMonth.Should().Be(2);
    }

    [Fact]
    public void NavigateToMonthEnd_OnSelectedDate_SelectsLastDayOfCurrentMonth()
    {
        // Arrange: September 18, 2026 (September has 30 days)
        CalendarViewModel vm = new(_gridService, _clockService);
        CalendarDayModel sept18 = vm.Days.First(d => d.DayNumber == 18 && d.IsCurrentMonth);
        vm.SelectDay(sept18);

        // Act
        vm.NavigateToMonthEnd();

        // Assert
        vm.CurrentYear.Should().Be(2026);
        vm.CurrentMonth.Should().Be(9);
        vm.SelectedDay.Should().NotBeNull();
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 9, 30));
        vm.SelectedDateFormatted.Should().Contain("September 30, 2026");
    }

    [Fact]
    public void NavigateToMonthEnd_OnLeapYearFebruary_SelectsFebruary29()
    {
        // Arrange: February 2024 (leap year)
        TestClockService clock = new(new DateTime(2024, 2, 10));
        CalendarViewModel vm = new(_gridService, clock);

        // Act
        vm.NavigateToMonthEnd();

        // Assert
        vm.SelectedDay.Should().NotBeNull();
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2024, 2, 29));
    }

    [Fact]
    public void NavigateToMonthEnd_OnNonLeapYearFebruary_SelectsFebruary28()
    {
        // Arrange: February 2026 (non-leap year)
        TestClockService clock = new(new DateTime(2026, 2, 10));
        CalendarViewModel vm = new(_gridService, clock);

        // Act
        vm.NavigateToMonthEnd();

        // Assert
        vm.SelectedDay.Should().NotBeNull();
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 2, 28));
    }

    [Fact]
    public void NavigateToMonthEnd_OnThirtyDayMonth_SelectsDay30()
    {
        // Arrange: April 2026 (30 days)
        TestClockService clock = new(new DateTime(2026, 4, 10));
        CalendarViewModel vm = new(_gridService, clock);

        // Act
        vm.NavigateToMonthEnd();

        // Assert
        vm.SelectedDay.Should().NotBeNull();
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 4, 30));
    }

    [Fact]
    public void NavigateToMonthEnd_OnThirtyOneDayMonth_SelectsDay31()
    {
        // Arrange: October 2026 (31 days)
        TestClockService clock = new(new DateTime(2026, 10, 10));
        CalendarViewModel vm = new(_gridService, clock);

        // Act
        vm.NavigateToMonthEnd();

        // Assert
        vm.SelectedDay.Should().NotBeNull();
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 10, 31));
    }

    // === PageUp/PageDown with FirstDayOfWeek ===

    [Fact]
    public void NavigateNextMonthKeepingSelection_WithSundayFirstDayOfWeek_SelectsCorrectDay()
    {
        // Arrange: September 15, 2026, Sunday-first grid
        CalendarViewModel vm = new(_gridService, _clockService);
        vm.FirstDayOfWeek = DayOfWeek.Sunday;
        CalendarDayModel sept15 = vm.Days.First(d => d.DayNumber == 15 && d.IsCurrentMonth);
        vm.SelectDay(sept15);

        // Act
        vm.NavigateNextMonthKeepingSelection();

        // Assert
        vm.CurrentMonth.Should().Be(10);
        vm.SelectedDay.Should().NotBeNull();
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 10, 15));
        vm.DayHeaders[0].Should().Be("Su");
    }

    [Fact]
    public void NavigatePreviousMonthKeepingSelection_WithMondayFirstDayOfWeek_SelectsCorrectDay()
    {
        // Arrange: September 10, 2026, Monday-first grid (default)
        CalendarViewModel vm = new(_gridService, _clockService);
        CalendarDayModel sept10 = vm.Days.First(d => d.DayNumber == 10 && d.IsCurrentMonth);
        vm.SelectDay(sept10);

        // Act
        vm.NavigatePreviousMonthKeepingSelection();

        // Assert
        vm.CurrentMonth.Should().Be(8);
        vm.SelectedDay.Should().NotBeNull();
        vm.SelectedDay!.Date.Should().Be(new DateOnly(2026, 8, 10));
        vm.DayHeaders[0].Should().Be("Mo");
    }
}
