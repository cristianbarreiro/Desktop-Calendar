using CalendarWidget.App.Services;
using CalendarWidget.App.Windows;
using CalendarWidget.Presentation.Services;
using CalendarWidget.Presentation.ViewModels;
using CalendarWidget.UnitTests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CalendarWidget.UnitTests.App;

public sealed class WindowManagerTests
{
    private readonly IServiceProvider _serviceProvider;

    public WindowManagerTests()
    {
        ServiceCollection services = new();
        services.AddSingleton<IWindowManager, WindowManager>();
        services.AddSingleton<IClockService>(new TestClockService(new DateTime(2026, 9, 24)));
        services.AddSingleton<ICalendarGridService, CalendarGridService>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<WidgetViewModel>();
        services.AddTransient<CalendarViewModel>();
        services.AddTransient<NotesViewModel>();
        services.AddTransient<SettingsViewModel>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void WindowManager_InitialVisibilityStates_AreFalse()
    {
        // Arrange
        WindowManager sut = new(_serviceProvider);

        // Assert
        sut.IsFullApplicationVisible.Should().BeFalse();
        sut.IsWidgetVisible.Should().BeFalse();
    }
}
