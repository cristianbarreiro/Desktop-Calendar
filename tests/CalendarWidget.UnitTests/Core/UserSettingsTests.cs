using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Exceptions;
using FluentAssertions;

namespace CalendarWidget.UnitTests.Core;

/// <summary>
/// Unit tests for <see cref="UserSettings"/> defaults, validation, and cloning.
/// </summary>
public sealed class UserSettingsTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        UserSettings settings = new();

        settings.Theme.Should().Be(AppThemeMode.Dark);
        settings.AlwaysOnTop.Should().BeFalse();
        settings.StartWithWindows.Should().BeFalse();
        settings.WidgetOpacity.Should().Be(1.0);
        settings.FirstDayOfWeek.Should().Be(DayOfWeek.Monday);
        settings.DateFormat.Should().Be("Default");
        settings.TimeFormat.Should().Be(TimeFormatOption.TwentyFourHour);
    }

    [Fact]
    public void Validate_DefaultSettings_DoesNotThrow()
    {
        UserSettings settings = new();

        Action act = () => settings.Validate();

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(0.75)]
    [InlineData(1.0)]
    public void Validate_ValidOpacityRange_DoesNotThrow(double opacity)
    {
        UserSettings settings = new() { WidgetOpacity = opacity };

        Action act = () => settings.Validate();

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0.49)]
    [InlineData(0.0)]
    [InlineData(-0.1)]
    [InlineData(1.01)]
    [InlineData(2.0)]
    public void Validate_OpacityOutOfRange_ThrowsDomainValidationException(double opacity)
    {
        UserSettings settings = new() { WidgetOpacity = opacity };

        Action act = () => settings.Validate();

        act.Should().Throw<DomainValidationException>().WithMessage("*opacity*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyOrNullDateFormat_ThrowsDomainValidationException(string? dateFormat)
    {
        UserSettings settings = new() { DateFormat = dateFormat! };

        Action act = () => settings.Validate();

        act.Should().Throw<DomainValidationException>().WithMessage("*Date format*");
    }

    [Fact]
    public void Validate_InvalidThemeEnum_ThrowsDomainValidationException()
    {
        UserSettings settings = new() { Theme = (AppThemeMode)99 };

        Action act = () => settings.Validate();

        act.Should().Throw<DomainValidationException>().WithMessage("*Invalid theme*");
    }

    [Fact]
    public void Validate_InvalidTimeFormatEnum_ThrowsDomainValidationException()
    {
        UserSettings settings = new() { TimeFormat = (TimeFormatOption)99 };

        Action act = () => settings.Validate();

        act.Should().Throw<DomainValidationException>().WithMessage("*Invalid time format*");
    }

    [Fact]
    public void Validate_InvalidFirstDayOfWeekEnum_ThrowsDomainValidationException()
    {
        UserSettings settings = new() { FirstDayOfWeek = (DayOfWeek)99 };

        Action act = () => settings.Validate();

        act.Should().Throw<DomainValidationException>().WithMessage("*Invalid first day of week*");
    }

    [Fact]
    public void Clone_CreatesIndependentDeepCopy()
    {
        UserSettings original = new()
        {
            Theme = AppThemeMode.Dark,
            AlwaysOnTop = true,
            StartWithWindows = true,
            WidgetOpacity = 0.85,
            FirstDayOfWeek = DayOfWeek.Sunday,
            DateFormat = "MM/dd/yyyy",
            TimeFormat = TimeFormatOption.TwelveHour
        };

        UserSettings clone = original.Clone();

        clone.Should().BeEquivalentTo(original);
        clone.WidgetOpacity = 0.6;
        clone.WidgetOpacity.Should().NotBe(original.WidgetOpacity);
    }
}
