using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoDock.Services;

namespace MemoDock.ViewModels
{
    public partial class ClipboardItemViewModel : ObservableObject
    {
        public long Id { get; }
        public string Type { get; }

        [ObservableProperty] private string? text;
        public string? ContentPath { get; }
        [ObservableProperty] private bool isPinned;
        public DateTime UpdatedAt { get; }

        public ClipboardItemViewModel(long id, string type, string? text, string? contentPath, bool isPinned, DateTime updatedAt)
        {
            Id = id;
            Type = type;
            this.text = text;
            ContentPath = contentPath;
            this.isPinned = isPinned;
            UpdatedAt = updatedAt;
        }

        public string? DisplayText
        {
            get
            {
                if (Type != "text") return null;
                if (!string.IsNullOrEmpty(Text)) return Text;
                if (ContentPath == null) return null;
                return StorageService.TryLoadText(ContentPath);
            }
        }

        public BitmapImage? Thumbnail
        {
            get
            {
                if (Type != "image" || ContentPath == null) return null;
                try
                {
                    var bytes = File.ReadAllBytes(ContentPath);
                    using var ms = new MemoryStream(bytes);
                    var img = new BitmapImage();
                    img.BeginInit();
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.StreamSource = ms;
                    img.DecodePixelWidth = 128;
                    img.EndInit();
                    img.Freeze();
                    return img;
                }
                catch { return null; }
            }
        }

        [RelayCommand]
        private void CopyToClipboard()
        {
            try
            {
                void Try(Action a)
                {
                    var tries = 3;
                    while (true)
                    {
                        try { a(); break; }
                        catch (COMException) { if (--tries == 0) throw; Thread.Sleep(50); }
                    }
                }

                if (Type == "text")
                {
                    var txt = DisplayText ?? (ContentPath != null ? StorageService.TryLoadText(ContentPath) : null);
                    if (txt != null) Try(() => System.Windows.Clipboard.SetText(txt));
                }
                else if (Type == "image" && ContentPath != null)
                {
                    var bi = new BitmapImage(new Uri(ContentPath));
                    Try(() => System.Windows.Clipboard.SetImage(bi));
                }
            }
            catch { }
        }
    }
}
