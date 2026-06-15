using System.Windows;
using System.Windows.Controls;
using ScreenMonitor.Core.Models;
using WpfApp = System.Windows.Application;
using WpfMedia = System.Windows.Media;

namespace ScreenMonitor.UI.Views;

public partial class DashboardView : Page
{
    private System.Windows.Threading.DispatcherTimer? _refreshTimer;
    private List<UsageItem> _allUsage = new();
    private int _currentPage = 0;
    private const int PageSize = 5;

    private WpfMedia.SolidColorBrush? _textMuted;
    private WpfMedia.SolidColorBrush? _textPrimary;
    private WpfMedia.SolidColorBrush? _textSecondary;
    private WpfMedia.SolidColorBrush? _bgInput;
    private WpfMedia.SolidColorBrush? _borderSoft;

    public DashboardView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void CacheBrushes()
    {
        _textMuted  = (WpfMedia.SolidColorBrush)FindResource("TextMuted");
        _textPrimary = (WpfMedia.SolidColorBrush)FindResource("TextPrimary");
        _textSecondary = (WpfMedia.SolidColorBrush)FindResource("TextSecondary");
        _bgInput    = (WpfMedia.SolidColorBrush)FindResource("BgInput");
        _borderSoft = (WpfMedia.SolidColorBrush)FindResource("BorderSoft");
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        CacheBrushes();
        TodayDateText.Text = DateTime.Now.ToString("yyyy 年 M 月 d 日 dddd");
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

    private void Settings_Click(object sender, RoutedEventArgs e)
        => NavigationService?.Navigate(new SettingsView());

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

            var totalSec = summary.TotalActiveSeconds + summary.TotalIdleSeconds;
            TotalTimeText.Text = FormatDuration(totalSec);
            ActiveTimeText.Text = FormatDuration(summary.TotalActiveSeconds);
            IdleTimeText.Text = FormatDuration(summary.TotalIdleSeconds);

            if (totalSec > 0)
            {
                ActivePctText.Text = (summary.TotalActiveSeconds * 100 / totalSec) + "%";
                IdlePctText.Text = (summary.TotalIdleSeconds * 100 / totalSec) + "%";
            }
            else
            {
                ActivePctText.Text = "";
                IdlePctText.Text = "";
            }

            var ignored = app.Monitor.IgnoredProcesses;
            _allUsage = usage
                .Where(u => !ignored.Contains(u.ProcessName, StringComparer.OrdinalIgnoreCase))
                .Select(u => new UsageItem
                {
                    ProcessName = u.ProcessName,
                    TotalSeconds = u.TotalSeconds,
                    Count = u.Count
                }).ToList();

            AppsCountText.Text = _allUsage.Count.ToString();

            if (_currentPage > MaxPage) _currentPage = MaxPage;
            if (_currentPage < 0) _currentPage = 0;
            RenderChart();
        }
        catch { }
    }

    private void RenderChart()
    {
        ChartPanel.Children.Clear();

        if (_allUsage.Count == 0)
        {
            NoDataText.Visibility = Visibility.Visible;
            PaginationPanel.Visibility = Visibility.Collapsed;
            return;
        }
        NoDataText.Visibility = Visibility.Collapsed;

        var maxSeconds = _allUsage.Max(u => u.TotalSeconds);
        if (maxSeconds <= 0) maxSeconds = 1;

        var pageItems = _allUsage
            .Skip(_currentPage * PageSize)
            .Take(PageSize)
            .ToList();

        var gradBrushes = new WpfMedia.Brush[]
        {
            (WpfMedia.Brush)FindResource("GradBlue"),
            (WpfMedia.Brush)FindResource("GradGreen"),
            (WpfMedia.Brush)FindResource("GradAmber"),
            (WpfMedia.Brush)FindResource("GradRed"),
            (WpfMedia.Brush)FindResource("GradBlue"),
        };

        for (int i = 0; i < pageItems.Count; i++)
        {
            var item = pageItems[i];
            var pct = (double)item.TotalSeconds / maxSeconds;

            var row = new Grid
            {
                Margin = new Thickness(16, 6, 16, 6),
                Height = 40,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });

            var rankText = new TextBlock
            {
                Text = "#" + (_currentPage * PageSize + i + 1),
                FontSize = 10,
                Foreground = _textMuted,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            Grid.SetColumn(rankText, 0);
            row.Children.Add(rankText);

            var nameText = new TextBlock
            {
                Text = item.ProcessName,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis,
                ToolTip = item.ProcessName + " - " + FormatDuration(item.TotalSeconds),
                Foreground = _textPrimary
            };
            nameText.MouseDown += (s, e2) => NavigateToDetail(item.ProcessName);
            Grid.SetColumn(nameText, 0);
            row.Children.Add(nameText);

            var barBg = new Border
            {
                Height = 22,
                CornerRadius = new CornerRadius(6),
                Background = _bgInput,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 8, 0)
            };

            var barFill = new Border
            {
                Height = 22,
                CornerRadius = new CornerRadius(6),
                Background = gradBrushes[i % gradBrushes.Length],
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                Width = System.Math.Max(30, pct * 500),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = item.ProcessName + " - " + FormatDuration(item.TotalSeconds)
            };
            barFill.MouseDown += (s, e2) => NavigateToDetail(item.ProcessName);
            barBg.Child = barFill;

            Grid.SetColumn(barBg, 1);
            row.Children.Add(barBg);

            var durText = new TextBlock
            {
                Text = FormatDuration(item.TotalSeconds),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = _textSecondary
            };
            Grid.SetColumn(durText, 2);
            row.Children.Add(durText);

            if (i < pageItems.Count - 1)
            {
                var sep = new Border
                {
                    Height = 1,
                    Background = _borderSoft,
                    Margin = new Thickness(16, 0, 16, 0)
                };
                ChartPanel.Children.Add(sep);
            }

            ChartPanel.Children.Add(row);
        }

        var totalPages = MaxPage + 1;
        if (totalPages > 1)
        {
            PaginationPanel.Visibility = Visibility.Visible;
            PageInfo.Text = (_currentPage + 1) + " / " + totalPages;
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