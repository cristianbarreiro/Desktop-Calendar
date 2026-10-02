using CalendarWidget.Presentation.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CalendarWidget.Presentation.ViewModels;

/// <summary>
/// ViewModel for the main application window shell.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IWindowManager _windowManager;
    private readonly HomeViewModel _homeViewModel;
    private readonly CalendarViewModel _calendarViewModel;
    private readonly NotesViewModel _notesViewModel;
    private readonly SettingsViewModel _settingsViewModel;

    [ObservableProperty]
    private ViewModelBase _currentViewModel;

    [ObservableProperty]
    private NavigationTab _selectedTab = NavigationTab.Home;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="windowManager">Window orchestration service.</param>
    /// <param name="homeViewModel">Home page view model.</param>
    /// <param name="calendarViewModel">Calendar page view model.</param>
    /// <param name="notesViewModel">Notes page view model.</param>
    /// <param name="settingsViewModel">Settings page view model.</param>
    public MainWindowViewModel(
        IWindowManager windowManager,
        HomeViewModel homeViewModel,
        CalendarViewModel calendarViewModel,
        NotesViewModel notesViewModel,
        SettingsViewModel settingsViewModel)
    {
        _windowManager = windowManager;
        _homeViewModel = homeViewModel;
        _calendarViewModel = calendarViewModel;
        _notesViewModel = notesViewModel;
        _settingsViewModel = settingsViewModel;

        _currentViewModel = _homeViewModel;
    }

    /// <summary>
    /// Navigates to the Home view.
    /// </summary>
    [RelayCommand]
    public void NavigateHome()
    {
        SelectedTab = NavigationTab.Home;
        CurrentViewModel = _homeViewModel;
    }

    /// <summary>
    /// Navigates to the Calendar view.
    /// </summary>
    [RelayCommand]
    public void NavigateCalendar()
    {
        SelectedTab = NavigationTab.Calendar;
        CurrentViewModel = _calendarViewModel;
    }

    /// <summary>
    /// Navigates to the Notes view.
    /// </summary>
    [RelayCommand]
    public void NavigateNotes()
    {
        SelectedTab = NavigationTab.Notes;
        CurrentViewModel = _notesViewModel;
    }

    /// <summary>
    /// Navigates to the Settings view.
    /// </summary>
    [RelayCommand]
    public void NavigateSettings()
    {
        SelectedTab = NavigationTab.Settings;
        CurrentViewModel = _settingsViewModel;
    }

    /// <summary>
    /// Switches the display from the full application to the compact widget.
    /// </summary>
    [RelayCommand]
    public void SwitchToWidget()
    {
        _windowManager.ShowWidget();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _calendarViewModel.Dispose();
        _notesViewModel.Dispose();
    }
}
