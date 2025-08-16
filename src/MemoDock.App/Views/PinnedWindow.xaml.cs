using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MemoDock.Services;
using MemoDock.ViewModels;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace MemoDock.Views
{
    public partial class PinnedWindow : Window
    {
        private System.Windows.Point _dragStart;
        private ClipboardItemViewModel? _dragItem;

        public PinnedWindow()
        {
            InitializeComponent();
            this.Closed += (_, __) => StorageEvents.EntriesMutated -= OnEntriesMutated;
            StorageEvents.EntriesMutated += OnEntriesMutated;
        }

        private void OnEntriesMutated()
        {
            if (DataContext is PinnedViewModel vm)
                _ = vm.RefreshAsync();
        }

        private void PinnedList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(null);
            var item = FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject);
            _dragItem = item?.DataContext as ClipboardItemViewModel;
        }

        private void PinnedList_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (_dragItem == null || !_dragItem.IsPinned) return;
            if ((e.GetPosition(null) - _dragStart).Length < 6) return;

            DragDrop.DoDragDrop(PinnedList, _dragItem, DragDropEffects.Move);
        }

        private void PinnedList_DragOver(object sender, DragEventArgs e)
        {
            var src = e.Data.GetData(typeof(ClipboardItemViewModel)) as ClipboardItemViewModel;
            e.Effects = (src != null && src.IsPinned) ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private async void PinnedList_Drop(object sender, DragEventArgs e)
        {
            var src = e.Data.GetData(typeof(ClipboardItemViewModel)) as ClipboardItemViewModel;
            if (src == null || !src.IsPinned) return;

            var pos = e.GetPosition(PinnedList);
            var destItem = FindAncestor<ListBoxItem>(PinnedList.InputHitTest(pos) as DependencyObject);
            var dst = destItem?.DataContext as ClipboardItemViewModel;

            var pinned = PinnedList.Items.OfType<object>().OfType<ClipboardItemViewModel>().ToList();

            int from = pinned.IndexOf(src);
            int to = dst != null ? pinned.IndexOf(dst) : pinned.Count - 1;
            if (from < 0 || to < 0 || from == to) return;

            pinned.RemoveAt(from);
            pinned.Insert(to, src);

            var map = pinned.Select((vm, i) => (vm.Id, i)).ToList();
            StorageService.UpdatePinOrder(map);

            if (DataContext is PinnedViewModel vm) await vm.RefreshAsync();
        }

        private static T? FindAncestor<T>(DependencyObject? d) where T : DependencyObject
        {
            while (d != null && d is not T) d = VisualTreeHelper.GetParent(d);
            return d as T;
        }
    }
}
