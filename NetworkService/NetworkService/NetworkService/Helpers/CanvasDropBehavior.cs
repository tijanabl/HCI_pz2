using NetworkService.Model;
using NetworkService.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NetworkService.Helpers
{
    public static class CanvasDropBehavior
    {
        public static readonly DependencyProperty CanvasIndexProperty =
            DependencyProperty.RegisterAttached(
                "CanvasIndex",
                typeof(int),
                typeof(CanvasDropBehavior),
                new PropertyMetadata(-1, OnCanvasIndexChanged));

        public static int GetCanvasIndex(DependencyObject obj)
            => (int)obj.GetValue(CanvasIndexProperty);

        public static void SetCanvasIndex(DependencyObject obj, int value)
            => obj.SetValue(CanvasIndexProperty, value);

        private static void OnCanvasIndexChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (!(d is Canvas canvas)) return;
            if ((int)e.NewValue < 0) return;

            canvas.AllowDrop = true;
            canvas.DragOver += Canvas_DragOver;
            canvas.Drop += Canvas_Drop;
            canvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
            canvas.MouseLeftButtonDown += (s, args) =>
            {
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    LineDrawingBehavior.OnCanvasCtrlClick(GetCanvasIndex((Canvas)s));
                    args.Handled = true;
                }
            };
        }

        private static NetworkDisplayViewModel GetVM()
            => MainWindowViewModel.DisplayVM;

        private static void Canvas_DragOver(object sender, DragEventArgs e)
        {
            if (!(sender is Canvas canvas)) return;
            int index = GetCanvasIndex(canvas);
            var vm = GetVM();
            bool occupied = vm != null &&
                            vm.CanvasEntityMap.ContainsKey(index) &&
                            vm.CanvasEntityMap[index] != null;
            e.Effects = occupied ? DragDropEffects.None : DragDropEffects.Move;
            e.Handled = true;
        }

        private static void Canvas_Drop(object sender, DragEventArgs e)
        {
            if (!(sender is Canvas canvas)) return;
            int index = GetCanvasIndex(canvas);
            var vm = GetVM();
            if (vm == null) return;

            if (e.Data.GetData(typeof(EntityDisplayItem)) is EntityDisplayItem item)
            {
                bool occupied = vm.CanvasEntityMap.ContainsKey(index) &&
                                vm.CanvasEntityMap[index] != null;
                if (occupied) { e.Handled = true; return; }

                DerEntity entity = MainWindowViewModel.AllEntities
                    .FirstOrDefault(en => en.Id == item.Id);

                if (entity != null)
                {
                    vm.CanvasEntityMap[index] = entity;
                    UpdateCanvasVisual(canvas, entity, index);
                    vm.RefreshGroupedEntities();
                    Application.Current.Dispatcher.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Loaded,
                        new Action(() => LineDrawingBehavior.RedrawLines(vm)));
                }

                vm.IsDragging = false;
                vm.DraggedItem = null;
                e.Handled = true;
                return;
            }

            if (e.Data.GetData(typeof(int)) is int sourceIndex)
            {
                bool occupied = vm.CanvasEntityMap.ContainsKey(index) &&
                                vm.CanvasEntityMap[index] != null;
                if (occupied || sourceIndex == index) { e.Handled = true; return; }

                if (vm.CanvasEntityMap.ContainsKey(sourceIndex))
                {
                    DerEntity entity = vm.CanvasEntityMap[sourceIndex];
                    vm.CanvasEntityMap.Remove(sourceIndex);

                    Canvas sourceCanvas = FindCanvasByIndexPublic(sourceIndex);
                    if (sourceCanvas != null)
                        ResetCanvasVisual(sourceCanvas, sourceIndex);

                    vm.CanvasEntityMap[index] = entity;

                    vm.UpdateConnectionsForMove(sourceIndex, index);

                    UpdateCanvasVisual(canvas, entity, index);
                    vm.RefreshGroupedEntities();

                    Application.Current.Dispatcher.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Loaded,
                        new Action(() => LineDrawingBehavior.RedrawLines(vm)));
                }

                e.Handled = true;
            }
        }

        private static void Canvas_MouseLeftButtonDown(object sender,
            MouseButtonEventArgs e)
        {
            if (!(sender is Canvas canvas)) return;
            int index = GetCanvasIndex(canvas);
            var vm = GetVM();

            if (vm == null) return;
            if (!vm.CanvasEntityMap.ContainsKey(index)) return;
            if (vm.CanvasEntityMap[index] == null) return;

            DragDrop.DoDragDrop(canvas, index, DragDropEffects.Move);
        }

        public static Canvas FindCanvasByIndexPublic(int index)
        {
            if (Application.Current?.MainWindow == null) return null;
            return FindCanvasRecursive(Application.Current.MainWindow, index);
        }

        private static Canvas FindCanvasRecursive(DependencyObject parent, int index)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is Canvas c && GetCanvasIndex(c) == index)
                    return c;
                var result = FindCanvasRecursive(child, index);
                if (result != null) return result;
            }
            return null;
        }

        public static void UpdateCanvasVisual(Canvas canvas, DerEntity entity, int index)
        {
            try
            {
                var bmp = new BitmapImage(
                    new Uri(entity.EntityType.ImagePath, UriKind.RelativeOrAbsolute));
                canvas.Background = new ImageBrush(bmp)
                { Stretch = Stretch.UniformToFill, Opacity = 1.0 };
            }
            catch
            {
                canvas.Background = new SolidColorBrush(Color.FromRgb(36, 52, 71));
            }

            foreach (UIElement child in canvas.Children)
            {
                if (child is TextBlock tb && (string)tb.Tag == "number")
                    tb.Visibility = Visibility.Collapsed;
            }

            TextBlock infoTb = null;
            foreach (UIElement child in canvas.Children)
            {
                if (child is TextBlock tb && (string)tb.Tag == "info")
                { infoTb = tb; break; }
            }

            if (infoTb != null)
            {
                infoTb.Text = $"ID:{entity.Id}  {entity.CurrentValueDisplay}";
                infoTb.FontSize = 8;
                infoTb.TextAlignment = TextAlignment.Center;
                infoTb.Background = new SolidColorBrush(Color.FromArgb(180, 26, 35, 50));
                infoTb.Foreground = entity.IsValueValid
                    ? new SolidColorBrush(Color.FromRgb(23, 177, 105))
                    : new SolidColorBrush(Color.FromRgb(231, 76, 60));
                infoTb.Padding = new Thickness(2);
                infoTb.Width = canvas.ActualWidth > 0 ? canvas.ActualWidth : 200;
                Canvas.SetLeft(infoTb, 0);
                Canvas.SetTop(infoTb,
                    canvas.ActualHeight > 0 ? canvas.ActualHeight - 20 : 80);
            }

            if (canvas.Tag == null)
            {
                canvas.Tag = "initialized";
                canvas.SizeChanged += (s, ev) =>
                {
                    if (infoTb == null) return;
                    infoTb.Width = canvas.ActualWidth;
                    Canvas.SetTop(infoTb, canvas.ActualHeight - 20);
                };
            }

            bool hasClose = canvas.Children.OfType<Button>()
                .Any(b => (string)b.Tag == "close");

            if (!hasClose)
            {
                Button closeBtn = new Button
                {
                    Content = "✕",
                    Tag = "close",
                    Width = 22,
                    Height = 22,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Background = new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(0),
                    Cursor = Cursors.Hand,
                    ToolTip = "Ukloni sa mreže",
                    Style = null   
                };

                Canvas.SetLeft(closeBtn, 2);
                Canvas.SetTop(closeBtn, 2);

                int capturedIndex = index;
                closeBtn.Click += (s, ev) =>
                {
                    var vm = GetVM();
                    if (vm == null) return;
                    vm.CanvasEntityMap.Remove(capturedIndex);
                    vm.RemoveConnectionsForIndex(capturedIndex);
                    ResetCanvasVisual(canvas, capturedIndex);
                    vm.RefreshGroupedEntities();
                };

                canvas.Children.Add(closeBtn);
            }

            Action updateInfo = () =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (UIElement child in canvas.Children)
                    {
                        if (child is TextBlock tb && (string)tb.Tag == "info")
                        {
                            tb.Text = $"ID:{entity.Id}  {entity.CurrentValueDisplay}";
                            tb.Foreground = entity.IsValueValid
                                ? new SolidColorBrush(Color.FromRgb(23, 177, 105))
                                : new SolidColorBrush(Color.FromRgb(231, 76, 60));
                        }
                    }
                });
            };

            System.ComponentModel.PropertyChangedEventHandler handler = null;
            handler = (s, pe) =>
            {
                if (pe.PropertyName != "CurrentValue" &&
                    pe.PropertyName != "IsValueValid") return;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var vm = GetVM();
                    if (vm == null) return;
                    if (!vm.CanvasEntityMap.ContainsKey(index) ||
                        vm.CanvasEntityMap[index]?.Id != entity.Id)
                    {
                        entity.PropertyChanged -= handler;
                        return;
                    }

                    foreach (UIElement child in canvas.Children)
                    {
                        if (child is TextBlock tb && (string)tb.Tag == "info")
                        {
                            tb.Text = $"ID:{entity.Id}  {entity.CurrentValueDisplay}";
                            tb.Foreground = entity.IsValueValid
                                ? new SolidColorBrush(Color.FromRgb(23, 177, 105))
                                : new SolidColorBrush(Color.FromRgb(231, 76, 60));
                        }
                    }
                });
            };

            entity.PropertyChanged += handler;
        }

        public static void ResetCanvasVisual(Canvas canvas, int index)
        {
            var toRemove = canvas.Children.OfType<Button>().ToList();
            foreach (var btn in toRemove)
                canvas.Children.Remove(btn);

            canvas.Background = new SolidColorBrush(Color.FromRgb(36, 52, 71));
            canvas.Effect = null;
            canvas.Tag = null; 

            foreach (UIElement child in canvas.Children)
            {
                if (child is TextBlock tb)
                {
                    if ((string)tb.Tag == "number")
                    {
                        tb.Text = index.ToString();
                        tb.Visibility = Visibility.Visible;
                        tb.Background = Brushes.Transparent;
                        tb.FontSize = 14;
                        Canvas.SetLeft(tb, index < 10 ? 38 : 32);
                        Canvas.SetTop(tb, 35);
                    }
                    else if ((string)tb.Tag == "info")
                    {
                        tb.Text = string.Empty;
                        tb.Background = Brushes.Transparent;
                        tb.Foreground = Brushes.White;
                        tb.Width = double.NaN;
                        tb.Height = double.NaN;
                        tb.Padding = new Thickness(0);
                        tb.FontSize = 9;
                        Canvas.SetLeft(tb, 4);
                        Canvas.SetTop(tb, 68);
                    }
                }
            }
        }
    }
}