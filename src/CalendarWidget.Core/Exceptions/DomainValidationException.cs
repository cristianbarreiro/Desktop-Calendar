namespace CalendarWidget.Core.Exceptions;

/// <summary>
/// Exception thrown when a domain business rule or invariant is violated.
/// </summary>
public sealed class DomainValidationException : Exception
{
    public DomainValidationException(string message) : base(message)
    {
    }

    public DomainValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
