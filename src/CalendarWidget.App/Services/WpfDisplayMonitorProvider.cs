using System.Windows;
using FormsScreen = System.Windows.Forms.Screen;

namespace CalendarWidget.App.Services;

/// <summary>
/// Provides display monitor bounds using Windows desktop screen APIs.
/// </summary>
public sealed class WpfDisplayMonitorProvider : IDisplayMonitorProvider
{
    /// <inheritdoc />
    public IReadOnlyList<DisplayArea> GetDisplayAreas()
    {
        try
        {
            FormsScreen[] screens = FormsScreen.AllScreens;
            if (screens.Length > 0)
            {
                var areas = new List<DisplayArea>(screens.Length);
                foreach (FormsScreen screen in screens)
                {
                    System.Drawing.Rectangle workArea = screen.WorkingArea;
                    areas.Add(new DisplayArea(workArea.Left, workArea.Top, workArea.Width, workArea.Height));
                }

                return areas;
            }
        }
        catch
        {
            // Fallback to WPF SystemParameters if Screen.AllScreens fails
        }

        Rect wpfWorkArea = SystemParameters.WorkArea;
        return [new DisplayArea(wpfWorkArea.Left, wpfWorkArea.Top, wpfWorkArea.Width, wpfWorkArea.Height)];
    }

    /// <inheritdoc />
    public DisplayArea GetPrimaryDisplayArea()
    {
        try
        {
            FormsScreen? primary = FormsScreen.PrimaryScreen;
            if (primary is not null)
            {
                System.Drawing.Rectangle workArea = primary.WorkingArea;
                return new DisplayArea(workArea.Left, workArea.Top, workArea.Width, workArea.Height);
            }
        }
        catch
        {
            // Fallback to WPF SystemParameters
        }

        Rect wpfWorkArea = SystemParameters.WorkArea;
        return new DisplayArea(wpfWorkArea.Left, wpfWorkArea.Top, wpfWorkArea.Width, wpfWorkArea.Height);
    }
}
