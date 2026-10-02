using System.IO;
using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Exceptions;
using CalendarWidget.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CalendarWidget.IntegrationTests.Settings;

/// <summary>
/// Integration tests for <see cref="FileSettingsRepository"/> file persistence and fallback behavior.
/// </summary>
public sealed class FileSettingsRepositoryTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _settingsFilePath;

    public FileSettingsRepositoryTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"cw_settings_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
        _settingsFilePath = Path.Combine(_tempDirectory, "settings.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task LoadSettingsAsync_WhenFileDoesNotExist_ReturnsDefaults()
    {
        FileSettingsRepository repo = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);

        UserSettings settings = await repo.LoadSettingsAsync();

        settings.Should().NotBeNull();
        settings.Theme.Should().Be(AppThemeMode.Dark);
        settings.WidgetOpacity.Should().Be(1.0);
        settings.AlwaysOnTop.Should().BeFalse();
        settings.DateFormat.Should().Be("Default");
        settings.TimeFormat.Should().Be(TimeFormatOption.TwentyFourHour);
    }

    [Fact]
    public async Task SaveSettingsAsync_PersistsToFile_AndLoadSettingsAsync_ReadsBackCorrectly()
    {
        FileSettingsRepository repo = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);

        UserSettings toSave = new()
        {
            Theme = AppThemeMode.Light,
            AlwaysOnTop = true,
            StartWithWindows = true,
            WidgetOpacity = 0.75,
            FirstDayOfWeek = DayOfWeek.Sunday,
            DateFormat = "yyyy-MM-dd",
            TimeFormat = TimeFormatOption.TwelveHour
        };

        await repo.SaveSettingsAsync(toSave);

        File.Exists(_settingsFilePath).Should().BeTrue();

        UserSettings loaded = await repo.LoadSettingsAsync();
        loaded.Theme.Should().Be(AppThemeMode.Light);
        loaded.AlwaysOnTop.Should().BeTrue();
        loaded.StartWithWindows.Should().BeTrue();
        loaded.WidgetOpacity.Should().Be(0.75);
        loaded.FirstDayOfWeek.Should().Be(DayOfWeek.Sunday);
        loaded.DateFormat.Should().Be("yyyy-MM-dd");
        loaded.TimeFormat.Should().Be(TimeFormatOption.TwelveHour);
    }

    [Fact]
    public async Task SaveSettingsAsync_WhenValidationFails_ThrowsDomainValidationException()
    {
        FileSettingsRepository repo = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);

        UserSettings invalid = new()
        {
            WidgetOpacity = 0.2 // Invalid: < 0.5
        };

        Func<Task> act = async () => await repo.SaveSettingsAsync(invalid);

        await act.Should().ThrowAsync<DomainValidationException>();
    }

    [Fact]
    public async Task SaveSettingsAsync_FromMultipleRepositories_ForSamePath_IsSerializedAndSafe()
    {
        FileSettingsRepository repoA = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        FileSettingsRepository repoB = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        using Barrier barrier = new(2);

        Task taskA = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            for (int i = 0; i < 20; i++)
            {
                UserSettings settings = new()
                {
                    Theme = (i % 2 == 0) ? AppThemeMode.Dark : AppThemeMode.Light,
                    WidgetOpacity = Math.Min(1.0, 0.55 + (i * 0.015)),
                    MainWindowLeft = 100 + i,
                    MainWindowTop = 200 + i,
                    DateFormat = "yyyy-MM-dd"
                };

                await repoA.SaveSettingsAsync(settings);
            }
        });

        Task taskB = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            for (int i = 0; i < 20; i++)
            {
                UserSettings settings = new()
                {
                    Theme = (i % 2 == 0) ? AppThemeMode.Light : AppThemeMode.Dark,
                    WidgetOpacity = Math.Min(1.0, 0.65 + (i * 0.02)),
                    MainWindowLeft = 300 + i,
                    MainWindowTop = 400 + i,
                    DateFormat = "dd/MM/yyyy"
                };

                await repoB.SaveSettingsAsync(settings);
            }
        });

        await Task.WhenAll(taskA, taskB);

        FileSettingsRepository verifyRepo = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);
        UserSettings finalSettings = await verifyRepo.LoadSettingsAsync();

        finalSettings.MainWindowLeft.Should().BeOneOf(119, 319);
        finalSettings.MainWindowTop.Should().BeOneOf(219, 419);
        finalSettings.DateFormat.Should().BeOneOf("yyyy-MM-dd", "dd/MM/yyyy");
        finalSettings.WidgetOpacity.Should().BeInRange(0.55, 1.0);
    }

    [Fact]
    public async Task LoadSettingsAsync_WhenFileCorrupted_ReturnsDefaultsAndBacksUpCorruptFile()
    {
        await File.WriteAllTextAsync(_settingsFilePath, "{ invalid json content: 123 ]");
        FileSettingsRepository repo = new(_settingsFilePath, NullLogger<FileSettingsRepository>.Instance);

        UserSettings loaded = await repo.LoadSettingsAsync();

        loaded.Should().NotBeNull();
        loaded.Theme.Should().Be(AppThemeMode.Dark);

        // Corrupted file should have been renamed with .corrupted extension
        string[] corruptedFiles = Directory.GetFiles(_tempDirectory, "*.corrupted*");
        corruptedFiles.Should().NotBeEmpty();
    }
}
