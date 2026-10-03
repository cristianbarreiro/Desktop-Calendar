using CalendarWidget.Core.Entities;
using CalendarWidget.Presentation.Models;
using CalendarWidget.Presentation.Services;
using CalendarWidget.Presentation.ViewModels;
using CalendarWidget.UnitTests.Fakes;
using FluentAssertions;

namespace CalendarWidget.UnitTests.Presentation;

public sealed class CalendarViewModelEventTests
{
    private readonly CalendarGridService _gridService = new();
    // Clock fixed to 2026-09-15 so "today" is mid-month
    private readonly TestClockService _clockService = new(new DateTime(2026, 9, 15, 10, 0, 0));

    private (CalendarViewModel vm, TestCalendarEventRepository repo) MakeVm()
    {
        TestCalendarEventRepository repo = new();
        TestScopeFactory scopeFactory = new(repo);
        CalendarViewModel vm = new(_gridService, _clockService, scopeFactory);
        return (vm, repo);
    }

    private static CalendarEvent MakeEvent(DateOnly date, string title = "Test", bool allDay = false)
    {
        DateTime start = allDay
            ? DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
            : DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(10, 0)), DateTimeKind.Utc);
        DateTime end = allDay
            ? DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(23, 59, 59)), DateTimeKind.Utc)
            : DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(11, 0)), DateTimeKind.Utc);

        return new CalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = title,
            StartTime = start,
            EndTime = end,
            IsAllDay = allDay,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    // ── Event indicators ──────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshGridWithEvents_EventOnDay15_SetsHasEventsForDay15()
    {
        (CalendarViewModel vm, TestCalendarEventRepository repo) = MakeVm();
        repo.Seed(MakeEvent(new DateOnly(2026, 9, 15)));

        await vm.RefreshGridWithEventsForTestAsync();

        CalendarDayModel? day15 = vm.Days.FirstOrDefault(d => d.Date == new DateOnly(2026, 9, 15));
        day15.Should().NotBeNull();
        day15!.HasEvents.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshGridWithEvents_NoEvents_AllDaysHaveNoEvents()
    {
        (CalendarViewModel vm, _) = MakeVm();

        await vm.RefreshGridWithEventsForTestAsync();

        vm.Days.Should().AllSatisfy(d => d.HasEvents.Should().BeFalse());
    }

    [Fact]
    public async Task RefreshGridWithEvents_EventOnDay15_OtherDaysRemainFalse()
    {
        (CalendarViewModel vm, TestCalendarEventRepository repo) = MakeVm();
        repo.Seed(MakeEvent(new DateOnly(2026, 9, 15)));

        await vm.RefreshGridWithEventsForTestAsync();

        vm.Days.Where(d => d.Date != new DateOnly(2026, 9, 15))
            .Should().AllSatisfy(d => d.HasEvents.Should().BeFalse());
    }

    [Fact]
    public async Task RefreshGridWithEvents_TwoEventsOnSameDay_DayHasEventsTrue()
    {
        (CalendarViewModel vm, TestCalendarEventRepository repo) = MakeVm();
        repo.Seed(MakeEvent(new DateOnly(2026, 9, 20), "Event A"));
        repo.Seed(MakeEvent(new DateOnly(2026, 9, 20), "Event B"));

        await vm.RefreshGridWithEventsForTestAsync();

        vm.Days.First(d => d.Date == new DateOnly(2026, 9, 20)).HasEvents.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshGridWithEvents_MultiDayEvent_AllSpannedDaysHaveEvents()
    {
        (CalendarViewModel vm, TestCalendarEventRepository repo) = MakeVm();
        // Event spans Sep 10–12 UTC
        repo.Seed(new CalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = "Conference",
            StartTime = new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 9, 12, 23, 59, 59, DateTimeKind.Utc),
            IsAllDay = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        await vm.RefreshGridWithEventsForTestAsync();

        vm.Days.First(d => d.Date == new DateOnly(2026, 9, 10)).HasEvents.Should().BeTrue();
        vm.Days.First(d => d.Date == new DateOnly(2026, 9, 11)).HasEvents.Should().BeTrue();
        vm.Days.First(d => d.Date == new DateOnly(2026, 9, 12)).HasEvents.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshGridWithEvents_EventStartsBeforeVisibleRange_StillMarksDay()
    {
        (CalendarViewModel vm, TestCalendarEventRepository repo) = MakeVm();
        // Event starts Aug 31 (leading cell in Sep grid) and ends Sep 1
        repo.Seed(new CalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = "Overlap",
            StartTime = new DateTime(2026, 8, 31, 22, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 9, 1, 2, 0, 0, DateTimeKind.Utc),
            IsAllDay = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        await vm.RefreshGridWithEventsForTestAsync();

        vm.Days.First(d => d.Date == new DateOnly(2026, 9, 1)).HasEvents.Should().BeTrue();
    }

    // ── Create event ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveEventForm_ValidNewEvent_AddsToRepositoryAndRefreshes()
    {
        (CalendarViewModel vm, TestCalendarEventRepository repo) = MakeVm();
        CalendarDayModel day15 = vm.Days.First(d => d.Date == new DateOnly(2026, 9, 15) && d.IsCurrentMonth);
        vm.SelectDay(day15);

        vm.OpenCreateEventForm();
        vm.EventForm.Title = "New Event";
        vm.EventForm.StartTime = new DateTime(2026, 9, 15, 10, 0, 0);
        vm.EventForm.EndTime = new DateTime(2026, 9, 15, 11, 0, 0);

        await vm.SaveEventFormForTestAsync();

        repo.All.Should().ContainSingle(e => e.Title == "New Event");
        repo.All.Single().CalendarId.Should().Be(CalendarIdentity.LocalCalendarId);
        vm.IsEventFormVisible.Should().BeFalse();
    }

    [Fact]
    public async Task SaveEventForm_InvalidTitle_DoesNotPersistAndKeepsFormOpen()
    {
        (CalendarViewModel vm, TestCalendarEventRepository repo) = MakeVm();
        CalendarDayModel day15 = vm.Days.First(d => d.Date == new DateOnly(2026, 9, 15) && d.IsCurrentMonth);
        vm.SelectDay(day15);

        vm.OpenCreateEventForm();
        vm.EventForm.Title = string.Empty;

        await vm.SaveEventFormForTestAsync();

        repo.All.Should().BeEmpty();
        vm.IsEventFormVisible.Should().BeTrue();
        vm.EventForm.ValidationError.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SaveEventForm_EndBeforeStart_DoesNotPersistAndSetsError()
    {
        (CalendarViewModel vm, TestCalendarEventRepository repo) = MakeVm();
        CalendarDayModel day15 = vm.Days.First(d => d.Date == new DateOnly(2026, 9, 15) && d.IsCurrentMonth);
        vm.SelectDay(day15);

        vm.OpenCreateEventForm();
        vm.EventForm.Title = "Bad Event";
        vm.EventForm.StartTime = new DateTime(2026, 9, 15, 11, 0, 0);
        vm.EventForm.EndTime = new DateTime(2026, 9, 15, 10, 0, 0);

        await vm.SaveEventFormForTestAsync();

        repo.All.Should().BeEmpty();
        vm.EventForm.ValidationError.Should().NotBeEmpty();
    }

    // ── Edit event ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveEventForm_EditExistingEvent_UpdatesRepositoryAndRefreshes()
    {
        (CalendarViewModel vm, TestCalendarEventRepository repo) = MakeVm();
        CalendarEvent existing = MakeEvent(new DateOnly(2026, 9, 15), "Original");
        repo.Seed(existing);

        CalendarDayModel day15 = vm.Days.First(d => d.Date == new DateOnly(2026, 9, 15) && d.IsCurrentMonth);
        vm.SelectDay(day15);
        await vm.LoadSelectedDayEventsForTestAsync();

        vm.EventForm = new EventFormViewModel
        {
            EditingId = existing.Id,
            Title = "Updated",
            Description = string.Empty,
            StartTime = existing.StartTime.ToLocalTime(),
            EndTime = existing.EndTime.ToLocalTime(),
        };
        vm.IsEventFormVisible = true;

        await vm.SaveEventFormForTestAsync();

        repo.All.Should().ContainSingle(e => e.Title == "Updated");
        vm.IsEventFormVisible.Should().BeFalse();
    }

    // ── Delete event ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmDeleteEvent_RemovesEventFromRepository()
    {
        (CalendarViewModel vm, TestCalendarEventRepository repo) = MakeVm();
        CalendarEvent ev = MakeEvent(new DateOnly(2026, 9, 15));
        repo.Seed(ev);

        await vm.ConfirmDeleteEventForTestAsync(ev.Id);

        repo.All.Should().BeEmpty();
    }

    [Fact]
    public async Task ConfirmDeleteEvent_LastEventOnDay_HasEventsBecomesFalse()
    {
        (CalendarViewModel vm, TestCalendarEventRepository repo) = MakeVm();
        CalendarEvent ev = MakeEvent(new DateOnly(2026, 9, 15));
        repo.Seed(ev);
        await vm.RefreshGridWithEventsForTestAsync();
        vm.Days.First(d => d.Date == new DateOnly(2026, 9, 15)).HasEvents.Should().BeTrue();

        await vm.ConfirmDeleteEventForTestAsync(ev.Id);

        vm.Days.First(d => d.Date == new DateOnly(2026, 9, 15)).HasEvents.Should().BeFalse();
    }

    // ── Selected day events ───────────────────────────────────────────────────

    [Fact]
    public async Task LoadSelectedDayEvents_DayWithEvents_PopulatesSelectedDayEvents()
    {
        (CalendarViewModel vm, TestCalendarEventRepository repo) = MakeVm();
        repo.Seed(MakeEvent(new DateOnly(2026, 9, 15), "Morning Meeting"));

        CalendarDayModel day15 = vm.Days.First(d => d.Date == new DateOnly(2026, 9, 15) && d.IsCurrentMonth);
        vm.SelectDay(day15);
        await vm.LoadSelectedDayEventsForTestAsync();

        vm.SelectedDayEvents.Should().ContainSingle(e => e.Title == "Morning Meeting");
    }

    [Fact]
    public async Task LoadSelectedDayEvents_DayWithNoEvents_ReturnsEmptyList()
    {
        (CalendarViewModel vm, _) = MakeVm();
        CalendarDayModel day20 = vm.Days.First(d => d.Date == new DateOnly(2026, 9, 20) && d.IsCurrentMonth);
        vm.SelectDay(day20);
        await vm.LoadSelectedDayEventsForTestAsync();

        vm.SelectedDayEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadSelectedDayEvents_AllDayEvent_TimeLabelIsAllDay()
    {
        (CalendarViewModel vm, TestCalendarEventRepository repo) = MakeVm();
        repo.Seed(MakeEvent(new DateOnly(2026, 9, 15), "Holiday", allDay: true));

        CalendarDayModel day15 = vm.Days.First(d => d.Date == new DateOnly(2026, 9, 15) && d.IsCurrentMonth);
        vm.SelectDay(day15);
        await vm.LoadSelectedDayEventsForTestAsync();

        vm.SelectedDayEvents.Should().ContainSingle(e => e.TimeLabel == "All day");
    }

    // ── Month navigation reloads indicators ───────────────────────────────────

    [Fact]
    public async Task PreviousMonth_AfterNavigation_EventIndicatorsReflectNewMonth()
    {
        (CalendarViewModel vm, TestCalendarEventRepository repo) = MakeVm();
        repo.Seed(MakeEvent(new DateOnly(2026, 8, 10)));

        vm.PreviousMonth();
        await Task.Delay(50); // allow async refresh to complete

        vm.CurrentMonth.Should().Be(8);
        vm.Days.First(d => d.Date == new DateOnly(2026, 8, 10)).HasEvents.Should().BeTrue();
    }

    // ── OpenCreateEventForm ───────────────────────────────────────────────────

    [Fact]
    public void OpenCreateEventForm_WithSelectedDay_SetsFormVisibleAndInitializesForm()
    {
        (CalendarViewModel vm, _) = MakeVm();
        CalendarDayModel day15 = vm.Days.First(d => d.Date == new DateOnly(2026, 9, 15) && d.IsCurrentMonth);
        vm.SelectDay(day15);

        vm.OpenCreateEventForm();

        vm.IsEventFormVisible.Should().BeTrue();
        vm.EventForm.IsEditing.Should().BeFalse();
        vm.EventForm.Title.Should().BeEmpty();
    }

    [Fact]
    public void OpenCreateEventForm_WithNoSelectedDay_DoesNotOpenForm()
    {
        (CalendarViewModel vm, _) = MakeVm();
        vm.SelectDay(null);

        vm.OpenCreateEventForm();

        vm.IsEventFormVisible.Should().BeFalse();
    }

    [Fact]
    public void CancelEventForm_ClosesFormAndClearsError()
    {
        (CalendarViewModel vm, _) = MakeVm();
        CalendarDayModel day15 = vm.Days.First(d => d.Date == new DateOnly(2026, 9, 15) && d.IsCurrentMonth);
        vm.SelectDay(day15);
        vm.OpenCreateEventForm();
        vm.IsEventFormVisible.Should().BeTrue();

        vm.CancelEventForm();

        vm.IsEventFormVisible.Should().BeFalse();
    }

    // ── Delete confirmation ───────────────────────────────────────────────────

    [Fact]
    public void RequestDeleteEvent_ShowsConfirmationWithCorrectTitle()
    {
        (CalendarViewModel vm, _) = MakeVm();
        EventListItemModel item = new(Guid.NewGuid(), "My Event", "10:00 – 11:00", null, false);

        vm.RequestDeleteEvent(item);

        vm.IsDeleteConfirmVisible.Should().BeTrue();
        vm.PendingDeleteTitle.Should().Be("My Event");
    }

    [Fact]
    public void CancelDeleteEvent_HidesConfirmationAndClearsPending()
    {
        (CalendarViewModel vm, _) = MakeVm();
        EventListItemModel item = new(Guid.NewGuid(), "My Event", "10:00 – 11:00", null, false);
        vm.RequestDeleteEvent(item);

        vm.CancelDeleteEvent();

        vm.IsDeleteConfirmVisible.Should().BeFalse();
        vm.PendingDeleteTitle.Should().BeEmpty();
    }

    [Fact]
    public async Task RefreshGridWithEvents_EventStartsInsideVisibleRangeEndsAfter_MarksCoveredDays()
    {
        (CalendarViewModel vm, TestCalendarEventRepository repo) = MakeVm();
        // Event starts Sep 28 and ends Oct 10 (after September month)
        repo.Seed(new CalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = "Trip",
            StartTime = new DateTime(2026, 9, 28, 9, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Utc),
            IsAllDay = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        await vm.RefreshGridWithEventsForTestAsync();

        vm.Days.First(d => d.Date == new DateOnly(2026, 9, 28)).HasEvents.Should().BeTrue();
        vm.Days.First(d => d.Date == new DateOnly(2026, 9, 29)).HasEvents.Should().BeTrue();
        vm.Days.First(d => d.Date == new DateOnly(2026, 9, 30)).HasEvents.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshGridWithEvents_EventEndsExactlyAtMidnight_DoesNotMarkDayAfter()
    {
        (CalendarViewModel vm, TestCalendarEventRepository repo) = MakeVm();
        // Event on Sep 15 ending at 2026-09-16 00:00:00 UTC
        repo.Seed(new CalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = "Evening Party",
            StartTime = new DateTime(2026, 9, 15, 20, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc),
            IsAllDay = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        await vm.RefreshGridWithEventsForTestAsync();

        vm.Days.First(d => d.Date == new DateOnly(2026, 9, 15)).HasEvents.Should().BeTrue();
        vm.Days.First(d => d.Date == new DateOnly(2026, 9, 16)).HasEvents.Should().BeFalse();
    }

    [Fact]
    public async Task LoadSelectedDayEvents_MultipleEvents_ReturnedInChronologicalOrder()
    {
        (CalendarViewModel vm, TestCalendarEventRepository repo) = MakeVm();
        repo.Seed(new CalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = "Afternoon Meeting",
            StartTime = new DateTime(2026, 9, 15, 14, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 9, 15, 15, 0, 0, DateTimeKind.Utc),
            IsAllDay = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        repo.Seed(new CalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = "Morning Standup",
            StartTime = new DateTime(2026, 9, 15, 9, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 9, 15, 9, 30, 0, DateTimeKind.Utc),
            IsAllDay = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        CalendarDayModel day15 = vm.Days.First(d => d.Date == new DateOnly(2026, 9, 15) && d.IsCurrentMonth);
        vm.SelectDay(day15);
        await vm.LoadSelectedDayEventsForTestAsync();

        vm.SelectedDayEvents.Should().HaveCount(2);
        vm.SelectedDayEvents[0].Title.Should().Be("Morning Standup");
        vm.SelectedDayEvents[1].Title.Should().Be("Afternoon Meeting");
    }

    [Fact]
    public async Task LoadSelectedDayEvents_NonAllDayEvent_FormatsLocalTimeLabel()
    {
        (CalendarViewModel vm, TestCalendarEventRepository repo) = MakeVm();
        DateTime utcStart = new(2026, 9, 15, 14, 0, 0, DateTimeKind.Utc);
        DateTime utcEnd = new(2026, 9, 15, 15, 30, 0, DateTimeKind.Utc);
        repo.Seed(new CalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = "Work Session",
            StartTime = utcStart,
            EndTime = utcEnd,
            IsAllDay = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        CalendarDayModel day15 = vm.Days.First(d => d.Date == new DateOnly(2026, 9, 15) && d.IsCurrentMonth);
        vm.SelectDay(day15);
        await vm.LoadSelectedDayEventsForTestAsync();

        string expected = $"{utcStart.ToLocalTime():HH:mm} \u2013 {utcEnd.ToLocalTime():HH:mm}";
        vm.SelectedDayEvents.Should().ContainSingle(e => e.TimeLabel == expected);
    }

    [Fact]
    public async Task SaveEventForm_EditExistingEvent_PreservesCreatedAtAndUpdatesUpdatedAt()
    {
        (CalendarViewModel vm, TestCalendarEventRepository repo) = MakeVm();
        DateTime originalCreated = DateTime.UtcNow.AddDays(-2);
        DateTime originalUpdated = DateTime.UtcNow.AddDays(-1);
        CalendarEvent existing = new()
        {
            Id = Guid.NewGuid(),
            Title = "Old Title",
            StartTime = new DateTime(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 9, 15, 11, 0, 0, DateTimeKind.Utc),
            IsAllDay = false,
            CreatedAt = originalCreated,
            UpdatedAt = originalUpdated,
        };
        repo.Seed(existing);

        CalendarDayModel day15 = vm.Days.First(d => d.Date == new DateOnly(2026, 9, 15) && d.IsCurrentMonth);
        vm.SelectDay(day15);

        vm.EventForm = new EventFormViewModel
        {
            EditingId = existing.Id,
            Title = "Renamed Title",
            Description = "Updated description",
            StartTime = existing.StartTime.ToLocalTime(),
            EndTime = existing.EndTime.ToLocalTime(),
            IsAllDay = false,
        };
        vm.IsEventFormVisible = true;

        await vm.SaveEventFormForTestAsync();

        CalendarEvent saved = repo.All.Single(e => e.Id == existing.Id);
        saved.Title.Should().Be("Renamed Title");
        saved.CreatedAt.Should().Be(originalCreated);
        saved.UpdatedAt.Should().BeAfter(originalUpdated);
    }

    [Fact]
    public async Task HasSelectedDayEvents_And_HasNoSelectedDayEvents_ReflectDayState()
    {
        (CalendarViewModel vm, TestCalendarEventRepository repo) = MakeVm();
        CalendarDayModel day15 = vm.Days.First(d => d.Date == new DateOnly(2026, 9, 15) && d.IsCurrentMonth);
        CalendarDayModel day20 = vm.Days.First(d => d.Date == new DateOnly(2026, 9, 20) && d.IsCurrentMonth);

        repo.Seed(MakeEvent(new DateOnly(2026, 9, 15)));

        // Select day with events
        vm.SelectDay(day15);
        await vm.LoadSelectedDayEventsForTestAsync();
        vm.HasSelectedDay.Should().BeTrue();
        vm.HasSelectedDayEvents.Should().BeTrue();
        vm.HasNoSelectedDayEvents.Should().BeFalse();

        // Select day without events
        vm.SelectDay(day20);
        await vm.LoadSelectedDayEventsForTestAsync();
        vm.HasSelectedDay.Should().BeTrue();
        vm.HasSelectedDayEvents.Should().BeFalse();
        vm.HasNoSelectedDayEvents.Should().BeTrue();

        // Clear selection
        vm.SelectDay(null);
        vm.HasSelectedDay.Should().BeFalse();
        vm.HasSelectedDayEvents.Should().BeFalse();
        vm.HasNoSelectedDayEvents.Should().BeFalse();
    }
}
