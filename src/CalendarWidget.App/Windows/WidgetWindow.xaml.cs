using System.Windows;
using System.Windows.Input;
using CalendarWidget.Presentation.ViewModels;

namespace CalendarWidget.App.Windows;

/// <summary>
/// Interaction logic for the compact desktop calendar widget window.
/// </summary>
public partial class WidgetWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WidgetWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The view model for the widget window.</param>
    public WidgetWindow(WidgetViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnHeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
