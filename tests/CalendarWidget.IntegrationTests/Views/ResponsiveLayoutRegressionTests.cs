using System.Windows;
using System.Windows.Controls;
using CalendarWidget.IntegrationTests.Helpers;
using CalendarWidget.Presentation.Views;
using FluentAssertions;

namespace CalendarWidget.IntegrationTests.Views;

[Collection(WpfTestCollection.Name)]
public sealed class ResponsiveLayoutRegressionTests : IDisposable
{
    private readonly WpfTestContext _wpfContext = new();

    public void Dispose()
    {
        _wpfContext.Dispose();
    }

    [Fact]
    public void CalendarView_CalendarGrid_UsesAdditionalVerticalSpaceWhenResized()
    {
        _wpfContext.Invoke(() =>
        {
            CalendarView view = new();
            MeasureAndArrange(view, 1200, 900);
            ItemsControl calendarGrid = (ItemsControl)view.FindName("CalendarGrid")!;
            double maximizedGridHeight = calendarGrid.ActualHeight;

            MeasureAndArrange(view, 1200, 600);

            calendarGrid.ActualHeight.Should().BeLessThan(maximizedGridHeight);
            (maximizedGridHeight - calendarGrid.ActualHeight).Should().BeGreaterThan(200);
        });
    }

    [Fact]
    public void SettingsView_Sections_ExpandWithWideViewportAndAdaptWhenNarrowed()
    {
        _wpfContext.Invoke(() =>
        {
            SettingsView view = new();
            MeasureAndArrange(view, 1600, 900);
            FrameworkElement sections = (FrameworkElement)view.FindName("SettingsSections")!;
            double wideSectionsWidth = sections.ActualWidth;

            MeasureAndArrange(view, 750, 700);

            wideSectionsWidth.Should().BeGreaterThan(1400);
            sections.ActualWidth.Should().BeLessThan(wideSectionsWidth);
            sections.ActualWidth.Should().BeGreaterThan(600);
        });
    }

    [Fact]
    public void SettingsView_ShortViewport_ContainsLayoutAndScrollsWithinContentArea()
    {
        _wpfContext.Invoke(() =>
        {
            SettingsView view = new();
            MeasureAndArrange(view, 900, 420);

            ScrollViewer scrollViewer = (ScrollViewer)view.FindName("SettingsScrollViewer")!;
            FrameworkElement sections = (FrameworkElement)view.FindName("SettingsSections")!;

            scrollViewer.ActualHeight.Should().BeApproximately(420, 1);
            scrollViewer.ScrollableHeight.Should().BeGreaterThan(0);
            sections.ActualHeight.Should().BeGreaterThan(scrollViewer.ViewportHeight);

            MeasureAndArrange(view, 900, 700);

            scrollViewer.ActualHeight.Should().BeApproximately(700, 1);
            scrollViewer.ScrollableHeight.Should().BeGreaterThan(0);
        });
    }

    private static void MeasureAndArrange(FrameworkElement element, double width, double height)
    {
        element.Width = width;
        element.Height = height;
        element.Measure(new Size(width, height));
        element.Arrange(new Rect(0, 0, width, height));
        element.UpdateLayout();
    }
}
