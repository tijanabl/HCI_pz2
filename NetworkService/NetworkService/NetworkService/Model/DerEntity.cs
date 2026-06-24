using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetworkService.Helpers;

namespace NetworkService.Model
{
    public class DerEntity : ValidationBase
    {
        public static HashSet<int> UsedIds = new HashSet<int>();

        private int _id;
        private string _name;
        private EntityType _entityType;
        private double _currentValue;
        private bool _isValueValid;

        public const double MinValidValue = 1.0;
        public const double MaxValidValue = 5.0;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged("Id"); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged("Name"); }
        }

        public EntityType EntityType
        {
            get => _entityType;
            set { _entityType = value; OnPropertyChanged("EntityType"); OnPropertyChanged("ImagePath"); }
        }

        public string ImagePath => EntityType?.ImagePath ?? "";

        public double CurrentValue
        {
            get => _currentValue;
            set
            {
                _currentValue = value;
                OnPropertyChanged("CurrentValue");
                OnPropertyChanged("CurrentValueDisplay");
                IsValueValid = value >= MinValidValue && value <= MaxValidValue;
            }
        }

        public string CurrentValueDisplay =>
            $"{_currentValue:F2} MW";

        public bool IsValueValid
        {
            get => _isValueValid;
            set { _isValueValid = value; OnPropertyChanged("IsValueValid"); }
        }

        public int? OriginalId { get; set; }

        protected override void ValidateSelf()
        {
            if (_id <= 0)
            {
                ValidationErrors["Id"] = "ID mora biti pozitivan cijeli broj.";
            }
            else if (UsedIds.Contains(_id) && OriginalId != _id)
            {
                ValidationErrors["Id"] = "Entitet sa ovim ID-em vec postoji.";
            }

            if (string.IsNullOrWhiteSpace(_name))
            {
                ValidationErrors["Name"] = "Naziv ne smije biti prazan.";
            }
            else if (_name.Length < 2)
            {
                ValidationErrors["Name"] = "Naziv mora imati najmanje 2 karaktera.";
            }

            if (_entityType == null)
            {
                ValidationErrors["EntityType"] = "Tip entiteta mora biti odabran.";
            }
        }
    }
}
