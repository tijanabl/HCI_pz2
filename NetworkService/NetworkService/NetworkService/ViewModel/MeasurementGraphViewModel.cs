using NetworkService.Helpers;
using NetworkService.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace NetworkService.ViewModel
{
    public class GraphPoint
    {
        public double X { get; set; }           
        public double Y { get; set; }           
        public double Diameter { get; set; }    
        public double Radius => Diameter / 2;
        public string ValueText { get; set; }   
        public string TimeText { get; set; }    
        public Brush FillColor { get; set; }   
        public double LabelX { get; set; }      
        public double LabelY { get; set; }      
        public double LineX2 { get; set; }      
        public double LineY2 { get; set; }      
        public bool HasLine { get; set; }       
        public double CenterX { get; set; }     
        public double CenterY { get; set; }     
    }

    public class MeasurementEntry
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public bool IsValid { get; set; }
    }

    public class MeasurementGraphViewModel : BindableBase
    {
        private const double CanvasW = 900;
        private const double CanvasH = 500;
        private const double MarginL = 60;
        private const double MarginR = 60;
        private const double MarginT = 60;
        private const double MarginB = 80;
        private const double CircleDiameter = 50;

        public ObservableCollection<DerEntity> AllEntities
            => MainWindowViewModel.AllEntities;

        private DerEntity _selectedEntity;
        public DerEntity SelectedEntity
        {
            get => _selectedEntity;
            set
            {
                _selectedEntity = value;
                OnPropertyChanged("SelectedEntity");
                RecalculateGraph();
            }
        }

        private ObservableCollection<GraphPoint> _graphPoints;
        public ObservableCollection<GraphPoint> GraphPoints
        {
            get => _graphPoints;
            set => SetProperty(ref _graphPoints, value);
        }

        private Dictionary<int, List<MeasurementEntry>> _history
            = new Dictionary<int, List<MeasurementEntry>>();

        public MeasurementGraphViewModel()
        {
            GraphPoints = new ObservableCollection<GraphPoint>();
        }

        public void OnNewMeasurement(DerEntity entity, double value)
        {
            if (!_history.ContainsKey(entity.Id))
                _history[entity.Id] = new List<MeasurementEntry>();

            _history[entity.Id].Add(new MeasurementEntry
            {
                Timestamp = DateTime.Now,
                Value = value,
                IsValid = value >= DerEntity.MinValidValue && value <= DerEntity.MaxValidValue
            });

            if (_history[entity.Id].Count > 5)
                _history[entity.Id].RemoveAt(0);

            if (SelectedEntity?.Id == entity.Id)
                RecalculateGraph();
        }

        private void RecalculateGraph()
        {
            GraphPoints.Clear();
            if (SelectedEntity == null) return;

            if (!_history.TryGetValue(SelectedEntity.Id, out var history)
                || history.Count == 0) return;

            double drawW = CanvasW - MarginL - MarginR;
            double drawH = CanvasH - MarginT - MarginB;
            int count = history.Count;

            double minV = history.Min(m => m.Value);
            double maxV = history.Max(m => m.Value);
            if (maxV == minV) { maxV = minV + 1; }

            var centers = new List<(double cx, double cy)>();

            for (int i = 0; i < count; i++)
            {
                double cx = MarginL + (count == 1 ? drawW / 2 : i * drawW / (count - 1));
                double norm = (history[i].Value - minV) / (maxV - minV);
                double cy = MarginT + drawH - norm * drawH;
                centers.Add((cx, cy));
            }

            for (int i = 0; i < count; i++)
            {
                var (cx, cy) = centers[i];
                var m = history[i];

                var pt = new GraphPoint
                {
                    CenterX = cx,
                    CenterY = cy,
                    X = cx - CircleDiameter / 2,
                    Y = cy - CircleDiameter / 2,
                    Diameter = CircleDiameter,
                    ValueText = $"{m.Value:F1}",
                    TimeText = m.Timestamp.ToString("HH:mm:ss"),
                    FillColor = m.IsValid
                        ? new SolidColorBrush(Color.FromRgb(23, 177, 105))
                        : new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                    LabelX = cx - 30,
                    LabelY = CanvasH - MarginB + 8,
                    HasLine = i < count - 1
                };

                if (pt.HasLine)
                {
                    pt.LineX2 = centers[i + 1].cx - cx + CircleDiameter / 2;
                    pt.LineY2 = centers[i + 1].cy - cy + CircleDiameter / 2;
                }

                GraphPoints.Add(pt);
            }
        }
    }
}