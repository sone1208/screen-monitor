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
                ChartGrid.Visibility = Visibility.Collapsed;
                XAxisPanel.Visibility = Visibility.Collapsed;
                TotalTimeText.Text = "总使用时间：0 分钟";
                return;
            }

            var totalSeconds = appSessions.Sum(s => s.DurationSeconds);
            TotalTimeText.Text = "总使用时间：" + FormatDuration(totalSeconds);

            // 按小时统计
            var fromHour = new DateTime(from.Year, from.Month, from.Day, from.Hour, 0, 0);
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

        double chartHeight = 260;
        double maxMinutes = 60.0;
        int totalSlots = 24;
        double colWidth = 600.0 / totalSlots;

        // 参考线 45分、30分、15分
        for (int i = 1; i <= 3; i++)
        {
            var line = new Border
            {
                Height = 1,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, chartHeight * i / 4),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2A, 0x2C, 0x4A))
            };
            ChartGrid.Children.Add(line);
        }

        // 柱子
        for (int i = 0; i < totalSlots; i++)
        {
            var minUsed = hourlySeconds[i] / 60.0;
            var barH = (minUsed / maxMinutes) * chartHeight;
            if (barH < 0) barH = 0;
            if (barH > chartHeight) barH = chartHeight;
            if (barH < 1 && hourlySeconds[i] > 0) barH = 2;

            System.Windows.Media.Brush barColor;
            if (hourlySeconds[i] == 0)
                barColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1C, 0x1E, 0x3A));
            else if (minUsed >= 45)
                barColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x4D, 0x4F));
            else if (minUsed >= 15)
                barColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFA, 0xAD, 0x14));
            else
                barColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x4A, 0x8E, 0xFF));

            var bar = new Border
            {
                Width = colWidth - 4,
                Height = barH,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                CornerRadius = new CornerRadius(3, 3, 0, 0),
                Margin = new Thickness(i * colWidth + 2, 0, 0, 0),
                Background = barColor,
                ToolTip = string.Format("{0}:00 - {1}",
                    fromHour.AddHours(i).Hour.ToString("00"),
                    FormatDuration(hourlySeconds[i]))
            };
            ChartGrid.Children.Add(bar);
        }

        // X轴标签
        XAxisPanel.Children.Clear();
        for (int i = 0; i < totalSlots; i += 2)
        {
            var hour = fromHour.AddHours(i).Hour;
            var lbl = new TextBlock
            {
                Text = hour.ToString("00") + ":00",
                FontSize = 9,
                Width = colWidth * 2,
                TextAlignment = TextAlignment.Center,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x50, 0x52, 0x70))
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

