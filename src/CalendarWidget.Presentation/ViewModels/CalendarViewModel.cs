using System.Globalization;
using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Exceptions;
using CalendarWidget.Core.Interfaces;
using CalendarWidget.Presentation.Models;
using CalendarWidget.Presentation.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace CalendarWidget.Presentation.ViewModels;

/// <summary>
/// ViewModel for the main application calendar view.
/// </summary>
public sealed partial class CalendarViewModel : ViewModelBase, IDisposable
{
    private readonly ICalendarGridService _gridService;
    private readonly IClockService _clockService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISettingsService? _settingsService;
    private readonly IDateTimeFormatService? _formatService;
    private readonly IDataManagementService? _dataManagementService;

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
    [NotifyPropertyChangedFor(nameof(HasSelectedDay))]
    [NotifyPropertyChangedFor(nameof(HasNoSelectedDayEvents))]
    private CalendarDayModel? _selectedDay;

    [ObservableProperty]
    private string _selectedDateFormatted = string.Empty;

    [ObservableProperty]
    private string _selectedDayHeader = string.Empty;

    [ObservableProperty]
    private string _selectedDateHeader = string.Empty;

    [ObservableProperty]
    private DayOfWeek _firstDayOfWeek = DayOfWeek.Monday;

    // ── Day detail panel ──────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedDayEvents))]
    [NotifyPropertyChangedFor(nameof(HasNoSelectedDayEvents))]
    private IReadOnlyList<EventListItemModel> _selectedDayEvents = [];

    /// <summary>Gets whether the currently selected day has any events.</summary>
    public bool HasSelectedDayEvents => SelectedDayEvents.Count > 0;

    /// <summary>Gets whether a day is selected but has no events.</summary>
    public bool HasNoSelectedDayEvents => SelectedDay is not null && SelectedDayEvents.Count == 0;

    /// <summary>Gets whether a day is currently selected.</summary>
    public bool HasSelectedDay => SelectedDay is not null;

    [ObservableProperty]
    private bool _isLoadingEvents;

    // ── Event form ────────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _isEventFormVisible;

    [ObservableProperty]
    private EventFormViewModel _eventForm = new();

    // ── Delete confirmation ───────────────────────────────────────────────────

    [ObservableProperty]
    private bool _isDeleteConfirmVisible;

    [ObservableProperty]
    private Guid _pendingDeleteId;

    [ObservableProperty]
    private string _pendingDeleteTitle = string.Empty;

    // ── Status / error ────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarViewModel"/> class.
    /// </summary>
    public CalendarViewModel(
        ICalendarGridService gridService,
        IClockService clockService,
        IServiceScopeFactory scopeFactory)
        : this(gridService, clockService, scopeFactory, null, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarViewModel"/> class with full settings support.
    /// </summary>
    public CalendarViewModel(
        ICalendarGridService gridService,
        IClockService clockService,
        IServiceScopeFactory scopeFactory,
        ISettingsService? settingsService,
        IDateTimeFormatService? formatService,
        IDataManagementService? dataManagementService)
    {
        _gridService = gridService;
        _clockService = clockService;
        _scopeFactory = scopeFactory;
        _settingsService = settingsService;
        _formatService = formatService;
        _dataManagementService = dataManagementService;

        if (_settingsService is not null)
        {
            _firstDayOfWeek = _settingsService.CurrentSettings.FirstDayOfWeek;
            _settingsService.SettingsChanged += OnSettingsChanged;
        }

        if (_dataManagementService is not null)
        {
            _dataManagementService.DataChanged += OnDataChanged;
        }

        if (_formatService is not null)
        {
            _formatService.FormatChanged += OnFormatChanged;
        }

        DateOnly today = _clockService.Today;
        _currentYear = today.Year;
        _currentMonth = today.Month;

        RefreshGrid();

        CalendarDayModel? todayModel = Days.FirstOrDefault(d => d.IsToday && d.IsCurrentMonth);
        if (todayModel is not null)
        {
            SelectDay(todayModel);
        }
    }

    /// <summary>
    /// Responds to changes in <see cref="FirstDayOfWeek"/> by regenerating headers and the calendar grid.
    /// </summary>
    partial void OnFirstDayOfWeekChanged(DayOfWeek value)
    {
        RefreshGrid();
    }

    // ── Month navigation ──────────────────────────────────────────────────────

    /// <summary>Navigates to the previous month.</summary>
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

    /// <summary>Navigates to the next month.</summary>
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

    /// <summary>Resets the calendar view and selection to the current system date.</summary>
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

    /// <summary>Alias for <see cref="Today"/>.</summary>
    [RelayCommand]
    public void GoToToday() => Today();

    // ── Day selection ─────────────────────────────────────────────────────────

    /// <summary>Selects the specified calendar day and loads its events.</summary>
    [RelayCommand]
    public void SelectDay(CalendarDayModel? day)
    {
        if (day is null)
        {
            SelectedDay = null;
            UpdateSelectedDateText(null);
            SelectedDayEvents = [];
            return;
        }

        SelectedDay = day;
        UpdateSelectedDateText(day);
        _ = LoadSelectedDayEventsAsync();
    }

    // ── Keyboard navigation ───────────────────────────────────────────────────

    /// <summary>Navigates the selected date by the given number of days.</summary>
    public void NavigateByDays(int daysDelta)
    {
        DateOnly baseDate = SelectedDay?.Date ?? new DateOnly(CurrentYear, CurrentMonth, 1);
        DateOnly targetDate = baseDate.AddDays(daysDelta);

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

    /// <summary>Navigates one day left.</summary>
    [RelayCommand]
    public void NavigateLeft() => NavigateByDays(-1);

    /// <summary>Navigates one day right.</summary>
    [RelayCommand]
    public void NavigateRight() => NavigateByDays(1);

    /// <summary>Navigates one week up.</summary>
    [RelayCommand]
    public void NavigateUp() => NavigateByDays(-7);

    /// <summary>Navigates one week down.</summary>
    [RelayCommand]
    public void NavigateDown() => NavigateByDays(7);

    /// <summary>Navigates to the previous month preserving day-of-month selection.</summary>
    [RelayCommand]
    public void NavigatePreviousMonthKeepingSelection() => NavigateMonthWithSelection(forward: false);

    /// <summary>Navigates to the next month preserving day-of-month selection.</summary>
    [RelayCommand]
    public void NavigateNextMonthKeepingSelection() => NavigateMonthWithSelection(forward: true);

    /// <summary>Moves selection to the first or last day of the current month.</summary>
    public void NavigateToMonthBoundary(bool lastDay)
    {
        int targetDay = lastDay ? DateTime.DaysInMonth(CurrentYear, CurrentMonth) : 1;
        DateOnly targetDate = new(CurrentYear, CurrentMonth, targetDay);
        CalendarDayModel? targetModel = Days.FirstOrDefault(d => d.Date == targetDate);
        if (targetModel is not null)
        {
            SelectDay(targetModel);
        }
    }

    /// <summary>Moves selection to the first day of the current month.</summary>
    [RelayCommand]
    public void NavigateToMonthStart() => NavigateToMonthBoundary(lastDay: false);

    /// <summary>Moves selection to the last day of the current month.</summary>
    [RelayCommand]
    public void NavigateToMonthEnd() => NavigateToMonthBoundary(lastDay: true);

    // ── Event CRUD ────────────────────────────────────────────────────────────

    /// <summary>Opens the event creation form for the currently selected day.</summary>
    [RelayCommand]
    public void OpenCreateEventForm()
    {
        if (SelectedDay is null)
            return;

        DateTime baseDate = SelectedDay.Date.ToDateTime(TimeOnly.MinValue);
        DateTime now = _clockService.Now;
        DateTime startLocal = baseDate.Date == now.Date
            ? new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0)
            : baseDate;

        EventForm = new EventFormViewModel
        {
            Title = string.Empty,
            Description = string.Empty,
            StartTime = startLocal,
            EndTime = startLocal.AddHours(1),
            IsAllDay = false,
        };

        IsEventFormVisible = true;
    }

    /// <summary>Opens the event edit form populated with the given event's data.</summary>
    [RelayCommand]
    public void OpenEditEventForm(EventListItemModel item)
    {
        _ = LoadAndOpenEditFormAsync(item.Id);
    }

    /// <summary>Saves the current event form (create or update).</summary>
    [RelayCommand]
    public void SaveEventForm()
    {
        _ = SaveEventFormAsync();
    }

    /// <summary>Cancels and closes the event form.</summary>
    [RelayCommand]
    public void CancelEventForm()
    {
        IsEventFormVisible = false;
        EventForm.ClearError();
    }

    /// <summary>Requests deletion of the specified event (shows confirmation).</summary>
    [RelayCommand]
    public void RequestDeleteEvent(EventListItemModel item)
    {
        PendingDeleteId = item.Id;
        PendingDeleteTitle = item.Title;
        IsDeleteConfirmVisible = true;
    }

    /// <summary>Confirms and executes the pending event deletion.</summary>
    [RelayCommand]
    public void ConfirmDeleteEvent()
    {
        _ = ConfirmDeleteEventAsync();
    }

    /// <summary>Cancels the pending deletion.</summary>
    [RelayCommand]
    public void CancelDeleteEvent()
    {
        IsDeleteConfirmVisible = false;
        PendingDeleteId = Guid.Empty;
        PendingDeleteTitle = string.Empty;
    }

    // ── Test helpers (internal) ─────────────────────────────────────────────

    /// <summary>Exposes <see cref="RefreshGridWithEventsAsync"/> for unit testing.</summary>
    internal Task RefreshGridWithEventsForTestAsync() => RefreshGridWithEventsAsync();

    /// <summary>Exposes <see cref="SaveEventFormAsync"/> for unit testing.</summary>
    internal Task SaveEventFormForTestAsync() => SaveEventFormAsync();

    /// <summary>Exposes <see cref="LoadSelectedDayEventsAsync"/> for unit testing.</summary>
    internal Task LoadSelectedDayEventsForTestAsync() => LoadSelectedDayEventsAsync();

    /// <summary>Exposes delete confirmation for unit testing with a specific ID.</summary>
    internal async Task ConfirmDeleteEventForTestAsync(Guid id)
    {
        PendingDeleteId = id;
        await ConfirmDeleteEventAsync();
    }

    // ── Private async helpers ─────────────────────────────────────────────────

    private async Task LoadSelectedDayEventsAsync()
    {
        if (SelectedDay is null)
        {
            SelectedDayEvents = [];
            return;
        }

        IsLoadingEvents = true;
        try
        {
            DateOnly date = SelectedDay.Date;
            DateTime rangeStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            DateTime rangeEnd = rangeStart.AddDays(1);

            using IServiceScope scope = _scopeFactory.CreateScope();
            ICalendarEventRepository repo = scope.ServiceProvider.GetRequiredService<ICalendarEventRepository>();
            IReadOnlyList<CalendarEvent> events =
                await repo.GetByDateRangeAsync(rangeStart, rangeEnd);

            SelectedDayEvents = events
                .OrderBy(e => e.StartTime)
                .Select(MapToListItem)
                .ToList();
        }
        catch (Exception)
        {
            SelectedDayEvents = [];
            StatusMessage = "Failed to load events.";
        }
        finally
        {
            IsLoadingEvents = false;
        }
    }

    private async Task LoadAndOpenEditFormAsync(Guid id)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        ICalendarEventRepository repo = scope.ServiceProvider.GetRequiredService<ICalendarEventRepository>();
        CalendarEvent? ev = await repo.GetByIdAsync(id);
        if (ev is null)
        {
            StatusMessage = "Event no longer exists.";
            await LoadSelectedDayEventsAsync();
            return;
        }

        EventForm = new EventFormViewModel
        {
            EditingId = ev.Id,
            Title = ev.Title,
            Description = ev.Description ?? string.Empty,
            StartTime = ev.StartTime.ToLocalTime(),
            EndTime = ev.EndTime.ToLocalTime(),
            IsAllDay = ev.IsAllDay,
        };

        IsEventFormVisible = true;
    }

    private async Task SaveEventFormAsync()
    {
        if (!EventForm.IsValid())
            return;

        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            ICalendarEventRepository repo = scope.ServiceProvider.GetRequiredService<ICalendarEventRepository>();

            if (EventForm.IsEditing)
            {
                CalendarEvent? existing = await repo.GetByIdAsync(EventForm.EditingId!.Value);
                if (existing is null)
                {
                    EventForm.ValidationError = "Event no longer exists.";
                    return;
                }

                existing.Title = EventForm.Title.Trim();
                existing.Description = string.IsNullOrWhiteSpace(EventForm.Description)
                    ? null
                    : EventForm.Description.Trim();
                existing.StartTime = EventForm.IsAllDay
                    ? DateTime.SpecifyKind(EventForm.StartTime.Date, DateTimeKind.Utc)
                    : EventForm.StartTime.ToUniversalTime();
                existing.EndTime = EventForm.IsAllDay
                    ? DateTime.SpecifyKind(EventForm.StartTime.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc)
                    : EventForm.EndTime.ToUniversalTime();
                existing.IsAllDay = EventForm.IsAllDay;
                existing.UpdatedAt = DateTime.UtcNow;

                existing.Validate();
                await repo.UpdateAsync(existing);
            }
            else
            {
                DateTime startUtc = EventForm.IsAllDay
                    ? DateTime.SpecifyKind(EventForm.StartTime.Date, DateTimeKind.Utc)
                    : EventForm.StartTime.ToUniversalTime();
                DateTime endUtc = EventForm.IsAllDay
                    ? DateTime.SpecifyKind(EventForm.StartTime.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc)
                    : EventForm.EndTime.ToUniversalTime();

                CalendarEvent newEvent = new()
                {
                    Id = Guid.NewGuid(),
                    Title = EventForm.Title.Trim(),
                    Description = string.IsNullOrWhiteSpace(EventForm.Description)
                        ? null
                        : EventForm.Description.Trim(),
                    StartTime = startUtc,
                    EndTime = endUtc,
                    IsAllDay = EventForm.IsAllDay,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                newEvent.Validate();
                await repo.AddAsync(newEvent);
            }

            IsEventFormVisible = false;
            EventForm.ClearError();
            await RefreshGridWithEventsAsync();
            await LoadSelectedDayEventsAsync();
        }
        catch (DomainValidationException ex)
        {
            EventForm.ValidationError = ex.Message;
        }
        catch (Exception)
        {
            EventForm.ValidationError = "Failed to save event. Please try again.";
        }
    }

    private async Task ConfirmDeleteEventAsync()
    {
        Guid id = PendingDeleteId;
        IsDeleteConfirmVisible = false;
        PendingDeleteId = Guid.Empty;
        PendingDeleteTitle = string.Empty;

        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            ICalendarEventRepository repo = scope.ServiceProvider.GetRequiredService<ICalendarEventRepository>();
            await repo.DeleteAsync(id);
            await RefreshGridWithEventsAsync();
            await LoadSelectedDayEventsAsync();
        }
        catch (Exception)
        {
            StatusMessage = "Failed to delete event.";
        }
    }

    // ── Grid refresh with event indicators ───────────────────────────────────

    private async Task RefreshGridWithEventsAsync()
    {
        if (Days.Count == 0)
            return;

        DateOnly firstVisible = Days[0].Date;
        DateOnly lastVisible = Days[Days.Count - 1].Date;

        DateTime rangeStart = firstVisible.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        DateTime rangeEnd = lastVisible.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1);

        using IServiceScope scope = _scopeFactory.CreateScope();
        ICalendarEventRepository repo = scope.ServiceProvider.GetRequiredService<ICalendarEventRepository>();
        IReadOnlyList<CalendarEvent> events =
            await repo.GetByDateRangeAsync(rangeStart, rangeEnd);

        HashSet<DateOnly> datesWithEvents = BuildDatesWithEvents(events);
        ApplyEventIndicators(datesWithEvents);
    }

    private static HashSet<DateOnly> BuildDatesWithEvents(IReadOnlyList<CalendarEvent> events)
    {
        HashSet<DateOnly> dates = [];
        foreach (CalendarEvent ev in events)
        {
            // Use UTC dates to determine which calendar days are covered.
            // The grid cells are also compared against UTC-based dates from the range query.
            DateOnly start = DateOnly.FromDateTime(ev.StartTime);
            DateOnly end = DateOnly.FromDateTime(ev.EndTime);
            // If EndTime is exactly midnight, the event ends at the start of that day — don't mark it.
            if (ev.EndTime.TimeOfDay == TimeSpan.Zero && end > start)
                end = end.AddDays(-1);
            DateOnly current = start;
            while (current <= end)
            {
                dates.Add(current);
                current = current.AddDays(1);
            }
        }

        return dates;
    }

    private void ApplyEventIndicators(HashSet<DateOnly> datesWithEvents)
    {
        List<CalendarDayModel> updated = new(Days.Count);
        foreach (CalendarDayModel cell in Days)
        {
            updated.Add(cell with { HasEvents = datesWithEvents.Contains(cell.Date) });
        }

        Days = updated;

        if (SelectedDay is not null)
        {
            CalendarDayModel? match = Days.FirstOrDefault(d => d.Date == SelectedDay.Date);
            if (match is not null)
            {
                SelectedDay = match;
            }
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private EventListItemModel MapToListItem(CalendarEvent ev)
    {
        string timeLabel;
        if (ev.IsAllDay)
        {
            timeLabel = "All day";
        }
        else if (_formatService is not null)
        {
            timeLabel = _formatService.FormatEventRange(ev.StartTime, ev.EndTime, ev.IsAllDay);
        }
        else
        {
            DateTime startLocal = ev.StartTime.Kind == DateTimeKind.Utc ? ev.StartTime.ToLocalTime() : ev.StartTime;
            DateTime endLocal = ev.EndTime.Kind == DateTimeKind.Utc ? ev.EndTime.ToLocalTime() : ev.EndTime;
            timeLabel = $"{startLocal:HH:mm} \u2013 {endLocal:HH:mm}";
        }

        return new EventListItemModel(
            Id: ev.Id,
            Title: ev.Title,
            TimeLabel: timeLabel,
            Description: ev.Description,
            IsAllDay: ev.IsAllDay);
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
        SelectedDateFormatted = _formatService is not null
            ? $"{dt.ToString("dddd", CultureInfo.CurrentCulture)}, {_formatService.FormatDate(dt)}"
            : dt.ToString("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture);

        SelectedDateHeader = dt.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();
        SelectedDayHeader = day.IsToday ? "TODAY" : SelectedDateHeader;
    }

    private void OnSettingsChanged(object? sender, UserSettings settings)
    {
        FirstDayOfWeek = settings.FirstDayOfWeek;
        RefreshGrid();
        UpdateSelectedDateText(SelectedDay);
    }

    private void OnDataChanged(object? sender, EventArgs e)
    {
        _ = RefreshGridWithEventsAsync();
        _ = LoadSelectedDayEventsAsync();
    }

    private void OnFormatChanged(object? sender, EventArgs e)
    {
        UpdateSelectedDateText(SelectedDay);
        _ = LoadSelectedDayEventsAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_settingsService is not null)
        {
            _settingsService.SettingsChanged -= OnSettingsChanged;
        }

        if (_dataManagementService is not null)
        {
            _dataManagementService.DataChanged -= OnDataChanged;
        }

        if (_formatService is not null)
        {
            _formatService.FormatChanged -= OnFormatChanged;
        }
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
                SelectedDayEvents = [];
            }
        }

        _ = RefreshGridWithEventsAsync();
    }

    private void NavigateMonthWithSelection(bool forward)
    {
        int currentDay = SelectedDay?.Date.Day ?? 1;

        if (forward)
        {
            if (CurrentMonth == 12) { CurrentYear++; CurrentMonth = 1; }
            else { CurrentMonth++; }
        }
        else
        {
            if (CurrentMonth == 1) { CurrentYear--; CurrentMonth = 12; }
            else { CurrentMonth--; }
        }

        RefreshGrid();

        int clampedDay = Math.Min(currentDay, DateTime.DaysInMonth(CurrentYear, CurrentMonth));
        DateOnly targetDate = new(CurrentYear, CurrentMonth, clampedDay);
        CalendarDayModel? targetModel = Days.FirstOrDefault(d => d.Date == targetDate);
        if (targetModel is not null)
        {
            SelectDay(targetModel);
        }
    }
}
