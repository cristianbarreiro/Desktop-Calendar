using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CalendarWidget.App.Windows;
using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Interfaces;
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
public sealed class NotesViewRegressionTests
{
    private static readonly System.Collections.Concurrent.BlockingCollection<Action> StaQueue = new();

    static NotesViewRegressionTests()
    {
        Thread staThread = new(RunStaPump)
        {
            IsBackground = true,
            Name = "NotesViewRegressionTests.STA"
        };
        staThread.SetApartmentState(ApartmentState.STA);
        staThread.Start();
    }

    private static void RunStaPump()
    {
        foreach (Action action in StaQueue.GetConsumingEnumerable())
        {
            action();
        }
    }

    private static void RunInSta(Action action)
    {
        Exception? exception = null;
        using ManualResetEventSlim completed = new();

        StaQueue.Add(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                completed.Set();
            }
        });

        completed.Wait();

        if (exception is not null)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    private static void EnsureApplicationWithTheme()
    {
        Application app = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };

        Uri themeUri = new("pack://application:,,,/CalendarWidget.Presentation;component/Resources/Theme.xaml", UriKind.Absolute);
        if (!app.Resources.MergedDictionaries.Any(d => d.Source == themeUri))
        {
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        }
    }

    [Fact]
    public void NotesView_WhenInstantiatedDirectly_InitializesWithoutException()
    {
        RunInSta(() =>
        {
            EnsureApplicationWithTheme();

            Action instantiate = () => _ = new NotesView();
            instantiate.Should().NotThrow();
        });
    }

    [Fact]
    public void MainWindow_WhenNavigatedToNotes_DataTemplateMaterializesNotesViewWithoutException()
    {
        RunInSta(() =>
        {
            EnsureApplicationWithTheme();

            ServiceCollection services = new();
            services.AddSingleton<ICalendarGridService, CalendarGridService>();
            services.AddSingleton<IClockService, SystemClockService>();
            services.AddSingleton<IWindowManager, StubWindowManager>();
            services.AddSingleton<CalendarViewModel>();
            services.AddSingleton<NotesViewModel>();
            services.AddSingleton<SettingsViewModel>();
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
