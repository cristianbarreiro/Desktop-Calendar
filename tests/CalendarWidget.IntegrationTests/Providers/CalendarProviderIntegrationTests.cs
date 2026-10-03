using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Exceptions;
using CalendarWidget.Core.Interfaces;
using CalendarWidget.Core.Models;
using CalendarWidget.Infrastructure.Persistence;
using CalendarWidget.Infrastructure.Services;
using CalendarWidget.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CalendarWidget.IntegrationTests.Providers;

public sealed class CalendarProviderIntegrationTests
{
    [Fact]
    public async Task MicrosoftSynchronization_PersistsAndReusesOpaqueDeltaLinkWithoutDuplicates()
    {
        await using SqliteTestContext database = await SqliteTestContext.CreateAsync();
        EfCalendarCatalogRepository catalogs = new(database.Context);
        EfCalendarEventRepository events = new(database.Context);
        EfCalendarSyncRepository sync = new(database.Context);
        CalendarAccount account = new()
        {
            Id = Guid.NewGuid(),
            Provider = CalendarProvider.Microsoft,
            ProviderAccountId = "microsoft-account",
            DisplayName = "Microsoft account",
            CreatedAt = DateTime.UtcNow,
        };
        Calendar calendar = ExternalCalendar(account, "microsoft-calendar");
        calendar.IsEnabled = true;
        await catalogs.SaveAccountAsync(account);
        await catalogs.SaveCalendarAsync(calendar);
        const string deltaLink = "https://graph.microsoft.com/v1.0/me/calendars/id/calendarView/delta?$deltatoken=opaque%2Bvalue";
        FakeCalendarProvider provider = new(CalendarProvider.Microsoft);
        provider.SetSnapshot(calendar.ExternalId, [MakeEvent("event-id", "Imported event")], deltaLink);
        CalendarSynchronizationService service = new(catalogs, events, sync, new ProviderRegistry(provider));

        CalendarSynchronizationResult firstRun = await service.SynchronizeCalendarAsync(calendar.Id);
        CalendarSynchronizationResult secondRun = await service.SynchronizeCalendarAsync(calendar.Id);

        firstRun.EventsImported.Should().Be(1);
        secondRun.EventsImported.Should().Be(0);
        secondRun.Skipped.Should().Be(1);
        (await sync.GetStateAsync(calendar.Id))!.Cursor.Should().Be(deltaLink);
        provider.LastCursor.Should().Be(deltaLink);
        (await events.GetAllAsync()).Should().ContainSingle(item => item.Title == "Imported event");
    }

    [Fact]
    public async Task MicrosoftSynchronization_SupportsBothDirectionsIncrementalDeletesAndMultipleCalendars()
    {
        await using SqliteTestContext database = await SqliteTestContext.CreateAsync();
        EfCalendarCatalogRepository catalogs = new(database.Context);
        EfCalendarEventRepository events = new(database.Context);
        EfCalendarSyncRepository sync = new(database.Context);
        CalendarAccount account = new()
        {
            Id = Guid.NewGuid(),
            Provider = CalendarProvider.Microsoft,
            ProviderAccountId = "microsoft-user",
            DisplayName = "Microsoft user",
            CreatedAt = DateTime.UtcNow,
        };
        Calendar first = ExternalCalendar(account, "first-calendar");
        Calendar second = ExternalCalendar(account, "second-calendar");
        first.IsEnabled = true;
        second.IsEnabled = true;
        await catalogs.SaveAccountAsync(account);
        await catalogs.SaveCalendarAsync(first);
        await catalogs.SaveCalendarAsync(second);
        FakeCalendarProvider provider = new(CalendarProvider.Microsoft);
        provider.SetSnapshot(first.ExternalId, [], "first-cursor");
        provider.SetSnapshot(second.ExternalId, [], "second-cursor");
        CalendarSynchronizationService service = new(catalogs, events, sync, new ProviderRegistry(provider));
        DateTime now = DateTime.UtcNow;
        CalendarEvent firstLocalEvent = LocalEvent(first.Id, "Local first", now);
        CalendarEvent secondLocalEvent = LocalEvent(second.Id, "Local second", now);
        await events.AddAsync(firstLocalEvent);
        await events.AddAsync(secondLocalEvent);

        (await service.SynchronizeCalendarAsync(first.Id)).EventsExported.Should().Be(1);
        (await service.SynchronizeCalendarAsync(second.Id)).EventsExported.Should().Be(1);
        (await sync.GetMappingsForCalendarAsync(CalendarProvider.Microsoft, account.Id, first.Id))
            .Should().ContainSingle().Which.InternalEventId.Should().Be(firstLocalEvent.Id);
        (await sync.GetMappingsForCalendarAsync(CalendarProvider.Microsoft, account.Id, second.Id))
            .Should().ContainSingle().Which.InternalEventId.Should().Be(secondLocalEvent.Id);

        firstLocalEvent.Title = "Updated locally";
        firstLocalEvent.UpdatedAt = now.AddMinutes(1);
        await events.UpdateAsync(firstLocalEvent);
        (await service.SynchronizeCalendarAsync(first.Id)).Updated.Should().Be(1);
        (await provider.GetEventsForRangeAsync(account.Id, first.ExternalId, DateTime.MinValue, DateTime.MaxValue))
            .Should().ContainSingle().Which.Event.Title.Should().Be("Updated locally");
        (await service.DeleteEventAsync(firstLocalEvent.Id)).Deleted.Should().Be(1);
        (await provider.GetEventsForRangeAsync(account.Id, first.ExternalId, DateTime.MinValue, DateTime.MaxValue))
            .Should().BeEmpty();

        CalendarProviderEvent remoteEvent = MakeEvent("remote-event", "Imported from Outlook", "v1");
        provider.SetChanges(first.ExternalId,
            [new CalendarProviderChange(CalendarProviderChangeType.Upsert, remoteEvent.ExternalEventId,
                remoteEvent.ExternalVersion, remoteEvent.Event)], "first-delta-1");
        (await service.SynchronizeCalendarAsync(first.Id)).EventsImported.Should().Be(1);
        provider.SetChanges(first.ExternalId,
            [new CalendarProviderChange(CalendarProviderChangeType.Upsert, remoteEvent.ExternalEventId,
                "v1", remoteEvent.Event)], "first-delta-2");
        (await service.SynchronizeCalendarAsync(first.Id)).Skipped.Should().Be(1);
        (await events.GetAllAsync()).Should().ContainSingle(item => item.Title == "Imported from Outlook");

        CalendarProviderEvent changedRemoteEvent = MakeEvent("remote-event", "Updated in Outlook", "v2");
        provider.SetChanges(first.ExternalId,
            [new CalendarProviderChange(CalendarProviderChangeType.Upsert, changedRemoteEvent.ExternalEventId,
                changedRemoteEvent.ExternalVersion, changedRemoteEvent.Event)], "first-delta-3");
        (await service.SynchronizeCalendarAsync(first.Id)).Updated.Should().Be(1);
        (await events.GetAllAsync()).Should().ContainSingle(item => item.Title == "Updated in Outlook");
        provider.SetChanges(first.ExternalId,
            [new CalendarProviderChange(CalendarProviderChangeType.Delete, "remote-event", null, null)], "first-delta-4");
        (await service.SynchronizeCalendarAsync(first.Id)).Deleted.Should().Be(1);
        (await events.GetAllAsync()).Should().ContainSingle(item => item.Id == secondLocalEvent.Id);
    }

    [Fact]
    public async Task MicrosoftSynchronization_WhenRemoteWriteFails_RetainsLocalEventAndMapping()
    {
        await using SqliteTestContext database = await SqliteTestContext.CreateAsync();
        EfCalendarCatalogRepository catalogs = new(database.Context);
        EfCalendarEventRepository events = new(database.Context);
        EfCalendarSyncRepository sync = new(database.Context);
        CalendarAccount account = new()
        {
            Id = Guid.NewGuid(),
            Provider = CalendarProvider.Microsoft,
            ProviderAccountId = "microsoft-user",
            DisplayName = "Microsoft user",
            CreatedAt = DateTime.UtcNow,
        };
        Calendar calendar = ExternalCalendar(account, "write-failure-calendar");
        calendar.IsEnabled = true;
        await catalogs.SaveAccountAsync(account);
        await catalogs.SaveCalendarAsync(calendar);
        FakeCalendarProvider provider = new(CalendarProvider.Microsoft);
        provider.SetSnapshot(calendar.ExternalId, [], "cursor-before-failure");
        CalendarSynchronizationService service = new(catalogs, events, sync, new ProviderRegistry(provider));
        CalendarEvent local = LocalEvent(calendar.Id, "Before update", DateTime.UtcNow);
        await events.AddAsync(local);
        (await service.SynchronizeCalendarAsync(calendar.Id)).EventsExported.Should().Be(1);
        provider.FailWrites = true;
        local.Title = "Must remain after API failure";
        local.UpdatedAt = DateTime.UtcNow.AddMinutes(1);
        await events.UpdateAsync(local);

        CalendarSynchronizationResult result = await service.SynchronizeCalendarAsync(calendar.Id);

        result.Failed.Should().Be(1);
        result.Failures.Should().ContainSingle(failure => failure.Error.Contains("simulated provider write failure"));
        (await events.GetByIdAsync(local.Id))!.Title.Should().Be("Must remain after API failure");
        (await sync.GetMappingsAsync(local.Id)).Should().ContainSingle();
    }

    [Fact]
    public async Task SynchronizeEnabledCalendars_ContinuesAcrossProvidersAndIsolatesCalendarState()
    {
        await using SqliteTestContext database = await SqliteTestContext.CreateAsync();
        EfCalendarCatalogRepository catalogs = new(database.Context);
        CalendarAccount googleAccount = new()
        {
            Id = Guid.NewGuid(),
            Provider = CalendarProvider.Google,
            ProviderAccountId = "google-user",
            DisplayName = "Google",
            CreatedAt = DateTime.UtcNow,
        };
        CalendarAccount microsoftAccount = new()
        {
            Id = Guid.NewGuid(),
            Provider = CalendarProvider.Microsoft,
            ProviderAccountId = "microsoft-user",
            DisplayName = "Microsoft",
            CreatedAt = DateTime.UtcNow,
        };
        Calendar failedGoogleCalendar = ExternalCalendar(googleAccount, "google-failing");
        Calendar successfulGoogleCalendar = ExternalCalendar(googleAccount, "google-success");
        Calendar successfulMicrosoftCalendar = ExternalCalendar(microsoftAccount, "microsoft-success");
        Calendar disabledCalendar = ExternalCalendar(microsoftAccount, "microsoft-disabled");
        failedGoogleCalendar.IsEnabled = true;
        successfulGoogleCalendar.IsEnabled = true;
        successfulMicrosoftCalendar.IsEnabled = true;
        await catalogs.SaveAccountAsync(googleAccount);
        await catalogs.SaveAccountAsync(microsoftAccount);
        await catalogs.SaveCalendarAsync(failedGoogleCalendar);
        await catalogs.SaveCalendarAsync(successfulGoogleCalendar);
        await catalogs.SaveCalendarAsync(successfulMicrosoftCalendar);
        await catalogs.SaveCalendarAsync(disabledCalendar);

        FakeCalendarProvider google = new(CalendarProvider.Google);
        google.SetSnapshot(failedGoogleCalendar.ExternalId, [], "google-failure-cursor");
        google.SetReadFailure(failedGoogleCalendar.ExternalId);
        google.SetSnapshot(successfulGoogleCalendar.ExternalId,
            [MakeEvent("google-event", "Google import")], "google-success-cursor");
        FakeCalendarProvider microsoft = new(CalendarProvider.Microsoft);
        microsoft.SetSnapshot(successfulMicrosoftCalendar.ExternalId,
            [MakeEvent("microsoft-event", "Microsoft import")], "microsoft-success-cursor");

        ServiceCollection registrations = new();
        registrations.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={database.DbPath}"));
        registrations.AddScoped<ICalendarCatalogRepository, EfCalendarCatalogRepository>();
        registrations.AddScoped<ICalendarEventRepository, EfCalendarEventRepository>();
        registrations.AddScoped<ICalendarSyncRepository, EfCalendarSyncRepository>();
        registrations.AddScoped<ICalendarConflictPolicy, LocalWinsCalendarConflictPolicy>();
        registrations.AddScoped<CalendarSynchronizationService>();
        registrations.AddSingleton<ICalendarProviderRegistry>(new ProviderRegistry(google, microsoft));
        await using ServiceProvider serviceProvider = registrations.BuildServiceProvider();
        ScopedCalendarSynchronizationService service = new(serviceProvider.GetRequiredService<IServiceScopeFactory>());

        CalendarSynchronizationBatchResult result = await service.SynchronizeEnabledCalendarsAsync();
        CalendarSynchronizationBatchResult repeated = await service.SynchronizeEnabledCalendarsAsync();

        result.Calendars.Should().HaveCount(3);
        result.Failed.Should().Be(1);
        result.Succeeded.Should().BeFalse();
        result.Calendars.Single(item => item.CalendarId == failedGoogleCalendar.Id).Result.Failed.Should().Be(1);
        result.Calendars.Single(item => item.CalendarId == successfulGoogleCalendar.Id).Result.Created.Should().Be(1);
        result.Calendars.Single(item => item.CalendarId == successfulMicrosoftCalendar.Id).Result.Created.Should().Be(1);
        repeated.Calendars.Single(item => item.CalendarId == successfulGoogleCalendar.Id).Result.Skipped.Should().Be(1);
        repeated.Calendars.Single(item => item.CalendarId == successfulMicrosoftCalendar.Id).Result.Skipped.Should().Be(1);
        google.CursorsSeen[successfulGoogleCalendar.ExternalId].Should().Be("google-success-cursor");
        microsoft.CursorsSeen[successfulMicrosoftCalendar.ExternalId].Should().Be("microsoft-success-cursor");
        CalendarSyncState? failedState = await new EfCalendarSyncRepository(database.Context).GetStateAsync(failedGoogleCalendar.Id);
        failedState.Should().NotBeNull();
        failedState!.Cursor.Should().BeNull();
        failedState.LastError.Should().Be("simulated provider read failure");
        (await new EfCalendarSyncRepository(database.Context).GetStateAsync(successfulGoogleCalendar.Id))!.Cursor
            .Should().Be("google-success-cursor");
        (await new EfCalendarSyncRepository(database.Context).GetStateAsync(successfulMicrosoftCalendar.Id))!.Cursor
            .Should().Be("microsoft-success-cursor");
        (await new EfCalendarSyncRepository(database.Context).GetMappingsForCalendarAsync(
            CalendarProvider.Google, googleAccount.Id, successfulGoogleCalendar.Id)).Should().ContainSingle();
        (await new EfCalendarSyncRepository(database.Context).GetMappingsForCalendarAsync(
            CalendarProvider.Microsoft, microsoftAccount.Id, successfulMicrosoftCalendar.Id)).Should().ContainSingle();
        (await new EfCalendarEventRepository(database.Context).GetAllAsync()).Select(item => item.Title)
            .Should().BeEquivalentTo("Google import", "Microsoft import");
    }

    [Fact]
    public async Task Synchronization_WhenBothSidesChange_ReportsConflictAndUsesLocalWinsPolicy()
    {
        await using SqliteTestContext database = await SqliteTestContext.CreateAsync();
        EfCalendarCatalogRepository catalogs = new(database.Context);
        EfCalendarEventRepository events = new(database.Context);
        EfCalendarSyncRepository sync = new(database.Context);
        CalendarAccount account = new()
        {
            Id = Guid.NewGuid(),
            Provider = CalendarProvider.Microsoft,
            ProviderAccountId = "conflict-user",
            DisplayName = "Conflict user",
            CreatedAt = DateTime.UtcNow,
        };
        Calendar calendar = ExternalCalendar(account, "conflict-calendar");
        calendar.IsEnabled = true;
        await catalogs.SaveAccountAsync(account);
        await catalogs.SaveCalendarAsync(calendar);
        FakeCalendarProvider provider = new(CalendarProvider.Microsoft);
        provider.SetSnapshot(calendar.ExternalId, [MakeEvent("conflict-event", "Remote original", "v1")], "cursor-1");
        CalendarSynchronizationService service = new(catalogs, events, sync, new ProviderRegistry(provider));
        await service.SynchronizeCalendarAsync(calendar.Id);
        CalendarEvent local = (await events.GetAllAsync()).Should().ContainSingle().Subject;
        local.Title = "Local edit";
        local.UpdatedAt = DateTime.UtcNow.AddMinutes(5);
        await events.UpdateAsync(local);
        provider.SetSnapshot(calendar.ExternalId, [MakeEvent("conflict-event", "Remote edit", "v2")], "cursor-2");

        CalendarSynchronizationResult result = await service.SynchronizeCalendarAsync(calendar.Id);

        result.Conflicts.Should().Be(1,
            $"created={result.Created}, updated={result.Updated}, failed={result.Failed}, skipped={result.Skipped}; failures={string.Join(" | ", result.Failures.Select(failure => failure.Error))}");
        result.Items.Should().ContainSingle(item => item.Outcome == CalendarSynchronizationOutcome.Conflict);
        (await events.GetByIdAsync(local.Id))!.Title.Should().Be("Local edit");
        (await provider.GetEventsForRangeAsync(account.Id, calendar.ExternalId, DateTime.MinValue, DateTime.MaxValue))
            .Should().ContainSingle().Which.Event.Title.Should().Be("Local edit");
    }

    [Fact]
    public async Task Synchronization_WhenManualConflictPolicyIsSelected_PreservesBothSidesAndCursor()
    {
        await using SqliteTestContext database = await SqliteTestContext.CreateAsync();
        EfCalendarCatalogRepository catalogs = new(database.Context);
        EfCalendarEventRepository events = new(database.Context);
        EfCalendarSyncRepository sync = new(database.Context);
        CalendarAccount account = new()
        {
            Id = Guid.NewGuid(),
            Provider = CalendarProvider.Microsoft,
            ProviderAccountId = "manual-conflict-user",
            DisplayName = "Manual conflict user",
            CreatedAt = DateTime.UtcNow,
        };
        Calendar calendar = ExternalCalendar(account, "manual-conflict-calendar");
        calendar.IsEnabled = true;
        await catalogs.SaveAccountAsync(account);
        await catalogs.SaveCalendarAsync(calendar);
        FakeCalendarProvider provider = new(CalendarProvider.Microsoft);
        provider.SetSnapshot(calendar.ExternalId, [MakeEvent("manual-event", "Remote original", "v1")], "cursor-before-conflict");
        CalendarSynchronizationService initialService = new(catalogs, events, sync, new ProviderRegistry(provider));
        await initialService.SynchronizeCalendarAsync(calendar.Id);
        CalendarEvent local = (await events.GetAllAsync()).Should().ContainSingle().Subject;
        local.Title = "Local edit requiring review";
        local.UpdatedAt = DateTime.UtcNow.AddMinutes(5);
        await events.UpdateAsync(local);
        provider.SetSnapshot(calendar.ExternalId, [MakeEvent("manual-event", "Remote edit requiring review", "v2")], "cursor-after-conflict");
        CalendarSynchronizationService service = new(
            catalogs, events, sync, new ProviderRegistry(provider), new ManualResolutionConflictPolicy());

        CalendarSynchronizationResult result = await service.SynchronizeCalendarAsync(calendar.Id);

        result.Conflicts.Should().Be(1);
        result.Cursor.Should().Be("cursor-before-conflict");
        (await sync.GetStateAsync(calendar.Id))!.Cursor.Should().Be("cursor-before-conflict");
        (await events.GetByIdAsync(local.Id))!.Title.Should().Be("Local edit requiring review");
        (await provider.GetEventsForRangeAsync(account.Id, calendar.ExternalId, DateTime.MinValue, DateTime.MaxValue))
            .Should().ContainSingle().Which.Event.Title.Should().Be("Remote edit requiring review");
        (await sync.GetMappingsAsync(local.Id)).Should().ContainSingle().Which.ExternalVersion.Should().Be("v1");
    }

    [Fact]
    public async Task Synchronization_ImportsOncePersistsOpaqueCursorAndIsolatesCalendars()
    {
        await using SqliteTestContext database = await SqliteTestContext.CreateAsync();
        EfCalendarCatalogRepository catalogs = new(database.Context);
        EfCalendarEventRepository events = new(database.Context);
        EfCalendarSyncRepository sync = new(database.Context);
        CalendarAccount account = new()
        {
            Id = Guid.NewGuid(),
            Provider = CalendarProvider.Google,
            ProviderAccountId = "account",
            DisplayName = "Account",
            CreatedAt = DateTime.UtcNow,
        };
        Calendar first = ExternalCalendar(account, "first");
        Calendar second = ExternalCalendar(account, "second");
        first.IsEnabled = true;
        second.IsEnabled = true;
        await catalogs.SaveAccountAsync(account);
        await catalogs.SaveCalendarAsync(first);
        await catalogs.SaveCalendarAsync(second);
        FakeCalendarProvider provider = new(CalendarProvider.Google);
        provider.SetSnapshot(first.ExternalId, [MakeEvent("remote-id", "First calendar")], "opaque:google-token");
        provider.SetSnapshot(second.ExternalId, [MakeEvent("other-id", "Second calendar")], "opaque:other-token");
        CalendarSynchronizationService service = new(catalogs, events, sync, new ProviderRegistry(provider));

        CalendarSynchronizationResult firstRun = await service.SynchronizeCalendarAsync(first.Id);
        CalendarSynchronizationResult secondRun = await service.SynchronizeCalendarAsync(first.Id);

        firstRun.EventsImported.Should().Be(1);
        secondRun.EventsImported.Should().Be(0);
        secondRun.Skipped.Should().Be(1);
        (await events.GetAllAsync()).Should().ContainSingle(item =>
            item.Title == "First calendar" && item.CalendarId == first.Id);
        (await sync.GetMappingsForCalendarAsync(CalendarProvider.Google, account.Id, first.Id))
            .Should().ContainSingle().Which.ExternalEventId.Should().Be("remote-id");
        (await sync.GetStateAsync(first.Id))!.Cursor.Should().Be("opaque:google-token");
        (await sync.GetStateAsync(second.Id)).Should().BeNull();
        provider.LastCursor.Should().Be("opaque:google-token");
        provider.SetSnapshot(first.ExternalId, [MakeEvent("remote-id", "Updated in Google", "v2")], "opaque:updated");
        CalendarSynchronizationResult remoteUpdate = await service.SynchronizeCalendarAsync(first.Id);
        remoteUpdate.Updated.Should().Be(1, $"failed={remoteUpdate.Failed}; failures={string.Join(" | ", remoteUpdate.Failures.Select(failure => failure.Error))}; conflicts={remoteUpdate.Conflicts}");
        (await events.GetAllAsync()).Single(item => item.CalendarId == first.Id).Title.Should().Be("Updated in Google");
        provider.SetSnapshot(first.ExternalId, [], "opaque:deleted");
        CalendarSynchronizationResult remoteDelete = await service.SynchronizeCalendarAsync(first.Id);
        remoteDelete.Deleted.Should().Be(1);
        (await events.GetAllAsync()).Should().BeEmpty();
        second.IsEnabled = false;
        await catalogs.SaveCalendarAsync(second);
        Func<Task> synchronizeDisabled = () => service.SynchronizeCalendarAsync(second.Id);
        await synchronizeDisabled.Should().ThrowAsync<DomainValidationException>();
    }

    [Fact]
    public async Task LocalProvider_UsesExistingRepositoryForCrudAndFullSnapshots()
    {
        await using SqliteTestContext database = await SqliteTestContext.CreateAsync();
        EfCalendarEventRepository events = new(database.Context);
        EfCalendarCatalogRepository catalogs = new(database.Context);
        LocalCalendarProvider provider = new(events, catalogs);
        CalendarProviderCalendar local = (await provider.GetCalendarsAsync(null)).Should().ContainSingle().Subject;
        CalendarEvent model = MakeEvent("unused", "Local event").Event;

        CalendarProviderEventIdentity created = await provider.CreateEventAsync(null, local.ExternalCalendarId, model);
        IReadOnlyList<CalendarProviderEvent> range = await provider.GetEventsForRangeAsync(
            null, local.ExternalCalendarId, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));
        model.Title = "Updated local event";
        CalendarProviderEventIdentity updated = await provider.UpdateEventAsync(
            null, local.ExternalCalendarId, created.ExternalEventId, created.ExternalVersion, model);

        (await events.GetByIdAsync(Guid.Parse(updated.ExternalEventId)))!.Title.Should().Be("Updated local event");
        updated.ExternalVersion.Should().NotBe(created.ExternalVersion);
        range.Should().ContainSingle();
        provider.SupportsIncrementalChanges.Should().BeFalse();
        (await provider.GetChangesAsync(null, local.ExternalCalendarId, null)).IsFullSnapshot.Should().BeTrue();
        await provider.DeleteEventAsync(null, local.ExternalCalendarId, created.ExternalEventId, updated.ExternalVersion);
        (await events.GetByIdAsync(Guid.Parse(created.ExternalEventId))).Should().BeNull();
    }

    [Fact]
    public async Task FakeProvider_SupportsCreateUpdateRangeAndDelete()
    {
        FakeCalendarProvider provider = new(CalendarProvider.Google);
        provider.SetSnapshot("calendar", [], "opaque:initial");
        CalendarEvent model = MakeEvent("unused", "Created remotely").Event;

        CalendarProviderEventIdentity created = await provider.CreateEventAsync(null, "calendar", model);
        (await provider.GetEventsForRangeAsync(null, "calendar", DateTime.MinValue, DateTime.MaxValue))
            .Should().ContainSingle().Which.Event.Title.Should().Be("Created remotely");
        model.Title = "Updated remotely";
        CalendarProviderEventIdentity updated = await provider.UpdateEventAsync(
            null, "calendar", created.ExternalEventId, created.ExternalVersion, model);
        updated.ExternalVersion.Should().NotBe(created.ExternalVersion);
        (await provider.GetEventsForRangeAsync(null, "calendar", DateTime.MinValue, DateTime.MaxValue))
            .Should().ContainSingle().Which.Event.Title.Should().Be("Updated remotely");
        await provider.DeleteEventAsync(null, "calendar", updated.ExternalEventId, updated.ExternalVersion);
        (await provider.GetEventsForRangeAsync(null, "calendar", DateTime.MinValue, DateTime.MaxValue)).Should().BeEmpty();
    }

    [Fact]
    public async Task Synchronization_ExportsUpdatesAndDeletesMappedLocalEvents()
    {
        await using SqliteTestContext database = await SqliteTestContext.CreateAsync();
        EfCalendarCatalogRepository catalogs = new(database.Context);
        EfCalendarEventRepository events = new(database.Context);
        EfCalendarSyncRepository sync = new(database.Context);
        CalendarAccount account = new()
        {
            Id = Guid.NewGuid(),
            Provider = CalendarProvider.Google,
            ProviderAccountId = "writes",
            DisplayName = "Writes",
            CreatedAt = DateTime.UtcNow,
        };
        Calendar calendar = ExternalCalendar(account, "write-calendar");
        calendar.IsEnabled = true;
        await catalogs.SaveAccountAsync(account);
        await catalogs.SaveCalendarAsync(calendar);
        FakeCalendarProvider provider = new(CalendarProvider.Google);
        provider.SetSnapshot(calendar.ExternalId, [], "cursor-1");
        CalendarSynchronizationService service = new(catalogs, events, sync, new ProviderRegistry(provider));
        DateTime now = DateTime.UtcNow;
        CalendarEvent local = new()
        {
            Id = Guid.NewGuid(),
            Title = "Local created",
            StartTime = now,
            EndTime = now.AddHours(1),
            CreatedAt = now,
            UpdatedAt = now,
        };
        await events.AddAsync(local);

        CalendarSynchronizationResult createResult = await service.SynchronizeCalendarAsync(calendar.Id);
        CalendarEventMapping mapping = (await sync.GetMappingsAsync(local.Id)).Should().ContainSingle().Subject;
        (await provider.GetEventsForRangeAsync(account.Id, calendar.ExternalId, DateTime.MinValue, DateTime.MaxValue))
            .Should().ContainSingle().Which.Event.Title.Should().Be("Local created");

        mapping.LastSyncedAt = DateTime.UtcNow.AddMinutes(-1);
        await sync.SaveMappingAsync(mapping);
        local.Title = "Local updated";
        local.UpdatedAt = DateTime.UtcNow;
        await events.UpdateAsync(local);
        CalendarSynchronizationResult updateResult = await service.SynchronizeCalendarAsync(calendar.Id);
        (await provider.GetEventsForRangeAsync(account.Id, calendar.ExternalId, DateTime.MinValue, DateTime.MaxValue))
            .Should().ContainSingle().Which.Event.Title.Should().Be("Local updated");

        provider.FailWrites = true;
        CalendarSynchronizationResult failedDelete = await service.DeleteEventAsync(local.Id);
        failedDelete.Failed.Should().Be(1);
        (await events.GetByIdAsync(local.Id)).Should().NotBeNull();
        (await sync.GetMappingsAsync(local.Id)).Should().ContainSingle();
        (await sync.GetPendingOperationsAsync(calendar.Id)).Should().ContainSingle()
            .Which.Type.Should().Be(PendingCalendarOperationType.Delete);

        provider.FailWrites = false;
        CalendarSynchronizationResult deleteResult = await service.SynchronizeCalendarAsync(calendar.Id);
        deleteResult.Deleted.Should().Be(1);
        (await events.GetByIdAsync(local.Id)).Should().BeNull();
        (await sync.GetPendingOperationsAsync(calendar.Id)).Should().BeEmpty();
        (await provider.GetEventsForRangeAsync(account.Id, calendar.ExternalId, DateTime.MinValue, DateTime.MaxValue)).Should().BeEmpty();
        createResult.EventsExported.Should().Be(1);
        updateResult.Updated.Should().Be(1);
        failedDelete.Failures.Should().ContainSingle();
    }

    [Fact]
    public async Task Synchronization_WhenRemoteCreateFails_RetainsLocalEventAndDoesNotAdvanceCursor()
    {
        await using SqliteTestContext database = await SqliteTestContext.CreateAsync();
        EfCalendarCatalogRepository catalogs = new(database.Context);
        EfCalendarEventRepository events = new(database.Context);
        EfCalendarSyncRepository sync = new(database.Context);
        CalendarAccount account = new()
        {
            Id = Guid.NewGuid(),
            Provider = CalendarProvider.Google,
            ProviderAccountId = "failure",
            DisplayName = "Failure",
            CreatedAt = DateTime.UtcNow,
        };
        Calendar calendar = ExternalCalendar(account, "failure-calendar");
        calendar.IsEnabled = true;
        await catalogs.SaveAccountAsync(account);
        await catalogs.SaveCalendarAsync(calendar);
        FakeCalendarProvider provider = new(CalendarProvider.Google) { FailWrites = true };
        provider.SetSnapshot(calendar.ExternalId, [], "cursor-not-to-commit");
        CalendarSynchronizationService service = new(catalogs, events, sync, new ProviderRegistry(provider));
        DateTime now = DateTime.UtcNow;
        CalendarEvent local = new()
        {
            Id = Guid.NewGuid(),
            Title = "Must remain",
            StartTime = now,
            EndTime = now.AddHours(1),
            CreatedAt = now,
            UpdatedAt = now,
        };
        await events.AddAsync(local);

        CalendarSynchronizationResult result = await service.SynchronizeCalendarAsync(calendar.Id);

        result.Failed.Should().Be(1);
        result.Failures.Should().ContainSingle(failure => failure.Error.Contains("simulated provider write failure"));
        (await events.GetByIdAsync(local.Id)).Should().NotBeNull();
        (await sync.GetMappingsAsync(local.Id)).Should().BeEmpty();
        CalendarSyncState? failedState = await sync.GetStateAsync(calendar.Id);
        failedState.Should().NotBeNull();
        failedState!.Cursor.Should().BeNull();
        failedState.LastError.Should().Contain("simulated provider write failure");
        (await sync.GetPendingOperationsAsync(calendar.Id)).Should().ContainSingle()
            .Which.Type.Should().Be(PendingCalendarOperationType.Upsert);

        provider.FailWrites = false;
        CalendarSynchronizationResult retried = await service.SynchronizeCalendarAsync(calendar.Id);

        retried.Failed.Should().Be(0);
        (await sync.GetPendingOperationsAsync(calendar.Id)).Should().BeEmpty();
        (await sync.GetMappingsAsync(local.Id)).Should().ContainSingle();
    }

    [Fact]
    public async Task DeleteEvent_WithCalendarMismatchedMapping_FailsAndPreservesLocalEvent()
    {
        await using SqliteTestContext database = await SqliteTestContext.CreateAsync();
        EfCalendarCatalogRepository catalogs = new(database.Context);
        EfCalendarEventRepository events = new(database.Context);
        EfCalendarSyncRepository sync = new(database.Context);
        CalendarAccount account = new()
        {
            Id = Guid.NewGuid(),
            Provider = CalendarProvider.Google,
            ProviderAccountId = "mapping",
            DisplayName = "Mapping",
            CreatedAt = DateTime.UtcNow,
        };
        Calendar first = ExternalCalendar(account, "first");
        Calendar second = ExternalCalendar(account, "second");
        await catalogs.SaveAccountAsync(account);
        await catalogs.SaveCalendarAsync(first);
        await catalogs.SaveCalendarAsync(second);
        DateTime now = DateTime.UtcNow;
        CalendarEvent local = new()
        {
            Id = Guid.NewGuid(),
            CalendarId = second.Id,
            Title = "Do not delete remotely",
            StartTime = now,
            EndTime = now.AddHours(1),
            CreatedAt = now,
            UpdatedAt = now,
        };
        await events.AddAsync(local);
        CalendarEventMapping invalidMapping = new()
        {
            InternalEventId = local.Id,
            Provider = CalendarProvider.Google,
            AccountId = account.Id,
            CalendarId = first.Id,
            ExternalEventId = "wrong-calendar-event",
        };
        await sync.SaveMappingAsync(invalidMapping);
        FakeCalendarProvider provider = new(CalendarProvider.Google);
        provider.SetSnapshot(first.ExternalId, [], "first-cursor");
        provider.SetSnapshot(second.ExternalId, [], "second-cursor");
        CalendarSynchronizationService service = new(catalogs, events, sync, new ProviderRegistry(provider));

        CalendarSynchronizationResult result = await service.DeleteEventAsync(local.Id);

        result.Failed.Should().Be(1);
        result.Items.Should().ContainSingle(item => item.Outcome == CalendarSynchronizationOutcome.Failed);
        (await events.GetByIdAsync(local.Id)).Should().NotBeNull();
        (await sync.GetMappingsAsync(local.Id)).Should().ContainSingle();
    }

    private static Calendar ExternalCalendar(CalendarAccount account, string externalId) => new()
    {
        Id = Guid.NewGuid(),
        Provider = account.Provider,
        AccountId = account.Id,
        Name = externalId,
        ExternalId = externalId,
        CreatedAt = DateTime.UtcNow,
    };

    private static CalendarProviderEvent MakeEvent(string id, string title, string version = "v1")
    {
        DateTime now = DateTime.UtcNow;
        return new CalendarProviderEvent(id, version, new CalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = title,
            StartTime = now,
            EndTime = now.AddHours(1),
            CreatedAt = now,
            UpdatedAt = now,
        });
    }

    private static CalendarEvent LocalEvent(Guid calendarId, string title, DateTime start) => new()
    {
        Id = Guid.NewGuid(),
        CalendarId = calendarId,
        Title = title,
        StartTime = start,
        EndTime = start.AddHours(1),
        CreatedAt = start,
        UpdatedAt = start,
    };

    private sealed class ProviderRegistry(params ICalendarProvider[] providers) : ICalendarProviderRegistry
    {
        public ICalendarProvider GetProvider(CalendarProvider type) =>
            providers.SingleOrDefault(provider => provider.Provider == type)
            ?? throw new NotSupportedException();
    }

    private sealed class ManualResolutionConflictPolicy : ICalendarConflictPolicy
    {
        public CalendarConflictResolution Resolve(CalendarEvent localEvent, CalendarProviderChange remoteChange) =>
            CalendarConflictResolution.ManualResolutionRequired;
    }

    private sealed class FakeCalendarProvider(CalendarProvider providerType) : ICalendarProvider
    {
        private readonly Dictionary<string, List<CalendarProviderChange>> calendars = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string?> cursors = new(StringComparer.Ordinal);
        private readonly Dictionary<string, bool> fullSnapshots = new(StringComparer.Ordinal);
        private readonly HashSet<string> failedReads = new(StringComparer.Ordinal);
        public CalendarProvider Provider => providerType;
        public bool SupportsIncrementalChanges => true;
        public bool SupportsEventWrites => true;
        public string? LastCursor { get; private set; }
        public Dictionary<string, string?> CursorsSeen { get; } = new(StringComparer.Ordinal);
        public bool FailWrites { get; set; }

        public void SetReadFailure(string calendarId) => failedReads.Add(calendarId);

        public void SetSnapshot(string calendarId, IReadOnlyList<CalendarProviderEvent> events, string cursor)
        {
            calendars[calendarId] = events.Select(item => new CalendarProviderChange(
                CalendarProviderChangeType.Upsert, item.ExternalEventId, item.ExternalVersion, item.Event)).ToList();
            cursors[calendarId] = cursor;
            fullSnapshots[calendarId] = true;
        }

        public void SetChanges(string calendarId, IReadOnlyList<CalendarProviderChange> changes, string cursor)
        {
            calendars[calendarId] = changes.ToList();
            cursors[calendarId] = cursor;
            fullSnapshots[calendarId] = false;
        }

        public Task<IReadOnlyList<CalendarProviderCalendar>> GetCalendarsAsync(Guid? accountId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CalendarProviderCalendar>>(calendars.Keys
                .Select(key => new CalendarProviderCalendar(key, key)).ToArray());

        public Task<IReadOnlyList<CalendarProviderEvent>> GetEventsForRangeAsync(Guid? accountId, string externalCalendarId, DateTime rangeStartUtc, DateTime rangeEndUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CalendarProviderEvent>>(calendars[externalCalendarId]
                .Where(change => change.Type == CalendarProviderChangeType.Upsert && change.Event is not null)
                .Select(change => new CalendarProviderEvent(change.ExternalEventId, change.ExternalVersion, change.Event!)).ToArray());

        public Task<CalendarProviderChangePage> GetChangesAsync(Guid? accountId, string externalCalendarId, string? cursor, CancellationToken cancellationToken = default)
        {
            LastCursor = cursor;
            CursorsSeen[externalCalendarId] = cursor;
            if (failedReads.Contains(externalCalendarId))
                throw new InvalidOperationException("simulated provider read failure");
            return Task.FromResult(new CalendarProviderChangePage(
                calendars[externalCalendarId].ToArray(), cursors[externalCalendarId], fullSnapshots[externalCalendarId]));
        }

        public Task<CalendarProviderEventIdentity> CreateEventAsync(Guid? accountId, string externalCalendarId, CalendarEvent calendarEvent, CancellationToken cancellationToken = default)
        {
            if (FailWrites)
                throw new InvalidOperationException("simulated provider write failure");
            string id = Guid.NewGuid().ToString("D");
            calendars[externalCalendarId].Add(new CalendarProviderChange(CalendarProviderChangeType.Upsert, id, "created", calendarEvent));
            return Task.FromResult(new CalendarProviderEventIdentity(id, "created"));
        }

        public Task<CalendarProviderEventIdentity> UpdateEventAsync(Guid? accountId, string externalCalendarId, string externalEventId, string? expectedVersion, CalendarEvent calendarEvent, CancellationToken cancellationToken = default)
        {
            if (FailWrites)
                throw new InvalidOperationException("simulated provider write failure");
            string version = Guid.NewGuid().ToString("N");
            List<CalendarProviderChange> changes = calendars[externalCalendarId];
            changes.RemoveAll(change => change.ExternalEventId == externalEventId);
            changes.Add(new CalendarProviderChange(CalendarProviderChangeType.Upsert, externalEventId, version, calendarEvent));
            return Task.FromResult(new CalendarProviderEventIdentity(externalEventId, version));
        }

        public Task DeleteEventAsync(Guid? accountId, string externalCalendarId, string externalEventId, string? expectedVersion, CancellationToken cancellationToken = default)
        {
            if (FailWrites)
                throw new InvalidOperationException("simulated provider write failure");
            calendars[externalCalendarId].RemoveAll(change => change.ExternalEventId == externalEventId);
            return Task.CompletedTask;
        }
    }
}
