using CalendarWidget.Core.Exceptions;

namespace CalendarWidget.Core.ValueObjects;

/// <summary>
/// Immutable value object representing an inclusive date and time range.
/// </summary>
public readonly record struct DateRange
{
    public DateTime Start { get; }
    public DateTime End { get; }

    public DateRange(DateTime start, DateTime end)
    {
        if (end < start)
        {
            throw new DomainValidationException($"End date '{end:u}' cannot be earlier than start date '{start:u}'.");
        }

        Start = start;
        End = end;
    }

    /// <summary>
    /// Checks whether a given timestamp falls within this range (inclusive).
    /// </summary>
    public bool Contains(DateTime dateTime) => dateTime >= Start && dateTime <= End;

    /// <summary>
    /// Checks whether this range overlaps with another date range.
    /// </summary>
    public bool Overlaps(DateRange other) => Start <= other.End && other.Start <= End;
}
