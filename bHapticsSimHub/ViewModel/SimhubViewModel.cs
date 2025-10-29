using bHapticsSimHub.ViewModel;
using FMOD;
using Microsoft.Win32;
using Newtonsoft.Json;
using SimHub.Plugins.DataPlugins.RGBDriver.Settings;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace bHapticsSimHub
{
    public class SimhubViewModel: INotifyPropertyChanged
    {
        private static readonly SimhubViewModel instance = new SimhubViewModel();
        public static SimhubViewModel Instance => instance;
        public ObservableCollection<SimhubProfile> Profiles { get; set; }
        public ObservableCollection<EffectProfile> Items { get; set; }
        public ObservableCollection<EffectProfile> SleevesItems { get; set; }
        public ObservableCollection<EffectProfile> EventItems { get; set; }
        public ObservableCollection<MotorInfo> Motors { get; set; }
        public ObservableCollection<MotorInfo> SleevesMotors { get; set; }
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
        public static RelayCommand SelectProfileCommonad { get; set; }
        public static RelayCommand DeleteProfileCommonad { get; set; }

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

        private bool _isSleevesSelected;
        public bool IsSleevesSelected
        {
            get => _isSleevesSelected;
            set
            {
                if (_isSleevesSelected != value)
                {
                    _isSleevesSelected = value;
                    OnPropertyChanged(nameof(IsSleevesSelected));
                    OnPropertyChanged(nameof(SleevesVisibility));
                }
            }
        }

        private bool _isEventSelected;
        public bool IsEventSelected
        {
            get => _isEventSelected;
            set
            {
                if (_isEventSelected != value)
                {
                    _isEventSelected = value;
                    OnPropertyChanged(nameof(IsEventSelected));
                    OnPropertyChanged(nameof(EventVisibility));
                }
            }
        }

        private SimhubProfile _selectedProfile;
        public SimhubProfile SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                if (_selectedProfile != value)
                {
                    _selectedProfile = value;
                    OnPropertyChanged(nameof(SelectedProfile));
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
        public Visibility SleevesVisibility
        {
            get => _isSleevesSelected ? Visibility.Visible : Visibility.Collapsed;
        }
        public Visibility EventVisibility
        {
            get => _isEventSelected ? Visibility.Visible : Visibility.Collapsed;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string[] ProfileList { get; set; } = { "General" };
        private string[] _profileFileNameList { get; set; } = {"general.bhaptics" };
        private int _profileIndex = 0;
        private bHapticsDB _db;

        static SimhubViewModel()
        {
            
        }

        private SimhubViewModel()
        {
            IsAllSelected = true;
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
            MotorPositionCloseCommonad = new RelayCommand((obj) => { MotorPositionVisible = Visibility.Collapsed; });
            SelectModeCommonad = new RelayCommand(SelectMode);
            ResetCommonad = new RelayCommand(Reset);
            ImportCommonad = new RelayCommand(Import);
            ExportCommonad = new RelayCommand(Export);
            SelectProfileCommonad = new RelayCommand(SelectProfile);
            DeleteProfileCommonad = new RelayCommand(DeleteProfile);

            Profiles = new ObservableCollection<SimhubProfile>();

            InitializeMotors();

            _db = new bHapticsDB();
            _db.Initialized += async() =>
            {
                await InitProfile();

                var result = _db.GetSimhubProfileList();
                SimhubUtils.WriteLog($"Initialized DB result: {result.Count}");
                if (result.Count > 0)
                {
                    foreach(var profile in result)
                    {
                        Profiles.Add(profile);
                    }
                }

                Profiles = new ObservableCollection<SimhubProfile>(Profiles.OrderByDescending(x => x.IsDefault));

                SelectedProfile = Profiles[0];

                Items = new ObservableCollection<EffectProfile>(SelectedProfile.Profile.TactSuit);
                SleevesItems = new ObservableCollection<EffectProfile>(SelectedProfile.Profile.TactSleeve);

                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(SleevesItems));
                OnPropertyChanged(nameof(Profiles));
            };
            _db.Initialize();
        }

        private void DeleteProfile(object obj)
        {
            SimhubUtils.WriteLog("DeleteProfile");
            if (!(obj is SimhubProfile profile))
            {
                return;
            }

            try
            {
                SimhubUtils.WriteLog($"DeleteProfile: {profile.Id}");
                _db.RemoveSimhubProfile(profile.Id);
                var findItem = Profiles.FirstOrDefault(x=> x.Id.Equals(profile.Id));
                Profiles.Remove(findItem);
            }
            catch (Exception ex)
            {
                SimhubUtils.WriteErrorLog($"DeleteProfile Exception {ex.Message}");
            }
        }

        private void SelectProfile(object obj)
        {
            SimhubUtils.WriteLog("SelectProfile");

            if (!(obj is SimhubProfile profile))
            {
                return;
            }

            try
            {
                SimhubUtils.WriteLog($"SelectProfile: {profile.Id}");
                var gprofile = _db.GetSimhubProfile(profile.Id);
                if (gprofile != null)
                {
                    SelectedProfile = gprofile;
                    Items = new ObservableCollection<EffectProfile>(gprofile.Profile.TactSuit);
                    SleevesItems = new ObservableCollection<EffectProfile>(gprofile.Profile.TactSleeve);

                    OnPropertyChanged(nameof(Items));
                    OnPropertyChanged(nameof(SleevesItems));
                }
            }
            catch (Exception ex)
            {
                SimhubUtils.WriteErrorLog($"SelectProfile Exception {ex.Message}");
            }
        }

        private async Task InitProfile()
        {
            try
            {
                Uri resourceUri = new Uri("pack://application:,,,/bHapticsSimHub;component/Profile/general.bhaptics", UriKind.Absolute);
                using (Stream stream = Application.GetResourceStream(resourceUri).Stream)
                using (StreamReader reader = new StreamReader(stream))
                {
                    string jsonContent = reader.ReadToEnd();
                    var result = await SetProfile(jsonContent);
                    if (result != null)
                    {
                        var profile = new SimhubProfile
                        {
                            Profile = result,
                            Id = "General",
                            IsDefault = true
                        };

                        Profiles.Add(profile);
                        SelectedProfile = profile;
                        OnPropertyChanged(nameof(Profiles));
                    }
                }
            }
            catch (Exception ex)
            {
                SimhubUtils.WriteErrorLog($"profile setting Exception {ex.Message}");
            }
        }

        private void Export(object obj)
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
                    if (File.Exists(saveFileDialog.FileName))
                    {
                        File.WriteAllText(saveFileDialog.FileName, "");
                    }

                    var profile = new Profile
                    {
                        TactSuit = new ObservableCollection<EffectProfile>(Items),
                        TactSleeve = new ObservableCollection<EffectProfile>(SleevesItems),
                    };

                    var jsonString = JsonConvert.SerializeObject(profile, Formatting.Indented);
                    File.AppendAllText(saveFileDialog.FileName, jsonString);
                }
                catch (Exception ex)
                {
                    SimhubUtils.WriteErrorLog($"ExportCommonad Exception {ex.Message}");
                }
            }
        }

        private async Task<Profile> SetProfile(string content)
        {
            SimhubUtils.WriteLog("SetProfile");
            var data = JsonConvert.DeserializeObject<Profile>(content);
            if (data != null)
            {
                Items = new ObservableCollection<EffectProfile>(data.TactSuit);
                SleevesItems = new ObservableCollection<EffectProfile>(data.TactSleeve);

                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(SleevesItems));

                SimhubUtils.WriteLog("SetProfile OnPropertyChanged");

                return data;
            }

            return null;
        }

        private void Import(object obj)
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
                        Task.Run(async() =>
                        {
                            var result = await SetProfile(content);
                            if (result != null)
                            {
                                var profile = new SimhubProfile
                                {
                                    Profile = result,
                                    Id = Path.GetFileNameWithoutExtension(filePath),
                                    IsDefault = false
                                };

                                SimhubUtils.WriteLog($"Import Id {profile.Id}");

                                var gprofile = _db.GetSimhubProfile(profile.Id);
                                if (gprofile == null)
                                {
                                    _db.InsertSimhubProfile(profile);
                                    SelectedProfile = profile;
                                    Profiles.Add(profile);
                                }
                                else
                                {
                                    MessageBox.Show("A profile with the same name already exists.");
                                }
                                
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    SimhubUtils.WriteErrorLog($"ImportCommonad Exception {ex.Message}");
                }
            }
        }

        private void Reset(object obj)
        {
            try
            {
                Uri resourceUri = new Uri("pack://application:,,,/bHapticsSimHub;component/Profile/general.bhaptics", UriKind.Absolute);
                using (Stream stream = Application.GetResourceStream(resourceUri).Stream)
                using (StreamReader reader = new StreamReader(stream))
                {
                    string jsonContent = reader.ReadToEnd();
                    var data = JsonConvert.DeserializeObject<Profile>(jsonContent);
                    if (data != null)
                    {
                        Items = new ObservableCollection<EffectProfile>(data.TactSuit);
                        SleevesItems = new ObservableCollection<EffectProfile>(data.TactSleeve);

                        OnPropertyChanged(nameof(Items));
                        OnPropertyChanged(nameof(SleevesItems));
                    }
                }
            }
            catch (Exception ex)
            {
                SimhubUtils.WriteErrorLog($"ResetCommonad Exception {ex.Message}");
            }
        }

        private void SelectMode(object obj)
        {
            if (!(obj is string mode))
            {
                return;
            }

            if (mode.Equals("all"))
            {
                IsAllSelected = true;
                IsFrontSelected = false;
                IsBackSelected = false;
                IsSleevesSelected = false;
                IsEventSelected = false;
            }
            else if (mode.Equals("front"))
            {
                IsAllSelected = false;
                IsFrontSelected = true;
                IsBackSelected = false;
                IsSleevesSelected = false;
                IsEventSelected = false;
            }
            else if (mode.Equals("back"))
            {
                IsAllSelected = false;
                IsFrontSelected = false;
                IsBackSelected = true;
                IsSleevesSelected = false;
                IsEventSelected = false;
            }
            else if (mode.Equals("Sleeves"))
            {
                IsAllSelected = false;
                IsFrontSelected = false;
                IsBackSelected = false;
                IsSleevesSelected = true;
                IsEventSelected = false;
            }
            else if (mode.Equals("Event"))
            {
                IsAllSelected = false;
                IsFrontSelected = false;
                IsBackSelected = false;
                IsSleevesSelected = false;
                IsEventSelected = true;
            }
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

        public void SetProfileSleevesMotorIntensity(int index, int intensity)
        {
            if (index < 1)
            {
                return;
            }

            if (index > 16)
            {
                return;
            }

            foreach (var item in SleevesItems)
            {
                item.MotorInfos[index - 1].Intensity = intensity;
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

            SleevesMotors = new ObservableCollection<MotorInfo>();

            // Left Sleeves
            for (int i = 1; i <= 3; i++)
            {
                SleevesMotors.Add(new MotorInfo
                {
                    Name = $"Left {i}",
                    Intensity = 50,
                    Index = i,
                });
            }

            // Right Sleeves
            for (int i = 1; i <= 3; i++)
            {
                SleevesMotors.Add(new MotorInfo
                {
                    Name = $"Right {i}",
                    Intensity = 50,
                    Index = i + 3,
                });
            }
        }
    }
}