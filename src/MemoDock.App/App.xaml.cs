using System;
using System.Windows;
using MemoDock.App.Services;
using MemoDock.Services;
using MemoDock.App.Views;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace MemoDock.App
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                DatabaseService.Instance.Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "MemoDock couldn't initialize its local database.\n\n" + ex.Message,
                    "MemoDock – startup error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(-1);
                return;
            }

            try { SettingsService.Instance.Load(); } catch (Exception ex) { Logger.Log("Settings load failed at startup", ex); }
            try { TrayService.Instance.Ensure(); } catch (Exception ex) { Logger.Log("Tray init failed", ex); }

            try { ClipboardService.Instance.Start(); } catch (Exception ex) { Logger.Log("Clipboard start failed", ex); }

            var win = new MainWindow();
            MainWindow = win;
            win.Show();

            try { GlobalHotkeyService.Instance.RegisterDefault(); } catch (Exception ex) { Logger.Log("Global hotkey init failed", ex); }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try { GlobalHotkeyService.Instance.Dispose(); } catch { }
            try { TrayService.Instance.Dispose(); } catch { }
            base.OnExit(e);
        }
    }
}
