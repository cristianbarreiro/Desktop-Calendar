namespace CalendarWidget.Core.Exceptions;

public sealed class CalendarProviderConcurrencyException(string message) : Exception(message);
