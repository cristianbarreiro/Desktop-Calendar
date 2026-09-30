using System.IO;
using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Models;
using CalendarWidget.Presentation.ViewModels;
using CalendarWidget.UnitTests.Fakes;
using FluentAssertions;

namespace CalendarWidget.UnitTests.Presentation;

/// <summary>
/// Unit tests for <see cref="SettingsViewModel"/> user preferences, runtime updates, and data maintenance.
/// </summary>
public sealed class SettingsViewModelTests
{
    private readonly TestSettingsService _settingsService;
    private readonly TestThemeService _themeService;
    private readonly TestWindowsStartupService _startupService;
    private readonly TestDataManagementService _dataManagementService;
    private readonly TestFileDialogService _fileDialogService;

    public SettingsViewModelTests()
    {
        _settingsService = new TestSettingsService();
        _themeService = new TestThemeService();
        _startupService = new TestWindowsStartupService();
        _dataManagementService = new TestDataManagementService();
        _fileDialogService = new TestFileDialogService();
    }

    private SettingsViewModel CreateViewModel()
    {
        return new SettingsViewModel(
            _settingsService,
            _themeService,
            _startupService,
            _dataManagementService,
            _fileDialogService);
    }

    [Fact]
    public void Constructor_InitializesPropertiesFromCurrentSettings()
    {
        _settingsService.CurrentSettings.Theme = AppThemeMode.Light;
        _settingsService.CurrentSettings.AlwaysOnTop = true;
        _settingsService.CurrentSettings.WidgetOpacity = 0.75;
        _settingsService.CurrentSettings.FirstDayOfWeek = DayOfWeek.Sunday;
        _settingsService.CurrentSettings.TimeFormat = TimeFormatOption.TwelveHour;
        _settingsService.CurrentSettings.DateFormat = "yyyy-MM-dd";
        _startupService.IsStartupEnabledValue = true;

        SettingsViewModel vm = CreateViewModel();

        vm.SelectedTheme.Should().Be("Light");
        vm.AlwaysOnTop.Should().BeTrue();
        vm.WidgetOpacityPercent.Should().Be(75);
        vm.SelectedFirstDayOfWeek.Should().Be("Sunday");
        vm.SelectedTimeFormat.Should().Be("12-hour");
        vm.SelectedDateFormat.Should().Be("YYYY-MM-DD");
        vm.StartWithWindows.Should().BeTrue();
    }

    [Theory]
    [InlineData("Light", AppThemeMode.Light)]
    [InlineData("Dark", AppThemeMode.Dark)]
    [InlineData("System", AppThemeMode.System)]
    public async Task SelectedTheme_Changed_AppliesThemeAndPersists(string themeName, AppThemeMode expectedMode)
    {
        _settingsService.CurrentSettings.Theme = expectedMode == AppThemeMode.Dark ? AppThemeMode.Light : AppThemeMode.Dark;
        SettingsViewModel vm = CreateViewModel();

        vm.SelectedTheme = themeName;
        await vm.WaitForPendingSavesAsync();

        _themeService.ActiveTheme.Should().Be(expectedMode);
        _themeService.ApplyCount.Should().BeGreaterThan(0);
        _settingsService.CurrentSettings.Theme.Should().Be(expectedMode);
        _settingsService.SaveCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AlwaysOnTop_Changed_PersistsSetting()
    {
        SettingsViewModel vm = CreateViewModel();

        vm.AlwaysOnTop = true;
        await vm.WaitForPendingSavesAsync();

        _settingsService.CurrentSettings.AlwaysOnTop.Should().BeTrue();
        _settingsService.SaveCount.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData(85, 0.85)]
    [InlineData(50, 0.5)]
    [InlineData(70, 0.7)]
    public async Task WidgetOpacityPercent_Changed_PersistsClampedSetting(int inputPercent, double expectedOpacity)
    {
        SettingsViewModel vm = CreateViewModel();

        vm.WidgetOpacityPercent = inputPercent;
        await vm.WaitForPendingSavesAsync();

        _settingsService.CurrentSettings.WidgetOpacity.Should().Be(expectedOpacity);
        _settingsService.SaveCount.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("Monday", DayOfWeek.Monday)]
    [InlineData("Sunday", DayOfWeek.Sunday)]
    public async Task SelectedFirstDayOfWeek_Changed_PersistsSetting(string inputDay, DayOfWeek expectedDay)
    {
        _settingsService.CurrentSettings.FirstDayOfWeek = expectedDay == DayOfWeek.Monday ? DayOfWeek.Sunday : DayOfWeek.Monday;
        SettingsViewModel vm = CreateViewModel();

        vm.SelectedFirstDayOfWeek = inputDay;
        await vm.WaitForPendingSavesAsync();

        _settingsService.CurrentSettings.FirstDayOfWeek.Should().Be(expectedDay);
        _settingsService.SaveCount.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("12-hour", TimeFormatOption.TwelveHour)]
    [InlineData("24-hour", TimeFormatOption.TwentyFourHour)]
    public async Task SelectedTimeFormat_Changed_PersistsSetting(string inputFormat, TimeFormatOption expectedOption)
    {
        _settingsService.CurrentSettings.TimeFormat = expectedOption == TimeFormatOption.TwentyFourHour ? TimeFormatOption.TwelveHour : TimeFormatOption.TwentyFourHour;
        SettingsViewModel vm = CreateViewModel();

        vm.SelectedTimeFormat = inputFormat;
        await vm.WaitForPendingSavesAsync();

        _settingsService.CurrentSettings.TimeFormat.Should().Be(expectedOption);
        _settingsService.SaveCount.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("YYYY-MM-DD", "yyyy-MM-dd")]
    [InlineData("MM/DD/YYYY", "MM/dd/yyyy")]
    [InlineData("DD/MM/YYYY", "dd/MM/yyyy")]
    [InlineData("System Default", "Default")]
    public async Task SelectedDateFormat_Changed_PersistsSetting(string inputFormat, string expectedFormat)
    {
        _settingsService.CurrentSettings.DateFormat = expectedFormat == "Default" ? "yyyy-MM-dd" : "Default";
        SettingsViewModel vm = CreateViewModel();

        vm.SelectedDateFormat = inputFormat;
        await vm.WaitForPendingSavesAsync();

        _settingsService.CurrentSettings.DateFormat.Should().Be(expectedFormat);
        _settingsService.SaveCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task StartWithWindows_WhenSuccessful_ConfiguresServiceAndPersists()
    {
        SettingsViewModel vm = CreateViewModel();
        _startupService.ReturnSuccessOnSet = true;

        vm.StartWithWindows = true;
        await vm.WaitForPendingSavesAsync();

        _startupService.SetCallCount.Should().Be(1);
        _startupService.IsStartupEnabledValue.Should().BeTrue();
        _settingsService.CurrentSettings.StartWithWindows.Should().BeTrue();
    }

    [Fact]
    public void StartWithWindows_WhenFails_RevertsValueAndSetsErrorMessage()
    {
        SettingsViewModel vm = CreateViewModel();
        _startupService.ReturnSuccessOnSet = false;

        vm.StartWithWindows = true;

        vm.StartWithWindows.Should().BeFalse();
        vm.HasErrorMessage.Should().BeTrue();
        vm.ErrorMessage.Should().Contain("Windows startup");
    }

    [Fact]
    public async Task PersistSettings_WhenSaveFails_SetsErrorMessage()
    {
        SettingsViewModel vm = CreateViewModel();
        _settingsService.ThrowOnSave = true;

        vm.AlwaysOnTop = true;
        await vm.WaitForPendingSavesAsync();

        vm.HasErrorMessage.Should().BeTrue();
        vm.ErrorMessage.Should().Contain("Failed to save settings");
    }

    [Fact]
    public async Task RapidSequentialChanges_CoalescesAndPersistsLatestState()
    {
        SettingsViewModel vm = CreateViewModel();

        // Perform multiple rapid sequential modifications
        vm.AlwaysOnTop = true;
        vm.WidgetOpacityPercent = 50;
        vm.SelectedTheme = "Light";
        vm.SelectedFirstDayOfWeek = "Sunday";
        vm.WidgetOpacityPercent = 90;
        vm.SelectedTheme = "Dark";

        await vm.WaitForPendingSavesAsync();

        _settingsService.CurrentSettings.AlwaysOnTop.Should().BeTrue();
        _settingsService.CurrentSettings.WidgetOpacity.Should().Be(0.9);
        _settingsService.CurrentSettings.Theme.Should().Be(AppThemeMode.Dark);
        _settingsService.CurrentSettings.FirstDayOfWeek.Should().Be(DayOfWeek.Sunday);

        UserSettings finalSaved = _settingsService.SavedHistory.Last();
        finalSaved.AlwaysOnTop.Should().BeTrue();
        finalSaved.WidgetOpacity.Should().Be(0.9);
        finalSaved.Theme.Should().Be(AppThemeMode.Dark);
        finalSaved.FirstDayOfWeek.Should().Be(DayOfWeek.Sunday);
    }

    [Fact]
    public async Task OutOfOrderCompletion_ControlledByFake_AlwaysPersistsLatestState()
    {
        SettingsViewModel vm = CreateViewModel();

        TaskCompletionSource<bool> tcsFirstSaveStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> tcsCanFirstSaveComplete = new(TaskCreationOptions.RunContinuationsAsynchronously);

        bool firstSaveHookExecuted = false;
        _settingsService.OnSaveHook = async _ =>
        {
            if (!firstSaveHookExecuted)
            {
                firstSaveHookExecuted = true;
                tcsFirstSaveStarted.TrySetResult(true);
                await tcsCanFirstSaveComplete.Task;
            }
        };

        // Trigger change A (Theme = Light)
        vm.SelectedTheme = "Light";

        // Wait until save A has entered the save pipeline and paused
        await tcsFirstSaveStarted.Task;

        // While save A is paused, user rapidly triggers change B (Theme = Dark, Opacity = 80%)
        vm.SelectedTheme = "Dark";
        vm.WidgetOpacityPercent = 80;

        // Release save A to complete
        tcsCanFirstSaveComplete.TrySetResult(true);

        // Await worker completion
        await vm.WaitForPendingSavesAsync();

        // The latest state must be persisted without being overwritten by earlier snapshots
        _settingsService.CurrentSettings.Theme.Should().Be(AppThemeMode.Dark);
        _settingsService.CurrentSettings.WidgetOpacity.Should().Be(0.8);
        _settingsService.SavedHistory.Last().Theme.Should().Be(AppThemeMode.Dark);
        _settingsService.SavedHistory.Last().WidgetOpacity.Should().Be(0.8);
    }

    [Fact]
    public async Task SaveFailure_DoesNotCrashUI_AndAllowsNewerValidStateToBePersisted()
    {
        SettingsViewModel vm = CreateViewModel();
        _settingsService.ThrowOnSave = true;

        vm.AlwaysOnTop = true;
        await vm.WaitForPendingSavesAsync();

        vm.HasErrorMessage.Should().BeTrue();
        vm.ErrorMessage.Should().Contain("Failed to save settings");

        // Recover: subsequent valid save succeeds
        _settingsService.ThrowOnSave = false;
        vm.SelectedTheme = "Light";
        await vm.WaitForPendingSavesAsync();

        vm.HasErrorMessage.Should().BeFalse();
        _settingsService.CurrentSettings.Theme.Should().Be(AppThemeMode.Light);
        _settingsService.CurrentSettings.AlwaysOnTop.Should().BeTrue();
    }

    [Fact]
    public async Task ExportDataAsync_WhenDialogCancelled_DoesNothing()
    {
        SettingsViewModel vm = CreateViewModel();
        _fileDialogService.SaveFileDialogResult = null;

        await vm.ExportDataAsync();

        _dataManagementService.ExportCount.Should().Be(0);
        vm.HasSuccessMessage.Should().BeFalse();
    }

    [Fact]
    public async Task ExportDataAsync_WhenSuccessful_WritesFileAndSetsSuccessMessage()
    {
        SettingsViewModel vm = CreateViewModel();
        string tempFile = Path.Combine(Path.GetTempPath(), $"test-export-{Guid.NewGuid():N}.json");
        _fileDialogService.SaveFileDialogResult = tempFile;

        try
        {
            await vm.ExportDataAsync();

            _dataManagementService.ExportCount.Should().Be(1);
            File.Exists(tempFile).Should().BeTrue();
            vm.HasSuccessMessage.Should().BeTrue();
            vm.SuccessMessage.Should().Contain("Data exported successfully");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task ExportDataAsync_WhenExportFails_SetsErrorMessage()
    {
        SettingsViewModel vm = CreateViewModel();
        string tempFile = Path.Combine(Path.GetTempPath(), $"test-export-{Guid.NewGuid():N}.json");
        _fileDialogService.SaveFileDialogResult = tempFile;
        _dataManagementService.ThrowOnExport = true;

        await vm.ExportDataAsync();

        vm.HasErrorMessage.Should().BeTrue();
        vm.ErrorMessage.Should().Contain("Export failed");
    }

    [Fact]
    public async Task ImportDataAsync_WhenDialogCancelled_DoesNothing()
    {
        SettingsViewModel vm = CreateViewModel();
        _fileDialogService.OpenFileDialogResult = null;

        await vm.ImportDataAsync();

        _dataManagementService.ImportCount.Should().Be(0);
    }

    [Fact]
    public async Task ImportDataAsync_WhenSuccessful_ImportsAndSetsSuccessMessage()
    {
        SettingsViewModel vm = CreateViewModel();
        string tempFile = Path.Combine(Path.GetTempPath(), $"test-import-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(tempFile, "{\"version\":1}");
        _fileDialogService.OpenFileDialogResult = tempFile;

        _dataManagementService.ImportResultToReturn = new DataImportResult
        {
            Success = true,
            EventsImported = 3,
            NotesImported = 2,
            EventsSkipped = 1,
            NotesSkipped = 0
        };

        try
        {
            await vm.ImportDataAsync();

            _dataManagementService.ImportCount.Should().Be(1);
            vm.HasSuccessMessage.Should().BeTrue();
            vm.SuccessMessage.Should().Contain("3 events");
            vm.SuccessMessage.Should().Contain("2 notes");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task ImportDataAsync_WhenImportResultUnsuccessful_SetsErrorMessage()
    {
        SettingsViewModel vm = CreateViewModel();
        string tempFile = Path.Combine(Path.GetTempPath(), $"test-import-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(tempFile, "{}");
        _fileDialogService.OpenFileDialogResult = tempFile;

        _dataManagementService.ImportResultToReturn = new DataImportResult
        {
            Success = false,
            ErrorMessage = "Incompatible schema version."
        };

        try
        {
            await vm.ImportDataAsync();

            vm.HasErrorMessage.Should().BeTrue();
            vm.ErrorMessage.Should().Contain("Incompatible schema version");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void ResetWorkflow_RequestCancelConfirm_WorksCorrectly()
    {
        SettingsViewModel vm = CreateViewModel();

        vm.RequestResetData();
        vm.IsResetConfirmationVisible.Should().BeTrue();

        vm.CancelResetData();
        vm.IsResetConfirmationVisible.Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmResetDataAsync_WhenConfirmed_ResetsDataAndSetsSuccessMessage()
    {
        SettingsViewModel vm = CreateViewModel();
        vm.RequestResetData();

        await vm.ConfirmResetDataAsync();

        vm.IsResetConfirmationVisible.Should().BeFalse();
        _dataManagementService.ResetCount.Should().Be(1);
        vm.HasSuccessMessage.Should().BeTrue();
        vm.SuccessMessage.Should().Contain("reset");
    }

    [Fact]
    public async Task ConfirmResetDataAsync_WhenFails_SetsErrorMessage()
    {
        SettingsViewModel vm = CreateViewModel();
        _dataManagementService.ThrowOnReset = true;

        await vm.ConfirmResetDataAsync();

        vm.HasErrorMessage.Should().BeTrue();
        vm.ErrorMessage.Should().Contain("Reset failed");
    }

    [Fact]
    public void DismissMessages_ClearsMessages()
    {
        SettingsViewModel vm = CreateViewModel();
        vm.SuccessMessage = "Test Success";
        vm.ErrorMessage = "Test Error";

        vm.DismissSuccessMessage();
        vm.HasSuccessMessage.Should().BeFalse();

        vm.DismissErrorMessage();
        vm.HasErrorMessage.Should().BeFalse();
    }
}
