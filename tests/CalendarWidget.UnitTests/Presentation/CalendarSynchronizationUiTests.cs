using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Interfaces;
using CalendarWidget.Core.Models;
using CalendarWidget.Presentation.Services;
using CalendarWidget.Presentation.ViewModels;
using CalendarWidget.UnitTests.Fakes;
using FluentAssertions;

namespace CalendarWidget.UnitTests.Presentation;

public sealed class CalendarSynchronizationUiTests
{
    [Theory]
    [InlineData(CalendarProvider.Google)]
    [InlineData(CalendarProvider.Microsoft)]
    public async Task SaveEventForm_SelectedExternalCalendar_AssociatesEvent(CalendarProvider provider)
    {
        TestCalendarEventRepository repository = new();
        CalendarViewModel viewModel = new(new CalendarGridService(), new TestClockService(new DateTime(2026, 9, 15)),
            new TestScopeFactory(repository));
        viewModel.SelectDay(viewModel.Days.First(day => day.Date == new DateOnly(2026, 9, 15)));
        viewModel.OpenCreateEventForm();
        viewModel.EventForm.Title = "Provider event";
        CalendarSelectionItemViewModel target = new()
        {
            CalendarId = Guid.NewGuid(),
            Provider = provider,
            AccountName = "Account",
            CalendarName = "Personal",
            IsEnabled = true,
        };
        viewModel.AvailableCalendars.Add(target);
        viewModel.EventForm.CalendarId = target.CalendarId;

        await viewModel.SaveEventFormForTestAsync();

        repository.All.Should().ContainSingle().Which.CalendarId.Should().Be(viewModel.EventForm.CalendarId);
        viewModel.EventForm.CanChangeCalendar.Should().BeTrue();
        target.DisplayName.Should().Be($"{provider} — Account — Personal");
    }

    [Fact]
    public async Task LoadCalendarSettings_DisconnectedAccount_ShowsConnectionAndCalendarState()
    {
        CalendarAccount google = MakeAccount(CalendarProvider.Google, isConnected: false);
        Calendar googleCalendar = MakeCalendar(google, CalendarProvider.Google);
        CalendarAccount microsoft = MakeAccount(CalendarProvider.Microsoft, isConnected: true);
        Calendar microsoftCalendar = MakeCalendar(microsoft, CalendarProvider.Microsoft);
        CalendarConnectionFake connections = new(google, googleCalendar, microsoft, microsoftCalendar);
        SettingsViewModel viewModel = CreateSettingsViewModel(connections, new SynchronizationFake(), connections);

        await viewModel.LoadCalendarSettingsAsync();

        viewModel.GoogleConnectionStatus.Should().Contain("Disconnected");
        viewModel.GoogleCalendars.Should().ContainSingle().Which.IsAccountConnected.Should().BeFalse();
        viewModel.MicrosoftConnectionStatus.Should().Be("Connected");
        viewModel.MicrosoftCalendars.Should().ContainSingle().Which.IsAccountConnected.Should().BeTrue();
    }

    [Fact]
    public async Task DisconnectGoogleAccount_UpdatesConnectionStateAndFeedback()
    {
        CalendarAccount google = MakeAccount(CalendarProvider.Google, isConnected: true);
        Calendar googleCalendar = MakeCalendar(google, CalendarProvider.Google);
        CalendarConnectionFake connections = new(google, googleCalendar);
        SettingsViewModel viewModel = CreateSettingsViewModel(connections, new SynchronizationFake());
        await viewModel.LoadCalendarSettingsAsync();

        await viewModel.DisconnectGoogleAccountAsync();

        viewModel.GoogleConnectionStatus.Should().Contain("Disconnected");
        viewModel.GoogleCalendars.Should().ContainSingle().Which.IsAccountConnected.Should().BeFalse();
        viewModel.SuccessMessage.Should().Contain("disconnected");
    }

    [Fact]
    public async Task SyncNow_EnabledCalendarsSucceeded_ShowsSummary()
    {
        SettingsViewModel viewModel = CreateSettingsViewModel(new CalendarConnectionFake(),
            new SynchronizationFake(new CalendarSynchronizationBatchResult([])));

        await viewModel.SyncNowAsync();

        viewModel.SuccessMessage.Should().Contain("No enabled external calendars");
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task SyncNow_ProviderFailed_PresentsError()
    {
        CalendarSynchronizationFailure failure = new(null, null, "Provider unavailable");
        CalendarSynchronizationResult result = new(0, 0, 0, 0, 1, 0, null, [failure], []);
        CalendarSynchronizationBatchResult batch = new(
            [new CalendarSynchronizationCalendarResult(Guid.NewGuid(), CalendarProvider.Google, Guid.NewGuid(), result)]);
        SettingsViewModel viewModel = CreateSettingsViewModel(new CalendarConnectionFake(), new SynchronizationFake(batch));

        await viewModel.SyncNowAsync();

        viewModel.ErrorMessage.Should().Contain("Provider unavailable");
        viewModel.SuccessMessage.Should().BeNull();
    }

    private static SettingsViewModel CreateSettingsViewModel(
        ICalendarConnectionService google,
        ICalendarSynchronizationService synchronization,
        IMicrosoftCalendarConnectionService? microsoft = null) => new(
        new TestSettingsService(), new TestThemeService(), new TestWindowsStartupService(),
        new TestDataManagementService(), new TestFileDialogService(), google, synchronization, microsoft);

    private static CalendarAccount MakeAccount(CalendarProvider provider, bool isConnected) => new()
    {
        Id = Guid.NewGuid(),
        Provider = provider,
        ProviderAccountId = Guid.NewGuid().ToString(),
        DisplayName = $"{provider} account",
        CreatedAt = DateTime.UtcNow,
        IsConnected = isConnected,
    };

    private static Calendar MakeCalendar(CalendarAccount account, CalendarProvider provider) => new()
    {
        Id = Guid.NewGuid(),
        Provider = provider,
        AccountId = account.Id,
        Name = "Personal",
        ExternalId = Guid.NewGuid().ToString(),
        IsEnabled = true,
        CreatedAt = DateTime.UtcNow,
    };

    private sealed class CalendarConnectionFake : ICalendarConnectionService, IMicrosoftCalendarConnectionService
    {
        private readonly CalendarAccount[] _googleAccounts;
        private readonly Calendar[] _googleCalendars;
        private readonly CalendarAccount[] _microsoftAccounts;
        private readonly Calendar[] _microsoftCalendars;

        public CalendarConnectionFake(CalendarAccount? google = null, Calendar? googleCalendar = null,
            CalendarAccount? microsoft = null, Calendar? microsoftCalendar = null)
        {
            _googleAccounts = google is null ? [] : [google];
            _googleCalendars = googleCalendar is null ? [] : [googleCalendar];
            _microsoftAccounts = microsoft is null ? [] : [microsoft];
            _microsoftCalendars = microsoftCalendar is null ? [] : [microsoftCalendar];
        }

        public Task<CalendarAccount> ConnectGoogleAccountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_googleAccounts[0]);
        public Task<IReadOnlyList<CalendarAccount>> GetGoogleAccountsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CalendarAccount>>(_googleAccounts);
        public Task DisconnectGoogleAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
        {
            _googleAccounts.Single(account => account.Id == accountId).IsConnected = false;
            foreach (Calendar calendar in _googleCalendars)
                calendar.IsEnabled = false;
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<Calendar>> DiscoverGoogleCalendarsAsync(Guid accountId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Calendar>>(_googleCalendars);
        public Task SetCalendarEnabledAsync(Guid calendarId, bool enabled, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<CalendarAccount> ConnectMicrosoftAccountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_microsoftAccounts[0]);
        public Task<IReadOnlyList<CalendarAccount>> GetMicrosoftAccountsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CalendarAccount>>(_microsoftAccounts);
        public Task DisconnectMicrosoftAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
        {
            _microsoftAccounts.Single(account => account.Id == accountId).IsConnected = false;
            foreach (Calendar calendar in _microsoftCalendars)
                calendar.IsEnabled = false;
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<Calendar>> DiscoverMicrosoftCalendarsAsync(Guid accountId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Calendar>>(_microsoftCalendars);
        public Task SetMicrosoftCalendarEnabledAsync(Guid calendarId, bool enabled, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class SynchronizationFake(CalendarSynchronizationBatchResult? batch = null) : ICalendarSynchronizationService
    {
        public Task<CalendarSynchronizationBatchResult> SynchronizeEnabledCalendarsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(batch ?? new CalendarSynchronizationBatchResult([]));
        public Task<CalendarSynchronizationResult> SynchronizeCalendarAsync(Guid calendarId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CalendarSynchronizationResult(0, 0, 0, 0, 0, 0, null, [], []));
        public Task<CalendarSynchronizationResult> DeleteEventAsync(Guid eventId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CalendarSynchronizationResult(0, 0, 0, 0, 0, 0, null, [], []));
    }
}
