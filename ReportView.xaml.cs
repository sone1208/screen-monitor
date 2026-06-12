using System.Windows;
using System.Windows.Controls;
using ScreenMonitor.Core.Models;

// 解决 WinForms 和 WPF 命名空间冲突
using WpfApp = System.Windows.Application;
using WpfMessageBox = System.Windows.MessageBox;
using MediaColor = System.Windows.Media.Color;
using MediaColorConverter = System.Windows.Media.ColorConverter;
navtive namespace ScreenMonitor.UI.Views;

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
            ? "Running - collecting data"
            : "Paused";

        // Load ignored processes
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
        }
        IgnoreInput.Clear();
    }

    private async void ApplyRetention_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(RetentionInput.Text, out int days) && days > 0)
        {
            var app = (App)WpfApp.Current;
            var deleted = await app.Aggregation.CleanupOldDataAsync(days);
            WpfMessageBox.Show($"Cleaned up {deleted} old records.", "Data Retention",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            WpfMessageBox.Show("Please enter a valid number of days.", "Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}