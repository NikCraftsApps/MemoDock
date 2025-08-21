using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MemoDock.App.Services;
using MemoDock.Services;
using MemoDock.ViewModels;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using ListBox = System.Windows.Controls.ListBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace MemoDock.Views
{
    public partial class PinnedWindow : Window
    {
        private Point _dragStart;
        private bool _isDragging;

        public PinnedWindow()
        {
            InitializeComponent();

            PinnedEvents.Changed += OnPinnedChanged;
            this.Closed += (_, __) => PinnedEvents.Changed -= OnPinnedChanged;
        }

        private async void OnPinnedChanged()
        {
            if (DataContext is PinnedViewModel vm)
                await vm.LoadAsync();
        }

        private void PinnedList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(null);
            _isDragging = false;
        }

        private void PinnedList_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (_isDragging) return;

            var pos = e.GetPosition(null);
            if (Math.Abs(pos.X - _dragStart.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(pos.Y - _dragStart.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                _isDragging = true;
                if (sender is ListBox lb && lb.SelectedItem != null)
                {
                    DragDrop.DoDragDrop(lb, lb.SelectedItem, DragDropEffects.Move);
                }
                _isDragging = false;
            }
        }

        private void PinnedList_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void PinnedList_Drop(object sender, DragEventArgs e)
        {
            if (sender is not ListBox lb) return;
            var data = e.Data.GetData(typeof(ClipboardItemViewModel)) as ClipboardItemViewModel;
            if (data == null) return;

            var point = e.GetPosition(lb);
            int targetIndex = GetIndexUnderPoint(lb, point);
            if (targetIndex < 0) targetIndex = lb.Items.Count - 1;

            var vm = DataContext as PinnedViewModel;
            if (vm == null) return;

            var sourceIndex = vm.Pinned.IndexOf(data);
            if (sourceIndex < 0 || sourceIndex == targetIndex) return;

            vm.Pinned.Move(sourceIndex, targetIndex);

            var map = vm.Pinned.Select((it, idx) => (id: it.Id, order: idx)).ToList();
            try
            {
                StorageService.UpdatePinOrder(map);
            }
            catch (Exception ex) { Logger.Log("Pinned reorder failed", ex); }

            PinnedEvents.Raise();
        }

        private static int GetIndexUnderPoint(ListBox lb, Point p)
        {
            for (int i = 0; i < lb.Items.Count; i++)
            {
                var item = (ListBoxItem)lb.ItemContainerGenerator.ContainerFromIndex(i);
                if (item == null) continue;
                var rect = new Rect(item.TranslatePoint(new Point(), lb), new Size(item.ActualWidth, item.ActualHeight));
                if (rect.Contains(p)) return i;
            }
            return -1;
        }
    }
}
