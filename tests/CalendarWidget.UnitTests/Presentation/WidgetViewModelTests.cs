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
}
