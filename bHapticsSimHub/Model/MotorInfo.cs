using LiteDB;
using Newtonsoft.Json;
using System.ComponentModel;

namespace bHapticsSimHub
{
    public class MotorInfo : INotifyPropertyChanged
    {
        private bool _isSelected;
        private int _intensity;
        private int _index;
        private string _name;
        [BsonIgnore]
        public event PropertyChangedEventHandler PropertyChanged;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public int Intensity
        {
            get => _intensity;
            set
            {
                if (_intensity != value)
                {
                    _intensity = value;
                    OnPropertyChanged(nameof(Intensity));
                    OnPropertyChanged(nameof(Width));
                }
            }
        }
        [JsonIgnore]
        public double Width
        {
            get => 118 * (_intensity / 100.0);
        }
        [JsonIgnore]
        public int Index
        {
            get => _index;
            set
            {
                if (_index != value)
                {
                    _index = value;
                    OnPropertyChanged(nameof(Index));
                }
            }
        }
        [BsonIgnore]
        [JsonIgnore]
        public RelayCommand StatusCommonad { get; set; }
        [BsonIgnore]
        [JsonIgnore]
        public RelayCommand PlusCommonad { get; set; }
        [BsonIgnore]
        [JsonIgnore]
        public RelayCommand MinusCommonad { get; set; }

        public MotorInfo()
        {
            StatusCommonad = new RelayCommand((obj) =>
            {
                IsSelected = !IsSelected;
            });

            PlusCommonad = new RelayCommand((obj) =>
            {
                Intensity++;

                if (Intensity > 100) Intensity = 100;
            });

            MinusCommonad = new RelayCommand((obj) =>
            {
                Intensity--;

                if (Intensity < 0) Intensity = 0;
            });
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
