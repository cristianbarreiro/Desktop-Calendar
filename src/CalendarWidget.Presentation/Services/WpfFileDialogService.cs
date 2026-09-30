using Microsoft.Win32;

namespace CalendarWidget.Presentation.Services;

/// <summary>
/// WPF implementation of <see cref="IFileDialogService"/> using standard Windows dialogs.
/// </summary>
public sealed class WpfFileDialogService : IFileDialogService
{
    /// <inheritdoc />
    public string? ShowSaveFileDialog(string title, string defaultFileName, string filter)
    {
        SaveFileDialog dialog = new()
        {
            Title = title,
            FileName = defaultFileName,
            Filter = filter,
            AddExtension = true,
        };

        bool? result = dialog.ShowDialog();
        return result == true ? dialog.FileName : null;
    }

    /// <inheritdoc />
    public string? ShowOpenFileDialog(string title, string filter)
    {
        OpenFileDialog dialog = new()
        {
            Title = title,
            Filter = filter,
            CheckFileExists = true,
        };

        bool? result = dialog.ShowDialog();
        return result == true ? dialog.FileName : null;
    }
}
