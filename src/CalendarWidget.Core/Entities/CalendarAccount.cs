using CalendarWidget.Core.Enums;
using CalendarWidget.Core.Exceptions;

namespace CalendarWidget.Core.Entities;

/// <summary>
/// Provider-neutral identity for a connected external calendar account.
/// Authentication secrets are stored outside this entity.
/// </summary>
public sealed class CalendarAccount
{
    public Guid Id { get; set; }
    public CalendarProvider Provider { get; set; }
    public required string ProviderAccountId { get; set; }
    public required string DisplayName { get; set; }
    public bool IsConnected { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public void Validate()
    {
        if (Id == Guid.Empty)
            throw new DomainValidationException("Account identifier is required.");

        if (!Enum.IsDefined(Provider) || Provider == CalendarProvider.Local)
            throw new DomainValidationException("Local calendars do not have external accounts.");

        if (string.IsNullOrWhiteSpace(ProviderAccountId) || ProviderAccountId.Length > 500)
            throw new DomainValidationException("Provider account identifier is required and cannot exceed 500 characters.");

        if (string.IsNullOrWhiteSpace(DisplayName) || DisplayName.Length > 200)
            throw new DomainValidationException("Account display name is required and cannot exceed 200 characters.");
    }
}
