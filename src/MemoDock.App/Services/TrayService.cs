using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using MemoDock.Services;
using MemoDock.Views;
using WpfApp = System.Windows.Application;

namespace MemoDock.App.Services
{
    public sealed class TrayService : IDisposable
    {
        public static TrayService Instance { get; } = new TrayService();
        public static bool IsExitRequested { get; private set; }

        private NotifyIcon? _tray;
        private ToolStripMenuItem? _pauseItem;

        public void Ensure()
        {
            if (_tray != null) return;

            var trayPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Tray.ico");
            Icon icon = File.Exists(trayPath) ? new Icon(trayPath) : SystemIcons.Application;

            var menu = new ContextMenuStrip();

            menu.Items.Add("Open", null, (s, e) => Dispatch(ShowMain));
            menu.Items.Add("Pinned", null, (s, e) => Dispatch(ShowPinned));
            menu.Items.Add(new ToolStripSeparator());

            bool pausedNow = ClipboardService.Instance.IsPaused
                             || !SettingsService.Instance.Settings.ListenEnabled;

            _pauseItem = new ToolStripMenuItem(pausedNow ? "Resume listening" : "Pause listening")
            {
                Checked = pausedNow,
                CheckOnClick = false
            };
            _pauseItem.Click += (s, e) => Dispatch(TogglePause);
            menu.Items.Add(_pauseItem);

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Settings", null, (s, e) => Dispatch(ShowSettings));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (s, e) => Dispatch(ExitApp));

            _tray = new NotifyIcon
            {
                Icon = icon,
                Visible = true,
                Text = "MemoDock",
                ContextMenuStrip = menu
            };

            _tray.Click += (s, e) =>
            {
                if (e is MouseEventArgs me && me.Button == MouseButtons.Left)
                    Dispatch(ShowMain);
            };
            _tray.DoubleClick += (s, e) => Dispatch(ShowMain);
        }

        private static void Dispatch(Action action)
            => WpfApp.Current?.Dispatcher?.Invoke(action);

        private void TogglePause()
        {
            bool newPaused = !ClipboardService.Instance.IsPaused;
            ClipboardService.Instance.IsPaused = newPaused;

            SettingsService.Instance.Settings.ListenEnabled = !newPaused;
            SettingsService.Instance.Save();

            if (_pauseItem != null)
            {
                _pauseItem.Checked = newPaused;
                _pauseItem.Text = newPaused ? "Resume listening" : "Pause listening";
            }

            _tray?.ShowBalloonTip(
                1000, "MemoDock",
                newPaused ? "Listening paused" : "Listening resumed",
                ToolTipIcon.Info);
        }

        private void ShowMain()
        {
            if (WpfApp.Current.MainWindow is not MemoDock.App.Views.MainWindow w)
            {
                w = new MemoDock.App.Views.MainWindow();
                WpfApp.Current.MainWindow = w;
            }
            if (!w.IsVisible) w.Show();
            if (w.WindowState == WindowState.Minimized) w.WindowState = WindowState.Normal;
            w.Activate();
        }

        private void ShowPinned()
        {
            foreach (Window w in WpfApp.Current.Windows)
            {
                if (w is PinnedWindow l)
                {
                    if (!l.IsVisible) l.Show();
                    l.Activate();
                    return;
                }
            }
            var win = new PinnedWindow { Owner = WpfApp.Current.MainWindow };
            win.Show();
        }

        private void ShowSettings()
        {
            var win = new SettingsWindow { Owner = WpfApp.Current.MainWindow };
            win.ShowDialog();

            if (_pauseItem != null)
            {
                bool pausedNow = ClipboardService.Instance.IsPaused
                                 || !SettingsService.Instance.Settings.ListenEnabled;
                _pauseItem.Checked = pausedNow;
                _pauseItem.Text = pausedNow ? "Resume listening" : "Pause listening";
            }
        }

        private void ExitApp()
        {
            IsExitRequested = true;
            try
            {
                if (_tray != null) { _tray.Visible = false; _tray.Dispose(); _tray = null; }
            }
            catch { }

            WpfApp.Current.Shutdown();
        }

        public void Dispose()
        {
            if (_tray != null)
            {
                _tray.Visible = false;
                _tray.Dispose();
                _tray = null;
            }
        }
    }
}
