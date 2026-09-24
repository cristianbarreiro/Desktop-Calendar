using CommunityToolkit.Mvvm.ComponentModel;

namespace CalendarWidget.Presentation.ViewModels;

/// <summary>
/// ViewModel for the main application notes view shell.
/// </summary>
public sealed partial class NotesViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "Notes";

    [ObservableProperty]
    private string _emptyStateTitle = "No Notes Yet";

    [ObservableProperty]
    private string _emptyStateDescription = "Quick capture notes and memo management will be introduced in Phase 9.";
}
