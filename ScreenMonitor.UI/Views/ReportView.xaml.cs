using System.Windows;
using System.Windows.Controls;
using WpfApp = System.Windows.Application;
using WpfMsgBox = System.Windows.MessageBox;

namespace ScreenMonitor.UI.Views;

public partial class ReportView : Page
{
    public ReportView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        ReportDatePicker.SelectedDate = DateTime.Today;
        await LoadData();
    }

    private async System.Threading.Tasks.Task LoadData()
    {
        try
        {
            var app = (App)WpfApp.Current;
            var date = DateOnly.FromDateTime(ReportDatePicker.SelectedDate ?? DateTime.Today);

            var summary = await app.Aggregation.GenerateDailySummaryAsync(date);
            var sessions = await app.Repository.GetByDateRangeAsync(date, date);

            RptActiveText.Text = FormatDuration(summary.TotalActiveSeconds);
            RptSessionsText.Text = sessions.Count.ToString();

            SessionList.ItemsSource = sessions.Select(s => new
            {
                s.ProcessName,
                s.WindowTitle,
                StartTime = s.StartTime.ToString("HH:mm:ss"),
                DurationText = FormatDuration(s.DurationSeconds)
            }).ToList();
        }
        catch { }
    }

    private async void OnDateChanged(object sender, SelectionChangedEventArgs e)
        => await LoadData();

    private async void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            FileName = "screen-monitor-" + DateTime.Today.ToString("yyyy-MM-dd") + ".csv"
        };
        if (dialog.ShowDialog() == true)
        {
            var app = (App)WpfApp.Current;
            var date = DateOnly.FromDateTime(ReportDatePicker.SelectedDate ?? DateTime.Today);
            await app.Aggregation.ExportToCsvAsync(dialog.FileName, date, date);
            WpfMsgBox.Show("CSV exported successfully.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void ExportJson_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            FileName = "screen-monitor-" + DateTime.Today.ToString("yyyy-MM-dd") + ".json"
        };
        if (dialog.ShowDialog() == true)
        {
            var app = (App)WpfApp.Current;
            var date = DateOnly.FromDateTime(ReportDatePicker.SelectedDate ?? DateTime.Today);
            await app.Aggregation.ExportToJsonAsync(dialog.FileName, date, date);
            WpfMsgBox.Show("JSON exported successfully.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
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