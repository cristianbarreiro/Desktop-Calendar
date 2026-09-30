using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Interfaces;
using CalendarWidget.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CalendarWidget.IntegrationTests.Settings;

/// <summary>
/// Concurrency and ordering tests for <see cref="SettingsService"/>.
/// </summary>
public sealed class SettingsServiceOrderingTests
{
    private sealed class InMemorySettingsRepository : ISettingsRepository
    {
        public UserSettings StoredSettings { get; set; } = new();

        public int SaveCallCount { get; private set; }

        public bool ThrowOnSave { get; set; }

        public Task<UserSettings> LoadSettingsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(StoredSettings.Clone());
        }

        public Task SaveSettingsAsync(UserSettings settings, CancellationToken cancellationToken = default)
        {
            if (ThrowOnSave)
            {
                throw new IOException("Simulated storage write error.");
            }

            SaveCallCount++;
            StoredSettings = settings.Clone();
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task SaveSettingsAsync_RapidSequentialSaves_PersistsLatestSettings()
    {
        InMemorySettingsRepository repo = new();
        using SettingsService service = new(repo, NullLogger<SettingsService>.Instance);
        await service.InitializeAsync();

        UserSettings s1 = new() { Theme = AppThemeMode.Light, WidgetOpacity = 0.6 };
        UserSettings s2 = new() { Theme = AppThemeMode.Dark, WidgetOpacity = 0.7 };
        UserSettings s3 = new() { Theme = AppThemeMode.System, WidgetOpacity = 0.85 };

        Task t1 = service.SaveSettingsAsync(s1);
        Task t2 = service.SaveSettingsAsync(s2);
        Task t3 = service.SaveSettingsAsync(s3);

        await Task.WhenAll(t1, t2, t3);

        service.CurrentSettings.Theme.Should().Be(AppThemeMode.System);
        service.CurrentSettings.WidgetOpacity.Should().Be(0.85);
        repo.StoredSettings.Theme.Should().Be(AppThemeMode.System);
        repo.StoredSettings.WidgetOpacity.Should().Be(0.85);
    }

    [Fact]
    public async Task SaveSettingsAsync_OutOfOrderCompletion_DropsStaleSnapshot()
    {
        InMemorySettingsRepository repo = new();
        using SettingsService service = new(repo, NullLogger<SettingsService>.Instance);
        await service.InitializeAsync();

        UserSettings settingA = new() { Theme = AppThemeMode.Light, WidgetOpacity = 0.5 };
        UserSettings settingB = new() { Theme = AppThemeMode.Dark, WidgetOpacity = 0.95 };

        TaskCompletionSource<bool> tcsCanAProceed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> tcsAHasStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        bool hasAHookFired = false;
        service.BeforeAcquireLockForTesting = async () =>
        {
            if (!hasAHookFired)
            {
                hasAHookFired = true;
                tcsAHasStarted.TrySetResult(true);
                await tcsCanAProceed.Task;
            }
        };

        // Start save A in background; it will increment version to 1 and wait on tcsCanAProceed before acquiring lock
        Task taskA = Task.Run(() => service.SaveSettingsAsync(settingA));

        // Wait until A has incremented its version and paused
        await tcsAHasStarted.Task;

        // Save B runs now; it increments version to 2, acquires lock, saves to repo, and updates CurrentSettings
        await service.SaveSettingsAsync(settingB);

        // Verify B completed and is current
        service.CurrentSettings.Theme.Should().Be(AppThemeMode.Dark);
        service.CurrentSettings.WidgetOpacity.Should().Be(0.95);
        repo.StoredSettings.Theme.Should().Be(AppThemeMode.Dark);

        // Now release A so it acquires lock with stale version 1
        tcsCanAProceed.TrySetResult(true);
        await taskA;

        // A must be dropped and must NOT overwrite B
        service.CurrentSettings.Theme.Should().Be(AppThemeMode.Dark);
        service.CurrentSettings.WidgetOpacity.Should().Be(0.95);
        repo.StoredSettings.Theme.Should().Be(AppThemeMode.Dark);
        repo.StoredSettings.WidgetOpacity.Should().Be(0.95);
    }

    [Fact]
    public async Task SaveSettingsAsync_WhenRepositoryThrows_PreservesStateAndSubsequentSaveSucceeds()
    {
        InMemorySettingsRepository repo = new();
        using SettingsService service = new(repo, NullLogger<SettingsService>.Instance);
        await service.InitializeAsync();

        UserSettings original = service.CurrentSettings.Clone();

        repo.ThrowOnSave = true;
        UserSettings failing = new() { Theme = AppThemeMode.Light, WidgetOpacity = 0.7 };

        Func<Task> act = async () => await service.SaveSettingsAsync(failing);
        await act.Should().ThrowAsync<IOException>();

        // CurrentSettings was not corrupted by the failed save
        service.CurrentSettings.Theme.Should().Be(original.Theme);
        service.CurrentSettings.WidgetOpacity.Should().Be(original.WidgetOpacity);

        // Subsequent valid save succeeds
        repo.ThrowOnSave = false;
        UserSettings valid = new() { Theme = AppThemeMode.Light, WidgetOpacity = 0.8 };
        await service.SaveSettingsAsync(valid);

        service.CurrentSettings.Theme.Should().Be(AppThemeMode.Light);
        service.CurrentSettings.WidgetOpacity.Should().Be(0.8);
        repo.StoredSettings.Theme.Should().Be(AppThemeMode.Light);
    }
}
