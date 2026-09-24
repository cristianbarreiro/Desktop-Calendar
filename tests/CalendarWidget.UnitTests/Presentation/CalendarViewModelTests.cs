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

    [Fact]
    public void Constructor_InitializesWithCurrentMonthAnd42Cells()
    {
        // Act
        CalendarViewModel vm = new(_gridService, _clockService);

        // Assert
        vm.CurrentYear.Should().Be(2026);
        vm.CurrentMonth.Should().Be(9);
        vm.Days.Should().HaveCount(42);
        vm.SelectedDay.Should().NotBeNull();
        vm.SelectedDay!.DayNumber.Should().Be(24);
    }

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
    }

    [Fact]
    public void SelectDay_ValidDay_UpdatesSelectedDayAndFormattedText()
    {
        // Arrange
        CalendarViewModel vm = new(_gridService, _clockService);
        CalendarDayModel targetDay = vm.Days.First(d => d.DayNumber == 15 && d.IsCurrentMonth);

        // Act
        vm.SelectDay(targetDay);

        // Assert
        vm.SelectedDay.Should().Be(targetDay);
        vm.SelectedDateFormatted.Should().Contain("September 15, 2026");
    }

    [Fact]
    public void Today_WhenNavigatedAway_NavigatesBackToCurrentDate()
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
        vm.SelectedDay.Should().NotBeNull();
        vm.SelectedDay!.DayNumber.Should().Be(24);
    }
}
