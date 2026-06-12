using System.Windows;
using System.Windows.Controls;
using WpfApp = System.Windows.Application;

namespace ScreenMonitor.UI.Views;

public partial class AppDetailView : Page
{
    private readonly string _processName;

    public AppDetailView(string processName)
    {
        InitializeComponent();
        _processName = processName;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) => await LoadData();

    private void Back_Click(object sender, RoutedEventArgs e) => NavigationService?.GoBack();

    private async System.Threading.Tasks.Task LoadData()
    {
        try
        {
            var app = (App)WpfApp.Current;
            var now = DateTime.Now;
            var from = now.AddHours(-24);

            AppNameText.Text = _processName;

            var fromDate = DateOnly.FromDateTime(from);
            var toDate = DateOnly.FromDateTime(now);
            var sessions = await app.Repository.GetByDateRangeAsync(fromDate, toDate);

            var appSessions = sessions
                .Where(s => s.ProcessName.Equals(_processName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (appSessions.Count == 0)
            {
                NoDataText.Visibility = Visibility.Visible;
                TotalTimeText.Text = "总使用时间：0 分钟";
                return;
            }

            NoDataText.Visibility = Visibility.Collapsed;

            var totalSeconds = appSessions.Sum(s => s.DurationSeconds);
            TotalTimeText.Text = "总使用时间：" + FormatDuration(totalSeconds);

            // 修复：从当前小时往前推 23 小时，确保第 24 个 slot 覆盖当前小时
            var currentHourStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);
            var fromHour = currentHourStart.AddHours(-23);
            var hourlySeconds = new long[24];

            foreach (var session in appSessions)
            {
                var segStart = session.StartTime > fromHour ? session.StartTime : fromHour;
                var segEnd = session.EndTime;

                while (segStart < segEnd)
                {
                    var slotEnd = new DateTime(segStart.Year, segStart.Month, segStart.Day, segStart.Hour, 0, 0).AddHours(1);
                    var thisEnd = segEnd < slotEnd ? segEnd : slotEnd;
                    var sec = (long)(thisEnd - segStart).TotalSeconds;
                    if (sec < 0) sec = 0;

                    var idx = (int)((segStart - fromHour).TotalHours);
                    if (idx >= 0 && idx < 24)
                        hourlySeconds[idx] += sec;

                    segStart = thisEnd;
                }
            }

            RenderChart(hourlySeconds, fromHour);
        }
        catch (Exception ex)
        {
            NoDataText.Text = "加载数据失败：" + ex.Message;
            NoDataText.Visibility = Visibility.Visible;
        }
    }

    private void RenderChart(long[] hourlySeconds, DateTime fromHour)
    {
        ChartGrid.Children.Clear();
        ChartGrid.RowDefinitions.Clear();
        ChartGrid.ColumnDefinitions.Clear();
        ChartGrid.RowDefinitions.Add(new RowDefinition());
        ChartGrid.ColumnDefinitions.Add(new ColumnDefinition());

        double chartHeight = 240;
        double maxMinutes = 60.0;
        int totalSlots = 24;

        double availWidth = ChartGrid.ActualWidth > 100 ? ChartGrid.ActualWidth : 560;
        double colW = availWidth / totalSlots;

        // 6 条水平参考线（每 10 分钟一条）
        for (int m = 10; m <= 60; m += 10)
        {
            double ratio = m / maxMinutes;
            double y = chartHeight * (1 - ratio);
            var line = new Border
            {
                Height = 1,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                Margin = new Thickness(0, y, 0, 0),
                Background = new System.Windows.Media.SolidColorBrush(
                    m % 30 == 0
                        ? System.Windows.Media.Color.FromRgb(0x3A, 0x3A, 0x5C)
                        : System.Windows.Media.Color.FromRgb(0x2A, 0x2A, 0x48))
            };
            ChartGrid.Children.Add(line);
        }

        // Y 轴刻度（0, 15, 30, 45, 60）
        var yLabels = new[] {
            new { Text = "60", Top = 0.0 },
            new { Text = "45", Top = chartHeight * 0.25 },
            new { Text = "30", Top = chartHeight * 0.5 },
            new { Text = "15", Top = chartHeight * 0.75 },
            new { Text = "0", Top = chartHeight - 14.0 }
        };
        foreach (var yl in yLabels)
        {
            var lbl = new TextBlock
            {
                Text = yl.Text + "分",
                FontSize = 9,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x70, 0x70, 0x90)),
                Margin = new Thickness(-48, yl.Top, 0, 0),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            ChartGrid.Children.Add(lbl);
        }

        // 柱子颜色
        var clrNormal = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3B, 0x82, 0xF6));
        var clrEmpty = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1A, 0x1A, 0x30));

        double maxBarH = chartHeight;

        for (int i = 0; i < totalSlots; i++)
        {
            var usedMin = hourlySeconds[i] / 60.0;
            var barH = Math.Min((usedMin / maxMinutes) * maxBarH, maxBarH);
            if (barH < 0) barH = 0;
            if (barH < 1.5 && hourlySeconds[i] > 0) barH = 2;

            var brush = hourlySeconds[i] == 0 ? clrEmpty : clrNormal;

            var bar = new Border
            {
                Width = Math.Max(colW - 2, 2),
                Height = barH,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                CornerRadius = new CornerRadius(2, 2, 0, 0),
                Margin = new Thickness(i * colW + 1, 0, 0, 0),
                Background = brush,
                ToolTip = string.Format("{0}:00 - {1}",
                    fromHour.AddHours(i).Hour.ToString("00"),
                    FormatDuration(hourlySeconds[i]))
            };
            ChartGrid.Children.Add(bar);
        }

        // X 轴标签（每 2 小时标一个）
        XAxisPanel.Children.Clear();
        XAxisPanel.Width = availWidth;
        for (int i = 0; i < totalSlots; i += 2)
        {
            var hour = fromHour.AddHours(i).Hour;
            var lbl = new TextBlock
            {
                Text = hour.ToString("00") + ":00",
                FontSize = 9,
                Width = colW * 2,
                TextAlignment = TextAlignment.Center,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x70, 0x70, 0x90))
            };
            XAxisPanel.Children.Add(lbl);
        }
    }

    private static string FormatDuration(long seconds)
    {
        if (seconds < 60) return seconds + " 秒";
        var mins = seconds / 60;
        var secs = seconds % 60;
        if (mins < 60) return mins + " 分 " + secs + " 秒";
        var h = mins / 60;
        var m = mins % 60;
        return h + " 时 " + m + " 分";
    }
}
