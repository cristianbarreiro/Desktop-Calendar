using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CalendarWidget.App.Windows;
using CalendarWidget.Core.Interfaces;
using CalendarWidget.IntegrationTests.Helpers;
using CalendarWidget.Presentation.Services;
using CalendarWidget.Presentation.ViewModels;
using CalendarWidget.Presentation.Views;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CalendarWidget.IntegrationTests.Views;

[Collection(WpfTestCollection.Name)]
public sealed class MainWindowViewportRegressionTests : IDisposable
{
    private readonly WpfTestContext _wpfContext = new();

    public void Dispose() => _wpfContext.Dispose();

    [Fact]
    public void MainWindow_ViewportsAcrossWindowStates_StretchViewsAndKeepSettingsScrollable()
    {
        _wpfContext.Invoke(() =>
        {
            ServiceCollection services = new();
            services.AddSingleton<ICalendarGridService, CalendarGridService>();
            services.AddSingleton<IClockService, SystemClockService>();
            services.AddSingleton<IWindowManager, StubWindowManager>();
            services.AddSingleton<CalendarViewModel>();
            services.AddSingleton<NotesViewModel>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<MainWindowViewModel>();

            using ServiceProvider provider = services.BuildServiceProvider();
            MainWindowViewModel viewModel = provider.GetRequiredService<MainWindowViewModel>();
            MainWindow window = new(viewModel);
            Grid root = (Grid)window.Content;
            Grid mainBody = (Grid)root.Children[1];
            ContentControl contentControl = (ContentControl)mainBody.Children[1];
            ColumnDefinition contentColumn = mainBody.ColumnDefinitions[1];

            (WindowState State, double Width, double Height)[] viewports =
            [
                (WindowState.Normal, 960, 620),
                (WindowState.Normal, 1240, 780),
                (WindowState.Maximized, 1600, 900),
                (WindowState.Normal, 1024, 680)
            ];

            foreach ((WindowState state, double width, double height) in viewports)
            {
                window.WindowState = state;
                Arrange(root, width, height);

                root.ActualWidth.Should().BeApproximately(width, 1);
                root.ActualHeight.Should().BeApproximately(height, 1);
                Rect bodyBounds = mainBody.TransformToAncestor(root)
                    .TransformBounds(new Rect(new Point(0, 0), mainBody.RenderSize));
                bodyBounds.Top.Should().BeApproximately(root.RowDefinitions[0].ActualHeight, 1);
                bodyBounds.Bottom.Should().BeLessThanOrEqualTo(root.ActualHeight + 1);

                foreach (Action navigate in new Action[]
                {
                    viewModel.NavigateCalendar,
                    viewModel.NavigateNotes,
                    viewModel.NavigateSettings
                })
                {
                    navigate();
                    root.UpdateLayout();
                    UserControl currentView = FindVisualChild<UserControl>(contentControl)!;

                    Rect contentBounds = contentControl.TransformToAncestor(mainBody)
                        .TransformBounds(new Rect(new Point(0, 0), contentControl.RenderSize));
                    contentBounds.Left.Should().BeApproximately(mainBody.ColumnDefinitions[0].ActualWidth, 1);
                    contentBounds.Right.Should().BeApproximately(mainBody.ActualWidth, 1);
                    contentControl.ActualWidth.Should().BeApproximately(contentColumn.ActualWidth, 1);
                    contentControl.ActualHeight.Should().BeApproximately(mainBody.ActualHeight, 1);
                    currentView.ActualWidth.Should().BeApproximately(contentControl.ActualWidth, 1);
                    currentView.ActualHeight.Should().BeApproximately(contentControl.ActualHeight, 1);

                    if (currentView is not SettingsView settingsView)
                    {
                        continue;
                    }

                    ScrollViewer scrollViewer = FindVisualChild<ScrollViewer>(settingsView)!;
                    FrameworkElement sections = (FrameworkElement)settingsView.FindName("SettingsSections")!;
                    FrameworkElement dataManagement = (FrameworkElement)settingsView.FindName("DataManagementSection")!;

                    settingsView.ActualHeight.Should().BeApproximately(contentControl.ActualHeight, 1);
                    scrollViewer.ActualHeight.Should().BeApproximately(settingsView.ActualHeight, 1);
                    if (height < 800)
                    {
                        sections.ActualHeight.Should().BeGreaterThan(scrollViewer.ViewportHeight);
                        scrollViewer.ScrollableHeight.Should().BeGreaterThan(0);
                    }

                    scrollViewer.ScrollToEnd();
                    settingsView.UpdateLayout();
                    Rect dataManagementBounds = dataManagement.TransformToAncestor(scrollViewer)
                        .TransformBounds(new Rect(new Point(0, 0), dataManagement.RenderSize));
                    dataManagementBounds.Top.Should().BeGreaterThanOrEqualTo(-1);
                    dataManagementBounds.Bottom.Should().BeLessThanOrEqualTo(scrollViewer.ViewportHeight + 1);
                }
            }

            window.Close();
        });
    }

    private static void Arrange(FrameworkElement element, double width, double height)
    {
        element.Measure(new Size(width, height));
        element.Arrange(new Rect(0, 0, width, height));
        element.UpdateLayout();
    }

    private static T? FindVisualChild<T>(DependencyObject parent)
        where T : DependencyObject
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match)
            {
                return match;
            }

            T? descendant = FindVisualChild<T>(child);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }

    private sealed class StubWindowManager : IWindowManager
    {
        public bool IsFullApplicationVisible => true;
        public bool IsWidgetVisible => false;
        public void ShowFullApplication() { }
        public void ShowWidget() { }
        public void MinimizeWidget() { }
        public void ActivateCurrentWindow() { }
    }
}
