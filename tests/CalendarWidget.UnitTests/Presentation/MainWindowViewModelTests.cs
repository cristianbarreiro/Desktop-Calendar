using CalendarWidget.Presentation.Services;
using CalendarWidget.Presentation.ViewModels;
using CalendarWidget.UnitTests.Fakes;
using FluentAssertions;

namespace CalendarWidget.UnitTests.Presentation;

public sealed class MainWindowViewModelTests
{
    private readonly TestWindowManager _windowManager = new();
    private readonly CalendarGridService _gridService = new();
    private readonly TestClockService _clockService = new(new DateTime(2026, 9, 24, 10, 0, 0));
    private readonly CalendarViewModel _calendarViewModel;
    private readonly NotesViewModel _notesViewModel = new();
    private readonly SettingsViewModel _settingsViewModel = new();
    private readonly MainWindowViewModel _sut;

    public MainWindowViewModelTests()
    {
        _calendarViewModel = new CalendarViewModel(_gridService, _clockService);
        _sut = new MainWindowViewModel(_windowManager, _calendarViewModel, _notesViewModel, _settingsViewModel);
    }

    [Fact]
    public void Constructor_InitializesWithCalendarViewAndSelectedTab()
    {
        // Assert
        _sut.CurrentViewModel.Should().Be(_calendarViewModel);
        _sut.SelectedTab.Should().Be(NavigationTab.Calendar);
    }

    [Fact]
    public void NavigateNotes_WhenInvoked_SetsCurrentViewModelToNotesAndUpdatesTab()
    {
        // Act
        _sut.NavigateNotes();

        // Assert
        _sut.CurrentViewModel.Should().Be(_notesViewModel);
        _sut.SelectedTab.Should().Be(NavigationTab.Notes);
    }

    [Fact]
    public void NavigateSettings_WhenInvoked_SetsCurrentViewModelToSettingsAndUpdatesTab()
    {
        // Act
        _sut.NavigateSettings();

        // Assert
        _sut.CurrentViewModel.Should().Be(_settingsViewModel);
        _sut.SelectedTab.Should().Be(NavigationTab.Settings);
    }

    [Fact]
    public void NavigateCalendar_WhenInvoked_SetsCurrentViewModelToCalendarAndUpdatesTab()
    {
        // Arrange
        _sut.NavigateNotes();

        // Act
        _sut.NavigateCalendar();

        // Assert
        _sut.CurrentViewModel.Should().Be(_calendarViewModel);
        _sut.SelectedTab.Should().Be(NavigationTab.Calendar);
    }

    [Fact]
    public void SwitchToWidget_WhenInvoked_CallsWindowManagerShowWidget()
    {
        // Act
        _sut.SwitchToWidget();

        // Assert
        _windowManager.ShowWidgetCallCount.Should().Be(1);
    }
}
