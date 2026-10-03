using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Exceptions;

namespace CalendarWidget.Core.Entities;

/// <summary>
/// A local or provider-owned calendar.
/// </summary>
public sealed class Calendar
{
    public Guid Id { get; set; }
    public CalendarProvider Provider { get; set; }
    public Guid? AccountId { get; set; }
    public required string Name { get; set; }
    public required string ExternalId { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }

    public void Validate()
    {
        if (Id == Guid.Empty)
            throw new DomainValidationException("Calendar identifier is required.");

        if (!Enum.IsDefined(Provider))
            throw new DomainValidationException("Calendar provider is invalid.");

        if (Provider == CalendarProvider.Local && AccountId is not null)
            throw new DomainValidationException("Local calendars cannot belong to an external account.");

        if (Provider == CalendarProvider.Local && Id != CalendarIdentity.LocalCalendarId)
            throw new DomainValidationException("The built-in local calendar has a stable identifier.");

        if (Id == CalendarIdentity.LocalCalendarId && Provider != CalendarProvider.Local)
            throw new DomainValidationException("The built-in local calendar cannot change providers.");

        if (Provider != CalendarProvider.Local && AccountId is null)
            throw new DomainValidationException("External calendars must belong to an account.");

        if (AccountId == Guid.Empty)
            throw new DomainValidationException("Calendar account identifier cannot be empty.");

        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 200)
            throw new DomainValidationException("Calendar name is required and cannot exceed 200 characters.");

        if (string.IsNullOrWhiteSpace(ExternalId) || ExternalId.Length > 500)
            throw new DomainValidationException("Calendar provider identifier is required and cannot exceed 500 characters.");
    }
}
