using CalendarWidget.App.Services;
using CalendarWidget.Core.Entities;
using CalendarWidget.Infrastructure.Services;
using CalendarWidget.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CalendarWidget.IntegrationTests.Windows;

public sealed class WindowPlacementIntegrationTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _settingsFilePath;

    public WindowPlacementIntegrationTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"cw_placement_test_{Guid.NewGuid():N}");
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
                // Best-effort cleanup of temp files
            }
        }
    }

    [Fact]
    public async Task Restart_WithPersistedBounds_RestoresBoundsAccurately()
    {
        // Session 1: Launch app, move window, save, and exit
        FileSettingsRepository repo1 = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        using SettingsService settingsService1 = new(repo1, NullLogger<SettingsService>.Instance);
        await settingsService1.InitializeAsync();

        TestDisplayMonitorProvider monitorProvider = new();
        using WindowPlacementService placementService1 = new(
            settingsService1,
            monitorProvider,
            debounceDelay: TimeSpan.FromMilliseconds(50));

        TestManagedWindow window1 = new() { Width = 960, Height = 620 };
        placementService1.ApplyMainWindowBounds(window1);

        // Move window to custom position
        placementService1.OnMainWindowBoundsChanged(450, 220, 1024, 768);
        placementService1.OnWidgetWindowBoundsChanged(200, 150);
        await placementService1.FlushPendingSaveAsync();

        // Session 2: Fresh launch (simulating restart)
        FileSettingsRepository repo2 = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        using SettingsService settingsService2 = new(repo2, NullLogger<SettingsService>.Instance);
        await settingsService2.InitializeAsync();

        using WindowPlacementService placementService2 = new(settingsService2, monitorProvider);
        TestManagedWindow window2 = new();
        placementService2.ApplyMainWindowBounds(window2);

        // Assert: coordinates restored from disk
        window2.Left.Should().Be(450);
        window2.Top.Should().Be(220);
        window2.Width.Should().Be(1024);
        window2.Height.Should().Be(768);

        settingsService2.CurrentSettings.WidgetWindowLeft.Should().Be(200);
        settingsService2.CurrentSettings.WidgetWindowTop.Should().Be(150);
    }

    [Fact]
    public async Task Restart_WithOffScreenBounds_RecoversToPrimaryCenter()
    {
        // Session 1: Corrupted or offscreen coordinates written to settings
        FileSettingsRepository repo = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        using SettingsService settingsService = new(repo, NullLogger<SettingsService>.Instance);
        await settingsService.InitializeAsync();

        UserSettings corruptSettings = settingsService.CurrentSettings.Clone();
        corruptSettings.MainWindowLeft = -9999;
        corruptSettings.MainWindowTop = -9999;
        corruptSettings.MainWindowWidth = 960;
        corruptSettings.MainWindowHeight = 620;
        await settingsService.SaveSettingsAsync(corruptSettings);

        // Session 2: Launch and apply bounds
        TestDisplayMonitorProvider monitorProvider = new(); // 1920x1080 primary
        using WindowPlacementService placementService = new(settingsService, monitorProvider);
        TestManagedWindow window = new();

        placementService.ApplyMainWindowBounds(window);

        // Assert: safely clamped and centered on primary monitor
        window.Left.Should().Be((1920 - 960) / 2);
        window.Top.Should().Be((1080 - 620) / 2);
        window.Width.Should().Be(960);
        window.Height.Should().Be(620);
    }

    [Fact]
    public async Task Restart_WithNegativeMultiMonitorBounds_PreservesPositionWhenMonitorPresent()
    {
        // Multi-monitor arrangement: Secondary monitor on the left (-1920, 0, 1920, 1080)
        TestDisplayMonitorProvider monitorProvider = new()
        {
            Displays = [new(-1920, 0, 1920, 1080), new(0, 0, 1920, 1080)],
            Primary = new(0, 0, 1920, 1080)
        };

        FileSettingsRepository repo = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        using SettingsService settingsService = new(repo, NullLogger<SettingsService>.Instance);
        await settingsService.InitializeAsync();

        using WindowPlacementService placementService = new(settingsService, monitorProvider);
        placementService.OnMainWindowBoundsChanged(-1400, 200, 960, 620);
        await placementService.FlushPendingSaveAsync();

        // Restart with same multi-monitor configuration
        TestManagedWindow window = new();
        placementService.ApplyMainWindowBounds(window);

        // Assert: negative virtual coordinates correctly preserved
        window.Left.Should().Be(-1400);
        window.Top.Should().Be(200);
    }

    [Fact]
    public async Task Restart_WhenSecondaryMonitorDisconnected_RecoversToPrimaryCenter()
    {
        // Step 1: Window was saved on external secondary monitor (-1920, 0)
        FileSettingsRepository repo = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        using SettingsService settingsService = new(repo, NullLogger<SettingsService>.Instance);
        await settingsService.InitializeAsync();

        UserSettings multiMonSettings = settingsService.CurrentSettings.Clone();
        multiMonSettings.MainWindowLeft = -1400;
        multiMonSettings.MainWindowTop = 200;
        multiMonSettings.MainWindowWidth = 960;
        multiMonSettings.MainWindowHeight = 620;
        await settingsService.SaveSettingsAsync(multiMonSettings);

        // Step 2: Cable disconnected — only single primary display at (0, 0, 1920, 1080) remains
        TestDisplayMonitorProvider singleMonitorProvider = new()
        {
            Displays = [new(0, 0, 1920, 1080)],
            Primary = new(0, 0, 1920, 1080)
        };

        using WindowPlacementService placementService = new(settingsService, singleMonitorProvider);
        TestManagedWindow window = new();
        placementService.ApplyMainWindowBounds(window);

        // Assert: automatically detected offscreen and recovered to primary center
        window.Left.Should().Be((1920 - 960) / 2);
        window.Top.Should().Be((1080 - 620) / 2);
    }

    [Fact]
    public async Task RapidDragging_FlushesOnWindowClose_PersistsLatestBounds()
    {
        FileSettingsRepository repo = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        using SettingsService settingsService = new(repo, NullLogger<SettingsService>.Instance);
        await settingsService.InitializeAsync();

        TestDisplayMonitorProvider monitorProvider = new();
        using WindowPlacementService placementService = new(
            settingsService,
            monitorProvider,
            debounceDelay: TimeSpan.FromSeconds(5)); // Long debounce

        // User rapidly drags window: 15 events in quick succession
        for (int i = 0; i < 15; i++)
        {
            placementService.OnMainWindowBoundsChanged(100 + (i * 10), 100 + (i * 5), 900, 600);
        }

        // Window closes immediately while debounce is still pending
        await placementService.FlushPendingSaveAsync();

        // Fresh instance reads back from disk
        FileSettingsRepository repo2 = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        using SettingsService settingsService2 = new(repo2, NullLogger<SettingsService>.Instance);
        await settingsService2.InitializeAsync();

        settingsService2.CurrentSettings.MainWindowLeft.Should().Be(240); // 100 + 14 * 10
        settingsService2.CurrentSettings.MainWindowTop.Should().Be(170);  // 100 + 14 * 5
    }
}
