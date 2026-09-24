using CommunityToolkit.Mvvm.ComponentModel;

namespace CalendarWidget.Presentation.ViewModels;

/// <summary>
/// ViewModel for the main application settings view shell.
/// </summary>
public sealed partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "Settings";

    [ObservableProperty]
    private string _selectedTheme = "Dark (Default)";

    [ObservableProperty]
    private bool _alwaysOnTop;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private double _widgetOpacity = 100.0;

    [ObservableProperty]
    private string _firstDayOfWeek = "Monday";

    [ObservableProperty]
    private string _timeFormat = "24-hour";
}
