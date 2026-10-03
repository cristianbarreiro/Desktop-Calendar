using System.Windows.Controls;
using CalendarWidget.Presentation.ViewModels;

namespace CalendarWidget.Presentation.Views;

/// <summary>
/// Interaction logic for SettingsView.xaml
/// </summary>
public partial class SettingsView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsView"/> class.
    /// </summary>
    public SettingsView()
    {
        InitializeComponent();
    }

    private async void SettingsView_OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
            await viewModel.LoadCalendarSettingsAsync();
    }
}
