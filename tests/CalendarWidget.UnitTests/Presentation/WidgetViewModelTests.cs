using CalendarWidget.Presentation.Models;
using CalendarWidget.Presentation.Services;
using CalendarWidget.Presentation.ViewModels;
using CalendarWidget.UnitTests.Fakes;
using FluentAssertions;

namespace CalendarWidget.UnitTests.Presentation;

public sealed class WidgetViewModelTests
{
    private readonly TestWindowManager _windowManager = new();
    private readonly CalendarGridService _gridService = new();
    private readonly TestClockService _clockService = new(new DateTime(2026, 9, 24, 20, 42, 15));
    private readonly WidgetViewModel _sut;

    public WidgetViewModelTests()
    {
        _sut = new WidgetViewModel(_windowManager, _gridService, _clockService);
    }

    [Fact]
    public void Constructor_InitializesWithCurrentTimeAndMonthGrid()
    {
        // Assert
        _sut.CurrentTimeText.Should().Be("20:42:15");
        _sut.MonthYearText.Should().Be("September 2026");
        _sut.Days.Should().HaveCount(42);
        _sut.SelectedDay.Should().NotBeNull();
        _sut.SelectedDay!.DayNumber.Should().Be(24);
        _sut.IsExpanded.Should().BeFalse();
    }

    [Fact]
    public void SelectDay_NewDay_SetsSelectedDayAndExpandsTray()
    {
        // Arrange
        CalendarDayModel targetDay = _sut.Days.First(d => d.DayNumber == 15 && d.IsCurrentMonth);

        // Act
        _sut.SelectDay(targetDay);

        // Assert
        _sut.SelectedDay.Should().Be(targetDay);
        _sut.IsExpanded.Should().BeTrue();
        _sut.SelectedDayHeader.Should().Contain("SEPTEMBER 15, 2026");
    }

    [Fact]
    public void SelectDay_SameDayWhenExpanded_CollapsesTray()
    {
        // Arrange
        CalendarDayModel targetDay = _sut.Days.First(d => d.DayNumber == 15 && d.IsCurrentMonth);
        _sut.SelectDay(targetDay);
        _sut.IsExpanded.Should().BeTrue();

        // Act: clicking the same day again toggles collapse
        _sut.SelectDay(targetDay);

        // Assert
        _sut.IsExpanded.Should().BeFalse();
    }

    [Fact]
    public void SelectDay_DifferentDayWhenExpanded_UpdatesSelectedDayAndKeepsExpanded()
    {
        // Arrange
        CalendarDayModel day1 = _sut.Days.First(d => d.DayNumber == 10 && d.IsCurrentMonth);
        CalendarDayModel day2 = _sut.Days.First(d => d.DayNumber == 20 && d.IsCurrentMonth);
        _sut.SelectDay(day1);
        _sut.IsExpanded.Should().BeTrue();

        // Act
        _sut.SelectDay(day2);

        // Assert
        _sut.SelectedDay.Should().Be(day2);
        _sut.IsExpanded.Should().BeTrue();
    }

    [Fact]
    public void SelectDay_TodayDay_SetsHeaderToTODAY()
    {
        // Arrange
        CalendarDayModel todayDay = _sut.Days.First(d => d.IsToday && d.IsCurrentMonth);

        // Act
        _sut.SelectDay(todayDay);

        // Assert
        _sut.SelectedDayHeader.Should().Be("TODAY");
    }

    [Fact]
    public void SelectDay_NonTodayDay_SetsHeaderToFormattedDate()
    {
        // Arrange
        CalendarDayModel targetDay = _sut.Days.First(d => d.DayNumber == 15 && d.IsCurrentMonth);

        // Act
        _sut.SelectDay(targetDay);

        // Assert
        _sut.SelectedDayHeader.Should().Be("SEPTEMBER 15, 2026");
    }

    [Fact]
    public void PreviousMonth_WhenInvoked_NavigatesToPreviousMonthAndUpdatesText()
    {
        // Act
        _sut.PreviousMonth();

        // Assert
        _sut.CurrentMonth.Should().Be(8);
        _sut.MonthYearText.Should().Be("August 2026");
        _sut.Days.Should().HaveCount(42);
    }

    [Fact]
    public void NextMonth_WhenInvoked_NavigatesToNextMonthAndUpdatesText()
    {
        // Act
        _sut.NextMonth();

        // Assert
        _sut.CurrentMonth.Should().Be(10);
        _sut.MonthYearText.Should().Be("October 2026");
        _sut.Days.Should().HaveCount(42);
    }

    [Fact]
    public void PreviousMonth_January_WrapsToDecemberPreviousYear()
    {
        // Arrange: Navigate to January 2026
        while (_sut.CurrentMonth != 1)
        {
            _sut.PreviousMonth();
        }

        _sut.CurrentYear.Should().Be(2026);
        _sut.CurrentMonth.Should().Be(1);

        // Act
        _sut.PreviousMonth();

        // Assert
        _sut.CurrentMonth.Should().Be(12);
        _sut.CurrentYear.Should().Be(2025);
        _sut.MonthYearText.Should().Be("December 2025");
    }

    [Fact]
    public void NextMonth_December_WrapsToJanuaryNextYear()
    {
        // Arrange: Navigate to December 2026
        while (_sut.CurrentMonth != 12)
        {
            _sut.NextMonth();
        }

        _sut.CurrentYear.Should().Be(2026);
        _sut.CurrentMonth.Should().Be(12);

        // Act
        _sut.NextMonth();

        // Assert
        _sut.CurrentMonth.Should().Be(1);
        _sut.CurrentYear.Should().Be(2027);
        _sut.MonthYearText.Should().Be("January 2027");
    }

    [Fact]
    public void SwitchToApp_WhenInvoked_CallsWindowManagerShowFullApplication()
    {
        // Act
        _sut.SwitchToApp();

        // Assert
        _windowManager.ShowFullApplicationCallCount.Should().Be(1);
    }

    [Fact]
    public void Minimize_WhenInvoked_CallsWindowManagerMinimizeWidget()
    {
        // Act
        _sut.Minimize();

        // Assert
        _windowManager.MinimizeWidgetCallCount.Should().Be(1);
    }

    [Fact]
    public void ClockTick_WhenTimeChanged_UpdatesCurrentTimeText()
    {
        // Act
        _clockService.AdvanceTime(TimeSpan.FromSeconds(5));

        // Assert
        _sut.CurrentTimeText.Should().Be("20:42:20");
    }

    // === Escape ===

    [Fact]
    public void Escape_WhenExpanded_CollapsesTray()
    {
        // Arrange
        CalendarDayModel targetDay = _sut.Days.First(d => d.DayNumber == 10 && d.IsCurrentMonth);
        _sut.SelectDay(targetDay);
        _sut.IsExpanded.Should().BeTrue();

        // Act
        _sut.Escape();

        // Assert
        _sut.IsExpanded.Should().BeFalse();
    }

    [Fact]
    public void Escape_WhenCollapsed_RemainsCollapsed()
    {
        // Arrange
        _sut.IsExpanded.Should().BeFalse();

        // Act
        _sut.Escape();

        // Assert
        _sut.IsExpanded.Should().BeFalse();
    }

    // === Keyboard Navigation ===

    [Fact]
    public void NavigateRight_MovesOneDay()
    {
        // Arrange - default selected day is 24
        _sut.SelectedDay!.DayNumber.Should().Be(24);

        // Act
        _sut.NavigateRight();

        // Assert
        _sut.SelectedDay!.Date.Should().Be(new DateOnly(2026, 9, 25));
    }

    [Fact]
    public void NavigateLeft_MovesOneDay()
    {
        // Arrange - default selected day is 24
        _sut.SelectedDay!.DayNumber.Should().Be(24);

        // Act
        _sut.NavigateLeft();

        // Assert
        _sut.SelectedDay!.Date.Should().Be(new DateOnly(2026, 9, 23));
    }

    [Fact]
    public void NavigateDown_MovesSevenDays()
    {
        // Arrange - default selected day is 24
        _sut.SelectedDay!.DayNumber.Should().Be(24);

        // Act
        _sut.NavigateDown();

        // Assert
        _sut.SelectedDay!.Date.Should().Be(new DateOnly(2026, 10, 1));
    }

    [Fact]
    public void NavigateUp_MovesSevenDays()
    {
        // Arrange - default selected day is 24
        _sut.SelectedDay!.DayNumber.Should().Be(24);

        // Act
        _sut.NavigateUp();

        // Assert
        _sut.SelectedDay!.Date.Should().Be(new DateOnly(2026, 9, 17));
    }

    [Fact]
    public void NavigateRight_CrossesMonthBoundary_UpdatesMonthAndYear()
    {
        // Arrange - select September 30
        CalendarDayModel day30 = _sut.Days.First(d => d.DayNumber == 30 && d.IsCurrentMonth);
        _sut.SelectDay(day30);

        // Act
        _sut.NavigateRight();

        // Assert
        _sut.SelectedDay!.Date.Should().Be(new DateOnly(2026, 10, 1));
        _sut.CurrentMonth.Should().Be(10);
        _sut.MonthYearText.Should().Be("October 2026");
    }

    [Fact]
    public void NavigateLeft_CrossesMonthBoundary_UpdatesMonthAndYear()
    {
        // Arrange - select September 1
        CalendarDayModel day1 = _sut.Days.First(d => d.DayNumber == 1 && d.IsCurrentMonth);
        _sut.SelectDay(day1);

        // Act
        _sut.NavigateLeft();

        // Assert
        _sut.SelectedDay!.Date.Should().Be(new DateOnly(2026, 8, 31));
        _sut.CurrentMonth.Should().Be(8);
        _sut.MonthYearText.Should().Be("August 2026");
    }

    [Fact]
    public void NavigateDown_CrossesMonthBoundary_UpdatesMonthAndYear()
    {
        // Arrange - select September 28 (28 + 7 = Oct 5)
        CalendarDayModel day28 = _sut.Days.First(d => d.DayNumber == 28 && d.IsCurrentMonth);
        _sut.SelectDay(day28);

        // Act
        _sut.NavigateDown();

        // Assert
        _sut.SelectedDay!.Date.Should().Be(new DateOnly(2026, 10, 5));
        _sut.CurrentMonth.Should().Be(10);
    }

    [Fact]
    public void NavigateUp_CrossesMonthBoundary_UpdatesMonthAndYear()
    {
        // Arrange - select September 3 (3 - 7 = Aug 27)
        CalendarDayModel day3 = _sut.Days.First(d => d.DayNumber == 3 && d.IsCurrentMonth);
        _sut.SelectDay(day3);

        // Act
        _sut.NavigateUp();

        // Assert
        _sut.SelectedDay!.Date.Should().Be(new DateOnly(2026, 8, 27));
        _sut.CurrentMonth.Should().Be(8);
    }

    [Fact]
    public void NavigateRight_CrossesYearBoundary_December31ToJanuary1()
    {
        // Arrange - navigate to December 2026
        while (_sut.CurrentMonth != 12)
        {
            _sut.NextMonth();
        }

        CalendarDayModel day31 = _sut.Days.First(d => d.DayNumber == 31 && d.IsCurrentMonth);
        _sut.SelectDay(day31);

        // Act
        _sut.NavigateRight();

        // Assert
        _sut.SelectedDay!.Date.Should().Be(new DateOnly(2027, 1, 1));
        _sut.CurrentMonth.Should().Be(1);
        _sut.CurrentYear.Should().Be(2027);
    }

    [Fact]
    public void NavigateLeft_CrossesYearBoundary_January1ToDecember31()
    {
        // Arrange - navigate to January 2026
        while (_sut.CurrentMonth != 1)
        {
            _sut.PreviousMonth();
        }

        CalendarDayModel day1 = _sut.Days.First(d => d.DayNumber == 1 && d.IsCurrentMonth);
        _sut.SelectDay(day1);

        // Act
        _sut.NavigateLeft();

        // Assert
        _sut.SelectedDay!.Date.Should().Be(new DateOnly(2025, 12, 31));
        _sut.CurrentMonth.Should().Be(12);
        _sut.CurrentYear.Should().Be(2025);
    }

    [Fact]
    public void Navigate_WhileExpanded_KeepsExpandedAndUpdatesSelection()
    {
        // Arrange
        CalendarDayModel targetDay = _sut.Days.First(d => d.DayNumber == 15 && d.IsCurrentMonth);
        _sut.SelectDay(targetDay);
        _sut.IsExpanded.Should().BeTrue();

        // Act
        _sut.NavigateRight();

        // Assert
        _sut.IsExpanded.Should().BeTrue();
        _sut.SelectedDay!.Date.Should().Be(new DateOnly(2026, 9, 16));
    }

    [Fact]
    public void Navigate_SelectedDayCorrectlyUpdatedAfterMonthSwitch()
    {
        // Arrange: select September 30 and navigate right to October 1
        CalendarDayModel day30 = _sut.Days.First(d => d.DayNumber == 30 && d.IsCurrentMonth);
        _sut.SelectDay(day30);
        _sut.NavigateRight();

        // Assert: the selected day should be a proper model in the new grid
        _sut.SelectedDay.Should().NotBeNull();
        _sut.SelectedDay!.Date.Should().Be(new DateOnly(2026, 10, 1));
        _sut.SelectedDay.IsCurrentMonth.Should().BeTrue();
    }

    // === Selected State Survives Transitions ===

    [Fact]
    public void SelectedState_SurvivesMonthNavigationWhenDateInGrid()
    {
        // Arrange: select September 30 which will appear as leading day in October grid
        CalendarDayModel day30 = _sut.Days.First(d => d.DayNumber == 30 && d.IsCurrentMonth);
        _sut.SelectDay(day30);

        // Act: navigate to next month — September 30 should be a leading day in October grid
        _sut.NextMonth();

        // Assert: selected day should be preserved since Sept 30 is visible in October's grid
        _sut.SelectedDay.Should().NotBeNull();
        _sut.SelectedDay!.Date.Should().Be(new DateOnly(2026, 9, 30));
    }

    [Fact]
    public void NextMonth_WhenSelectedDayLeavesVisibleGrid_ClearsSelectionAndCollapsesTray()
    {
        // Arrange: select September 15 which will NOT be in October grid
        CalendarDayModel day15 = _sut.Days.First(d => d.DayNumber == 15 && d.IsCurrentMonth);
        _sut.SelectDay(day15);
        _sut.IsExpanded.Should().BeTrue();
        _sut.SelectedDayHeader.Should().NotBeEmpty();

        // Act
        _sut.NextMonth();

        // Assert: selection cleared, header cleared, tray collapsed
        _sut.SelectedDay.Should().BeNull();
        _sut.SelectedDayHeader.Should().BeEmpty();
        _sut.IsExpanded.Should().BeFalse();
    }

    [Fact]
    public void PreviousMonth_WhenSelectedDayLeavesVisibleGrid_ClearsSelectionAndCollapsesTray()
    {
        // Arrange: select September 15 which will NOT be in August grid
        CalendarDayModel day15 = _sut.Days.First(d => d.DayNumber == 15 && d.IsCurrentMonth);
        _sut.SelectDay(day15);
        _sut.IsExpanded.Should().BeTrue();
        _sut.SelectedDayHeader.Should().NotBeEmpty();

        // Act
        _sut.PreviousMonth();

        // Assert: selection cleared, header cleared, tray collapsed
        _sut.SelectedDay.Should().BeNull();
        _sut.SelectedDayHeader.Should().BeEmpty();
        _sut.IsExpanded.Should().BeFalse();
    }

    [Fact]
    public void NavigateRight_WhenSelectedDayIsNull_SelectsFromFirstOfMonth()
    {
        // Arrange: clear selection by navigating to a month where selected date leaves grid
        CalendarDayModel day15 = _sut.Days.First(d => d.DayNumber == 15 && d.IsCurrentMonth);
        _sut.SelectDay(day15);
        _sut.NextMonth();
        _sut.SelectedDay.Should().BeNull();

        // Act: navigate right from base (October 1)
        _sut.NavigateRight();

        // Assert: October 2 is selected
        _sut.SelectedDay.Should().NotBeNull();
        _sut.SelectedDay!.Date.Should().Be(new DateOnly(2026, 10, 2));
    }
}
