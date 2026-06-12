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
            ? "Running - collecting data"
            : "Paused";

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
            WpfMsgBox.Show("Cleaned up " + deleted + " old records.", "Data Retention",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            WpfMsgBox.Show("Please enter a valid number of days.", "Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}