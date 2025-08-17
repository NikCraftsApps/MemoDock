using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoDock.Services;
using MemoDock.Views;

namespace MemoDock.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public ObservableCollection<ClipboardItemViewModel> Items { get; } = new();
        public ICollectionView View { get; }

        [ObservableProperty] private string search = "";
        [ObservableProperty] private bool showPinnedOnly = false;

        public IAsyncRelayCommand ClearUnpinnedAsyncCommand { get; }

        private readonly List<ClipboardItemViewModel> _subscribedSelection = new();
        public int SelectedCount => Items.Count(i => i.IsSelected);
        public bool HasSelection => SelectedCount > 0;
        private IEnumerable<long> SelectedIds() => Items.Where(i => i.IsSelected).Select(i => i.Id);

        private readonly DispatcherTimer _debounce = new() { Interval = TimeSpan.FromMilliseconds(250) };

        public MainViewModel()
        {
            View = CollectionViewSource.GetDefaultView(Items);
            View.Filter = Filter;

            ClearUnpinnedAsyncCommand = new AsyncRelayCommand(ClearUnpinnedAsync);

            _debounce.Tick += async (_, __) =>
            {
                _debounce.Stop();
                await RefreshAsync();
            };
            ClipboardService.Instance.Changed += () =>
            {
                _debounce.Stop();
                _debounce.Start();
            };

            _ = RefreshAsync();
        }

        private bool Filter(object obj)
        {
            if (obj is not ClipboardItemViewModel it) return false;
            if (ShowPinnedOnly && !it.IsPinned) return false;

            if (string.IsNullOrWhiteSpace(Search)) return true;
            var s = Search.Trim();

            return it.Type == "text"
                   && it.DisplayText != null
                   && it.DisplayText.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        partial void OnSearchChanged(string value)
        {
            View.Refresh();
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelection));
        }

        partial void OnShowPinnedOnlyChanged(bool value)
        {
            View.Refresh();
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelection));
        }

        public async Task RefreshAsync()
        {
            var list = await Task.Run(() =>
            {
                var result = new List<ClipboardItemViewModel>();
                using var conn = DatabaseService.Instance.OpenConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
SELECT id,type,text,content_path,is_pinned,updated_at,pin_order
FROM entries
ORDER BY is_pinned DESC, COALESCE(pin_order, 999999), updated_at DESC
LIMIT 1000;";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var id = r.GetInt64(0);
                    var type = r.GetString(1);
                    var text = r.IsDBNull(2) ? null : r.GetString(2);
                    var path = r.IsDBNull(3) ? null : r.GetString(3);
                    var pinned = r.GetInt32(4) == 1;
                    var updated = DateTime.Parse(r.GetString(5));
                    result.Add(new ClipboardItemViewModel(id, type, text, path, pinned, updated));
                }
                return result;
            });

            foreach (var it in _subscribedSelection)
                it.PropertyChanged -= OnItemSelectionChanged;
            _subscribedSelection.Clear();

            Items.Clear();
            foreach (var vm in list)
            {
                vm.PropertyChanged += OnItemSelectionChanged;
                _subscribedSelection.Add(vm);
                Items.Add(vm);
            }

            View.Refresh();

            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelection));
        }

        private void OnItemSelectionChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ClipboardItemViewModel.IsSelected))
            {
                OnPropertyChanged(nameof(SelectedCount));
                OnPropertyChanged(nameof(HasSelection));
            }
        }

        private async Task ClearUnpinnedAsync()
        {
            var ok = await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var res = System.Windows.MessageBox.Show(
                    "Delete all UNPINNED items? (files in /store will be removed as well)",
                    "Confirmation",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);
                return res == System.Windows.MessageBoxResult.Yes;
            });
            if (!ok) return;

            await Task.Run(() =>
            {
                using var conn = DatabaseService.Instance.OpenConnection();

                var paths = new List<string>();
                using (var c = conn.CreateCommand())
                {
                    c.CommandText = @"SELECT content_path FROM entries WHERE is_pinned=0 AND content_path IS NOT NULL
                                      UNION
                                      SELECT v.content_path FROM versions v
                                      JOIN entries e ON e.id=v.entry_id
                                      WHERE e.is_pinned=0 AND v.content_path IS NOT NULL";
                    using var r = c.ExecuteReader();
                    while (r.Read())
                        if (!r.IsDBNull(0)) paths.Add(r.GetString(0));
                }

                using (var c = conn.CreateCommand())
                {
                    c.CommandText = @"DELETE FROM versions WHERE entry_id IN (SELECT id FROM entries WHERE is_pinned=0);
                                      DELETE FROM entries WHERE is_pinned=0;";
                    c.ExecuteNonQuery();
                }

                foreach (var p in paths.Distinct())
                {
                    try { if (File.Exists(p)) File.Delete(p); } catch { /* ignore */ }
                }
            });

            await RefreshAsync();
        }

        [RelayCommand]
        private void TogglePin(ClipboardItemViewModel? item)
        {
            if (item == null) return;
            StorageService.SetPinned(new[] { item.Id }, item.IsPinned);
            _ = RefreshAsync();
        }

        [RelayCommand]
        private void OpenTimeline(ClipboardItemViewModel? item)
        {
            if (item == null) return;
            var w = new TimelineWindow(item.Id)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            w.ShowDialog();
        }

        [RelayCommand]
        private async Task PinSelectedAsync()
        {
            var ids = SelectedIds().ToArray();
            if (ids.Length == 0) return;
            await Task.Run(() => StorageService.SetPinned(ids, true));
            await RefreshAsync();
        }

        [RelayCommand]
        private async Task UnpinSelectedAsync()
        {
            var ids = SelectedIds().ToArray();
            if (ids.Length == 0) return;
            await Task.Run(() => StorageService.SetPinned(ids, false));
            await RefreshAsync();
        }

        [RelayCommand]
        private async Task DeleteSelectedAsync()
        {
            var ids = SelectedIds().ToArray();
            if (ids.Length == 0) return;

            var res = System.Windows.MessageBox.Show(
                $"Delete {ids.Length} selected item(s)? This action cannot be undone.",
                "Delete selected",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (res != System.Windows.MessageBoxResult.Yes) return;

            await Task.Run(() => StorageService.DeleteEntries(ids));
            await RefreshAsync();
        }
    }
}

