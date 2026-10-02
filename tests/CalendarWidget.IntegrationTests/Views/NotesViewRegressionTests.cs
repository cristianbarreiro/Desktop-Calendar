using System.Windows;
using System.Windows.Controls;
using CalendarWidget.App.Windows;
using CalendarWidget.Core.Interfaces;
using CalendarWidget.IntegrationTests.Helpers;
using CalendarWidget.Presentation.Services;
using CalendarWidget.Presentation.ViewModels;
using CalendarWidget.Presentation.Views;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CalendarWidget.IntegrationTests.Views;

/// <summary>
/// Regression tests verifying that <see cref="NotesView"/> and its DataTemplate materialization
/// in <see cref="MainWindow"/> load without XAML parse or element collection exceptions.
/// </summary>
[Collection(WpfTestCollection.Name)]
public sealed class NotesViewRegressionTests : IDisposable
{
    private readonly WpfTestContext _wpfContext = new();

    /// <inheritdoc />
    public void Dispose()
    {
        _wpfContext.Dispose();
    }

    [Fact]
    public void NotesView_WhenInstantiatedDirectly_InitializesWithoutException()
    {
        _wpfContext.Invoke(() =>
        {
            Action instantiate = () => _ = new NotesView();
            instantiate.Should().NotThrow();
        });
    }

    [Fact]
    public void MainWindow_WhenNavigatedToNotes_DataTemplateMaterializesNotesViewWithoutException()
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
            services.AddSingleton<HomeViewModel>();
            services.AddSingleton<MainWindowViewModel>();

            using ServiceProvider provider = services.BuildServiceProvider();
            MainWindowViewModel mainVm = provider.GetRequiredService<MainWindowViewModel>();

            MainWindow window = new(mainVm);

            // Act: Navigate to Notes
            mainVm.NavigateNotes();
            mainVm.CurrentViewModel.Should().BeOfType<NotesViewModel>();

            // Assert: Find the DataTemplate for NotesViewModel and materialize it
            ContentControl? contentControl = FindContentControl(window);
            contentControl.Should().NotBeNull("MainWindow should have a ContentControl host for current views");

            // Look up DataTemplate in ContentControl resources
            DataTemplate? dataTemplate = contentControl!.Resources.Values.OfType<DataTemplate>()
                .FirstOrDefault(dt => dt.DataType as Type == typeof(NotesViewModel));
            dataTemplate.Should().NotBeNull("DataTemplate for NotesViewModel should exist in ContentControl");

            Action materialize = () =>
            {
                object? content = dataTemplate!.LoadContent();
                content.Should().BeOfType<NotesView>("Materialized DataTemplate content should be NotesView");
            };
            materialize.Should().NotThrow();
        });
    }

    private static ContentControl? FindContentControl(DependencyObject parent)
    {
        foreach (object child in LogicalTreeHelper.GetChildren(parent))
        {
            if (child is ContentControl cc && cc.GetType() == typeof(ContentControl))
            {
                return cc;
            }

            if (child is DependencyObject depChild)
            {
                ContentControl? result = FindContentControl(depChild);
                if (result is not null)
                {
                    return result;
                }
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
