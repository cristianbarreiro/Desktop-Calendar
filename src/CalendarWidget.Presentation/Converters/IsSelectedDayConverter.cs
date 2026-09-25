using System.Globalization;
using System.Windows.Data;

namespace CalendarWidget.Presentation.Converters;

/// <summary>
/// Multi-value converter that determines whether a day cell is the currently selected day.
/// Expects two values: [0] = SelectedDay (CalendarDayModel?), [1] = current cell (CalendarDayModel).
/// Returns <c>true</c> when both represent the same date.
/// </summary>
public sealed class IsSelectedDayConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length == 2
            && values[0] is Models.CalendarDayModel selected
            && values[1] is Models.CalendarDayModel current)
        {
            return selected.Date == current.Date;
        }

        return false;
    }

    /// <inheritdoc />
    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
