using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using CalendarWidget.App.Windows;
using CalendarWidget.Core.Interfaces;
using CalendarWidget.IntegrationTests.Helpers;
using CalendarWidget.Presentation.Services;
using CalendarWidget.Presentation.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CalendarWidget.IntegrationTests.Views;

[Collection(WpfTestCollection.Name)]
public sealed class WindowCaptionControlsRegressionTests : IDisposable
{
    private readonly WpfTestContext _wpfContext = new();

    public void Dispose()
    {
        _wpfContext.Dispose();
    }

    [Fact]
    public void MainWindow_CaptionControls_HaveConsistentHitTargetsAndAccessibleNames()
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
            MainWindow window = new(provider.GetRequiredService<MainWindowViewModel>());

            Button minimize = (Button)window.FindName("MinimizeButton")!;
            Button maximize = (Button)window.FindName("MaximizeRestoreButton")!;
            Button close = (Button)window.FindName("CloseButton")!;
            Button[] controls = [minimize, maximize, close];

            controls.Should().OnlyContain(button => button.Width == 46);
            controls.Should().OnlyContain(button => button.Height == 36);
            AutomationProperties.GetName(minimize).Should().Be("Minimize");
            AutomationProperties.GetName(maximize).Should().Be("Maximize");
            AutomationProperties.GetName(close).Should().Be("Close");
            controls.Should().OnlyContain(button => button.Focusable);
            controls.Should().OnlyContain(button => button.Content is System.Windows.Shapes.Path);
        });
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
