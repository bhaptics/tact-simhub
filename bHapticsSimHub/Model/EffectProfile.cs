using LiteDB;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace bHapticsSimHub
{
    public class SimhubProfile
    {
        public Profile Profile { get; set; }
        public string Id { get; set; }
        public bool IsDefault { get; set; }
        public int VersionCode { get; set; }
        public Visibility IsVisible
        {
            get
            {
                if (IsDefault) return Visibility.Collapsed;
                return Visibility.Visible;
            }
        }
    }

    public class Profile
    {
        public ObservableCollection<EffectProfile> TactSuit { get; set; }
        public ObservableCollection<EffectProfile> TactSleeve { get; set; }
    }

    public class EffectProfile: INotifyPropertyChanged
    {
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        [BsonIgnore]
        public event PropertyChangedEventHandler PropertyChanged;
        [BsonIgnore]
        [JsonIgnore]
        public RelayCommand StatusCommonad { get; set; }
        [BsonIgnore]
        [JsonIgnore]
        public RelayCommand FrontStatusCommonad { get; set; }
        [BsonIgnore]
        [JsonIgnore]
        public RelayCommand BackStatusCommonad { get; set; }
        public string Name { get; set; }
        public ObservableCollection<MotorInfo> MotorInfos { get; set; }

        [JsonIgnore]
        public ObservableCollection<MotorInfo> FrontMotors
        {
            get
            {
                return new ObservableCollection<MotorInfo>(MotorInfos.Take(8).ToList());
            }
        }
        [JsonIgnore]
        public ObservableCollection<MotorInfo> BackMotors
        {
            get
            {
                return new ObservableCollection<MotorInfo>(MotorInfos.Skip(Math.Max(0, MotorInfos.Count - 8)).Take(8).ToList());
            }
        }
        [BsonIgnore]
        [JsonIgnore]
        private bool _isSelected;
        [BsonIgnore]
        [JsonIgnore]
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
        [BsonIgnore]
        [JsonIgnore]
        public double Width
        {
            get => 118 * (_intensity / 100.0);
        }
        [BsonIgnore]
        private int _intensity = 50;
        [JsonIgnore]
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
        [BsonIgnore]
        [JsonIgnore]
        public RelayCommand PlusCommonad { get; set; }
        [BsonIgnore]
        [JsonIgnore]
        public RelayCommand MinusCommonad { get; set; }

        public EffectProfile()
        {
            StatusCommonad = new RelayCommand((obj) =>
            {
                IsSelected = !IsSelected;
                foreach (var motor in MotorInfos)
                {
                    motor.IsSelected = IsSelected;
                }
            });

            FrontStatusCommonad = new RelayCommand((obj) =>
            {
                IsSelected = !IsSelected;
                foreach (var motor in FrontMotors)
                {
                    motor.IsSelected = IsSelected;
                }
            });

            BackStatusCommonad = new RelayCommand((obj) =>
            {
                IsSelected = !IsSelected;
                foreach (var motor in BackMotors)
                {
                    motor.IsSelected = IsSelected;
                }
            });

            PlusCommonad = new RelayCommand((obj) =>
            {
                if (!(obj is string pos))
                {
                    return;
                }

                Intensity++;

                if (Intensity > 100) Intensity = 100;

                if (pos.Equals("front"))
                {
                    foreach (var motor in FrontMotors)
                    {
                        motor.Intensity = Intensity;
                    }
                }
                else if (pos.Equals("back"))
                {
                    foreach (var motor in BackMotors)
                    {
                        motor.Intensity = Intensity;
                    }
                }
                else
                {
                    foreach (var motor in MotorInfos)
                    {
                        motor.Intensity = Intensity;
                    }
                }
            });

            MinusCommonad = new RelayCommand((obj) =>
            {
                if (!(obj is string pos))
                {
                    return;
                }

                Intensity--;

                if (Intensity < 0) Intensity = 0;

                if (pos.Equals("front"))
                {
                    foreach (var motor in FrontMotors)
                    {
                        motor.Intensity = Intensity;
                    }
                }
                else if (pos.Equals("back"))
                {
                    foreach (var motor in BackMotors)
                    {
                        motor.Intensity = Intensity;
                    }
                }
                else
                {
                    foreach (var motor in MotorInfos)
                    {
                        motor.Intensity = Intensity;
                    }
                }
            });
        }
    }
}
