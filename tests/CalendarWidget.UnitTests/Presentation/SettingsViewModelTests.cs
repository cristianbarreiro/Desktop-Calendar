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
    public void SelectedTheme_Changed_AppliesThemeAndPersists(string themeName, AppThemeMode expectedMode)
    {
        _settingsService.CurrentSettings.Theme = expectedMode == AppThemeMode.Dark ? AppThemeMode.Light : AppThemeMode.Dark;
        SettingsViewModel vm = CreateViewModel();

        vm.SelectedTheme = themeName;

        _themeService.ActiveTheme.Should().Be(expectedMode);
        _themeService.ApplyCount.Should().BeGreaterThan(0);
        _settingsService.CurrentSettings.Theme.Should().Be(expectedMode);
        _settingsService.SaveCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void AlwaysOnTop_Changed_PersistsSetting()
    {
        SettingsViewModel vm = CreateViewModel();

        vm.AlwaysOnTop = true;

        _settingsService.CurrentSettings.AlwaysOnTop.Should().BeTrue();
        _settingsService.SaveCount.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData(85, 0.85)]
    [InlineData(50, 0.5)]
    [InlineData(70, 0.7)]
    public void WidgetOpacityPercent_Changed_PersistsClampedSetting(int inputPercent, double expectedOpacity)
    {
        SettingsViewModel vm = CreateViewModel();

        vm.WidgetOpacityPercent = inputPercent;

        _settingsService.CurrentSettings.WidgetOpacity.Should().Be(expectedOpacity);
        _settingsService.SaveCount.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("Monday", DayOfWeek.Monday)]
    [InlineData("Sunday", DayOfWeek.Sunday)]
    public void SelectedFirstDayOfWeek_Changed_PersistsSetting(string inputDay, DayOfWeek expectedDay)
    {
        _settingsService.CurrentSettings.FirstDayOfWeek = expectedDay == DayOfWeek.Monday ? DayOfWeek.Sunday : DayOfWeek.Monday;
        SettingsViewModel vm = CreateViewModel();

        vm.SelectedFirstDayOfWeek = inputDay;

        _settingsService.CurrentSettings.FirstDayOfWeek.Should().Be(expectedDay);
        _settingsService.SaveCount.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("12-hour", TimeFormatOption.TwelveHour)]
    [InlineData("24-hour", TimeFormatOption.TwentyFourHour)]
    public void SelectedTimeFormat_Changed_PersistsSetting(string inputFormat, TimeFormatOption expectedOption)
    {
        _settingsService.CurrentSettings.TimeFormat = expectedOption == TimeFormatOption.TwentyFourHour ? TimeFormatOption.TwelveHour : TimeFormatOption.TwentyFourHour;
        SettingsViewModel vm = CreateViewModel();

        vm.SelectedTimeFormat = inputFormat;

        _settingsService.CurrentSettings.TimeFormat.Should().Be(expectedOption);
        _settingsService.SaveCount.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("YYYY-MM-DD", "yyyy-MM-dd")]
    [InlineData("MM/DD/YYYY", "MM/dd/yyyy")]
    [InlineData("DD/MM/YYYY", "dd/MM/yyyy")]
    [InlineData("System Default", "Default")]
    public void SelectedDateFormat_Changed_PersistsSetting(string inputFormat, string expectedFormat)
    {
        _settingsService.CurrentSettings.DateFormat = expectedFormat == "Default" ? "yyyy-MM-dd" : "Default";
        SettingsViewModel vm = CreateViewModel();

        vm.SelectedDateFormat = inputFormat;

        _settingsService.CurrentSettings.DateFormat.Should().Be(expectedFormat);
        _settingsService.SaveCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void StartWithWindows_WhenSuccessful_ConfiguresServiceAndPersists()
    {
        SettingsViewModel vm = CreateViewModel();
        _startupService.ReturnSuccessOnSet = true;

        vm.StartWithWindows = true;

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
    public void PersistSettings_WhenSaveFails_SetsErrorMessage()
    {
        SettingsViewModel vm = CreateViewModel();
        _settingsService.ThrowOnSave = true;

        vm.AlwaysOnTop = true;

        vm.HasErrorMessage.Should().BeTrue();
        vm.ErrorMessage.Should().Contain("Failed to save settings");
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
