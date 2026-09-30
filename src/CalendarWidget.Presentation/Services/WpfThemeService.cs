using System.Windows;
using CalendarWidget.Core.Enums;

namespace CalendarWidget.Presentation.Services;

/// <summary>
/// WPF implementation of <see cref="IThemeService"/> that dynamically updates application merged dictionaries.
/// </summary>
public sealed class WpfThemeService : IThemeService
{
    private const string DarkThemeUri = "pack://application:,,,/CalendarWidget.Presentation;component/Resources/Themes/Dark.xaml";
    private const string LightThemeUri = "pack://application:,,,/CalendarWidget.Presentation;component/Resources/Themes/Light.xaml";

    private readonly ISystemThemeDetector _systemThemeDetector;
    private ResourceDictionary? _activeThemeDictionary;

    /// <summary>
    /// Initializes a new instance of the <see cref="WpfThemeService"/> class.
    /// </summary>
    /// <param name="systemThemeDetector">Detector for operating system theme.</param>
    public WpfThemeService(ISystemThemeDetector systemThemeDetector)
    {
        _systemThemeDetector = systemThemeDetector;
    }

    /// <inheritdoc />
    public AppThemeMode ActiveTheme { get; private set; } = AppThemeMode.Dark;

    /// <inheritdoc />
    public event EventHandler<AppThemeMode>? ThemeChanged;

    /// <inheritdoc />
    public void ApplyTheme(AppThemeMode theme)
    {
        ActiveTheme = theme;

        bool useDark = theme switch
        {
            AppThemeMode.Dark => true,
            AppThemeMode.Light => false,
            AppThemeMode.System => _systemThemeDetector.IsSystemInDarkTheme(),
            _ => true,
        };

        string themeUri = useDark ? DarkThemeUri : LightThemeUri;

        if (Application.Current is null)
        {
            ThemeChanged?.Invoke(this, theme);
            return;
        }

        ExecuteOnDispatcher(() =>
        {
            ResourceDictionary newDictionary = new()
            {
                Source = new Uri(themeUri, UriKind.RelativeOrAbsolute),
            };

            if (_activeThemeDictionary is not null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(_activeThemeDictionary);
            }

            _activeThemeDictionary = newDictionary;
            Application.Current.Resources.MergedDictionaries.Add(_activeThemeDictionary);

            ThemeChanged?.Invoke(this, theme);
        });
    }

    private static void ExecuteOnDispatcher(Action action)
    {
        if (Application.Current is null)
        {
            action();
            return;
        }

        if (Application.Current.Dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            Application.Current.Dispatcher.Invoke(action);
        }
    }
}
