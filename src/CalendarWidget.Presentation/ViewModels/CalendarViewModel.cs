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
    /// Navigates to the previous month.
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
    /// Navigates to the next month.
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
    /// Resets the calendar view to the current system date.
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
    /// Selects the specified calendar day.
    /// </summary>
    /// <param name="day">The day model to select.</param>
    [RelayCommand]
    public void SelectDay(CalendarDayModel? day)
    {
        if (day is null)
        {
            return;
        }

        SelectedDay = day;
        DateTime dt = day.Date.ToDateTime(TimeOnly.MinValue);
        SelectedDateFormatted = dt.ToString("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture);
    }

    private void RefreshGrid()
    {
        MonthYearTitle = new DateTime(CurrentYear, CurrentMonth, 1)
            .ToString("MMMM yyyy", CultureInfo.InvariantCulture)
            .ToUpperInvariant();

        DayHeaders = _gridService.GetDayHeaders(FirstDayOfWeek);
        Days = _gridService.GenerateGrid(CurrentYear, CurrentMonth, FirstDayOfWeek, _clockService.Today);

        // Keep selection if date is still in new grid, otherwise select null or match
        if (SelectedDay is not null)
        {
            SelectedDay = Days.FirstOrDefault(d => d.Date == SelectedDay.Date);
        }
    }
}
