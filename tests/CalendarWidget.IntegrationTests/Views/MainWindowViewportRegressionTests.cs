using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
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
    public async Task MainWindow_RuntimeClientArea_PropagatesThroughViewsAndScrolling()
    {
        await _wpfContext.InvokeAsync(async () =>
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
            try
            {
                window.WindowState = WindowState.Normal;
                window.Show();
                await WaitForLayoutAsync(window);
                AssertMainWindowComposition(window, viewModel);

                double runtimeWidth = window.ActualWidth;
                double runtimeHeight = window.ActualHeight;
                double initialClientHeight = ((FrameworkElement)window.Content).ActualHeight;

                window.Width = runtimeWidth * 0.86;
                window.Height = runtimeHeight * 0.68;
                await WaitForLayoutAsync(window);
                ((FrameworkElement)window.Content).ActualHeight.Should().BeLessThan(initialClientHeight);
                AssertMainWindowComposition(window, viewModel);

                window.WindowState = WindowState.Maximized;
                await WaitForLayoutAsync(window);
                AssertMainWindowComposition(window, viewModel);

                window.WindowState = WindowState.Normal;
                await WaitForLayoutAsync(window);
                AssertMainWindowComposition(window, viewModel);

                runtimeWidth = window.ActualWidth;
                runtimeHeight = window.ActualHeight;
                window.Width = runtimeWidth * 0.82;
                window.Height = runtimeHeight * 0.58;
                await WaitForLayoutAsync(window);
                AssertMainWindowComposition(window, viewModel);
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static async Task WaitForLayoutAsync(Window window)
    {
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
    }

    private static void AssertMainWindowComposition(MainWindow window, MainWindowViewModel viewModel)
    {
        Grid root = (Grid)window.Content;
        Grid mainBody = (Grid)root.Children[1];
        ContentControl contentControl = (ContentControl)window.FindName("MainContentControl")!;

        window.ActualWidth.Should().BeGreaterThan(0);
        window.ActualHeight.Should().BeGreaterThan(0);
        root.ActualWidth.Should().BeGreaterThan(0);
        root.ActualHeight.Should().BeGreaterThan(0);
        root.RenderSize.Width.Should().BeApproximately(root.ActualWidth, 1);
        root.RenderSize.Height.Should().BeApproximately(root.ActualHeight, 1);
        root.ActualWidth.Should().BeLessThanOrEqualTo(window.ActualWidth + 2);
        root.ActualHeight.Should().BeLessThanOrEqualTo(window.ActualHeight + 2);

        Rect bodyBounds = BoundsRelativeTo(mainBody, root);
        bodyBounds.Left.Should().BeGreaterThanOrEqualTo(-1);
        bodyBounds.Top.Should().BeApproximately(root.RowDefinitions[0].ActualHeight, 1);
        bodyBounds.Right.Should().BeLessThanOrEqualTo(root.ActualWidth + 1);
        bodyBounds.Bottom.Should().BeLessThanOrEqualTo(root.ActualHeight + 1);
        mainBody.RenderSize.Width.Should().BeApproximately(mainBody.ActualWidth, 1);
        mainBody.RenderSize.Height.Should().BeApproximately(mainBody.ActualHeight, 1);

        Rect contentBounds = BoundsRelativeTo(contentControl, mainBody);
        contentBounds.Left.Should().BeApproximately(mainBody.ColumnDefinitions[0].ActualWidth, 1);
        contentBounds.Top.Should().BeGreaterThanOrEqualTo(-1);
        contentBounds.Right.Should().BeLessThanOrEqualTo(mainBody.ActualWidth + 1);
        contentBounds.Bottom.Should().BeLessThanOrEqualTo(mainBody.ActualHeight + 1);

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

            contentControl.ActualWidth.Should().BeApproximately(mainBody.ColumnDefinitions[1].ActualWidth, 1);
            contentControl.ActualHeight.Should().BeApproximately(mainBody.ActualHeight, 1);
            contentControl.RenderSize.Width.Should().BeApproximately(contentControl.ActualWidth, 1);
            contentControl.RenderSize.Height.Should().BeApproximately(contentControl.ActualHeight, 1);
            currentView.ActualWidth.Should().BeApproximately(contentControl.ActualWidth, 1);
            currentView.ActualHeight.Should().BeApproximately(contentControl.ActualHeight, 1);
            currentView.RenderSize.Width.Should().BeApproximately(currentView.ActualWidth, 1);
            currentView.RenderSize.Height.Should().BeApproximately(currentView.ActualHeight, 1);

            if (currentView is not SettingsView settingsView)
            {
                continue;
            }

            ScrollViewer scrollViewer = FindVisualChild<ScrollViewer>(settingsView)!;
            FrameworkElement sections = (FrameworkElement)settingsView.FindName("SettingsSections")!;
            FrameworkElement dataManagement = (FrameworkElement)settingsView.FindName("DataManagementSection")!;

            settingsView.ActualWidth.Should().BeApproximately(contentControl.ActualWidth, 1);
            settingsView.ActualHeight.Should().BeApproximately(contentControl.ActualHeight, 1);
            scrollViewer.ActualWidth.Should().BeApproximately(settingsView.ActualWidth, 1);
            scrollViewer.ActualHeight.Should().BeApproximately(settingsView.ActualHeight, 1);
            scrollViewer.ViewportHeight.Should().BeApproximately(scrollViewer.ActualHeight, 1);
            sections.RenderSize.Height.Should().BeApproximately(sections.ActualHeight, 1);
            sections.DesiredSize.Height.Should().BeGreaterThan(0);

            if (sections.ActualHeight > scrollViewer.ViewportHeight)
            {
                scrollViewer.ScrollableHeight.Should().BeGreaterThan(0);
            }

            scrollViewer.ScrollToEnd();
            settingsView.UpdateLayout();
            scrollViewer.VerticalOffset.Should().BeApproximately(scrollViewer.ScrollableHeight, 1);
            Rect dataManagementBounds = BoundsRelativeTo(dataManagement, scrollViewer);
            dataManagementBounds.Top.Should().BeGreaterThanOrEqualTo(-1);
            dataManagementBounds.Bottom.Should().BeLessThanOrEqualTo(scrollViewer.ViewportHeight + 1);
        }

        AssertClientInsideMonitorWorkArea(window, root);
    }

    private static Rect BoundsRelativeTo(FrameworkElement element, FrameworkElement ancestor) =>
        element.TransformToAncestor(ancestor).TransformBounds(new Rect(new Point(0, 0), element.RenderSize));

    private static void AssertClientInsideMonitorWorkArea(Window window, FrameworkElement client)
    {
        nint windowHandle = new WindowInteropHelper(window).Handle;
        nint monitor = MonitorFromWindow(windowHandle, MonitorDefaultToNearest);
        monitor.Should().NotBe(0);

        NativeMonitorInfo monitorInfo = new() { Size = Marshal.SizeOf<NativeMonitorInfo>() };
        GetMonitorInfo(monitor, ref monitorInfo).Should().BeTrue();

        Point topLeft = client.PointToScreen(new Point(0, 0));
        Point bottomRight = client.PointToScreen(new Point(client.ActualWidth, client.ActualHeight));
        NativeRect nativeClient = new();
        GetClientRect(windowHandle, ref nativeClient).Should().BeTrue();
        topLeft.X.Should().BeGreaterThanOrEqualTo(monitorInfo.Work.Left - 2,
            "state {0}, WPF client ({1},{2})-({3},{4}), HWND client ({5},{6})-({7},{8}), work area ({9},{10})-({11},{12})",
            window.WindowState,
            topLeft.X,
            topLeft.Y,
            bottomRight.X,
            bottomRight.Y,
            nativeClient.Left,
            nativeClient.Top,
            nativeClient.Right,
            nativeClient.Bottom,
            monitorInfo.Work.Left,
            monitorInfo.Work.Top,
            monitorInfo.Work.Right,
            monitorInfo.Work.Bottom);
        topLeft.Y.Should().BeGreaterThanOrEqualTo(monitorInfo.Work.Top - 2);
        bottomRight.X.Should().BeLessThanOrEqualTo(monitorInfo.Work.Right + 2);
        bottomRight.Y.Should().BeLessThanOrEqualTo(monitorInfo.Work.Bottom + 2);
    }

    private const uint MonitorDefaultToNearest = 2;

    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint window, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref NativeMonitorInfo monitorInfo);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(nint window, ref NativeRect clientRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
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
