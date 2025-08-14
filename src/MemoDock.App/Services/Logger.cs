using System;
using System.IO;
using MemoDock.Utils;

namespace MemoDock.Services
{
    public static class Logger
    {
        private static readonly object _lock = new();
        public static void Log(string msg, Exception? ex = null)
        {
            try
            {
                Directory.CreateDirectory(AppPaths.DataFolder);
                var path = Path.Combine(AppPaths.DataFolder, "logs.txt");
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}";
                if (ex != null) line += Environment.NewLine + ex;
                lock (_lock) File.AppendAllText(path, line + Environment.NewLine);
            }
            catch { /* ignore */ }
        }
    }
}
