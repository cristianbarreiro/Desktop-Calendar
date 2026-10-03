using System.Windows.Controls;
using CalendarWidget.Presentation.ViewModels;

namespace CalendarWidget.Presentation.Views;

/// <summary>
/// Interaction logic for CalendarView.xaml
/// </summary>
public partial class CalendarView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarView"/> class.
    /// </summary>
    public CalendarView()
    {
        InitializeComponent();
    }

    private async void CalendarView_OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is CalendarViewModel viewModel)
        {
            await viewModel.LoadAvailableCalendarsAsync();
            await viewModel.RefreshEventsAsync();
        }
    }
}
