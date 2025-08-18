using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using MemoDock.Services;
using Application = System.Windows.Application; // Logger

namespace MemoDock.App.Services
{
    /// <summary>
    /// Globalny skrót Ctrl+Shift+M do Show/Hide okna głównego.
    /// Stały (bez konfiguracji).
    /// </summary>
    public sealed class GlobalHotkeyService : IDisposable
    {
        public static GlobalHotkeyService Instance { get; } = new();

        private GlobalHotkeyService() { }

        private const int WM_HOTKEY = 0x0312;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const uint MOD_NOREPEAT = 0x4000;

        private const int HOTKEY_ID = 1;

        private HwndSource? _source;
        private IntPtr _hWnd = IntPtr.Zero;
        private bool _registered;

        public void RegisterDefault()
        {
            Unregister();

            var p = new HwndSourceParameters("MemoDock_GlobalHotkeyWnd")
            {
                Width = 0,
                Height = 0,
                PositionX = 0,
                PositionY = 0,
                ParentWindow = new IntPtr(-3) 
            };
            _source = new HwndSource(p);
            _source.AddHook(WndProc);
            _hWnd = _source.Handle;

            uint mods = MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT;
            uint vkM = (uint)'M';

            if (!RegisterHotKey(_hWnd, HOTKEY_ID, mods, vkM))
            {
                var err = Marshal.GetLastWin32Error();
                Logger.Log($"GlobalHotkey: RegisterHotKey failed (win32={err}) for Ctrl+Shift+M");
                try { _source.RemoveHook(WndProc); } catch { }
                try { _source.Dispose(); } catch { }
                _source = null;
                _hWnd = IntPtr.Zero;
                return;
            }

            _registered = true;
        }

        public void Unregister()
        {
            try
            {
                if (_registered && _hWnd != IntPtr.Zero)
                    UnregisterHotKey(_hWnd, HOTKEY_ID);
            }
            catch { /* ignore */ }
            finally
            {
                _registered = false;
                if (_source != null)
                {
                    try { _source.RemoveHook(WndProc); } catch { }
                    try { _source.Dispose(); } catch { }
                    _source = null;
                }
                _hWnd = IntPtr.Zero;
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wparam.ToInt32() == HOTKEY_ID)
            {
                handled = true;
                try { Application.Current?.Dispatcher?.Invoke(ToggleShowHide); }
                catch (Exception ex) { Logger.Log("GlobalHotkey toggle failed", ex); }
            }
            return IntPtr.Zero;
        }

        private static void ToggleShowHide()
        {
            var app = Application.Current;
            if (app == null) return;

            if (app.MainWindow is not Window w)
            {
                w = new MemoDock.App.Views.MainWindow();
                app.MainWindow = w;
            }

            if (!w.IsVisible || w.WindowState == WindowState.Minimized)
            {
                if (!w.IsVisible) w.Show();
                if (w.WindowState == WindowState.Minimized) w.WindowState = WindowState.Normal;
                w.Activate();
                w.Topmost = true;
                w.Topmost = false;
            }
            else
            {
                w.Hide();
            }
        }

        public void Dispose() => Unregister();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
