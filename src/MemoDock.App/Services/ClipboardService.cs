using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using MemoDock.App.Utils;
using System.Windows.Media.Imaging;

namespace MemoDock.Services
{
    public sealed class ClipboardService : IDisposable
    {
        public static ClipboardService Instance { get; } = new ClipboardService();

        private readonly System.Windows.Threading.DispatcherTimer _timer;
        public bool IsPaused { get; set; } = false;

        public event Action? Changed;

        private string? _lastHash;

        private ClipboardService()
        {
            _timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
            _timer.Tick += (_, __) => PollWrap();
            _timer.Start();
        }

        public void Dispose() => _timer.Stop();

        public void ForceRefresh() => Changed?.Invoke();

        private void PollWrap()
        {
            if (IsPaused) return;
            try { PollOnce(); }
            catch (Exception ex) { Logger.Log("Clipboard poll error", ex); }
        }

        private void PollOnce()
        {
            var txt = TryGetText();
            if (txt != null)
            {
                var h = Crypto.Sha256Hex(txt);
                if (!string.Equals(h, _lastHash, StringComparison.Ordinal))
                {
                    StorageService.SaveText(txt);
                    _lastHash = h;
                    Changed?.Invoke();
                }
                return;
            }

            var img = TryGetImagePng();
            if (img != null)
            {
                var h = Crypto.Sha256Hex(img);
                if (!string.Equals(h, _lastHash, StringComparison.Ordinal))
                {
                    StorageService.SaveImage(img);
                    _lastHash = h;
                    Changed?.Invoke();
                }
            }
        }

        private static string? TryGetText()
        {
            try
            {
                if (!System.Windows.Clipboard.ContainsText()) return null;
                var tries = 3;
                while (tries-- > 0)
                {
                    try { return System.Windows.Clipboard.GetText(); }
                    catch (COMException) { Thread.Sleep(30); }
                }
            }
            catch { }
            return null;
        }

        private static byte[]? TryGetImagePng()
        {
            try
            {
                if (!System.Windows.Clipboard.ContainsImage()) return null;
                var tries = 3;
                while (tries-- > 0)
                {
                    try
                    {
                        var src = System.Windows.Clipboard.GetImage();
                        if (src == null) return null;
                        using var ms = new MemoryStream();
                        var enc = new PngBitmapEncoder();
                        enc.Frames.Add(BitmapFrame.Create(src));
                        enc.Save(ms);
                        return ms.ToArray();
                    }
                    catch (COMException) { Thread.Sleep(30); }
                }
            }
            catch { }
            return null;
        }
    }
}
