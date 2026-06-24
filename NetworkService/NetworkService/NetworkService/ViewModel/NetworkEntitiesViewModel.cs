using NetworkService.Helpers;
using NetworkService.Model;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace NetworkService.ViewModel
{
    public class NetworkEntitiesViewModel : BindableBase
    {
        public MyICommand AddCommand { get; set; }
        public MyICommand DeleteCommand { get; set; }
        public MyICommand SearchCommand { get; set; }
        public MyICommand ClearSearchCommand { get; set; }
        public MyICommand SaveSearchCommand { get; set; }
        public MyICommand ConfirmDeleteCommand { get; set; }
        public MyICommand CancelDeleteCommand { get; set; }

        public ObservableCollection<DerEntity> AllEntities
            => MainWindowViewModel.AllEntities;

        private ObservableCollection<DerEntity> _filteredEntities;
        public ObservableCollection<DerEntity> FilteredEntities
        {
            get => _filteredEntities;
            set => SetProperty(ref _filteredEntities, value);
        }

        private DerEntity _selectedEntity;
        public DerEntity SelectedEntity
        {
            get => _selectedEntity;
            set
            {
                _selectedEntity = value;
                OnPropertyChanged("SelectedEntity");
            }
        }

        public ObservableCollection<EntityType> EntityTypes { get; set; }

        private EntityType _selectedType;
        public EntityType SelectedType
        {
            get => _selectedType;
            set { _selectedType = value; OnPropertyChanged("SelectedType"); }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged("SearchText");
                RefreshFiltered();
            }
        }

        private bool _searchByName = true;
        public bool SearchByName
        {
            get => _searchByName;
            set
            {
                _searchByName = value;
                OnPropertyChanged("SearchByName");
                RefreshFiltered();
            }
        }

        private bool _searchByType;
        public bool SearchByType
        {
            get => _searchByType;
            set
            {
                _searchByType = value;
                OnPropertyChanged("SearchByType");
                RefreshFiltered();
            }
        }
        private string _savedSearchMessage;
        public string SavedSearchMessage
        {
            get => _savedSearchMessage;
            set => SetProperty(ref _savedSearchMessage, value);
        }

        private void OnSaveSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                SavedSearchMessage = "Unesite tekst pretrage prije čuvanja.";
                return;
            }

            string mode = SearchByName ? "naziv" : "tip";
            string key = $"[{mode}] {SearchText}";

            if (SavedSearches.Contains(key))
            {
                SavedSearchMessage = "Ova pretraga je već sačuvana.";
                return;
            }

            SavedSearches.Add(key);
            SavedSearchMessage = $"✓ Pretraga '{key}' sačuvana.";
        }

        public ObservableCollection<string> SavedSearches { get; set; }
            = new ObservableCollection<string>();

        private string _selectedSavedSearch;
        public string SelectedSavedSearch
        {
            get => _selectedSavedSearch;
            set
            {
                _selectedSavedSearch = value;
                OnPropertyChanged("SelectedSavedSearch");
                if (!string.IsNullOrEmpty(value))
                    ApplySavedSearch(value);
            }
        }

        private bool _deleteDialogVisible;
        public bool DeleteDialogVisible
        {
            get => _deleteDialogVisible;
            set => SetProperty(ref _deleteDialogVisible, value);
        }

        private string _deleteConfirmMessage;
        public string DeleteConfirmMessage
        {
            get => _deleteConfirmMessage;
            set => SetProperty(ref _deleteConfirmMessage, value);
        }

        private DerEntity _entityToDelete;

        private bool _toastVisible;
        public bool ToastVisible
        {
            get => _toastVisible;
            set => SetProperty(ref _toastVisible, value);
        }

        private string _toastMessage;
        public string ToastMessage
        {
            get => _toastMessage;
            set => SetProperty(ref _toastMessage, value);
        }

        private bool _toastIsSuccess;
        public bool ToastIsSuccess
        {
            get => _toastIsSuccess;
            set => SetProperty(ref _toastIsSuccess, value);
        }

        private string _typeError;
        public string TypeError
        {
            get => _typeError;
            set => SetProperty(ref _typeError, value);
        }

        private int GetNextId()
        {
            if (AllEntities.Count == 0) return 1;
            return AllEntities.Max(e => e.Id) + 1;
        }

        public NetworkEntitiesViewModel()
        {
            EntityTypes = new ObservableCollection<EntityType>
            {
                new EntityType("Solarni panel",  "pack://application:,,,/Resources/Images/solar.jpg"),
                new EntityType("Vetrogenerator", "pack://application:,,,/Resources/Images/wind.jpg")
            };

            AddCommand = new MyICommand(OnAdd);
            DeleteCommand = new MyICommand(OnDeleteRequest);
            ConfirmDeleteCommand = new MyICommand(OnConfirmDelete);
            CancelDeleteCommand = new MyICommand(OnCancelDelete);
            SearchCommand = new MyICommand(RefreshFiltered);
            ClearSearchCommand = new MyICommand(OnClearSearch);
            SaveSearchCommand = new MyICommand(OnSaveSearch);

            if (AllEntities.Count == 0)
                SeedInitialEntities();

            FilteredEntities = new ObservableCollection<DerEntity>(AllEntities);
            AllEntities.CollectionChanged += (s, e) => RefreshFiltered();
        }

        private void OnAdd()
        {
            if (SelectedType == null)
            {
                TypeError = "Morate odabrati tip entiteta.";
                return;
            }

            TypeError = string.Empty;

            DerEntity entity = new DerEntity
            {
                Id = GetNextId(),
                Name = GenerateName(SelectedType),
                EntityType = SelectedType,
                CurrentValue = 0.0
            };

            DerEntity.UsedIds.Add(entity.Id);
            AllEntities.Add(entity);
            RefreshFiltered();
            ShowToast($"Entitet '{entity.Name}' uspješno dodan.", true);

            SelectedType = null;
            OnPropertyChanged("SelectedType");

            //MainWindowViewModel.RestartMeteringSimulator();
        }

        private string GenerateName(EntityType type)
        {
            int count = AllEntities.Count(e => e.EntityType?.Name == type.Name) + 1;
            return $"{type.Name} {count}";
        }

        private void OnDeleteRequest()
        {
            if (SelectedEntity == null)
            {
                ShowToast("Morate selektovati entitet za brisanje.", false);
                return;
            }

            _entityToDelete = SelectedEntity;
            DeleteConfirmMessage =
                $"Da li ste sigurni da zelite obrisati entitet '{SelectedEntity.Name}'?";
            DeleteDialogVisible = true;
        }

        private void OnConfirmDelete()
        {
            if (_entityToDelete == null) return;

            string name = _entityToDelete.Name;

            var displayVM = MainWindowViewModel.DisplayVM;
            if (displayVM != null)
            {
                int canvasIndex = -1;
                foreach (var kvp in displayVM.CanvasEntityMap)
                {
                    if (kvp.Value?.Id == _entityToDelete.Id)
                    {
                        canvasIndex = kvp.Key;
                        break;
                    }
                }

                if (canvasIndex >= 0)
                {
                    displayVM.CanvasEntityMap.Remove(canvasIndex);
                    displayVM.RemoveConnectionsForIndex(canvasIndex);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var canvas = CanvasDropBehavior.FindCanvasByIndexPublic(canvasIndex);
                        if (canvas != null)
                            CanvasDropBehavior.ResetCanvasVisual(canvas, canvasIndex);
                    });
                }
            }

            DerEntity.UsedIds.Remove(_entityToDelete.Id);
            AllEntities.Remove(_entityToDelete);
            _entityToDelete = null;
            SelectedEntity = null;
            DeleteDialogVisible = false;

            RefreshFiltered();
            ShowToast($"Entitet '{name}' obrisan.", true);

            //MainWindowViewModel.RestartMeteringSimulator();
        }

        private void OnCancelDelete()
        {
            _entityToDelete = null;
            DeleteDialogVisible = false;
        }

        private void RefreshFiltered()
        {
            var query = AllEntities.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string lower = SearchText.ToLower();
                if (SearchByName)
                    query = query.Where(e =>
                        e.Name?.ToLower().Contains(lower) == true);
                else
                    query = query.Where(e =>
                        e.EntityType?.Name?.ToLower().Contains(lower) == true);
            }

            FilteredEntities = new ObservableCollection<DerEntity>(query);
        }

        private void OnClearSearch()
        {
            SearchText = string.Empty;
            SearchByName = true;
            SearchByType = false;
        }

        private void ApplySavedSearch(string saved)
        {
            if (saved.StartsWith("[naziv]"))
            {
                SearchByName = true;
                SearchByType = false;
                SearchText = saved.Replace("[naziv]", "").Trim();
            }
            else if (saved.StartsWith("[tip]"))
            {
                SearchByName = false;
                SearchByType = true;
                SearchText = saved.Replace("[tip]", "").Trim();
            }
        }

        private void ShowToast(string message, bool success)
        {
            ToastMessage = message;
            ToastIsSuccess = success;
            ToastVisible = true;

            DispatcherTimer timer = new DispatcherTimer
            { Interval = TimeSpan.FromSeconds(3) };
            timer.Tick += (s, e) => { ToastVisible = false; timer.Stop(); };
            timer.Start();
        }

        private void SeedInitialEntities()
        {
            var solar = EntityTypes[0];
            var wind = EntityTypes[1];

            AllEntities.Add(new DerEntity
            { Id = 1, Name = "Solarni park Pancevo", EntityType = solar });
            AllEntities.Add(new DerEntity
            { Id = 2, Name = "Vjetropark Kovacica", EntityType = wind });
            AllEntities.Add(new DerEntity
            { Id = 3, Name = "Solarna elektrana DeLasol", EntityType = solar });

            DerEntity.UsedIds.UnionWith(new[] { 1, 2, 3 });
        }
    }
}