using System.Globalization;
using CalendarWidget.Presentation.Models;
using CalendarWidget.Presentation.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CalendarWidget.Presentation.ViewModels;

/// <summary>
/// ViewModel for the main application calendar view.
/// </summary>
public sealed partial class CalendarViewModel : ViewModelBase
{
    private readonly ICalendarGridService _gridService;
    private readonly IClockService _clockService;

    [ObservableProperty]
    private int _currentYear;

    [ObservableProperty]
    private int _currentMonth;

    [ObservableProperty]
    private string _monthYearText = string.Empty;

    [ObservableProperty]
    private string _monthYearTitle = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<CalendarDayModel> _days = [];

    [ObservableProperty]
    private IReadOnlyList<string> _dayHeaders = [];

    [ObservableProperty]
    private CalendarDayModel? _selectedDay;

    [ObservableProperty]
    private string _selectedDateFormatted = string.Empty;

    [ObservableProperty]
    private string _selectedDayHeader = string.Empty;

    [ObservableProperty]
    private string _selectedDateHeader = string.Empty;

    [ObservableProperty]
    private DayOfWeek _firstDayOfWeek = DayOfWeek.Monday;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarViewModel"/> class.
    /// </summary>
    /// <param name="gridService">Calendar grid computation service.</param>
    /// <param name="clockService">System clock service.</param>
    public CalendarViewModel(ICalendarGridService gridService, IClockService clockService)
    {
        _gridService = gridService;
        _clockService = clockService;

        DateOnly today = _clockService.Today;
        _currentYear = today.Year;
        _currentMonth = today.Month;

        RefreshGrid();

        // Select today by default
        CalendarDayModel? todayModel = Days.FirstOrDefault(d => d.IsToday && d.IsCurrentMonth);
        if (todayModel is not null)
        {
            SelectDay(todayModel);
        }
    }

    /// <summary>
    /// Responds to changes in <see cref="FirstDayOfWeek"/> by regenerating headers and the calendar grid.
    /// </summary>
    /// <param name="value">The new first day of the week.</param>
    partial void OnFirstDayOfWeekChanged(DayOfWeek value)
    {
        RefreshGrid();
    }

    /// <summary>
    /// Navigates to the previous month, crossing year boundaries from January to December.
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
    /// Navigates to the next month, crossing year boundaries from December to January.
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
    /// Resets the calendar view and selection to the current system date.
    /// </summary>
    [RelayCommand]
    public void Today()
    {
        DateOnly today = _clockService.Today;
        CurrentYear = today.Year;
        CurrentMonth = today.Month;

        RefreshGrid();

        CalendarDayModel? todayModel = Days.FirstOrDefault(d => d.IsToday && d.IsCurrentMonth);
        if (todayModel is not null)
        {
            SelectDay(todayModel);
        }
    }

    /// <summary>
    /// Resets the calendar view and selection to the current system date (alias for <see cref="Today"/>).
    /// </summary>
    [RelayCommand]
    public void GoToToday()
    {
        Today();
    }

    /// <summary>
    /// Selects the specified calendar day and updates formatted date representations.
    /// </summary>
    /// <param name="day">The day model to select, or <c>null</c> to clear selection.</param>
    [RelayCommand]
    public void SelectDay(CalendarDayModel? day)
    {
        if (day is null)
        {
            SelectedDay = null;
            UpdateSelectedDateText(null);
            return;
        }

        SelectedDay = day;
        UpdateSelectedDateText(day);
    }

    /// <summary>
    /// Navigates the selected date by the given number of days, crossing month and year boundaries as needed.
    /// </summary>
    /// <param name="daysDelta">Number of days to move (negative for previous, positive for next).</param>
    public void NavigateByDays(int daysDelta)
    {
        DateOnly baseDate = SelectedDay?.Date ?? new DateOnly(CurrentYear, CurrentMonth, 1);
        DateOnly targetDate = baseDate.AddDays(daysDelta);

        // If target date is outside the currently displayed month, navigate to that month
        if (targetDate.Year != CurrentYear || targetDate.Month != CurrentMonth)
        {
            CurrentYear = targetDate.Year;
            CurrentMonth = targetDate.Month;
            RefreshGrid();
        }

        CalendarDayModel? targetDay = Days.FirstOrDefault(d => d.Date == targetDate);
        if (targetDay is not null)
        {
            SelectDay(targetDay);
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

    private void UpdateSelectedDateText(CalendarDayModel? day)
    {
        if (day is null)
        {
            SelectedDateFormatted = string.Empty;
            SelectedDayHeader = string.Empty;
            SelectedDateHeader = string.Empty;
            return;
        }

        DateTime dt = day.Date.ToDateTime(TimeOnly.MinValue);
        SelectedDateFormatted = dt.ToString("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture);
        SelectedDateHeader = dt.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();
        SelectedDayHeader = day.IsToday
            ? "TODAY"
            : SelectedDateHeader;
    }

    private void RefreshGrid()
    {
        DateTime currentMonthDate = new(CurrentYear, CurrentMonth, 1);
        MonthYearText = currentMonthDate.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        MonthYearTitle = MonthYearText.ToUpperInvariant();

        DayHeaders = _gridService.GetDayHeaders(FirstDayOfWeek);
        Days = _gridService.GenerateGrid(CurrentYear, CurrentMonth, FirstDayOfWeek, _clockService.Today);

        if (SelectedDay is not null)
        {
            CalendarDayModel? matchingDay = Days.FirstOrDefault(d => d.Date == SelectedDay.Date);
            if (matchingDay is not null)
            {
                SelectedDay = matchingDay;
                UpdateSelectedDateText(matchingDay);
            }
            else
            {
                SelectedDay = null;
                UpdateSelectedDateText(null);
            }
        }
    }
}
