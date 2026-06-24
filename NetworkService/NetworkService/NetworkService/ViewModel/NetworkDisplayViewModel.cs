
using NetworkService.Helpers;
using NetworkService.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace NetworkService.ViewModel
{
    public class NetworkDisplayViewModel : BindableBase
    {
        public ObservableCollection<DerEntity> AllEntities
            => MainWindowViewModel.AllEntities;

        public bool IsDragging { get; set; }
        public EntityDisplayItem DraggedItem { get; set; }

        public Dictionary<int, DerEntity> CanvasEntityMap { get; set; }
            = new Dictionary<int, DerEntity>();

        private ObservableCollection<EntityTypeGroup> _groupedEntities;
        public ObservableCollection<EntityTypeGroup> GroupedEntities
        {
            get => _groupedEntities;
            set => SetProperty(ref _groupedEntities, value);
        }

        public MyICommand AutoArrangeCommand { get; set; }

        public List<(int, int)> Connections { get; set; } = new List<(int, int)>();

        public bool ConnectionExists(int from, int to)
            => Connections.Any(c =>
                (c.Item1 == from && c.Item2 == to) ||
                (c.Item1 == to && c.Item2 == from));

        public void AddConnection(int from, int to)
        {
            if (!ConnectionExists(from, to))
                Connections.Add((from, to));
        }

        public void RemoveConnectionsForIndex(int index)
        {
            Connections.RemoveAll(c => c.Item1 == index || c.Item2 == index);
            Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Loaded,
                new System.Action(() => LineDrawingBehavior.RedrawLines(this)));
        }

        public NetworkDisplayViewModel()
        {
            AutoArrangeCommand = new MyICommand(OnAutoArrange);
            MainWindowViewModel.AllEntities.CollectionChanged +=
                (s, e) => RefreshGroupedEntities();
            RefreshGroupedEntities();
        }

        public void RefreshGroupedEntities()
        {
            var placedIds = new HashSet<int>(
                CanvasEntityMap.Values
                    .Where(e => e != null)
                    .Select(e => e.Id));

            var groups = AllEntities
                .Where(e => !placedIds.Contains(e.Id))
                .GroupBy(e => e.EntityType?.Name ?? "Nepoznat")
                .Select(g => new EntityTypeGroup
                {
                    TypeName = g.Key,
                    Entities = new ObservableCollection<EntityDisplayItem>(
                        g.Select(e => new EntityDisplayItem
                        {
                            Id = e.Id,
                            Name = e.Name,
                            ImagePath = e.EntityType?.ImagePath ?? ""
                        }))
                }).ToList();

            GroupedEntities = new ObservableCollection<EntityTypeGroup>(groups);
        }

        private void OnAutoArrange()
        {
            int slot = 0;
            foreach (var entity in AllEntities)
            {
                bool placed = CanvasEntityMap.Values.Any(e => e?.Id == entity.Id);
                if (placed) continue;

                while (slot < 12 && CanvasEntityMap.ContainsKey(slot) && CanvasEntityMap[slot] != null) slot++;

                if (slot >= 12) break;
                CanvasEntityMap[slot] = entity;
                slot++;
            }

            RefreshGroupedEntities();
            OnPropertyChanged("CanvasEntityMap");
        }

        public void UpdateConnectionsForMove(int oldIndex, int newIndex)
        {
            for (int i = 0; i < Connections.Count; i++)
            {
                var conn = Connections[i];
                int from = conn.Item1 == oldIndex ? newIndex : conn.Item1;
                int to = conn.Item2 == oldIndex ? newIndex : conn.Item2;
                Connections[i] = (from, to);
            }
        }
    }
}
