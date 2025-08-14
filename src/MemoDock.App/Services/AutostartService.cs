using Microsoft.Win32;

namespace MemoDock.Services
{
    public static class AutoStartService
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "MemoDock";

        public static void Set(bool enabled)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key == null) return;
            if (enabled)
                key.SetValue(AppName, System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName);
            else
                key.DeleteValue(AppName, false);
        }

        public static bool Get()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            if (key == null) return false;
            return key.GetValue(AppName) != null;
        }
    }
}
