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
                ChartContainer.Visibility = Visibility.Collapsed;
                XAxisPanel.Visibility = Visibility.Collapsed;
                TotalTimeText.Text = "总使用时间：0 分钟";
                return;
            }

            var totalSeconds = appSessions.Sum(s => s.DurationSeconds);
            TotalTimeText.Text = "总使用时间：" + FormatDuration(totalSeconds);

            // 按小时统计总使用秒数
            var hourlySeconds = new long[24];
            var fromHour = from.Hour; // 起始小时

            foreach (var session in appSessions)
            {
                var start = session.StartTime;
                var end = session.EndTime;

                // 跨小时的会话需要拆分到各小时
                var current = start;
                while (current < end)
                {
                    var slotStart = new DateTime(current.Year, current.Month, current.Day, current.Hour, 0, 0);
                    var slotEnd = slotStart.AddHours(1);
                    var segEnd = end < slotEnd ? end : slotEnd;
                    var segSec = (long)(segEnd - current).TotalSeconds;
                    if (segSec < 0) segSec = 0;

                    var hourIndex = ((current.Hour - fromHour + 24) % 24);
                    hourlySeconds[hourIndex] += segSec;
                    current = segEnd;
                }
            }

            // 渲染：横向柱状条，每个小时一行
            var maxBarWidth = 500.0;
            var maxSecondsPerHour = 3600.0; // 1小时=3600秒=60分钟

            ChartContainer.Children.Clear();
            for (int i = 0; i < 24; i++)
            {
                var hour = (fromHour + i) % 24;
                var hourLabel = hour.ToString("00") + ":00";
                var secInHour = hourlySeconds[i];
                var frac = secInHour / maxSecondsPerHour;
                if (frac > 1.0) frac = 1.0;

                var row = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });

                // 小时标签
                var hourText = new TextBlock
                {
                    Text = hourLabel,
                    FontSize = 11,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x99, 0x99, 0x99))
                };
                Grid.SetColumn(hourText, 0);
                row.Children.Add(hourText);

                // 柱状条背景（灰色底）
                var barBg = new Border
                {
                    Height = 20,
                    CornerRadius = new CornerRadius(3),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xEC, 0xF0, 0xF1)),
                    Margin = new Thickness(4, 0, 4, 0)
                };
                Grid.SetColumn(barBg, 1);
                row.Children.Add(barBg);

                // 柱状条（实际用量）
                if (frac > 0)
                {
                    var bar = new Border
                    {
                        Height = 20,
                        CornerRadius = new CornerRadius(3),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                        Width = frac * maxBarWidth,
                        Background = new System.Windows.Media.SolidColorBrush(
                            secInHour >= 1800
                                ? System.Windows.Media.Color.FromRgb(0xE7, 0x4C, 0x3C) // >30分 = 红色
                                : secInHour >= 600
                                    ? System.Windows.Media.Color.FromRgb(0xF3, 0x9C, 0x12) // 10-30分 = 橙色
                                    : System.Windows.Media.Color.FromRgb(0x52, 0xBE, 0x80)), // <10分 = 绿色
                        ToolTip = hourLabel + " 使用 " + FormatDuration(secInHour)
                    };
                    Grid.SetColumn(bar, 1);
                    row.Children.Add(bar);
                }

                // 时长文本
                var durText = new TextBlock
                {
                    Text = FormatDuration(secInHour),
                    FontSize = 11,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        secInHour > 0
                            ? System.Windows.Media.Color.FromRgb(0x33, 0x33, 0x33)
                            : System.Windows.Media.Color.FromRgb(0xCC, 0xCC, 0xCC))
                };
                Grid.SetColumn(durText, 2);
                row.Children.Add(durText);

                ChartContainer.Children.Add(row);
            }

            // X轴标签（每4小时标一个）
            XAxisPanel.Children.Clear();
            for (int i = 0; i < 24; i += 4)
            {
                var hour = (fromHour + i) % 24;
                var lbl = new TextBlock
                {
                    Text = hour.ToString("00") + ":00",
                    FontSize = 10,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xBB, 0xBB, 0xBB)),
                    Margin = new Thickness(i % 4 == 0 ? 0 : 15, 0, 15, 0)
                };
                XAxisPanel.Children.Add(lbl);
            }
        }
        catch (Exception ex)
        {
            NoDataText.Text = "加载数据失败：" + ex.Message;
            NoDataText.Visibility = Visibility.Visible;
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
