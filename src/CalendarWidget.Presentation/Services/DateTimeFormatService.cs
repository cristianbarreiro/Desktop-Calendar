using System.Globalization;
using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Interfaces;

namespace CalendarWidget.Presentation.Services;

/// <summary>
/// Implements dynamic date and time formatting driven by <see cref="ISettingsService"/>.
/// </summary>
public sealed class DateTimeFormatService : IDateTimeFormatService
{
    private TimeFormatOption _timeFormat = TimeFormatOption.TwentyFourHour;
    private string _dateFormat = "Default";

    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimeFormatService"/> class.
    /// </summary>
    /// <param name="settingsService">Settings service for initial state and change subscription.</param>
    public DateTimeFormatService(ISettingsService settingsService)
    {
        UpdateSettings(settingsService.CurrentSettings);
        settingsService.SettingsChanged += (_, settings) => UpdateSettings(settings);
    }

    /// <summary>
    /// Initializes a new instance with default formats (for testing).
    /// </summary>
    public DateTimeFormatService()
    {
    }

    /// <inheritdoc />
    public event EventHandler? FormatChanged;

    /// <inheritdoc />
    public void UpdateSettings(UserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        bool changed = _timeFormat != settings.TimeFormat || _dateFormat != settings.DateFormat;
        _timeFormat = settings.TimeFormat;
        _dateFormat = settings.DateFormat;

        if (changed)
        {
            FormatChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <inheritdoc />
    public string FormatClockTime(DateTime time)
    {
        string pattern = _timeFormat == TimeFormatOption.TwelveHour
            ? "hh:mm:ss tt"
            : "HH:mm:ss";

        return time.ToString(pattern, CultureInfo.CurrentCulture);
    }

    /// <inheritdoc />
    public string FormatTime(DateTime time)
    {
        string pattern = _timeFormat == TimeFormatOption.TwelveHour
            ? "h:mm tt"
            : "HH:mm";

        return time.ToString(pattern, CultureInfo.CurrentCulture);
    }

    /// <inheritdoc />
    public string FormatDate(DateOnly targetDate)
    {
        DateTime dt = targetDate.ToDateTime(TimeOnly.MinValue);
        return FormatDate(dt);
    }

    /// <inheritdoc />
    public string FormatDate(DateTime dateTime)
    {
        string pattern = GetEffectiveDatePattern();
        return dateTime.ToString(pattern, CultureInfo.CurrentCulture);
    }

    /// <inheritdoc />
    public string FormatEventRange(DateTime startTime, DateTime endTime, bool isAllDay)
    {
        if (isAllDay)
        {
            return "All day";
        }

        DateTime startLocal = startTime.Kind == DateTimeKind.Utc ? startTime.ToLocalTime() : startTime;
        DateTime endLocal = endTime.Kind == DateTimeKind.Utc ? endTime.ToLocalTime() : endTime;

        return $"{FormatTime(startLocal)} \u2013 {FormatTime(endLocal)}";
    }

    private string GetEffectiveDatePattern()
    {
        return _dateFormat switch
        {
            "yyyy-MM-dd" => "yyyy-MM-dd",
            "MM/dd/yyyy" => "MM/dd/yyyy",
            "dd/MM/yyyy" => "dd/MM/yyyy",
            "Default" or _ => CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern,
        };
    }
}
