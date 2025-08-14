using System.Windows;

namespace MemoDock.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow() { InitializeComponent(); }
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
