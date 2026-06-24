using NetworkService.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NetworkService.Helpers
{
    public static class LineDrawingBehavior
    {
        public static readonly DependencyProperty EnableLinesProperty =
            DependencyProperty.RegisterAttached(
                "EnableLines",
                typeof(bool),
                typeof(LineDrawingBehavior),
                new PropertyMetadata(false, OnEnableChanged));

        public static bool GetEnableLines(DependencyObject obj)
            => (bool)obj.GetValue(EnableLinesProperty);

        public static void SetEnableLines(DependencyObject obj, bool value)
            => obj.SetValue(EnableLinesProperty, value);

        private static Canvas _linesCanvas;
        private static int _firstSelectedIndex = -1;

        private static void OnEnableChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (!(d is Canvas canvas)) return;
            if (!(bool)e.NewValue) return;

            _linesCanvas = canvas;

            canvas.Loaded += (s, ev) =>
            {
                var vm = MainWindowViewModel.DisplayVM;
                if (vm == null) return;

                vm.PropertyChanged += (sender, pe) =>
                {
                    if (pe.PropertyName == "GroupedEntities")
                    {
                        Application.Current.Dispatcher.BeginInvoke(
                            System.Windows.Threading.DispatcherPriority.Loaded,
                            new Action(() => RedrawLines(vm)));
                    }
                };
            };
        }

        public static void OnCanvasCtrlClick(int canvasIndex)
        {
            var vm = MainWindowViewModel.DisplayVM;
            if (vm == null) return;

            if (!vm.CanvasEntityMap.ContainsKey(canvasIndex) ||
                vm.CanvasEntityMap[canvasIndex] == null) return;

            if (_firstSelectedIndex == -1)
            {
                _firstSelectedIndex = canvasIndex;
                HighlightCanvas(canvasIndex, true);
            }
            else if (_firstSelectedIndex == canvasIndex)
            {
                HighlightCanvas(canvasIndex, false);
                _firstSelectedIndex = -1;
            }
            else
            {
                int from = _firstSelectedIndex;
                int to = canvasIndex;

                HighlightCanvas(_firstSelectedIndex, false);
                _firstSelectedIndex = -1;

                if (!vm.ConnectionExists(from, to))
                {
                    vm.AddConnection(from, to);
                    Application.Current.Dispatcher.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Loaded,
                        new Action(() => RedrawLines(vm)));
                }
            }
        }

        private static void HighlightCanvas(int index, bool highlight)
        {
            var canvas = CanvasDropBehavior.FindCanvasByIndexPublic(index);
            if (canvas == null) return;
            canvas.Effect = highlight
                ? new System.Windows.Media.Effects.DropShadowEffect
                { Color = Color.FromRgb(23, 177, 105), BlurRadius = 10, ShadowDepth = 0 }
                : null;
        }

        public static void RedrawLines(NetworkDisplayViewModel vm)
        {
            if (_linesCanvas == null) return;
            if (vm == null) return;

            _linesCanvas.Children.Clear();

            foreach (var conn in vm.Connections)
            {
                Point? p1 = GetCenter(conn.Item1);
                Point? p2 = GetCenter(conn.Item2);

                if (p1 == null || p2 == null) continue;

                _linesCanvas.Children.Add(new Line
                {
                    X1 = p1.Value.X,
                    Y1 = p1.Value.Y,
                    X2 = p2.Value.X,
                    Y2 = p2.Value.Y,
                    Stroke = new SolidColorBrush(Color.FromRgb(23, 177, 105)),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                });
            }
        }

        private static Point? GetCenter(int canvasIndex)
        {
            if (_linesCanvas == null) return null;
            if (_linesCanvas.ActualWidth == 0) return null;

            int col = canvasIndex % 3;
            int row = canvasIndex / 3;

            double margin = 4;
            double cellW = (_linesCanvas.ActualWidth - margin * 2) / 3;
            double cellH = (_linesCanvas.ActualHeight - margin * 2) / 4;

            double x = margin + col * cellW + cellW / 2;
            double y = margin + row * cellH + cellH / 2;

            return new Point(x, y);
        }
    }
}
