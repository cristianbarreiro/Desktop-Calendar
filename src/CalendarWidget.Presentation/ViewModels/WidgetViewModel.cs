using System.Globalization;
using CalendarWidget.Presentation.Models;
using CalendarWidget.Presentation.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CalendarWidget.Presentation.ViewModels;

/// <summary>
/// ViewModel for the compact desktop calendar widget window.
/// </summary>
public sealed partial class WidgetViewModel : ViewModelBase, IDisposable
{
    private readonly IWindowManager _windowManager;
    private readonly ICalendarGridService _gridService;
    private readonly IClockService _clockService;

    [ObservableProperty]
    private int _currentYear;

    [ObservableProperty]
    private int _currentMonth;

    [ObservableProperty]
    private string _monthYearText = string.Empty;

    [ObservableProperty]
    private string _currentTimeText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<CalendarDayModel> _days = [];

    [ObservableProperty]
    private IReadOnlyList<string> _dayHeaders = [];

    [ObservableProperty]
    private CalendarDayModel? _selectedDay;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private string _selectedDayHeader = string.Empty;

    [ObservableProperty]
    private DayOfWeek _firstDayOfWeek = DayOfWeek.Monday;

    /// <summary>
    /// Initializes a new instance of the <see cref="WidgetViewModel"/> class.
    /// </summary>
    /// <param name="windowManager">Window orchestration service.</param>
    /// <param name="gridService">Calendar grid calculation service.</param>
    /// <param name="clockService">Clock service providing current time and tick events.</param>
    public WidgetViewModel(
        IWindowManager windowManager,
        ICalendarGridService gridService,
        IClockService clockService)
    {
        _windowManager = windowManager;
        _gridService = gridService;
        _clockService = clockService;

        DateOnly today = _clockService.Today;
        _currentYear = today.Year;
        _currentMonth = today.Month;

        UpdateTimeText(_clockService.Now);
        _clockService.TimeChanged += OnClockTimeChanged;

        RefreshGrid();

        // Default selection to today
        CalendarDayModel? todayModel = Days.FirstOrDefault(d => d.IsToday && d.IsCurrentMonth);
        if (todayModel is not null)
        {
            _selectedDay = todayModel;
            UpdateSelectedDayHeader(todayModel);
        }
    }

    /// <summary>
    /// Navigates to the full desktop application window.
    /// </summary>
    [RelayCommand]
    public void SwitchToApp()
    {
        _windowManager.ShowFullApplication();
    }

    /// <summary>
    /// Minimizes the widget window.
    /// </summary>
    [RelayCommand]
    public void Minimize()
    {
        _windowManager.MinimizeWidget();
    }

    /// <summary>
    /// Navigates to the previous month in the widget.
    /// </summary>
    [RelayCommand]
    public void PreviousMonth()
    {
        if (CurrentMonth == 1)
        {
            CurrentYear--;
            CurrentMonth = 12;
        }
        else
        {
            CurrentMonth--;
        }

        RefreshGrid();
    }

    /// <summary>
    /// Navigates to the next month in the widget.
    /// </summary>
    [RelayCommand]
    public void NextMonth()
    {
        if (CurrentMonth == 12)
        {
            CurrentYear++;
            CurrentMonth = 1;
        }
        else
        {
            CurrentMonth++;
        }

        RefreshGrid();
    }

    /// <summary>
    /// Handles day cell selection and expands/collapses the detail tray.
    /// </summary>
    /// <param name="day">The clicked day model.</param>
    [RelayCommand]
    public void SelectDay(CalendarDayModel? day)
    {
        if (day is null)
        {
            return;
        }

        if (SelectedDay is not null && SelectedDay.Date == day.Date && IsExpanded)
        {
            // Toggle collapse when clicking currently selected day again
            IsExpanded = false;
        }
        else
        {
            SelectedDay = day;
            UpdateSelectedDayHeader(day);
            IsExpanded = true;
        }
    }

    /// <summary>
    /// Toggles the expanded day detail tray.
    /// </summary>
    [RelayCommand]
    public void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
    }

    /// <summary>
    /// Collapses the expanded detail tray. No-op when already collapsed.
    /// </summary>
    [RelayCommand]
    public void Escape()
    {
        if (IsExpanded)
        {
            IsExpanded = false;
        }
    }

    /// <summary>
    /// Navigates the selected date by the given number of days, crossing month boundaries as needed.
    /// </summary>
    /// <param name="daysDelta">Number of days to move (negative for previous, positive for next).</param>
    public void NavigateByDays(int daysDelta)
    {
        DateOnly baseDate = SelectedDay?.Date ?? new DateOnly(CurrentYear, CurrentMonth, 1);
        DateOnly targetDate = baseDate.AddDays(daysDelta);

        // If the target date is outside the currently displayed month, navigate to that month
        if (targetDate.Year != CurrentYear || targetDate.Month != CurrentMonth)
        {
            CurrentYear = targetDate.Year;
            CurrentMonth = targetDate.Month;
            RefreshGrid();
        }

        CalendarDayModel? targetDay = Days.FirstOrDefault(d => d.Date == targetDate);
        if (targetDay is not null)
        {
            SelectedDay = targetDay;
            UpdateSelectedDayHeader(targetDay);

            if (IsExpanded)
            {
                // Keep expanded when navigating with keyboard while expanded
                IsExpanded = true;
            }
        }
    }

    /// <summary>
    /// Navigates the selection one day to the left (previous day).
    /// </summary>
    [RelayCommand]
    public void NavigateLeft()
    {
        NavigateByDays(-1);
    }

    /// <summary>
    /// Navigates the selection one day to the right (next day).
    /// </summary>
    [RelayCommand]
    public void NavigateRight()
    {
        NavigateByDays(1);
    }

    /// <summary>
    /// Navigates the selection one week up (previous 7 days).
    /// </summary>
    [RelayCommand]
    public void NavigateUp()
    {
        NavigateByDays(-7);
    }

    /// <summary>
    /// Navigates the selection one week down (next 7 days).
    /// </summary>
    [RelayCommand]
    public void NavigateDown()
    {
        NavigateByDays(7);
    }

    private void OnClockTimeChanged(object? sender, DateTime time)
    {
        UpdateTimeText(time);
    }

    private void UpdateTimeText(DateTime time)
    {
        CurrentTimeText = time.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
    }

    private void UpdateSelectedDayHeader(CalendarDayModel day)
    {
        if (day.IsToday)
        {
            SelectedDayHeader = "TODAY";
        }
        else
        {
            DateTime dt = day.Date.ToDateTime(TimeOnly.MinValue);
            SelectedDayHeader = dt.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();
        }
    }

    private void RefreshGrid()
    {
        MonthYearText = new DateTime(CurrentYear, CurrentMonth, 1)
            .ToString("MMMM yyyy", CultureInfo.InvariantCulture);

        DayHeaders = _gridService.GetDayHeaders(FirstDayOfWeek);
        Days = _gridService.GenerateGrid(CurrentYear, CurrentMonth, FirstDayOfWeek, _clockService.Today);

        if (SelectedDay is not null)
        {
            SelectedDay = Days.FirstOrDefault(d => d.Date == SelectedDay.Date);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _clockService.TimeChanged -= OnClockTimeChanged;
    }
}
