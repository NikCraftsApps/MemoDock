using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using MemoDock.Services;
using Application = System.Windows.Application;

namespace MemoDock.ViewModels
{
    public partial class PinnedViewModel : ObservableObject
    {
        public ObservableCollection<ClipboardItemViewModel> Pinned { get; } = new();

        private readonly DispatcherTimer _debounce = new() { Interval = TimeSpan.FromMilliseconds(250) };

        public PinnedViewModel()
        {
            _debounce.Tick += async (_, __) =>
            {
                _debounce.Stop();
                await LoadAsync();
            };

            ClipboardService.Instance.Changed += () =>
            {
                _debounce.Stop();
                _debounce.Start();
            };

            _ = LoadAsync();
        }

        public async Task LoadAsync()
        {
            var list = await Task.Run(() =>
            {
                var res = new System.Collections.Generic.List<ClipboardItemViewModel>();
                using var conn = DatabaseService.Instance.OpenConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
SELECT id,type,text,content_path,is_pinned,updated_at
FROM entries
WHERE is_pinned=1
ORDER BY pin_order ASC, updated_at DESC
LIMIT 100;";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var id = r.GetInt64(0);
                    var type = r.GetString(1);
                    var text = r.IsDBNull(2) ? null : r.GetString(2);
                    var path = r.IsDBNull(3) ? null : r.GetString(3);
                    var pinned = r.GetInt32(4) == 1;
                    var updated = DateTime.Parse(r.GetString(5));
                    res.Add(new ClipboardItemViewModel(id, type, text, path, pinned, updated));
                }
                return res;
            });

            var d = Application.Current?.Dispatcher;
            if (d != null && !d.CheckAccess())
            {
                await d.InvokeAsync(() =>
                {
                    Pinned.Clear();
                    foreach (var it in list) Pinned.Add(it);
                }, DispatcherPriority.Background);
            }
            else
            {
                Pinned.Clear();
                foreach (var it in list) Pinned.Add(it);
            }
        }
    }
}
