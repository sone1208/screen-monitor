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

        _trayIcon = new NotifyIcon();
        _trayIcon.Text = "Screen Monitor";
        _trayIcon.Icon = CreateAppIcon();
        _trayIcon.Visible = true;

        var menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, (s, e2) => ShowMainWindow());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Pause/Resume", null, (s, e2) => ToggleMonitor());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (s, e2) => ExitApp());
        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (s, e2) => ShowMainWindow();

        _mainWindow = new MainWindow();
        _mainWindow.Closing += (s, e2) =>
        {
            e2.Cancel = true;
            _mainWindow.Hide();
            _trayIcon.ShowBalloonTip(1000, "Screen Monitor",
                "Minimized to tray, monitoring continues...",
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
            _trayIcon!.ShowBalloonTip(1000, "Screen Monitor", "Monitoring paused", ToolTipIcon.Info);
        }
        else
        {
            _monitor.Start();
            _trayIcon!.ShowBalloonTip(1000, "Screen Monitor", "Monitoring resumed", ToolTipIcon.Info);
        }
    }

    private async void ExitApp()
    {
        if (_aggregation != null)
            await _aggregation.CloseAllActiveSessionsAsync();
        (_monitor as IDisposable)?.Dispose();
        _trayIcon?.Dispose();
        _mainWindow?.Close();
        Shutdown();
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