using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScreenMonitor.Core.Models;
using WpfApp = System.Windows.Application;

namespace ScreenMonitor.UI.Views;

public partial class DashboardView : Page
{
    private System.Windows.Threading.DispatcherTimer? _refreshTimer;
    private List<UsageItem> _allUsage = new();
    private int _currentPage = 0;
    private const int PageSize = 5;

    public DashboardView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _refreshTimer = new System.Windows.Threading.DispatcherTimer();
        _refreshTimer.Interval = TimeSpan.FromSeconds(3);
        _refreshTimer.Tick += async (s, e2) => await RefreshData();
        _refreshTimer.Start();
        _ = RefreshData();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _refreshTimer?.Stop();
        _refreshTimer = null;
    }

    private void PrevPage_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage > 0) { _currentPage--; RenderChart(); }
    }

    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage < MaxPage) { _currentPage++; RenderChart(); }
    }

    private int MaxPage => ((_allUsage.Count - 1) / PageSize);

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

            // 过滤掉忽略列表中的应用
            var ignored = app.Monitor.IgnoredProcesses;
            _allUsage = usage
                .Where(u => !ignored.Contains(u.ProcessName, StringComparer.OrdinalIgnoreCase))
                .Select(u => new UsageItem
                {
                    ProcessName = u.ProcessName,
                    TotalSeconds = u.TotalSeconds,
                    Count = u.Count
                }).ToList();

            if (_currentPage > MaxPage) _currentPage = MaxPage;
            if (_currentPage < 0) _currentPage = 0;
            RenderChart();
        }
        catch { }
    }

    private void RenderChart()
    {
        ChartPanel.Children.Clear();
        PrevBtn.Visibility = Visibility.Collapsed;
        NextBtn.Visibility = Visibility.Collapsed;
        PageInfo.Visibility = Visibility.Collapsed;

        if (_allUsage.Count == 0)
        {
            NoDataText.Visibility = Visibility.Visible;
            return;
        }
        NoDataText.Visibility = Visibility.Collapsed;

        var maxSeconds = _allUsage.Max(u => u.TotalSeconds);
        if (maxSeconds <= 0) maxSeconds = 1;

        var pageItems = _allUsage
            .Skip(_currentPage * PageSize)
            .Take(PageSize)
            .ToList();

        var colors = new[] { "#4A8EFF", "#52C41A", "#FF4D4F", "#FAAD14", "#B37FEB" };

        for (int i = 0; i < pageItems.Count; i++)
        {
            var item = pageItems[i];
            var pct = (double)item.TotalSeconds / maxSeconds;
            var globalIndex = _currentPage * PageSize + i;

            var row = new Grid { Margin = new Thickness(0, 0, 0, 8), Cursor = System.Windows.Input.Cursors.Hand };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

            var nameText = new TextBlock
            {
                Text = item.ProcessName,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13,
                TextTrimming = TextTrimming.CharacterEllipsis,
                ToolTip = item.ProcessName + " - " + FormatDuration(item.TotalSeconds),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x4A, 0x8E, 0xFF))
            };
            nameText.MouseDown += (s, e2) => NavigateToDetail(item.ProcessName);
            Grid.SetColumn(nameText, 0);
            row.Children.Add(nameText);

            var barBorder = new Border
            {
                Height = 24,
                CornerRadius = new CornerRadius(4),
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colors[globalIndex % colors.Length])),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                Width = System.Math.Max(30, pct * 500),
                Margin = new Thickness(5, 0, 0, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = item.ProcessName + " - " + FormatDuration(item.TotalSeconds)
            };
            barBorder.MouseDown += (s, e2) => NavigateToDetail(item.ProcessName);
            Grid.SetColumn(barBorder, 1);
            row.Children.Add(barBorder);

            var durText = new TextBlock
            {
                Text = FormatDuration(item.TotalSeconds),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                FontSize = 13,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x8A, 0x8C, 0xA0))
            };
            Grid.SetColumn(durText, 2);
            row.Children.Add(durText);

            ChartPanel.Children.Add(row);
        }

        var totalPages = MaxPage + 1;
        if (totalPages > 1)
        {
            PageInfo.Text = (_currentPage + 1) + " / " + totalPages;
            PageInfo.Visibility = Visibility.Visible;
            PrevBtn.Visibility = _currentPage > 0 ? Visibility.Visible : Visibility.Collapsed;
            NextBtn.Visibility = _currentPage < totalPages - 1 ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void NavigateToDetail(string processName)
    {
        NavigationService?.Navigate(new AppDetailView(processName));
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

    public class UsageItem
    {
        public string ProcessName { get; set; } = "";
        public long TotalSeconds { get; set; }
        public int Count { get; set; }
    }
}
