using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ScreenMonitor.Core.Data;
using ScreenMonitor.Core.Interfaces;
using ScreenMonitor.Core.Services;

namespace ScreenMonitor.UI;

public partial class App : System.Windows.Application
{
    private NotifyIcon? _trayIcon;
    private System.Windows.Window? _mainWindow;
    private IWindowMonitorService? _monitor;
    private IIdleDetectionService? _idleDetector;
    private ISessionRepository? _repository;
    private IDataAggregationService? _aggregation;
    private bool _isExiting;
    private System.Threading.Mutex? _singleInstanceMutex;

    public ISessionRepository Repository => _repository!;
    public IWindowMonitorService Monitor => _monitor!;
    public IIdleDetectionService IdleDetector => _idleDetector!;
    public IDataAggregationService Aggregation => _aggregation!;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        // 单实例检测
        _singleInstanceMutex = new System.Threading.Mutex(true, "ScreenMonitor_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            // 已有实例运行 → 把它的窗口唤起到前台
            try
            {
                var hWnd = FindWindow(null, "屏幕监控");
                if (hWnd != IntPtr.Zero)
                {
                    ShowWindowAsync(hWnd, SW_RESTORE);
                    SetForegroundWindow(hWnd);
                }
            }
            catch { }
            Environment.Exit(0);
            return;
        }

        base.OnStartup(e);

        var dbPath = System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "data.json");
        _repository = new JsonSessionRepository(dbPath);
        _idleDetector = new IdleDetectionService(300);
        _aggregation = new DataAggregationService(_repository);
        _monitor = new WindowMonitorService(_repository, _idleDetector);

        var ignorePath = System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "ignorelist.json");
        ((WindowMonitorService)_monitor).SetIgnoreFilePath(ignorePath);

        _trayIcon = new NotifyIcon();
        _trayIcon.Text = "屏幕监控";
        _trayIcon.Icon = CreateAppIcon();
        _trayIcon.Visible = true;

        var menu = new ContextMenuStrip();
        menu.Items.Add("打开", null, (s, e2) => ShowMainWindow());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("暂停/恢复", null, (s, e2) => ToggleMonitor());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (s, e2) => ExitApp());
        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (s, e2) => ShowMainWindow();

        _mainWindow = new MainWindow();
        MainWindow = _mainWindow;
        _mainWindow.Title = "屏幕监控";
        _mainWindow.WindowState = System.Windows.WindowState.Normal;
        _mainWindow.ShowInTaskbar = true;
        _mainWindow.Show();
        _mainWindow.Activate();

        _mainWindow.Closing += (s, e2) =>
        {
            if (_isExiting) return;
            e2.Cancel = true;
            _mainWindow.Hide();
        };

        _monitor.Start();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null) return;
        _mainWindow.Show();
        _mainWindow.WindowState = System.Windows.WindowState.Normal;
        _mainWindow.Activate();
    }

    private void ToggleMonitor()
    {
        if (_monitor == null) return;
        if (_monitor.IsRunning)
        {
            _monitor.Stop();
            _trayIcon!.ShowBalloonTip(1000, "屏幕监控", "监控已暂停", ToolTipIcon.Info);
        }
        else
        {
            _monitor.Start();
            _trayIcon!.ShowBalloonTip(1000, "屏幕监控", "监控已恢复", ToolTipIcon.Info);
        }
    }

    private void ExitApp()
    {
        _isExiting = true;

        try
        {
            _monitor?.Stop();
            (_monitor as IDisposable)?.Dispose();
        }
        catch { }

        try
        {
            if (_aggregation != null)
                System.Threading.Tasks.Task.Run(() => _aggregation.CloseAllActiveSessionsAsync()).GetAwaiter().GetResult();
        }
        catch { }

        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();

        try { _mainWindow?.Close(); } catch { }
        try { System.Windows.Application.Current.Shutdown(); } catch { }
        try { Process.GetCurrentProcess().Kill(); } catch { }
    }

    // Win32 API
    private const int SW_RESTORE = 9;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

    private static Icon CreateAppIcon()
    {
        using var bmp = new Bitmap(16, 16);
        using var g = System.Drawing.Graphics.FromImage(bmp);
        g.Clear(Color.DarkCyan);
        using var brush = new SolidBrush(Color.White);
        g.FillRectangle(brush, 2, 2, 5, 12);
        g.FillRectangle(brush, 9, 5, 5, 9);
        return Icon.FromHandle(bmp.GetHicon());
    }
}
