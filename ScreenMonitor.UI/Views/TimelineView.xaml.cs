using System.Windows;
using System.Windows.Controls;
// using removed - use full names
using WpfApp = System.Windows.Application;

namespace ScreenMonitor.UI.Views;

public partial class TimelineView : Page
{
    public TimelineView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        TimelineDatePicker.SelectedDate = DateTime.Today;
        _ = LoadData();
    }

    private async System.Threading.Tasks.Task LoadData()
    {
        try
        {
            var app = (App)WpfApp.Current;
            var date = DateOnly.FromDateTime(TimelineDatePicker.SelectedDate ?? DateTime.Today);
            var sessions = await app.Repository.GetByDateRangeAsync(date, date);

            var panel = new StackPanel();
            var bars = new Dictionary<string, List<int>>();

            foreach (var s in sessions)
            {
                if (!bars.ContainsKey(s.ProcessName))
                    bars[s.ProcessName] = new();
                var startSlot = s.StartTime.Hour * 4 + s.StartTime.Minute / 15;
                bars[s.ProcessName].Add(startSlot);
            }

            foreach (var appBar in bars)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var label = new TextBlock
                {
                    Text = appBar.Key,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 5, 0)
                };
                Grid.SetColumn(label, 0);
                row.Children.Add(label);

                var barLine = new Grid();
                for (int h = 0; h < 24; h++)
                {
                    barLine.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
                    var bg = new Border
                    {
                        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xEC, 0xF0, 0xF1)),
                        Width = 40
                    };
                    Grid.SetColumn(bg, h);
                    barLine.Children.Add(bg);
                }

                foreach (var slot in appBar.Value)
                {
                    if (slot >= 0 && slot < 96)
                    {
                        var block = new Border
                        {
                            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x52, 0xBE, 0x80)),
                            Width = 38,
                            Height = 20,
                            CornerRadius = new CornerRadius(2),
                            Margin = new Thickness(1, 2, 1, 2)
                        };
                        Grid.SetColumn(block, slot / 4);
                        barLine.Children.Add(block);
                    }
                }

                Grid.SetColumn(barLine, 1);
                row.Children.Add(barLine);
                panel.Children.Add(row);
            }

            TimelinePanel.ItemsSource = new[] { panel };
        }
        catch { }
    }

    private async void OnDateChanged(object sender, SelectionChangedEventArgs e)
        => await LoadData();
}