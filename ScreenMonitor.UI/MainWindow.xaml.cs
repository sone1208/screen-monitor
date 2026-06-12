using System.Windows;
using System.Windows.Controls;
using ScreenMonitor.UI.Views;

namespace ScreenMonitor.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ContentFrame.Navigate(new DashboardView());
    }

    private void Dashboard_Click(object sender, RoutedEventArgs e)
        => ContentFrame.Navigate(new DashboardView());

    private void Reports_Click(object sender, RoutedEventArgs e)
        => ContentFrame.Navigate(new ReportView());

    private void Timeline_Click(object sender, RoutedEventArgs e)
        => ContentFrame.Navigate(new TimelineView());

    private void Settings_Click(object sender, RoutedEventArgs e)
        => ContentFrame.Navigate(new SettingsView());
}