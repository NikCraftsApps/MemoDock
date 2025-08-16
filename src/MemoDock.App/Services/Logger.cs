using System;
using System.IO;
using System.Linq;
using MemoDock.Utils;

namespace MemoDock.Services
{
    public static class Logger
    {
        private static readonly object _lock = new();

        private const long MaxSizeBytes = 5L * 1024 * 1024; // 5 MB
        private const int MaxFiles = 5; 

        public static void Log(string msg, Exception? ex = null)
        {
            try
            {
                Directory.CreateDirectory(AppPaths.DataFolder);
                var current = Path.Combine(AppPaths.DataFolder, "logs.txt");

                lock (_lock)
                {
                    if (File.Exists(current))
                    {
                        try
                        {
                            var fi = new FileInfo(current);
                            if (fi.Length > MaxSizeBytes)
                            {
                                var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                                var rotated = Path.Combine(AppPaths.DataFolder, $"logs_{ts}.txt");
                                File.Move(current, rotated, overwrite: false);
                                File.WriteAllText(current, string.Empty);

                                var files = Directory.GetFiles(AppPaths.DataFolder, "logs_*.txt")
                                                     .OrderByDescending(f => f)
                                                     .ToList();
                                if (files.Count >= MaxFiles)
                                {
                                    foreach (var old in files.Skip(MaxFiles - 1))
                                        try { File.Delete(old); } catch { }
                                }
                            }
                        }
                        catch { /* ignore rotation errors */ }
                    }

                    var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}";
                    if (ex != null) line += Environment.NewLine + ex;
                    File.AppendAllText(current, line + Environment.NewLine);
                }
            }
            catch { /* ignore */ }
        }
    }
}
