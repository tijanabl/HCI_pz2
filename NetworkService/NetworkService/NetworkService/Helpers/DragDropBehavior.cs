using NetworkService.Model;
using NetworkService.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NetworkService.Helpers
{
    public static class DragDropBehavior
    {
        public static readonly DependencyProperty EnableDragProperty =
            DependencyProperty.RegisterAttached(
                "EnableDrag",
                typeof(bool),
                typeof(DragDropBehavior),
                new PropertyMetadata(false, OnEnableDragChanged));

        public static bool GetEnableDrag(DependencyObject obj)
            => (bool)obj.GetValue(EnableDragProperty);

        public static void SetEnableDrag(DependencyObject obj, bool value)
            => obj.SetValue(EnableDragProperty, value);

        private static void OnEnableDragChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is TreeView treeView)) return;

            if ((bool)e.NewValue)
            {
                treeView.AllowDrop = true;
                treeView.SelectedItemChanged += TreeView_SelectedItemChanged;
                treeView.MouseLeftButtonUp += TreeView_MouseLeftButtonUp;
                treeView.Drop += TreeView_Drop;
            }
            else
            {
                treeView.SelectedItemChanged -= TreeView_SelectedItemChanged;
                treeView.MouseLeftButtonUp -= TreeView_MouseLeftButtonUp;
                treeView.Drop -= TreeView_Drop;
            }
        }

        private static void TreeView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(int)) is int sourceIndex)
            {
                var vm = MainWindowViewModel.DisplayVM;
                if (vm == null) return;

                if (vm.CanvasEntityMap.ContainsKey(sourceIndex) &&
                    vm.CanvasEntityMap[sourceIndex] != null)
                {
                    vm.CanvasEntityMap.Remove(sourceIndex);
                    vm.RemoveConnectionsForIndex(sourceIndex);

                    Canvas sourceCanvas = CanvasDropBehavior.FindCanvasByIndexPublic(sourceIndex);
                    if (sourceCanvas != null)
                        CanvasDropBehavior.ResetCanvasVisual(sourceCanvas, sourceIndex);

                    vm.RefreshGroupedEntities();
                }
                LineDrawingBehavior.RedrawLines(vm);
                e.Handled = true;
            }
        }

        private static void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!(sender is TreeView tv)) return;
            NetworkDisplayViewModel vm = MainWindowViewModel.DisplayVM;
            if (vm == null) return;
            if (e.NewValue is EntityTypeGroup) return;
            if (e.NewValue is EntityDisplayItem item && !vm.IsDragging)
            {
                vm.IsDragging = true;
                vm.DraggedItem = item;
                DragDrop.DoDragDrop(tv, item, DragDropEffects.Move);
            }
        }

        private static void TreeView_MouseLeftButtonUp(object sender,MouseButtonEventArgs e)
        {
            NetworkDisplayViewModel vm = MainWindowViewModel.DisplayVM;
            if (vm == null) return;
            vm.IsDragging = false;
            vm.DraggedItem = null;
        }
    }
}