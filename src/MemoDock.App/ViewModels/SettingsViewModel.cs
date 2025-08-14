using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoDock.Services;

namespace MemoDock.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        public AppSettings S => SettingsService.Instance.Settings;

        [RelayCommand]
        private void BrowseBackupFolder()
        {
            using var dlg = new System.Windows.Forms.FolderBrowserDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                S.BackupFolder = dlg.SelectedPath;
                OnPropertyChanged(nameof(S));
            }
        }

        [RelayCommand]
        private void Save()
        {
            SettingsService.Instance.Save();
            System.Windows.MessageBox.Show("Zapisano ustawienia.", "OK",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        [RelayCommand]
        private async Task ExportNow()
        {
            if (string.IsNullOrWhiteSpace(S.BackupFolder))
            {
                System.Windows.MessageBox.Show("Wybierz folder kopii.", "Uwaga",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            try
            {
                await Task.Run(() => ExportService.ExportAll(S.BackupFolder!));
                System.Windows.MessageBox.Show("Eksport zakończony.", "OK",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                Logger.Log("Export failed", ex);
                System.Windows.MessageBox.Show("Błąd eksportu. Szczegóły w logu.",
                    "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task ImportNow()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "MemoDock export (clips.json)|clips.json|JSON (*.json)|*.json|All files (*.*)|*.*"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                await Task.Run(() => ExportService.ImportAll(dlg.FileName));
                ClipboardService.Instance.ForceRefresh(); 
                System.Windows.MessageBox.Show("Import zakończony.", "OK",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                Logger.Log("Import failed", ex);
                System.Windows.MessageBox.Show("Błąd importu. Szczegóły w logu.",
                    "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
