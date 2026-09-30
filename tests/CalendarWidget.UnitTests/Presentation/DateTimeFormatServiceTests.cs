using System.Globalization;
using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Presentation.Services;
using CalendarWidget.UnitTests.Fakes;
using FluentAssertions;

namespace CalendarWidget.UnitTests.Presentation;

/// <summary>
/// Unit tests for <see cref="DateTimeFormatService"/>.
/// </summary>
public sealed class DateTimeFormatServiceTests
{
    [Fact]
    public void FormatClockTime_TwentyFourHour_FormatsCorrectly()
    {
        DateTimeFormatService service = new();
        service.UpdateSettings(new UserSettings { TimeFormat = TimeFormatOption.TwentyFourHour });
        DateTime testTime = new(2026, 9, 30, 14, 5, 9);

        string formatted = service.FormatClockTime(testTime);

        formatted.Should().Be("14:05:09");
    }

    [Fact]
    public void FormatClockTime_TwelveHour_FormatsWithAmPm()
    {
        DateTimeFormatService service = new();
        service.UpdateSettings(new UserSettings { TimeFormat = TimeFormatOption.TwelveHour });
        DateTime testTime = new(2026, 9, 30, 14, 5, 9);

        string formatted = service.FormatClockTime(testTime);

        formatted.Should().Contain("02:05:09");
    }

    [Fact]
    public void FormatTime_TwentyFourHour_FormatsWithoutAmPm()
    {
        DateTimeFormatService service = new();
        service.UpdateSettings(new UserSettings { TimeFormat = TimeFormatOption.TwentyFourHour });
        DateTime testTime = new(2026, 9, 30, 9, 15, 0);

        string formatted = service.FormatTime(testTime);

        formatted.Should().Be("09:15");
    }

    [Fact]
    public void FormatTime_TwelveHour_FormatsWithAmPm()
    {
        DateTimeFormatService service = new();
        service.UpdateSettings(new UserSettings { TimeFormat = TimeFormatOption.TwelveHour });
        DateTime testTime = new(2026, 9, 30, 15, 30, 0);

        string formatted = service.FormatTime(testTime);

        formatted.Should().Contain("3:30");
    }

    [Theory]
    [InlineData("yyyy-MM-dd", "2026-09-30")]
    [InlineData("MM/dd/yyyy", "09/30/2026")]
    [InlineData("dd/MM/yyyy", "30/09/2026")]
    public void FormatDate_CustomFormat_FormatsExpectedPattern(string pattern, string expected)
    {
        DateTimeFormatService service = new();
        service.UpdateSettings(new UserSettings { DateFormat = pattern });
        DateOnly testDate = new(2026, 9, 30);

        string formatted = service.FormatDate(testDate);

        formatted.Should().Be(expected);
    }

    [Fact]
    public void FormatEventRange_AllDay_ReturnsAllDay()
    {
        DateTimeFormatService service = new();
        string range = service.FormatEventRange(DateTime.Today, DateTime.Today.AddDays(1), isAllDay: true);

        range.Should().Be("All day");
    }

    [Fact]
    public void FormatEventRange_TimedEvent_ReturnsEnDashSeparatedTimes()
    {
        DateTimeFormatService service = new();
        service.UpdateSettings(new UserSettings { TimeFormat = TimeFormatOption.TwentyFourHour });
        DateTime start = new(2026, 9, 30, 10, 0, 0, DateTimeKind.Local);
        DateTime end = new(2026, 9, 30, 11, 30, 0, DateTimeKind.Local);

        string range = service.FormatEventRange(start, end, isAllDay: false);

        range.Should().Be("10:00 \u2013 11:30");
    }

    [Fact]
    public void UpdateSettings_FiresFormatChanged_WhenValuesDiffer()
    {
        TestSettingsService settingsService = new();
        DateTimeFormatService service = new(settingsService);
        bool fired = false;
        service.FormatChanged += (_, _) => fired = true;

        service.UpdateSettings(new UserSettings { TimeFormat = TimeFormatOption.TwelveHour });

        fired.Should().BeTrue();
    }
}
