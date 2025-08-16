using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;           
using System.Windows.Media;
using MemoDock.App.Services;
using MemoDock.Services;
using MemoDock.ViewModels;
using Button = System.Windows.Controls.Button;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;

namespace MemoDock.App.Views
{
    public partial class MainWindow : Window
    {
        private System.Windows.Point _dragStart;
        private ClipboardItemViewModel? _dragItem;

        public MainWindow()
        {
            InitializeComponent();
            Closing += OnClosingToTray;
        }

        private void OnClosingToTray(object? sender, CancelEventArgs e)
        {
            if (!TrayService.IsExitRequested)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            var w = new MemoDock.Views.SettingsWindow { Owner = this };
            w.ShowDialog();
        }

        private void ActionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.Placement = PlacementMode.Bottom;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void MainList_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(null);
            var item = FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject);
            _dragItem = item?.DataContext as ClipboardItemViewModel;
        }

        private void MainList_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (_dragItem == null || !_dragItem.IsPinned) return;
            if ((e.GetPosition(null) - _dragStart).Length < 6) return;

            DragDrop.DoDragDrop(MainList, _dragItem, DragDropEffects.Move);
        }

        private void MainList_DragOver(object sender, DragEventArgs e)
        {
            var src = e.Data.GetData(typeof(ClipboardItemViewModel)) as ClipboardItemViewModel;
            if (src == null || !src.IsPinned) { e.Effects = DragDropEffects.None; e.Handled = true; return; }

            var pos = e.GetPosition(MainList);
            var destItem = FindAncestor<ListBoxItem>(MainList.InputHitTest(pos) as DependencyObject);
            var dst = destItem?.DataContext as ClipboardItemViewModel;

            e.Effects = (dst != null && dst.IsPinned) ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private async void MainList_Drop(object sender, DragEventArgs e)
        {
            var src = e.Data.GetData(typeof(ClipboardItemViewModel)) as ClipboardItemViewModel;
            if (src == null || !src.IsPinned) return;

            var pos = e.GetPosition(MainList);
            var destItem = FindAncestor<ListBoxItem>(MainList.InputHitTest(pos) as DependencyObject);
            var dst = destItem?.DataContext as ClipboardItemViewModel;

            var list = MainList.Items.OfType<object>().OfType<ClipboardItemViewModel>().ToList();
            var pinned = list.Where(i => i.IsPinned).ToList();

            int from = pinned.IndexOf(src);
            int to = dst != null ? pinned.IndexOf(dst) : pinned.Count - 1;
            if (from < 0 || to < 0 || from == to) return;

            pinned.RemoveAt(from);
            pinned.Insert(to, src);

            var map = pinned.Select((vm, i) => (vm.Id, i)).ToList();
            StorageService.UpdatePinOrder(map);

            if (DataContext is MainViewModel vm) await vm.RefreshAsync();
        }

        private static T? FindAncestor<T>(DependencyObject? d) where T : DependencyObject
        {
            while (d != null && d is not T) d = VisualTreeHelper.GetParent(d);
            return d as T;
        }
    }
}
