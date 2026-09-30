using System.Windows;
using CalendarWidget.App.Services;
using CalendarWidget.Core.Entities;
using CalendarWidget.UnitTests.Fakes;
using FluentAssertions;

namespace CalendarWidget.UnitTests.App;

public sealed class WindowPlacementServiceTests
{
    private readonly TestSettingsService _settingsService = new();
    private readonly TestDisplayMonitorProvider _displayProvider = new();
    private readonly TestManagedWindow _window = new();

    [Fact]
    public void ApplyMainWindowBounds_WhenNoSettingsSaved_CentersOnPrimaryDisplay()
    {
        // Arrange
        _window.Width = 960;
        _window.Height = 620;
        using WindowPlacementService sut = new(_settingsService, _displayProvider);

        // Act
        sut.ApplyMainWindowBounds(_window);

        // Assert
        _window.WindowStartupLocation.Should().Be(WindowStartupLocation.Manual);
        _window.Left.Should().Be((1920 - 960) / 2);
        _window.Top.Should().Be((1080 - 620) / 2);
        _window.Width.Should().Be(960);
        _window.Height.Should().Be(620);
    }

    [Fact]
    public void ApplyMainWindowBounds_WhenSavedBoundsValid_RestoresBoundsAndSetsManualStartupLocation()
    {
        // Arrange
        _settingsService.CurrentSettings.MainWindowLeft = 250;
        _settingsService.CurrentSettings.MainWindowTop = 180;
        _settingsService.CurrentSettings.MainWindowWidth = 1000;
        _settingsService.CurrentSettings.MainWindowHeight = 700;

        using WindowPlacementService sut = new(_settingsService, _displayProvider);

        // Act
        sut.ApplyMainWindowBounds(_window);

        // Assert
        _window.WindowStartupLocation.Should().Be(WindowStartupLocation.Manual);
        _window.Left.Should().Be(250);
        _window.Top.Should().Be(180);
        _window.Width.Should().Be(1000);
        _window.Height.Should().Be(700);
    }

    [Fact]
    public void ApplyMainWindowBounds_WhenSavedBoundsOffScreen_RecoversToPrimaryCenter()
    {
        // Arrange: coordinates far off screen (-5000, -5000)
        _settingsService.CurrentSettings.MainWindowLeft = -5000;
        _settingsService.CurrentSettings.MainWindowTop = -5000;
        _settingsService.CurrentSettings.MainWindowWidth = 960;
        _settingsService.CurrentSettings.MainWindowHeight = 620;

        using WindowPlacementService sut = new(_settingsService, _displayProvider);

        // Act
        sut.ApplyMainWindowBounds(_window);

        // Assert: recovered to primary center
        _window.Left.Should().Be((1920 - 960) / 2);
        _window.Top.Should().Be((1080 - 620) / 2);
    }

    [Fact]
    public void ApplyWidgetWindowBounds_WhenSavedPositionValid_RestoresPosition()
    {
        // Arrange
        _settingsService.CurrentSettings.WidgetWindowLeft = 320;
        _settingsService.CurrentSettings.WidgetWindowTop = 140;

        using WindowPlacementService sut = new(_settingsService, _displayProvider);

        // Act
        sut.ApplyWidgetWindowBounds(_window);

        // Assert
        _window.WindowStartupLocation.Should().Be(WindowStartupLocation.Manual);
        _window.Left.Should().Be(320);
        _window.Top.Should().Be(140);
    }

    [Fact]
    public async Task OnMainWindowBoundsChanged_RapidConsecutiveCalls_DebouncesAndSavesOnce()
    {
        // Arrange: 50ms debounce for rapid test execution
        using WindowPlacementService sut = new(
            _settingsService,
            _displayProvider,
            debounceDelay: TimeSpan.FromMilliseconds(50));

        // Act: simulate 20 rapid movements during drag-resize
        for (int i = 0; i < 20; i++)
        {
            sut.OnMainWindowBoundsChanged(100 + i, 100 + i, 900 + i, 600 + i);
            await Task.Delay(5);
        }

        // Wait for debounce delay to expire
        await Task.Delay(120);

        // Assert: only one save was persisted
        _settingsService.SaveCount.Should().Be(1);
        _settingsService.CurrentSettings.MainWindowLeft.Should().Be(119);
        _settingsService.CurrentSettings.MainWindowTop.Should().Be(119);
        _settingsService.CurrentSettings.MainWindowWidth.Should().Be(919);
        _settingsService.CurrentSettings.MainWindowHeight.Should().Be(619);
    }

    [Fact]
    public async Task FlushPendingSaveAsync_PersistsPendingBoundsImmediately()
    {
        // Arrange: 1000ms debounce
        using WindowPlacementService sut = new(
            _settingsService,
            _displayProvider,
            debounceDelay: TimeSpan.FromMilliseconds(1000));

        sut.OnMainWindowBoundsChanged(400, 300, 850, 550);
        sut.OnWidgetWindowBoundsChanged(150, 120);

        _settingsService.SaveCount.Should().Be(0);

        // Act: flush immediately without waiting for debounce timer
        await sut.FlushPendingSaveAsync();

        // Assert: saved immediately
        _settingsService.SaveCount.Should().Be(1);
        _settingsService.CurrentSettings.MainWindowLeft.Should().Be(400);
        _settingsService.CurrentSettings.MainWindowTop.Should().Be(300);
        _settingsService.CurrentSettings.MainWindowWidth.Should().Be(850);
        _settingsService.CurrentSettings.MainWindowHeight.Should().Be(550);
        _settingsService.CurrentSettings.WidgetWindowLeft.Should().Be(150);
        _settingsService.CurrentSettings.WidgetWindowTop.Should().Be(120);
    }
}
