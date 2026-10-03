using CalendarWidget.Core.Entities;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CalendarWidget.Presentation.ViewModels;

/// <summary>
/// Observable form state for creating or editing a calendar event.
/// </summary>
public sealed partial class EventFormViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private DateTime _startTime = DateTime.Now;

    [ObservableProperty]
    private DateTime _endTime = DateTime.Now.AddHours(1);

    [ObservableProperty]
    private bool _isAllDay;

    [ObservableProperty]
    private string _validationError = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    [NotifyPropertyChangedFor(nameof(FormTitle))]
    [NotifyPropertyChangedFor(nameof(CanChangeCalendar))]
    private Guid? _editingId;

    [ObservableProperty]
    private Guid _calendarId = CalendarIdentity.LocalCalendarId;

    /// <summary>Gets whether the form is in edit mode.</summary>
    public bool IsEditing => EditingId.HasValue;

    public bool CanChangeCalendar => !IsEditing;

    /// <summary>Gets the form dialog title based on editing state.</summary>
    public string FormTitle => IsEditing ? "Edit Event" : "New Event";

    /// <summary>Clears all validation error text.</summary>
    public void ClearError() => ValidationError = string.Empty;

    /// <summary>
    /// Validates form fields and sets <see cref="ValidationError"/> if invalid.
    /// </summary>
    /// <returns><c>true</c> if the form is valid.</returns>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            ValidationError = "Title is required.";
            return false;
        }

        if (Title.Length > 200)
        {
            ValidationError = "Title cannot exceed 200 characters.";
            return false;
        }

        if (Description.Length > 2000)
        {
            ValidationError = "Description cannot exceed 2000 characters.";
            return false;
        }

        if (!IsAllDay && EndTime < StartTime)
        {
            ValidationError = "End time cannot be earlier than start time.";
            return false;
        }

        ValidationError = string.Empty;
        return true;
    }
}
