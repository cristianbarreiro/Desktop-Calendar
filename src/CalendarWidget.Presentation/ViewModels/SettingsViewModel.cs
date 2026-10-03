using System.Collections.ObjectModel;
using System.IO;
using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Interfaces;
using CalendarWidget.Core.Models;
using CalendarWidget.Presentation.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CalendarWidget.Presentation.ViewModels;

/// <summary>
/// ViewModel for the main application settings, appearance, preferences, and data maintenance.
/// </summary>
public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService? _settingsService;
    private readonly IThemeService? _themeService;
    private readonly IWindowsStartupService? _windowsStartupService;
    private readonly IDataManagementService? _dataManagementService;
    private readonly IFileDialogService? _fileDialogService;
    private readonly ICalendarConnectionService? _calendarConnectionService;
    private readonly IMicrosoftCalendarConnectionService? _microsoftCalendarConnectionService;
    private readonly ICalendarSynchronizationService? _calendarSynchronizationService;
    private bool _isInitializing = true;

    [ObservableProperty]
    private string _title = "Settings";

    [ObservableProperty]
    private string _selectedTheme = "Dark";

    [ObservableProperty]
    private bool _alwaysOnTop;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private int _widgetOpacityPercent = 100;

    [ObservableProperty]
    private string _selectedFirstDayOfWeek = "Monday";

    [ObservableProperty]
    private string _selectedTimeFormat = "24-hour";

    [ObservableProperty]
    private string _selectedDateFormat = "System Default";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSuccessMessage))]
    private string? _successMessage;

    /// <summary>
    /// Gets whether a success message is currently displayed.
    /// </summary>
    public bool HasSuccessMessage => !string.IsNullOrEmpty(SuccessMessage);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
    private string? _errorMessage;

    /// <summary>
    /// Gets whether an error message is currently displayed.
    /// </summary>
    public bool HasErrorMessage => !string.IsNullOrEmpty(ErrorMessage);

    [ObservableProperty]
    private bool _isResetConfirmationVisible;

    /// <summary>
    /// Gets available themes.
    /// </summary>
    public IReadOnlyList<string> ThemeOptions { get; } = ["Dark", "Light", "System"];

    /// <summary>
    /// Gets available first-day-of-week options.
    /// </summary>
    public IReadOnlyList<string> FirstDayOfWeekOptions { get; } = ["Monday", "Sunday"];

    /// <summary>
    /// Gets available time format options.
    /// </summary>
    public IReadOnlyList<string> TimeFormatOptions { get; } = ["24-hour", "12-hour"];

    /// <summary>
    /// Gets available short date format options.
    /// </summary>
    public IReadOnlyList<string> DateFormatOptions { get; } =
    [
        "System Default",
        "YYYY-MM-DD",
        "MM/DD/YYYY",
        "DD/MM/YYYY"
    ];

    public ObservableCollection<CalendarAccount> GoogleAccounts { get; } = [];
    public ObservableCollection<CalendarSelectionItemViewModel> GoogleCalendars { get; } = [];
    public ObservableCollection<CalendarAccount> MicrosoftAccounts { get; } = [];
    public ObservableCollection<CalendarSelectionItemViewModel> MicrosoftCalendars { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GoogleConnectionStatus))]
    private CalendarAccount? _selectedGoogleAccount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MicrosoftConnectionStatus))]
    private CalendarAccount? _selectedMicrosoftAccount;

    public string GoogleConnectionStatus => SelectedGoogleAccount is null
        ? "Not connected"
        : SelectedGoogleAccount.IsConnected ? "Connected" : "Disconnected — reconnect to sync";

    public string MicrosoftConnectionStatus => SelectedMicrosoftAccount is null
        ? "Not connected"
        : SelectedMicrosoftAccount.IsConnected ? "Connected" : "Disconnected — reconnect to sync";

    [RelayCommand]
    public async Task LoadCalendarSettingsAsync()
    {
        await LoadGoogleAccountsAsync();
        if (SelectedGoogleAccount is not null)
            await DiscoverGoogleCalendarsAsync();
        await LoadMicrosoftAccountsAsync();
        if (SelectedMicrosoftAccount is not null)
            await DiscoverMicrosoftCalendarsAsync();
    }

    [RelayCommand]
    public async Task DisconnectGoogleAccountAsync()
    {
        if (_calendarConnectionService is null || SelectedGoogleAccount is null)
            return;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            await _calendarConnectionService.DisconnectGoogleAccountAsync(SelectedGoogleAccount.Id);
            await LoadGoogleAccountsAsync();
            await DiscoverGoogleCalendarsAsync();
            SuccessMessage = "Google account disconnected. Its events remain available locally.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not disconnect Google account: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task DisconnectMicrosoftAccountAsync()
    {
        if (_microsoftCalendarConnectionService is null || SelectedMicrosoftAccount is null)
            return;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            await _microsoftCalendarConnectionService.DisconnectMicrosoftAccountAsync(SelectedMicrosoftAccount.Id);
            await LoadMicrosoftAccountsAsync();
            await DiscoverMicrosoftCalendarsAsync();
            SuccessMessage = "Microsoft account disconnected. Its events remain available locally.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not disconnect Microsoft account: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SyncNowAsync()
    {
        if (_calendarSynchronizationService is null)
        {
            ErrorMessage = "Calendar synchronization is unavailable.";
            return;
        }
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            CalendarSynchronizationBatchResult result = await _calendarSynchronizationService.SynchronizeEnabledCalendarsAsync();
            if (result.Calendars.Count == 0)
            {
                SuccessMessage = "No enabled external calendars to synchronize. Local Calendar is available offline.";
                return;
            }
            if (!result.Succeeded)
            {
                string? detail = result.Calendars.SelectMany(item => item.Result.Failures)
                    .Select(failure => failure.Error).FirstOrDefault();
                ErrorMessage = $"Sync finished with {result.Failed} failure(s). {detail}";
                return;
            }
            SuccessMessage = $"Sync complete: {result.Created} created/imported, {result.Updated} updated, " +
                $"{result.Deleted} deleted, {result.Skipped} skipped, {result.Conflicts} conflicts.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Calendar sync failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class for design-time use.
    /// </summary>
    public SettingsViewModel()
    {
        _isInitializing = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class with dependencies.
    /// </summary>
    public SettingsViewModel(
        ISettingsService settingsService,
        IThemeService themeService,
        IWindowsStartupService windowsStartupService,
        IDataManagementService dataManagementService,
        IFileDialogService fileDialogService)
        : this(settingsService, themeService, windowsStartupService, dataManagementService, fileDialogService, null, null)
    {
    }

    public SettingsViewModel(
        ISettingsService settingsService,
        IThemeService themeService,
        IWindowsStartupService windowsStartupService,
        IDataManagementService dataManagementService,
        IFileDialogService fileDialogService,
        ICalendarConnectionService? calendarConnectionService,
        ICalendarSynchronizationService? calendarSynchronizationService,
        IMicrosoftCalendarConnectionService? microsoftCalendarConnectionService = null)
    {
        _settingsService = settingsService;
        _themeService = themeService;
        _windowsStartupService = windowsStartupService;
        _dataManagementService = dataManagementService;
        _fileDialogService = fileDialogService;
        _calendarConnectionService = calendarConnectionService;
        _calendarSynchronizationService = calendarSynchronizationService;
        _microsoftCalendarConnectionService = microsoftCalendarConnectionService;

        LoadCurrentSettings();
        _isInitializing = false;
    }

    [RelayCommand]
    public async Task ConnectGoogleAccountAsync()
    {
        if (_calendarConnectionService is null)
            return;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            SelectedGoogleAccount = await _calendarConnectionService.ConnectGoogleAccountAsync();
            await LoadGoogleAccountsAsync();
            await DiscoverGoogleCalendarsAsync();
            SuccessMessage = "Google Calendar account connected. Select calendars and save to enable synchronization.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Google account connection failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task LoadGoogleAccountsAsync()
    {
        if (_calendarConnectionService is null)
            return;
        try
        {
            Guid? selectedId = SelectedGoogleAccount?.Id;
            IReadOnlyList<CalendarAccount> accounts = await _calendarConnectionService.GetGoogleAccountsAsync();
            GoogleAccounts.Clear();
            foreach (CalendarAccount account in accounts)
                GoogleAccounts.Add(account);
            SelectedGoogleAccount = GoogleAccounts.FirstOrDefault(account => account.Id == selectedId) ?? GoogleAccounts.FirstOrDefault();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not load Google accounts: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task DiscoverGoogleCalendarsAsync()
    {
        if (_calendarConnectionService is null || SelectedGoogleAccount is null)
            return;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            IReadOnlyList<Calendar> calendars = await _calendarConnectionService.DiscoverGoogleCalendarsAsync(SelectedGoogleAccount.Id);
            GoogleCalendars.Clear();
            foreach (Calendar calendar in calendars)
            {
                GoogleCalendars.Add(new CalendarSelectionItemViewModel
                {
                    CalendarId = calendar.Id,
                    AccountName = SelectedGoogleAccount.DisplayName,
                    CalendarName = calendar.Name,
                    Provider = CalendarProvider.Google,
                    IsAccountConnected = SelectedGoogleAccount.IsConnected,
                    IsEnabled = calendar.IsEnabled,
                });
            }
            SuccessMessage = $"Found {GoogleCalendars.Count} Google calendars.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not discover Google calendars: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SaveGoogleCalendarSelectionAsync()
    {
        if (_calendarConnectionService is null)
            return;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            foreach (CalendarSelectionItemViewModel calendar in GoogleCalendars)
                await _calendarConnectionService.SetCalendarEnabledAsync(calendar.CalendarId, calendar.IsEnabled);
            SuccessMessage = "Google calendar selection saved.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not save Google calendar selection: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SynchronizeGoogleCalendarsAsync()
    {
        if (_calendarSynchronizationService is null)
            return;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            int imported = 0;
            int updated = 0;
            int deleted = 0;
            int failed = 0;
            string? firstFailure = null;
            foreach (CalendarSelectionItemViewModel calendar in GoogleCalendars.Where(item => item.IsEnabled))
            {
                CalendarSynchronizationResult result = await _calendarSynchronizationService.SynchronizeCalendarAsync(calendar.CalendarId);
                imported += result.EventsImported;
                updated += result.EventsUpdated;
                deleted += result.Deleted;
                failed += result.Failed;
                if (firstFailure is null && result.Failures.Count > 0)
                    firstFailure = result.Failures[0].Error;
            }
            if (failed > 0)
            {
                ErrorMessage = $"Google Calendar sync completed with {failed} failed operation(s). " +
                    $"First failure: {firstFailure ?? "See synchronization details."} Local changes were retained where the provider write did not complete.";
            }
            else
            {
                SuccessMessage = $"Google Calendar sync complete: {imported} created/imported, {updated} updated, {deleted} deleted.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Google Calendar sync failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ConnectMicrosoftAccountAsync()
    {
        if (_microsoftCalendarConnectionService is null)
            return;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            SelectedMicrosoftAccount = await _microsoftCalendarConnectionService.ConnectMicrosoftAccountAsync();
            await LoadMicrosoftAccountsAsync();
            await DiscoverMicrosoftCalendarsAsync();
            SuccessMessage = "Microsoft Calendar account connected. Select calendars and save to enable synchronization.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Microsoft account connection failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task LoadMicrosoftAccountsAsync()
    {
        if (_microsoftCalendarConnectionService is null)
            return;
        try
        {
            Guid? selectedId = SelectedMicrosoftAccount?.Id;
            IReadOnlyList<CalendarAccount> accounts = await _microsoftCalendarConnectionService.GetMicrosoftAccountsAsync();
            MicrosoftAccounts.Clear();
            foreach (CalendarAccount account in accounts)
                MicrosoftAccounts.Add(account);
            SelectedMicrosoftAccount = MicrosoftAccounts.FirstOrDefault(account => account.Id == selectedId)
                ?? MicrosoftAccounts.FirstOrDefault();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not load Microsoft accounts: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task DiscoverMicrosoftCalendarsAsync()
    {
        if (_microsoftCalendarConnectionService is null || SelectedMicrosoftAccount is null)
            return;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            IReadOnlyList<Calendar> calendars = await _microsoftCalendarConnectionService
                .DiscoverMicrosoftCalendarsAsync(SelectedMicrosoftAccount.Id);
            MicrosoftCalendars.Clear();
            foreach (Calendar calendar in calendars)
            {
                MicrosoftCalendars.Add(new CalendarSelectionItemViewModel
                {
                    CalendarId = calendar.Id,
                    AccountName = SelectedMicrosoftAccount.DisplayName,
                    CalendarName = calendar.Name,
                    Provider = CalendarProvider.Microsoft,
                    IsAccountConnected = SelectedMicrosoftAccount.IsConnected,
                    IsEnabled = calendar.IsEnabled,
                });
            }
            SuccessMessage = $"Found {MicrosoftCalendars.Count} Microsoft calendars.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not discover Microsoft calendars: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SaveMicrosoftCalendarSelectionAsync()
    {
        if (_microsoftCalendarConnectionService is null)
            return;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            foreach (CalendarSelectionItemViewModel calendar in MicrosoftCalendars)
                await _microsoftCalendarConnectionService.SetMicrosoftCalendarEnabledAsync(calendar.CalendarId, calendar.IsEnabled);
            SuccessMessage = "Microsoft calendar selection saved.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not save Microsoft calendar selection: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SynchronizeMicrosoftCalendarsAsync()
    {
        if (_calendarSynchronizationService is null)
            return;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            int created = 0;
            int updated = 0;
            int deleted = 0;
            int failed = 0;
            string? firstFailure = null;
            foreach (CalendarSelectionItemViewModel calendar in MicrosoftCalendars.Where(item => item.IsEnabled))
            {
                CalendarSynchronizationResult result = await _calendarSynchronizationService
                    .SynchronizeCalendarAsync(calendar.CalendarId);
                created += result.Created;
                updated += result.Updated;
                deleted += result.Deleted;
                failed += result.Failed;
                if (firstFailure is null && result.Failures.Count > 0)
                    firstFailure = result.Failures[0].Error;
            }
            if (failed > 0)
            {
                ErrorMessage = $"Microsoft Calendar sync completed with {failed} failed operation(s). " +
                    $"First failure: {firstFailure ?? "See synchronization details."}";
            }
            else
            {
                SuccessMessage = $"Microsoft Calendar sync complete: {created} created/imported, {updated} updated, {deleted} deleted.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Microsoft Calendar sync failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadCurrentSettings()
    {
        if (_settingsService is null)
        {
            return;
        }

        UserSettings current = _settingsService.CurrentSettings;
        lock (_saveLock)
        {
            _currentSettingsState = current.Clone();
        }

        SelectedTheme = current.Theme switch
        {
            AppThemeMode.Light => "Light",
            AppThemeMode.System => "System",
            _ => "Dark",
        };

        AlwaysOnTop = current.AlwaysOnTop;
        WidgetOpacityPercent = (int)Math.Round(current.WidgetOpacity * 100);

        SelectedFirstDayOfWeek = current.FirstDayOfWeek == DayOfWeek.Sunday ? "Sunday" : "Monday";

        SelectedTimeFormat = current.TimeFormat == TimeFormatOption.TwelveHour ? "12-hour" : "24-hour";

        SelectedDateFormat = current.DateFormat switch
        {
            "yyyy-MM-dd" => "YYYY-MM-DD",
            "MM/dd/yyyy" => "MM/DD/YYYY",
            "dd/MM/yyyy" => "DD/MM/YYYY",
            _ => "System Default",
        };

        if (_windowsStartupService is not null)
        {
            StartWithWindows = _windowsStartupService.IsStartupEnabled();
        }
        else
        {
            StartWithWindows = current.StartWithWindows;
        }
    }

    partial void OnSelectedThemeChanged(string value)
    {
        if (_isInitializing || _settingsService is null)
        {
            return;
        }

        AppThemeMode mode = value switch
        {
            "Light" => AppThemeMode.Light,
            "System" => AppThemeMode.System,
            _ => AppThemeMode.Dark,
        };

        _themeService?.ApplyTheme(mode);
        PersistSettingsChange(s => s.Theme = mode);
    }

    partial void OnAlwaysOnTopChanged(bool value)
    {
        if (_isInitializing || _settingsService is null)
        {
            return;
        }

        PersistSettingsChange(s => s.AlwaysOnTop = value);
    }

    partial void OnWidgetOpacityPercentChanged(int value)
    {
        if (_isInitializing || _settingsService is null)
        {
            return;
        }

        int clamped = Math.Clamp(value, 50, 100);
        double opacity = clamped / 100.0;
        PersistSettingsChange(s => s.WidgetOpacity = opacity);
    }

    partial void OnSelectedFirstDayOfWeekChanged(string value)
    {
        if (_isInitializing || _settingsService is null)
        {
            return;
        }

        DayOfWeek day = value == "Sunday" ? DayOfWeek.Sunday : DayOfWeek.Monday;
        PersistSettingsChange(s => s.FirstDayOfWeek = day);
    }

    partial void OnSelectedTimeFormatChanged(string value)
    {
        if (_isInitializing || _settingsService is null)
        {
            return;
        }

        TimeFormatOption option = value.StartsWith("12", StringComparison.OrdinalIgnoreCase)
            ? TimeFormatOption.TwelveHour
            : TimeFormatOption.TwentyFourHour;

        PersistSettingsChange(s => s.TimeFormat = option);
    }

    partial void OnSelectedDateFormatChanged(string value)
    {
        if (_isInitializing || _settingsService is null)
        {
            return;
        }

        string rawFormat = value switch
        {
            "YYYY-MM-DD" => "yyyy-MM-dd",
            "MM/DD/YYYY" => "MM/dd/yyyy",
            "DD/MM/YYYY" => "dd/MM/yyyy",
            _ => "Default",
        };

        PersistSettingsChange(s => s.DateFormat = rawFormat);
    }

    partial void OnStartWithWindowsChanged(bool value)
    {
        if (_isInitializing || _settingsService is null)
        {
            return;
        }

        if (_windowsStartupService is not null)
        {
            bool success = _windowsStartupService.SetStartup(value);
            if (!success)
            {
                ErrorMessage = "Failed to update Windows startup configuration.";
                // Revert without retriggering
                _isInitializing = true;
                StartWithWindows = !value;
                _isInitializing = false;
                return;
            }
        }

        PersistSettingsChange(s => s.StartWithWindows = value);
    }

    private readonly Lock _saveLock = new();
    private UserSettings _currentSettingsState = new();
    private UserSettings? _pendingSaveSnapshot;
    private Task? _saveWorkerTask;

    /// <summary>
    /// Gets the current in-flight save task, if any (for testing and synchronization).
    /// </summary>
    internal Task? ActiveSaveTask
    {
        get
        {
            lock (_saveLock)
            {
                return _saveWorkerTask;
            }
        }
    }

    /// <summary>
    /// Waits for all pending asynchronous saves to complete.
    /// </summary>
    internal async Task WaitForPendingSavesAsync()
    {
        while (true)
        {
            Task? task;
            lock (_saveLock)
            {
                task = _saveWorkerTask;
            }

            if (task is null || task.IsCompleted)
            {
                break;
            }

            await task.ConfigureAwait(false);
        }
    }

    private void PersistSettingsChange(Action<UserSettings> updateAction)
    {
        if (_settingsService is null)
        {
            return;
        }

        lock (_saveLock)
        {
            updateAction(_currentSettingsState);
            _pendingSaveSnapshot = _currentSettingsState.Clone();

            if (_saveWorkerTask is null || _saveWorkerTask.IsCompleted)
            {
                _saveWorkerTask = ProcessPendingSavesAsync();
            }
        }
    }

    private async Task ProcessPendingSavesAsync()
    {
        if (_settingsService is null)
        {
            return;
        }

        while (true)
        {
            UserSettings snapshot;
            lock (_saveLock)
            {
                if (_pendingSaveSnapshot is null)
                {
                    _saveWorkerTask = null;
                    return;
                }

                snapshot = _pendingSaveSnapshot;
                _pendingSaveSnapshot = null;
            }

            IsSaving = true;
            try
            {
                await _settingsService.SaveSettingsAsync(snapshot);
                if (ErrorMessage?.StartsWith("Failed to save settings", StringComparison.Ordinal) == true)
                {
                    ErrorMessage = null;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to save settings: {ex.Message}";
            }
            finally
            {
                IsSaving = false;
            }
        }
    }

    /// <summary>
    /// Exports application data to a backup JSON file chosen by the user.
    /// </summary>
    [RelayCommand]
    public async Task ExportDataAsync()
    {
        if (_fileDialogService is null || _dataManagementService is null)
        {
            return;
        }

        string defaultFileName = $"desktop-calendar-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        string? targetPath = _fileDialogService.ShowSaveFileDialog(
            "Export Application Backup",
            defaultFileName,
            "JSON Backup (*.json)|*.json|All Files (*.*)|*.*");

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            string json = await _dataManagementService.ExportDataJsonAsync();
            await File.WriteAllTextAsync(targetPath, json);
            SuccessMessage = $"Data exported successfully to {Path.GetFileName(targetPath)}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Restores application data from a backup JSON file selected by the user.
    /// </summary>
    [RelayCommand]
    public async Task ImportDataAsync()
    {
        if (_fileDialogService is null || _dataManagementService is null)
        {
            return;
        }

        string? sourcePath = _fileDialogService.ShowOpenFileDialog(
            "Import Application Backup",
            "JSON Backup (*.json)|*.json|All Files (*.*)|*.*");

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            string json = await File.ReadAllTextAsync(sourcePath);
            DataImportResult result = await _dataManagementService.ImportDataJsonAsync(json);

            if (result.Success)
            {
                SuccessMessage = $"Data imported successfully: {result.EventsImported} events ({result.EventsSkipped} skipped), " +
                    $"{result.NotesImported} notes ({result.NotesSkipped} skipped), " +
                    $"{result.CalendarAccountsImported} accounts ({result.CalendarAccountsSkipped} skipped), " +
                    $"{result.CalendarsImported} calendars ({result.CalendarsSkipped} skipped), " +
                    $"{result.EventMappingsImported} event mappings ({result.EventMappingsSkipped} skipped), " +
                    $"{result.SyncStatesImported} sync states ({result.SyncStatesSkipped} skipped).";
                LoadCurrentSettings();
            }
            else
            {
                ErrorMessage = $"Import failed: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Import error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Displays the reset database confirmation dialog.
    /// </summary>
    [RelayCommand]
    public void RequestResetData()
    {
        IsResetConfirmationVisible = true;
    }

    /// <summary>
    /// Dismisses the reset database confirmation dialog.
    /// </summary>
    [RelayCommand]
    public void CancelResetData()
    {
        IsResetConfirmationVisible = false;
    }

    /// <summary>
    /// Confirms and performs the database factory reset.
    /// </summary>
    [RelayCommand]
    public async Task ConfirmResetDataAsync()
    {
        if (_dataManagementService is null)
        {
            IsResetConfirmationVisible = false;
            return;
        }

        IsResetConfirmationVisible = false;
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            await _dataManagementService.ResetAllDataAsync();
            SuccessMessage = "All calendar events and notes have been reset.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Reset failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Dismisses the success feedback message banner.
    /// </summary>
    [RelayCommand]
    public void DismissSuccessMessage()
    {
        SuccessMessage = null;
    }

    /// <summary>
    /// Dismisses the error feedback message banner.
    /// </summary>
    [RelayCommand]
    public void DismissErrorMessage()
    {
        ErrorMessage = null;
    }
}
