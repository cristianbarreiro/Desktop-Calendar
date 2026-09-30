using CalendarWidget.App.Services;
using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Infrastructure.Services;
using CalendarWidget.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CalendarWidget.IntegrationTests.Windows;

public sealed class SettingsAndPlacementConcurrencyIntegrationTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _settingsFilePath;

    public SettingsAndPlacementConcurrencyIntegrationTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"cw_concurrency_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
        _settingsFilePath = Path.Combine(_tempDirectory, "settings.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch
            {
                // Best-effort cleanup
            }
        }
    }

    [Fact]
    public async Task ConcurrentSettingsAndPlacementSaves_DoNotCorruptState()
    {
        FileSettingsRepository repo = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        using SettingsService settingsService = new(repo, NullLogger<SettingsService>.Instance);
        await settingsService.InitializeAsync();

        TestDisplayMonitorProvider monitorProvider = new();
        using WindowPlacementService placementService = new(
            settingsService,
            monitorProvider,
            debounceDelay: TimeSpan.FromMilliseconds(20));

        // Act: Concurrently run rapid settings updates and window moves
        List<Task> tasks = [];

        // Task A: Updates user settings (theme, opacity, etc.)
        tasks.Add(Task.Run(async () =>
        {
            for (int i = 0; i < 15; i++)
            {
                UserSettings updated = settingsService.CurrentSettings.Clone();
                updated.Theme = (i % 2 == 0) ? AppThemeMode.Dark : AppThemeMode.Light;
                updated.WidgetOpacity = 0.5 + (i * 0.03);
                await settingsService.SaveSettingsAsync(updated);
                await Task.Delay(5);
            }
        }));

        // Task B: Rapid window bounds changes
        tasks.Add(Task.Run(async () =>
        {
            for (int i = 0; i < 20; i++)
            {
                placementService.OnMainWindowBoundsChanged(100 + i, 120 + i, 800 + i, 600 + i);
                placementService.OnWidgetWindowBoundsChanged(50 + i, 60 + i);
                await Task.Delay(4);
            }
        }));

        await Task.WhenAll(tasks);
        await placementService.FlushPendingSaveAsync();

        // Assert: Read back from fresh repo instance
        FileSettingsRepository verifyRepo = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        UserSettings finalSettings = await verifyRepo.LoadSettingsAsync();

        // Verification: File contains valid, non-corrupted data combining both streams of updates
        finalSettings.Should().NotBeNull();
        finalSettings.MainWindowLeft.Should().Be(119);
        finalSettings.MainWindowTop.Should().Be(139);
        finalSettings.WidgetWindowLeft.Should().Be(69);
        finalSettings.WidgetWindowTop.Should().Be(79);
        finalSettings.WidgetOpacity.Should().BeInRange(0.5, 1.0);
    }

    [Fact]
    public async Task WindowMovement_WhileSettingsSavePending_BothPersistCorrectly()
    {
        FileSettingsRepository repo = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        using SettingsService settingsService = new(repo, NullLogger<SettingsService>.Instance);
        await settingsService.InitializeAsync();

        TestDisplayMonitorProvider monitorProvider = new();
        using WindowPlacementService placementService = new(
            settingsService,
            monitorProvider,
            debounceDelay: TimeSpan.FromMilliseconds(50));

        // Start saving user settings
        UserSettings updated = settingsService.CurrentSettings.Clone();
        updated.DateFormat = "dd/MM/yyyy";
        updated.TimeFormat = TimeFormatOption.TwelveHour;

        Task saveSettingsTask = settingsService.SaveSettingsAsync(updated);

        // Immediately move window while settings save is in-flight
        placementService.OnMainWindowBoundsChanged(333, 444, 950, 650);

        await saveSettingsTask;
        await placementService.FlushPendingSaveAsync();

        // Verify loaded state
        FileSettingsRepository verifyRepo = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        UserSettings reloaded = await verifyRepo.LoadSettingsAsync();

        reloaded.DateFormat.Should().Be("dd/MM/yyyy");
        reloaded.TimeFormat.Should().Be(TimeFormatOption.TwelveHour);
        reloaded.MainWindowLeft.Should().Be(333);
        reloaded.MainWindowTop.Should().Be(444);
        reloaded.MainWindowWidth.Should().Be(950);
        reloaded.MainWindowHeight.Should().Be(650);
    }

    [Fact]
    public async Task Shutdown_WithPendingPlacementSave_FlushesCleanly()
    {
        FileSettingsRepository repo = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        using SettingsService settingsService = new(repo, NullLogger<SettingsService>.Instance);
        await settingsService.InitializeAsync();

        TestDisplayMonitorProvider monitorProvider = new();
        using WindowPlacementService placementService = new(
            settingsService,
            monitorProvider,
            debounceDelay: TimeSpan.FromSeconds(10)); // Long debounce to ensure it is pending

        placementService.OnMainWindowBoundsChanged(777, 888, 1000, 700);

        // Simulate shutdown flush
        await placementService.FlushPendingSaveAsync();

        // Reload
        FileSettingsRepository verifyRepo = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        UserSettings reloaded = await verifyRepo.LoadSettingsAsync();

        reloaded.MainWindowLeft.Should().Be(777);
        reloaded.MainWindowTop.Should().Be(888);
        reloaded.MainWindowWidth.Should().Be(1000);
        reloaded.MainWindowHeight.Should().Be(700);
    }
}
