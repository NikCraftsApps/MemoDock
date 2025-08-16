using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Media.Imaging;
using MemoDock.App.Utils;

namespace MemoDock.Services
{
    public sealed class ClipboardService : IDisposable
    {
        public static ClipboardService Instance { get; } = new ClipboardService();

        private readonly System.Windows.Threading.DispatcherTimer _timer;
        private bool _started;

        public bool IsPaused { get; set; } = false;
        public event Action? Changed;

        private string? _lastHash;

        private ClipboardService()
        {
            _timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
            _timer.Tick += (_, __) => PollWrap();
            // UWAGA: nie startujemy tutaj. Wywo³aj Start() po DB init.
        }

        public void Start()
        {
            if (_started) return;
            _started = true;
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
            _started = false;
        }

        public void Dispose() => Stop();

        public void ForceRefresh() => Changed?.Invoke();

        private void PollWrap()
        {
            if (IsPaused) return;

            // DB jeszcze nie gotowa? pomiñ tick bez logowania
            if (!DatabaseService.Instance.IsInitialized) return;

            try
            {
                PollOnce();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("DB not initialized"))
            {
                // wycisz do czasu inicjalizacji
            }
            catch (Exception ex)
            {
                Logger.Log("Clipboard poll error", ex);
            }
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
