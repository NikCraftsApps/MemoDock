using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

        private void ActionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.Placement = PlacementMode.Bottom;
                btn.ContextMenu.IsOpen = true;
            }
        }
    }
}
