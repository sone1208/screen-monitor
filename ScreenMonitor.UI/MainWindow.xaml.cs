using System.Windows;
using ScreenMonitor.UI.Views;

namespace ScreenMonitor.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ContentFrame.Navigate(new DashboardView());
    }
}
