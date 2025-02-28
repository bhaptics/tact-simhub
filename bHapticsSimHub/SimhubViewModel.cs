using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace bHapticsSimHub
{
    public class MotorInfo : INotifyPropertyChanged
    {
        private bool _isSelected;
        private int _intensity;
        private int _index;
        private string _name;

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

        public double Width
        {
            get => 118 * (_intensity / 100.0);
        }

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

        public RelayCommand StatusCommonad { get; set; }
        public RelayCommand PlusCommonad { get; set; }
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

    public class EffectProfile
    {
        public string Name { get; set; }
        public ObservableCollection<MotorInfo> MotorInfos { get; set; }
        public ObservableCollection<MotorInfo> FrontMotors
        {
            get
            {
                return new ObservableCollection<MotorInfo>(MotorInfos.Take(8).ToList());
            }
        }

        public ObservableCollection<MotorInfo> BackMotors
        {
            get
            {
                return new ObservableCollection<MotorInfo>(MotorInfos.Skip(Math.Max(0, MotorInfos.Count - 8)).Take(8).ToList());
            }
        }
    }

    public class SimhubViewModel: INotifyPropertyChanged
    {
        private static readonly SimhubViewModel instance = new SimhubViewModel();
        public static SimhubViewModel Instance => instance;
        public ObservableCollection<EffectProfile> Items { get; set; }
        public ObservableCollection<MotorInfo> Motors { get; set; }
        public ObservableCollection<MotorInfo> FrontMotors
        {
            get
            {
                return new ObservableCollection<MotorInfo>(Motors.Take(8).ToList());
            }
        }

        public ObservableCollection<MotorInfo> BackMotors
        {
            get
            {
                return new ObservableCollection<MotorInfo>(Motors.Skip(Math.Max(0, Motors.Count - 8)).Take(8).ToList());
            }
        }

        public int LowForceSpeed { get; set; } = 15;
        public int HighForceMaxSpeed { get; set; } = 95;

        public RelayCommand MotorPositionViewCommonad { get; set; }
        public RelayCommand MotorPositionCloseCommonad { get; set; }
        public RelayCommand ImportCommonad { get; set; }
        public RelayCommand ExportCommonad { get; set; }
        public RelayCommand ResetCommonad { get; set; }
        public RelayCommand SelectModeCommonad { get; set; }

        private Visibility _motorPositionVisible = Visibility.Collapsed;
        public Visibility MotorPositionVisible
        {
            get => _motorPositionVisible;
            set
            {
                if (_motorPositionVisible != value)
                {
                    _motorPositionVisible = value;
                    OnPropertyChanged(nameof(MotorPositionVisible));
                }
            }
        }

        private bool _isAllSelected;
        public bool IsAllSelected
        {
            get => _isAllSelected;
            set
            {
                if (_isAllSelected != value)
                {
                    _isAllSelected = value;
                    OnPropertyChanged(nameof(IsAllSelected));
                    OnPropertyChanged(nameof(AllVisibility));
                }
            }
        }

        private bool _isFrontSelected;
        public bool IsFrontSelected
        {
            get => _isFrontSelected;
            set
            {
                if (_isFrontSelected != value)
                {
                    _isFrontSelected = value;
                    OnPropertyChanged(nameof(IsFrontSelected));
                    OnPropertyChanged(nameof(FrontVisibility));
                }
            }
        }

        private bool _isBackSelected;
        public bool IsBackSelected
        {
            get => _isBackSelected;
            set
            {
                if (_isBackSelected != value)
                {
                    _isBackSelected = value;
                    OnPropertyChanged(nameof(IsBackSelected));
                    OnPropertyChanged(nameof(BackVisibility));
                }
            }
        }

        public Visibility AllVisibility
        {
            get => _isAllSelected ? Visibility.Visible : Visibility.Collapsed;
        }

        public Visibility FrontVisibility
        {
            get => _isFrontSelected ? Visibility.Visible : Visibility.Collapsed;
        }

        public Visibility BackVisibility
        {
            get => _isBackSelected ? Visibility.Visible : Visibility.Collapsed;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        static SimhubViewModel()
        {
        }

        private SimhubViewModel()
        {
            IsAllSelected = true;

            SelectModeCommonad = new RelayCommand((obj) => 
            {
                if(!(obj is string mode))
                {
                    return;
                }

                if (mode.Equals("all"))
                {
                    IsAllSelected = true;
                    IsFrontSelected = false;
                    IsBackSelected = false;
                }
                else if (mode.Equals("front"))
                {
                    IsAllSelected = false;
                    IsFrontSelected = true;
                    IsBackSelected = false;
                }
                else if (mode.Equals("back"))
                {
                    IsAllSelected = false;
                    IsFrontSelected = false;
                    IsBackSelected = true;
                }
            });

            MotorPositionViewCommonad = new RelayCommand((obj) =>
            {
                if(MotorPositionVisible == Visibility.Collapsed)
                {
                    MotorPositionVisible = Visibility.Visible;
                }
                else if (MotorPositionVisible == Visibility.Visible)
                {
                    MotorPositionVisible = Visibility.Collapsed;
                }
            });

            MotorPositionCloseCommonad = new RelayCommand((obj) =>
            {
                MotorPositionVisible = Visibility.Collapsed;
            });

            ResetCommonad = new RelayCommand((obj) =>
            {
                try
                {
                    Uri resourceUri = new Uri("pack://application:,,,/bHapticsSimHub;component/reset.bhaptics", UriKind.Absolute);
                    using (Stream stream = Application.GetResourceStream(resourceUri).Stream)
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string jsonContent = reader.ReadToEnd();
                        var defaultProfiles = JsonConvert.DeserializeObject<ObservableCollection<EffectProfile>>(jsonContent);

                        if (defaultProfiles != null && defaultProfiles.Any())
                        {
                            Items = new ObservableCollection<EffectProfile>(defaultProfiles);
                            OnPropertyChanged(nameof(Items));
                        }
                    }
                }
                catch (Exception ex)
                {
                    SimHub.Logging.Current.Error($"[bHaptics] ResetCommonad Exception {ex.Message}", ex);
                }
            });

            ImportCommonad = new RelayCommand((obj) =>
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "bhaptics Profile 파일|*.bhaptics|텍스트 파일|*.txt|모든 파일|*.*",
                    FilterIndex = 1
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        string filePath = openFileDialog.FileName;
                        string content = File.ReadAllText(filePath);

                        if (Path.GetExtension(filePath).ToLower() == ".bhaptics")
                        {
                            // JSON 파일 처리
                            var data = JsonConvert.DeserializeObject<ObservableCollection<EffectProfile>>(content);
                            if (data != null && data.Any())
                            {
                                Items = new ObservableCollection<EffectProfile>(data);
                                OnPropertyChanged(nameof(Items));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        SimHub.Logging.Current.Error($"[bHaptics] ImportCommonad Exception {ex.Message}", ex);
                    }
                }
            } );

            ExportCommonad = new RelayCommand((obj) =>
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "bhaptics Profile 파일|*.bhaptics|텍스트 파일|*.txt",
                    FilterIndex = 1,
                    DefaultExt = "bhaptics"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        var jsonString = JsonConvert.SerializeObject(Items, Formatting.Indented);
                        File.WriteAllText(saveFileDialog.FileName, jsonString);
                    }
                    catch (Exception ex)
                    {
                        SimHub.Logging.Current.Error($"[bHaptics] ExportCommonad Exception {ex.Message}", ex);
                    }
                }
            });

            Items = new ObservableCollection<EffectProfile>(){
                new EffectProfile(){
                    Name = "ABS Active",
                    MotorInfos = new ObservableCollection<MotorInfo>()
                    {
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                    }
                },
                new EffectProfile(){
                    Name = "RPMS",
                    MotorInfos = new ObservableCollection<MotorInfo>()
                    {
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 80
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 80
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 60
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                    }
                },
                new EffectProfile(){
                    Name = "Speed",
                    MotorInfos = new ObservableCollection<MotorInfo>()
                    {
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 3
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 3
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 45
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 45
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 45
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 45
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 45
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                    }
                },
                new EffectProfile(){
                    Name = "TC Active",
                    MotorInfos = new ObservableCollection<MotorInfo>()
                    {
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                    }
                },
                new EffectProfile(){
                    Name = "Gear Shift",
                    MotorInfos = new ObservableCollection<MotorInfo>()
                    {
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 100
                        },
                    }
                },
                new EffectProfile(){
                    Name = "FrontLeft WheelRumble",
                    MotorInfos = new ObservableCollection<MotorInfo>()
                    {
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                    }
                },
                new EffectProfile(){
                    Name = "FrontRight WheelRumble",
                    MotorInfos = new ObservableCollection<MotorInfo>()
                    {
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                    }
                },
                new EffectProfile(){
                    Name = "RearLeft WheelRumble",
                    MotorInfos = new ObservableCollection<MotorInfo>()
                    {
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                    }
                },
                new EffectProfile(){
                    Name = "RearRight WheelRumble",
                    MotorInfos = new ObservableCollection<MotorInfo>()
                    {
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 40
                        },
                    }
                },
                new EffectProfile(){
                    Name = "Left Slip",
                    MotorInfos = new ObservableCollection<MotorInfo>()
                    {
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                    }
                },
                new EffectProfile(){
                    Name = "Right Slip",
                    MotorInfos = new ObservableCollection<MotorInfo>()
                    {
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = true,
                            Intensity = 50
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                        new MotorInfo()
                        {
                            IsSelected = false,
                            Intensity = 40
                        },
                    }
                },
            };

            InitializeMotors();
        }

        public void SetProfileMotorIntensity(int index, int intensity)
        {
            if (index < 1) 
            {
                return;
            }

            if (index > 16)
            {
                return;
            }

            foreach (var item in Items)
            {
                item.MotorInfos[index-1].Intensity = intensity;
            }
        }


        private void InitializeMotors()
        {
            Motors = new ObservableCollection<MotorInfo>();

            // Front motors
            for (int i = 1; i <= 8; i++)
            {
                Motors.Add(new MotorInfo
                {
                    Name = $"Front {i}",
                    Intensity = 50,
                    Index = i,
                });
            }

            // Back motors
            for (int i = 1; i <= 8; i++)
            {
                Motors.Add(new MotorInfo
                {
                    Name = $"Back {i}",
                    Intensity = 50,
                    Index = i + 8,
                });
            }
        }
    }
}
