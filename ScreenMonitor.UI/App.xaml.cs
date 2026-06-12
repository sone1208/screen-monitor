using System.Diagnostics;
using System.Drawing;
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

    public ISessionRepository Repository => _repository!;
    public IWindowMonitorService Monitor => _monitor!;
    public IIdleDetectionService IdleDetector => _idleDetector!;
    public IDataAggregationService Aggregation => _aggregation!;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
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
            _trayIcon.ShowBalloonTip(1000, "屏幕监控",
                "已最小化到托盘，监控继续中...",
                ToolTipIcon.Info);
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

    // 同步退出，避免 async void 导致消息循环残留
    private void ExitApp()
    {
        _isExiting = true;

        // 1. 停止监控并保存数据
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

        // 2. 销毁托盘图标（必须，否则 WinForms 消息循环会阻止退出）
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        // 3. 关闭主窗口
        try { _mainWindow?.Close(); } catch { }

        // 4. 关闭 WPF 调度器
        try { System.Windows.Application.Current.Shutdown(); } catch { }

        // 5. 强制终止当前进程，确保没有任何残留
        try { Process.GetCurrentProcess().Kill(); } catch { }
    }

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

