using System;
using System.IO;
using System.Text.Json;
using MemoDock.Utils;

namespace MemoDock.Services
{
    public class AppSettings
    {
        public bool ListenEnabled { get; set; } = true;
        public int MaxImageBytes { get; set; } = 3 * 1024 * 1024;
        public int RetentionItems { get; set; } = 2000;
        public bool AutoStart { get; set; } = false;
        public string? BackupFolder { get; set; } = null;
    }

    public class SettingsService
    {
        public static SettingsService Instance { get; } = new();
        private SettingsService() { }

        public AppSettings Settings { get; private set; } = new();

        public void Load()
        {
            try
            {
                if (File.Exists(AppPaths.ConfigPath))
                {
                    var json = File.ReadAllText(AppPaths.ConfigPath);
                    Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex) { Logger.Log("Settings load failed", ex); }

            try { Settings.AutoStart = AutoStartService.Get(); } catch { }

            ApplyRuntimeSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(AppPaths.DataFolder);
                var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(AppPaths.ConfigPath, json);
                try { AutoStartService.Set(Settings.AutoStart); } catch { }
            }
            catch (Exception ex) { Logger.Log("Settings save failed", ex); }

            ApplyRuntimeSettings();
        }

        public void ApplyRuntimeSettings()
        {
            ClipboardService.Instance.IsPaused = !Settings.ListenEnabled;
        }
    }
}
