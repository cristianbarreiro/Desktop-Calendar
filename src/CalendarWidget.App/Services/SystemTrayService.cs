using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CalendarWidget.Presentation.Services;

namespace CalendarWidget.App.Services;

/// <summary>
/// Manages the Windows notification area (System Tray) icon, context menu actions, and minimize-to-tray lifecycle.
/// </summary>
public sealed class SystemTrayService : ITrayService
{
    private readonly IWindowManager _windowManager;
    private readonly ApplicationLifetimeService _lifetimeService;
    private readonly Lock _lock = new();

    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;
    private Icon? _trayIcon;
    private bool _isInitialized;
    private bool _disposed;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemTrayService"/> class.
    /// </summary>
    /// <param name="windowManager">Window orchestration service.</param>
    /// <param name="lifetimeService">Application lifetime coordination service.</param>
    public SystemTrayService(
        IWindowManager windowManager,
        ApplicationLifetimeService lifetimeService)
    {
        _windowManager = windowManager;
        _lifetimeService = lifetimeService;
    }

    /// <inheritdoc />
    public bool IsVisible => _notifyIcon is not null && _notifyIcon.Visible;

    /// <inheritdoc />
    public void Initialize()
    {
        lock (_lock)
        {
            if (_isInitialized || _disposed)
            {
                return;
            }

            _trayIcon = CreateCalendarIcon();
            _contextMenu = CreateContextMenu();

            _notifyIcon = new NotifyIcon
            {
                Text = "Desktop Calendar",
                Icon = _trayIcon,
                ContextMenuStrip = _contextMenu,
                Visible = true,
            };

            _notifyIcon.MouseClick += OnTrayIconMouseClick;
            _notifyIcon.DoubleClick += OnTrayIconDoubleClick;

            _isInitialized = true;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_notifyIcon is not null)
            {
                _notifyIcon.MouseClick -= OnTrayIconMouseClick;
                _notifyIcon.DoubleClick -= OnTrayIconDoubleClick;
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }

            if (_contextMenu is not null)
            {
                _contextMenu.Dispose();
                _contextMenu = null;
            }

            if (_trayIcon is not null)
            {
                _trayIcon.Dispose();
                _trayIcon = null;
            }
        }
    }

    private void OnTrayIconMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ActivatePreferredWindow();
        }
    }

    private void OnTrayIconDoubleClick(object? sender, EventArgs e)
    {
        ActivatePreferredWindow();
    }

    private void ActivatePreferredWindow()
    {
        if (_windowManager.IsFullApplicationVisible)
        {
            _windowManager.ShowFullApplication();
        }
        else
        {
            _windowManager.ShowWidget();
        }
    }

    private ContextMenuStrip CreateContextMenu()
    {
        ContextMenuStrip menu = new();

        ToolStripMenuItem openAppItem = new("Open Application", null, (s, e) => _windowManager.ShowFullApplication());
        ToolStripMenuItem openWidgetItem = new("Open Widget", null, (s, e) => _windowManager.ShowWidget());
        ToolStripSeparator separator = new();
        ToolStripMenuItem exitItem = new("Exit", null, (s, e) => _lifetimeService.Shutdown());

        menu.Items.Add(openAppItem);
        menu.Items.Add(openWidgetItem);
        menu.Items.Add(separator);
        menu.Items.Add(exitItem);

        return menu;
    }

    private static Icon CreateCalendarIcon()
    {
        try
        {
            using Bitmap bitmap = new(16, 16);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                using SolidBrush bgBrush = new(Color.FromArgb(245, 245, 245));
                using Pen borderPen = new(Color.FromArgb(50, 50, 50), 1);
                using SolidBrush headerBrush = new(Color.FromArgb(37, 99, 235)); // Modern royal blue accent

                g.FillRectangle(bgBrush, 1, 2, 14, 12);
                g.DrawRectangle(borderPen, 1, 2, 13, 12);
                g.FillRectangle(headerBrush, 1, 2, 13, 4);

                using SolidBrush dotBrush = new(Color.FromArgb(70, 70, 70));
                g.FillRectangle(dotBrush, 4, 8, 2, 2);
                g.FillRectangle(dotBrush, 8, 8, 2, 2);
                g.FillRectangle(dotBrush, 11, 8, 2, 2);
                g.FillRectangle(dotBrush, 4, 11, 2, 2);
                g.FillRectangle(dotBrush, 8, 11, 2, 2);
            }

            IntPtr hIcon = bitmap.GetHicon();
            try
            {
                using Icon tempIcon = Icon.FromHandle(hIcon);
                return (Icon)tempIcon.Clone();
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }
        catch
        {
            return (Icon)SystemIcons.Application.Clone();
        }
    }
}
