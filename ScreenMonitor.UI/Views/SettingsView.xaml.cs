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

        StatusText.Text = app.Monitor.IsRunning
            ? "运行中 - 正在采集数据"
            : "已暂停";

        IgnoreList.ItemsSource = app.Monitor.IgnoredProcesses;
        IgnoreList.Items.Refresh();
    }

    private void AddIgnore_Click(object sender, RoutedEventArgs e)
    {
        var name = IgnoreInput.Text.Trim();
        if (string.IsNullOrEmpty(name)) return;

        var app = (App)WpfApp.Current;
        if (!app.Monitor.IgnoredProcesses.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            app.Monitor.IgnoredProcesses.Add(name);
            IgnoreList.Items.Refresh();
            // 保存到文件
            if (app.Monitor is ScreenMonitor.Core.Services.WindowMonitorService svc)
                svc.SaveIgnoreList();
        }
        IgnoreInput.Clear();
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
