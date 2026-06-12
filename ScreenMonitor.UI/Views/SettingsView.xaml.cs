using System.Windows;
using System.Windows.Controls;
using WpfApp = System.Windows.Application;
using WpfMsgBox = System.Windows.MessageBox;

namespace ScreenMonitor.UI.Views;

public partial class SettingsView : Page
{
    public SettingsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var app = (App)WpfApp.Current;

        DataPathText.Text = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.json"));

        if (app.Monitor.IsRunning)
        {
            StatusText.Text = "运行中 - 正在采集数据";
            StatusDot.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x22, 0xC5, 0x5E));
            StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x22, 0xC5, 0x5E));
        }
        else
        {
            StatusText.Text = "已暂停";
            StatusDot.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x70, 0x70, 0x90));
            StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x70, 0x70, 0x90));
        }

        IgnoreList.ItemsSource = app.Monitor.IgnoredProcesses;
    }

    private void Back_Click(object sender, RoutedEventArgs e)
        => NavigationService?.GoBack();

    private void AddIgnore_Click(object sender, RoutedEventArgs e)
    {
        var name = IgnoreInput.Text.Trim();
        if (string.IsNullOrEmpty(name)) return;

        var app = (App)WpfApp.Current;
        if (!app.Monitor.IgnoredProcesses.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            app.Monitor.IgnoredProcesses.Add(name);
            IgnoreList.Items.Refresh();
            if (app.Monitor is ScreenMonitor.Core.Services.WindowMonitorService svc)
                svc.SaveIgnoreList();
        }
        IgnoreInput.Clear();
    }

    private void RemoveIgnore_Click(object sender, RoutedEventArgs e)
    {
        var btn = (System.Windows.Controls.Button)sender;
        var processName = btn.DataContext as string;
        if (string.IsNullOrEmpty(processName)) return;

        var app = (App)WpfApp.Current;
        app.Monitor.IgnoredProcesses.Remove(processName);
        IgnoreList.Items.Refresh();
        if (app.Monitor is ScreenMonitor.Core.Services.WindowMonitorService svc)
            svc.SaveIgnoreList();
    }

    private async void ApplyRetention_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(RetentionInput.Text, out int days) && days > 0)
        {
            var app = (App)WpfApp.Current;
            var deleted = await app.Aggregation.CleanupOldDataAsync(days);
            WpfMsgBox.Show("已清理 " + deleted + " 条过期记录。", "数据保留",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            WpfMsgBox.Show("请输入有效的天数。", "错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
