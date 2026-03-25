using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WinForms = System.Windows.Forms;

namespace winAlert.Notifications;

/// <summary>
/// Native Windows tray icon implementation using Shell_NotifyIcon API.
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private const int NIM_ADD = 0x00000000;
    private const int NIM_MODIFY = 0x00000001;
    private const int NIM_DELETE = 0x00000002;
    private const int NIF_MESSAGE = 0x00000001;
    private const int NIF_ICON = 0x00000002;
    private const int NIF_TIP = 0x00000004;
    private const int WM_LBUTTONDBLCLK = 0x0000203;
    private const int WM_RBUTTONUP = 0x0000205;

    private NotifyIcon? _notifyIcon;
    private WinForms.ContextMenuStrip? _contextMenu;
    private bool _disposed;

    public event Action? ShowWindowRequested;
    public event Action? ExitRequested;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA pnid);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
    }

    public void Initialize(string tooltip)
    {
        if (_disposed) return;

        _contextMenu = new WinForms.ContextMenuStrip();
        
        var showItem = new WinForms.ToolStripMenuItem { Text = "Show winAlert" };
        showItem.Click += (_, _) => ShowWindowRequested?.Invoke();
        _contextMenu.Items.Add(showItem);
        
        _contextMenu.Items.Add(new WinForms.ToolStripSeparator());
        
        var exitItem = new WinForms.ToolStripMenuItem { Text = "Exit" };
        exitItem.Click += (_, _) => ExitRequested?.Invoke();
        _contextMenu.Items.Add(exitItem);

        // Load icon from resource stream using pack URI
        Icon? icon = null;
        try
        {
            var resourceUri = new Uri("pack://application:,,,/winAlert;component/Resources/Icons/app.ico");
            var streamInfo = System.Windows.Application.GetResourceStream(resourceUri);
            if (streamInfo != null)
            {
                icon = new Icon(streamInfo.Stream);
            }
        }
        catch
        {
            // Fallback to extracting from exe
        }

        if (icon == null)
        {
            try
            {
                var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                if (exePath.EndsWith(".dll"))
                {
                    exePath = exePath.Replace(".dll", ".exe");
                }
                icon = Icon.ExtractAssociatedIcon(exePath);
            }
            catch
            {
                icon = SystemIcons.Application;
            }
        }

        // Use Windows Forms NotifyIcon which is reliable
        _notifyIcon = new NotifyIcon
        {
            Text = tooltip.Length > 63 ? tooltip.Substring(0, 63) : tooltip,
            Visible = true,
            Icon = icon ?? SystemIcons.Application,
            ContextMenuStrip = _contextMenu
        };
        
        _notifyIcon.DoubleClick += (_, _) => ShowWindowRequested?.Invoke();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
        
        if (_contextMenu != null)
        {
            _contextMenu.Dispose(); // ContextMenuStrip has Dispose
            _contextMenu = null;
        }
    }
}
