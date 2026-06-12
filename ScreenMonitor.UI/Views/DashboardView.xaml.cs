using System.Windows;
using System.Windows.Controls;
// using removed - use full names
using ScreenMonitor.Core.Models;
// resolve WinForms conflict
using WpfApp = System.Windows.Application;

namespace ScreenMonitor.UI.Views;

public partial class DashboardView : Page
{
    public DashboardView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await RefreshData();
    }

    private async System.Threading.Tasks.Task RefreshData()
    {
        try
        {
            var app = (App)WpfApp.Current;
            var today = DateOnly.FromDateTime(DateTime.Now);

            var summary = await app.Aggregation.GenerateDailySummaryAsync(today);
            var usage = await app.Repository.GetUsageSummaryAsync(today);

            TotalTimeText.Text = FormatDuration(summary.TotalActiveSeconds + summary.TotalIdleSeconds);
            ActiveTimeText.Text = FormatDuration(summary.TotalActiveSeconds);
            IdleTimeText.Text = FormatDuration(summary.TotalIdleSeconds);
            AppsCountText.Text = usage.Count.ToString();

            ChartPanel.Children.Clear();
            if (usage.Count == 0)
            {
                NoDataText.Visibility = Visibility.Visible;
                return;
            }
            NoDataText.Visibility = Visibility.Collapsed;

            var maxSeconds = usage.Max(u => u.TotalSeconds);
            if (maxSeconds <= 0) maxSeconds = 1;

            var colors = new[] { "#3498DB", "#2ECC71", "#E74C3C", "#F39C12", "#9B59B6" };

            for (int i = 0; i < usage.Count && i < 10; i++)
            {
                var item = usage[i];
                var pct = (double)item.TotalSeconds / maxSeconds;

                var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

                var nameText = new TextBlock
                {
                    Text = item.ProcessName,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 13,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x44, 0x44, 0x44))
                };
                Grid.SetColumn(nameText, 0);
                row.Children.Add(nameText);

                var barBorder = new Border
                {
                    Height = 24,
                    CornerRadius = new CornerRadius(4),
                    Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colors[i % colors.Length])),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                    Width = System.Math.Max(30, pct * 500),
                    Margin = new Thickness(5, 0, 0, 0)
                };
                Grid.SetColumn(barBorder, 1);
                row.Children.Add(barBorder);

                var durText = new TextBlock
                {
                    Text = FormatDuration(item.TotalSeconds),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                    FontSize = 13,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x66, 0x66, 0x66))
                };
                Grid.SetColumn(durText, 2);
                row.Children.Add(durText);

                ChartPanel.Children.Add(row);
            }
        }
        catch { }
    }

    private static string FormatDuration(long seconds)
    {
        if (seconds < 60) return seconds + "s";
        if (seconds < 3600) return (seconds / 60) + "m " + (seconds % 60) + "s";
        var h = seconds / 3600;
        var m = (seconds % 3600) / 60;
        return h + "h " + m + "m";
    }
}