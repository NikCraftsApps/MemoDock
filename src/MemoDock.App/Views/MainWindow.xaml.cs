using System.ComponentModel;
using System.Windows;
using MemoDock.App.Services;

namespace MemoDock.App.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Closing += OnClosingToTray;
        }

        private void OnClosingToTray(object? sender, CancelEventArgs e)
        {
            if (!TrayService.IsExitRequested)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            var w = new MemoDock.Views.SettingsWindow { Owner = this };
            w.ShowDialog();
        }
    }
}
