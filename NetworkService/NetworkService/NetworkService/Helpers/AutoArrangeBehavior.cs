using NetworkService.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace NetworkService.Helpers
{
    public static class AutoArrangeBehavior
    {
        public static readonly DependencyProperty EnableAutoArrangeProperty =
            DependencyProperty.RegisterAttached(
                "EnableAutoArrange",
                typeof(bool),
                typeof(AutoArrangeBehavior),
                new PropertyMetadata(false, OnEnableChanged));

        public static bool GetEnableAutoArrange(DependencyObject obj)
            => (bool)obj.GetValue(EnableAutoArrangeProperty);

        public static void SetEnableAutoArrange(DependencyObject obj, bool value)
            => obj.SetValue(EnableAutoArrangeProperty, value);

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is UniformGrid grid)) return;
            if (!(bool)e.NewValue) return;

            grid.Loaded += (s, ev) =>
            {
                var vm = MainWindowViewModel.DisplayVM;
                if (vm == null) return;

                vm.PropertyChanged += (sender, pe) =>
                {
                    if (pe.PropertyName != "CanvasEntityMap") return;

                    foreach (UIElement child in grid.Children)
                    {
                        if (!(child is Canvas canvas)) continue;

                        int index = CanvasDropBehavior.GetCanvasIndex(canvas);
                        if (index < 0) continue;

                        if (vm.CanvasEntityMap.ContainsKey(index) &&
                            vm.CanvasEntityMap[index] != null)
                        {
                            CanvasDropBehavior.UpdateCanvasVisual(
                                canvas, vm.CanvasEntityMap[index], index);
                        }
                    }
                };
            };
        }
    }
}