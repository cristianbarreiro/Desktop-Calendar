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

    [Fact]
    public async Task FlushPendingSaveAsync_WhenDebounceCallbackStartedSaving_AwaitsActiveSaveDurably()
    {
        FileSettingsRepository repo = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        using SettingsService settingsService = new(repo, NullLogger<SettingsService>.Instance);
        await settingsService.InitializeAsync();

        TestDisplayMonitorProvider monitorProvider = new();
        TaskCompletionSource<bool> saveStartedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> allowSaveToCompleteTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        using WindowPlacementService placementService = new(
            settingsService,
            monitorProvider,
            debounceDelay: TimeSpan.FromMilliseconds(10))
        {
            BeforePersistHookForTesting = async () =>
            {
                saveStartedTcs.TrySetResult(true);
                await allowSaveToCompleteTcs.Task;
            }
        };

        // Act: Trigger placement change and wait for debounce save to actually start
        placementService.OnMainWindowBoundsChanged(555, 666, 800, 600);
        await saveStartedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Debounce save is now paused in flight. Call Flush immediately.
        Task flushTask = Task.Run(async () => await placementService.FlushPendingSaveAsync());

        // Flush must NOT return while the save is still in flight
        flushTask.IsCompleted.Should().BeFalse("Flush must await the active save");

        // Release the in-flight save
        allowSaveToCompleteTcs.TrySetResult(true);
        await flushTask.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert: Settings must be saved on disk
        FileSettingsRepository verifyRepo = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        UserSettings finalSettings = await verifyRepo.LoadSettingsAsync();
        finalSettings.MainWindowLeft.Should().Be(555);
        finalSettings.MainWindowTop.Should().Be(666);
    }

    [Fact]
    public async Task WindowBoundsChanged_WhileSaveInFlight_SubsequentPersistedStateContainsNewerPlacement()
    {
        FileSettingsRepository repo = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        using SettingsService settingsService = new(repo, NullLogger<SettingsService>.Instance);
        await settingsService.InitializeAsync();

        TestDisplayMonitorProvider monitorProvider = new();
        TaskCompletionSource<bool> firstSaveStartedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> allowFirstSaveTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int persistCount = 0;

        using WindowPlacementService placementService = new(
            settingsService,
            monitorProvider,
            debounceDelay: TimeSpan.FromMilliseconds(10))
        {
            BeforePersistHookForTesting = async () =>
            {
                int count = Interlocked.Increment(ref persistCount);
                if (count == 1)
                {
                    firstSaveStartedTcs.TrySetResult(true);
                    await allowFirstSaveTcs.Task;
                }
            }
        };

        // Move 1: left = 100
        placementService.OnMainWindowBoundsChanged(100, 200, 900, 600);
        await firstSaveStartedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // While save 1 is paused in flight, move 2 arrives: left = 300
        placementService.OnMainWindowBoundsChanged(300, 400, 950, 650);

        // Allow save 1 to finish
        allowFirstSaveTcs.TrySetResult(true);

        // Flush to ensure all pending work is done
        await placementService.FlushPendingSaveAsync();

        // Assert: disk must reflect the newer coordinates (300, 400), not the older ones
        FileSettingsRepository verifyRepo = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        UserSettings finalSettings = await verifyRepo.LoadSettingsAsync();
        finalSettings.MainWindowLeft.Should().Be(300);
        finalSettings.MainWindowTop.Should().Be(400);
        finalSettings.MainWindowWidth.Should().Be(950);
        finalSettings.MainWindowHeight.Should().Be(650);
    }

    [Fact]
    public async Task ConcurrentSettingsAndPlacement_DeterministicGating_PreservesBothStreams()
    {
        FileSettingsRepository repo = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        using SettingsService settingsService = new(repo, NullLogger<SettingsService>.Instance);
        await settingsService.InitializeAsync();

        TestDisplayMonitorProvider monitorProvider = new();
        using WindowPlacementService placementService = new(
            settingsService,
            monitorProvider,
            debounceDelay: TimeSpan.FromMilliseconds(10));

        // Barrier to start concurrent operations simultaneously
        using Barrier barrier = new(2);

        Task settingsTask = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            for (int i = 0; i < 10; i++)
            {
                UserSettings updated = settingsService.CurrentSettings.Clone();
                updated.Theme = (i % 2 == 0) ? AppThemeMode.Dark : AppThemeMode.Light;
                updated.WidgetOpacity = 0.6 + (i * 0.02);
                updated.DateFormat = "yyyy-MM-dd";
                await settingsService.SaveSettingsAsync(updated);
            }
        });

        Task placementTask = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            for (int i = 0; i < 10; i++)
            {
                placementService.OnMainWindowBoundsChanged(200 + i, 300 + i, 800, 600);
                placementService.OnWidgetWindowBoundsChanged(100 + i, 150 + i);
            }
        });

        await Task.WhenAll(settingsTask, placementTask);
        await placementService.FlushPendingSaveAsync();

        FileSettingsRepository verifyRepo = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        UserSettings finalSettings = await verifyRepo.LoadSettingsAsync();

        // Verification: both settings stream and placement stream values are preserved
        finalSettings.DateFormat.Should().Be("yyyy-MM-dd");
        finalSettings.WidgetOpacity.Should().BeInRange(0.6, 1.0);
        finalSettings.MainWindowLeft.Should().Be(209);
        finalSettings.MainWindowTop.Should().Be(309);
        finalSettings.WidgetWindowLeft.Should().Be(109);
        finalSettings.WidgetWindowTop.Should().Be(159);
    }

    [Fact]
    public async Task RepeatedFlushPendingSaveAsync_DoesNotDuplicateSavesOrCorruptState()
    {
        FileSettingsRepository repo = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        using SettingsService settingsService = new(repo, NullLogger<SettingsService>.Instance);
        await settingsService.InitializeAsync();

        TestDisplayMonitorProvider monitorProvider = new();
        using WindowPlacementService placementService = new(
            settingsService,
            monitorProvider,
            debounceDelay: TimeSpan.FromSeconds(10));

        placementService.OnMainWindowBoundsChanged(123, 456, 800, 600);

        // Call Flush multiple times sequentially
        await placementService.FlushPendingSaveAsync();
        await placementService.FlushPendingSaveAsync();
        await placementService.FlushPendingSaveAsync();

        FileSettingsRepository verifyRepo = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        UserSettings finalSettings = await verifyRepo.LoadSettingsAsync();

        finalSettings.MainWindowLeft.Should().Be(123);
        finalSettings.MainWindowTop.Should().Be(456);
    }

    [Fact]
    public async Task Dispose_WithInFlightOrPendingPlacementSave_DoesNotCorruptOrThrow()
    {
        FileSettingsRepository repo = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        using SettingsService settingsService = new(repo, NullLogger<SettingsService>.Instance);
        await settingsService.InitializeAsync();

        TestDisplayMonitorProvider monitorProvider = new();
        TaskCompletionSource<bool> saveStartedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> allowSaveTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        WindowPlacementService placementService = new(
            settingsService,
            monitorProvider,
            debounceDelay: TimeSpan.FromMilliseconds(10))
        {
            BeforePersistHookForTesting = async () =>
            {
                saveStartedTcs.TrySetResult(true);
                await allowSaveTcs.Task;
            }
        };

        placementService.OnMainWindowBoundsChanged(444, 555, 900, 600);
        await saveStartedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Queue another change and immediately dispose while first save is paused
        placementService.OnMainWindowBoundsChanged(888, 999, 900, 600);
        Action disposeAction = () => placementService.Dispose();
        disposeAction.Should().NotThrow();

        // Release the in-flight save so it finishes
        allowSaveTcs.TrySetResult(true);
        if (placementService.CurrentSaveTaskForTesting is not null)
        {
            await placementService.CurrentSaveTaskForTesting;
        }

        // Verification: file on disk was written by in-flight save, no crash or corrupted JSON
        FileSettingsRepository verifyRepo = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        UserSettings finalSettings = await verifyRepo.LoadSettingsAsync();
        finalSettings.MainWindowLeft.Should().Be(444);
        finalSettings.MainWindowTop.Should().Be(555);
    }
}
