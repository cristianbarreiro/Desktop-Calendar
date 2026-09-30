namespace CalendarWidget.Presentation.Services;

/// <summary>
/// Abstraction for displaying open/save file dialogs to the user.
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// Displays a save file dialog prompt.
    /// </summary>
    /// <param name="title">Dialog window title.</param>
    /// <param name="defaultFileName">Default destination file name.</param>
    /// <param name="filter">File type filter specification.</param>
    /// <returns>Selected file path, or null if cancelled.</returns>
    string? ShowSaveFileDialog(string title, string defaultFileName, string filter);

    /// <summary>
    /// Displays an open file dialog prompt.
    /// </summary>
    /// <param name="title">Dialog window title.</param>
    /// <param name="filter">File type filter specification.</param>
    /// <returns>Selected file path, or null if cancelled.</returns>
    string? ShowOpenFileDialog(string title, string filter);
}
