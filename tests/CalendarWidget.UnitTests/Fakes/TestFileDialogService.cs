using CalendarWidget.Presentation.Services;

namespace CalendarWidget.UnitTests.Fakes;

/// <summary>
/// Fake implementation of <see cref="IFileDialogService"/> for unit tests.
/// </summary>
public sealed class TestFileDialogService : IFileDialogService
{
    public string? SaveFileDialogResult { get; set; }
    public string? OpenFileDialogResult { get; set; }

    public string? ShowSaveFileDialog(string title, string defaultFileName, string filter) => SaveFileDialogResult;

    public string? ShowOpenFileDialog(string title, string filter) => OpenFileDialogResult;
}
