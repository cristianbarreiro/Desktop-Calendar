using CalendarWidget.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CalendarWidget.Presentation.ViewModels;

public sealed partial class CalendarSelectionItemViewModel : ObservableObject
{
    public Guid CalendarId { get; init; }
    public string AccountName { get; init; } = string.Empty;
    public string CalendarName { get; init; } = string.Empty;
    public CalendarProvider Provider { get; init; }
    public string DisplayName => Provider == CalendarProvider.Local
        ? CalendarName
        : $"{Provider} — {AccountName} — {CalendarName}" +
          (IsAccountConnected ? IsEnabled ? string.Empty : " (sync disabled)" : " (disconnected)");

    public bool IsAccountConnected { get; init; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    private bool _isEnabled;
}
