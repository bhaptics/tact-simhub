using GameReaderCommon;
using GameReaderCommon.Feedback;
using SimHub.Plugins;
using System;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace bHapticsSimHub
{
    [PluginDescription("bHaptics SimHub Plugin")]
    [PluginAuthor("bHaptics")]
    [PluginName("bHaptics SimHub Plugin")]
    public class bHapticsSimHub : IPlugin, IDataPlugin, IWPFSettingsV2
    {
        private static string _previousGear = null;
        private double previousSpeed = 0;
        private const int UPDATE_INTERVAL_MS = 15;
        private const double COLLISION_THRESHOLD = -35;
        private const int COLLISION_CHECK_FRAMES = 4;
        private const double MIN_SPEED_DIFF = 10; // 최소 속도 차이 (노이즈 필터링)

        private double[] speedBuffer = new double[COLLISION_CHECK_FRAMES];
        private int currentIndex = 0;
        private bool bufferFilled = false;
        private int _playCounterLeft = 0;
        private int _playCounterRight = 0;
        private int _playCounterLeftHigh = 0;
        private int _playCounterRightHigh = 0;

        private const double DECEL_THRESHOLD = -2.0; // m/s²
        private const double WHEEL_SLIP_THRESHOLD = 0.1;
        private const double TRACTION_LOSS_THRESHOLD = 0.2;

        private BhapticsWebsocket _websocket;
        public PluginManager PluginManager { get; set; }

        string IWPFSettingsV2.LeftMenuTitle => "bHaptics SimHub Plugin";

        public ImageSource PictureIcon => new BitmapImage(new Uri("pack://application:,,,/bHapticsSimHub;component/bhaptics_icon.ico"));

        private void WriteLog(string message)
        {
            SimHub.Logging.Current.Info($"[bHaptics] {message}");
        }

        public void Init(PluginManager pluginManager)
        {
            this.PluginManager = pluginManager;
            _websocket = new BhapticsWebsocket("67637d8ee5f9a222c0470965", "yMMWXvSZ6Usw5GvR6Tks");
            WriteLog($"Plugin Initialized!");
        }

        public void End(PluginManager pluginManager)
        {
        }

        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            return new SettingView(this);
        }

        private int[] GetMotorValue(ObservableCollection<MotorInfo> motorInfo)
        {
            var res = new int[32];

            for(var i  = 0; i < motorInfo.Count; i++)
            {
                if (i < 4)
                {
                    if (motorInfo[i].IsSelected)
                    {
                        res[i] = motorInfo[i].Intensity;
                        res[i + 4] = motorInfo[i].Intensity;
                    }
                }
                else if (i < 8)
                {
                    if (motorInfo[i].IsSelected)
                    {
                        res[i + 4] = motorInfo[i].Intensity;
                        res[i + 4 + 4] = motorInfo[i].Intensity;
                    }
                }
                else if (i < 12)
                {
                    if (motorInfo[i].IsSelected)
                    {
                        res[i + 8 ] = motorInfo[i].Intensity;
                        res[i + 4 + 8 ] = motorInfo[i].Intensity;
                    }
                }
                else
                {
                    if (motorInfo[i].IsSelected)
                    {
                        res[i + 12 ] = motorInfo[i].Intensity;
                        res[i + 4 + 12 ] = motorInfo[i].Intensity;
                    }
                }
            }

            //WriteLog($"GetMotorValue res: {string.Join(", ", res)}");
            return res;
        }

        private int[] MotorAdd(int[] a, double b)
        {
            var res = new int[a.Length];

            for (var i = 0; i < a.Length; i++)
            {
                res[i] = (int)(a[i] * b);
            }

            return res;
        }

        private int[] MotorAdd(int[] a, int[] b)
        {
            var res = new int[a.Length];

            for(var i = 0; i < a.Length; i++)
            {
                res[i] = MotorAdd(a[i], b[i]);
            }

            return res;
        }

        private int MotorAdd(int a, int b)
        {
            int result = Math.Max(a, b);

            if (a <= 0 || b <= 0)
            {
                return result;
            }

            int diff = Math.Abs(a - b);
            if (diff <= 1)
            {
                return Math.Min(100, result + 3);
            }

            if (diff <= 3)
            {
                return Math.Min(100, result + 2);
            }
            if (diff <= 9)
            {
                return Math.Min(100, result + 1);
            }

            return result;
        }

        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            // GameData null 체크
            if (data == null)
            {
                //WriteLog("DataUpdate: GameData is null");
                return;
            }

            //WriteLog($"DataUpdate GameRunning: {data.GameRunning}");

            // NewData null 체크
            if (data.NewData == null)
            {
                //WriteLog("DataUpdate: NewData is null");
                return;
            }

            // 게임이 실행 중일 때만 데이터 로깅
            if (data.GameRunning)
            {
                WriteLog($"DataUpdate SpeedKmh: {data.NewData.SpeedKmh}");
                WriteLog($"DataUpdate SpeedMph: {data.NewData.SpeedMph}");
                WriteLog($"DataUpdate Throttle: {data.NewData.Throttle}");
                WriteLog($"DataUpdate Brake: {data.NewData.Brake}");
                WriteLog($"DataUpdate BrakeBias: {data.NewData.BrakeBias}");
                WriteLog($"DataUpdate CarCoordinates({data.NewData.CarCoordinates.Length}): {string.Join(",", data.NewData.CarCoordinates)}");
               
                WriteLog($"DataUpdate FrontLeftWheelSlip: {data.NewData.FeedbackData.FrontLeftWheelSlip}");
                WriteLog($"DataUpdate FrontRightWheelSlip: {data.NewData.FeedbackData.FrontRightWheelSlip}");
                WriteLog($"DataUpdate RearLeftWheelSlip: {data.NewData.FeedbackData.RearLeftWheelSlip}");
                WriteLog($"DataUpdate RearRightWheelSlip: {data.NewData.FeedbackData.RearRightWheelSlip}");

                WriteLog($"DataUpdate FeedbackData.WheelInGrassOrGravel: {data.NewData.FeedbackData.WheelInGrassOrGravel[0]}");
                WriteLog($"DataUpdate FeedbackData.WheelInGrassOrGravel: {data.NewData.FeedbackData.WheelInGrassOrGravel[1]}");
                WriteLog($"DataUpdate FeedbackData.WheelInGrassOrGravel: {data.NewData.FeedbackData.WheelInGrassOrGravel[2]}");
                WriteLog($"DataUpdate FeedbackData.WheelInGrassOrGravel: {data.NewData.FeedbackData.WheelInGrassOrGravel[3]}");

                WriteLog($"DataUpdate OrientationPitch: {data.NewData.OrientationPitch}");
                WriteLog($"DataUpdate OrientationRoll: {data.NewData.OrientationRoll}");
                WriteLog($"DataUpdate OrientationYaw: {data.NewData.OrientationYaw}");

                WriteLog($"DataUpdate IsFlying: {data.NewData.FeedbackData.IsFlying}");

                var values = new int[32];

                if (data.NewData.ABSActive > 0)
                {
                    // Trigger ABS feedback effect
                    ApplyEffect("ABS_ACTIVE", 1.0);
                    _websocket.Play(GetMotorValue(SimhubViewModel.Instance.Items[0].MotorInfos));
                }

                // Traction Control Effects
                if (data.NewData.TCActive > 0)
                {
                    ApplyEffect("TC_ACTIVE", 1.0);
                    _websocket.Play(GetMotorValue(SimhubViewModel.Instance.Items[3].MotorInfos));
                }

                if (CheckGearChange(data.NewData.Gear))
                {
                    ApplyEffect("GEAR_SHIFT", 1.0);
                    _websocket.Play(GetMotorValue(SimhubViewModel.Instance.Items[4].MotorInfos));
                }

                if (data.NewData.Rpms > 0)
                {
                    var rpm = CalculateRpmOutput(data.NewData.Rpms, data.NewData.MaxRpm);
                    ApplyEffect("RPM_RATIO", rpm);
                    WriteLog($"DataUpdate Rpms: {data.NewData.Rpms},  MaxRpm: {data.NewData.MaxRpm}, rpm ratio: {rpm} ");
                    var rpmArr = GetMotorValue(SimhubViewModel.Instance.Items[1].MotorInfos);

                    values = MotorAdd(values, MotorAdd(rpmArr, rpm));
                }
                var speed = CalculateSpeedOutput(data.NewData.SpeedKmh, data.NewData.MaxSpeedKmh, SimhubViewModel.Instance.HighForceMaxSpeed);

                if (data.NewData.SpeedKmh > SimhubViewModel.Instance.LowForceSpeed)
                {
                    ApplyEffect("SPEED_RATIO", speed);
                    WriteLog($"DataUpdate SpeedKmh: {data.NewData.SpeedKmh},  MaxSpeedKmh: {data.NewData.MaxSpeedKmh}, SPEED_RATIO: {speed}, duration: {20 / speed} ");

                    var speedArr = GetMotorValue(SimhubViewModel.Instance.Items[2].MotorInfos);

                    values = MotorAdd(values, MotorAdd(speedArr, speed));
                }

                if(data.NewData.SpeedKmh > 10 && data.NewData.SpeedKmh < 50 && data.NewData.Throttle >= 100)
                {
                    //_websocket.PlayEvent("simhub_initial_acceleration");
                }

                if(data.NewData.Brake > 0 && data.NewData.SpeedKmh > 10)
                {
                    _websocket.PlayEvent("break", (float)(data.NewData.Brake / 100.0f));
                }

                var internalLowSpeed = 20;
                var internalHighSpeed = 5;
                var internalDurationSpeed = 10;

                if (data.NewData.FeedbackData.FrontLeftWheelRumble >= 1)
                {
                    var res = GetMotorValue(SimhubViewModel.Instance.Items[5].MotorInfos);
                    if (data.NewData.SpeedKmh > 50)
                    {
                        
                        if (_playCounterLeftHigh % internalHighSpeed == 0)  // 3은 진동 간격을 조절하는 값입니다. 필요에 따라 조정 가능
                        {
                            _websocket.Play(MotorAdd(res, speed), internalDurationSpeed);
                        }
                        _playCounterLeftHigh++;
                    }
                    else
                    {
                        if (_playCounterLeft % internalLowSpeed == 0)  // 3은 진동 간격을 조절하는 값입니다. 필요에 따라 조정 가능
                        {
                            _websocket.Play(res, 20);
                        }
                        _playCounterLeft++;
                    }
                }

                if (data.NewData.FeedbackData.FrontRightWheelRumble >= 1)
                {
                    var res = GetMotorValue(SimhubViewModel.Instance.Items[6].MotorInfos);

                    if (data.NewData.SpeedKmh > 50)
                    {
                        
                        if (_playCounterRightHigh % internalHighSpeed == 0)  // 3은 진동 간격을 조절하는 값입니다. 필요에 따라 조정 가능
                        {
                            _websocket.Play(MotorAdd(res, speed), internalDurationSpeed);
                        }
                        _playCounterRightHigh++;
                    }
                    else
                    {
                        if (_playCounterRight % internalLowSpeed == 0)  // 3은 진동 간격을 조절하는 값입니다. 필요에 따라 조정 가능
                        {
                            _websocket.Play(res, 20);
                        }
                        _playCounterRight++;
                    }
                }
                /*
                if (data.NewData.FeedbackData.RearLeftWheelRumble >= 1)
                {
                    _websocket.Play(GetMotorValue(SimhubViewModel.Instance.Items[7].MotorInfos), 20);
                }

                if (data.NewData.FeedbackData.RearRightWheelRumble >= 1)
                {
                    _websocket.Play(GetMotorValue(SimhubViewModel.Instance.Items[8].MotorInfos), 20);
                }
                */

                var slipinfo = AnalyzeSlip(data.NewData.FeedbackData);
                var yaw = slipinfo.direction;
                WriteLog($"slipinfo direction: {slipinfo.direction}, intensity: {slipinfo.intensity}, {slipinfo.isSlipping}");

                if (slipinfo.isSlipping)
                {
                    if (yaw == 1)
                    {
                        _websocket.Play(MotorAdd(GetMotorValue(SimhubViewModel.Instance.Items[10].MotorInfos), slipinfo.intensity), 20);
                    }
                    else if (yaw == 2)
                    {
                        _websocket.Play(MotorAdd(GetMotorValue(SimhubViewModel.Instance.Items[9].MotorInfos), slipinfo.intensity), 20);
                    }
                }

                // Acceleration G-Force (Rear)
                if (data.NewData.AccelerationSurge.HasValue)
                {
                    double rearGForce = data.NewData.AccelerationSurge.Value;
                    var res = Math.Max(0, rearGForce);

                    if (res > 0)
                    {
                        //WriteLog($"ACCELERATION_G_FORCE_REAR {res}");
                    }

                    //ApplyEffect("ACCELERATION_G_FORCE_REAR", res);
                }

                // Deceleration G-Force (Front)
                if (data.NewData.AccelerationSurge.HasValue)
                {
                    double frontGForce = -data.NewData.AccelerationSurge.Value;
                    var res = Math.Max(0, frontGForce);

                    if(res < 0)
                    {
                        //WriteLog($"DECELERATION_G_FORCE_FRONT {res}");
                    }
                    //ApplyEffect("DECELERATION_G_FORCE_FRONT", res);
                }

                if (CheckSuddenSpeedDrop(data.NewData.SpeedKmh))
                {
                    WriteLog("CheckSuddenSpeedDrop");
                    _websocket.PlayEvent("car_crash", 1.0f);
                }


                WriteLog($"DataUpdate IsInPit  : {data.NewData.IsInPit}");
                WriteLog($"DataUpdate IsInPitLane  : {data.NewData.IsInPitLane}");

                if (data.NewData.IsInPitLane == 1)
                {
                    values = MotorAdd(values, 0.1);
                }

                _websocket.Play(values);
            }
            else
            {
                WriteLog("Game is not running");
            }
        }
        private void ApplyEffect(string effectName, double intensity)
        {
            // Implementation would connect to your force feedback system
            // This is a placeholder for the actual force feedback implementation
            WriteLog($"Applying effect {effectName} with intensity {intensity}");
        }

        public double CalculateRpmOutput(double currentRpm, double maxRpm)
        {
            // RPM을 백분율로 변환
            double rpmPercentage = (currentRpm / maxRpm) * 100;
            WriteLog($"CalculateRpmOutput rpmPercentage: {rpmPercentage}, currentRpm: {currentRpm}, maxRpm: {maxRpm}");
            /*
            // 20% 미만인 경우
            if (rpmPercentage < 20)
            {
                return 0;
            }

            // 20-40% 구간 (급격한 초기 상승 후 완만)
            if (rpmPercentage <= 40)
            {
                double t = (rpmPercentage - 20) / 20; // 20-40% 구간을 0-1로 정규화
                return (95 * Math.Sqrt(t)) / 100; // 제곱근 함수로 급격한 초기 상승 구현
            }

            // 40-50% 구간 (급격한 하락)
            if (rpmPercentage <= 50)
            {
                double t = (rpmPercentage - 40) / 10; // 40-50% 구간을 0-1로 정규화
                return (95 * Math.Pow(1 - t, 2)) / 100; // 이차함수로 급격한 하락 구현
            }
            */

            // 85-100% 구간 (선형 증가)
            if (rpmPercentage >= 85)
            {
                // 0에서 0.60까지 선형 증가
                double t = (rpmPercentage - 85) / 15; // 85-100% 구간을 0-1로 정규화
                return (60 * t) / 100; // 0에서 0.60까지 선형 증가
            }

            // 50-85% 구간
            return 0;
        }

        private bool DetermineSurface(double slip, double rumble)
        {
            if (rumble == 0)
                return false;

            if (rumble >= 1)
            {
                if (slip > 3.0f)
                    return true;  // 매우 미끄러운 표면 (자갈)
                else if (slip > 1.0f)
                    return true;  // 잔디나 거친 표면
                else
                    return false; // rumble이 있어도 slip이 낮으면 아스팔트
            }

            return false;
        }

        public double CalculateSpeedOutput(double currentSpeed, double maxSpeed, double maxSpeedPercent)
        {
            // maxSpeedPercent가 0-100 범위를 벗어나면 조정
            maxSpeedPercent = Math.Max(0, Math.Min(100, maxSpeedPercent));

            // maxSpeedPercent에 따른 실제 최대 속도 계산
            double adjustedMaxSpeed = maxSpeed * (maxSpeedPercent / 100.0);

            // 현재 속도가 조정된 최대 속도를 넘지 않도록 제한
            currentSpeed = Math.Min(currentSpeed, adjustedMaxSpeed);

            // 0-1 사이의 비율로 변환
            return currentSpeed / adjustedMaxSpeed;
        }

        public bool CheckGearChange(string currentGear)
        {
            // 최초 실행 시 초기화
            if (_previousGear == null)
            {
                _previousGear = currentGear;
                return false;
            }

            // 기어 변경 감지
            if (currentGear != _previousGear)
            {
                _previousGear = currentGear;
                return true;
            }

            return false;
        }

        public bool CheckSuddenSpeedDrop(double currentSpeed)
        {
            // 버퍼에 현재 속도 저장
            speedBuffer[currentIndex] = currentSpeed;
            currentIndex = (currentIndex + 1) % COLLISION_CHECK_FRAMES;

            if (currentIndex == 0)
            {
                bufferFilled = true;
            }

            if (!bufferFilled)
            {
                return false;
            }

            // 가장 오래된 속도값 인덱스 계산
            var oldestIndex = (currentIndex) % COLLISION_CHECK_FRAMES;
            var oldestSpeed = speedBuffer[oldestIndex];

            var timeDiffSeconds = (COLLISION_CHECK_FRAMES * UPDATE_INTERVAL_MS) / 1000.0;
            var speedDifference = (currentSpeed - oldestSpeed) / timeDiffSeconds;

            // 속도 차이가 최소 임계값보다 작으면 무시
            if (Math.Abs(currentSpeed - oldestSpeed) < MIN_SPEED_DIFF)
            {
                return false;
            }

            if (speedDifference <= COLLISION_THRESHOLD)
            {
                WriteLog($"Collision detected! Rate: {speedDifference:F2} km/h/s, " +
                        $"Speed change: {currentSpeed - oldestSpeed:F2} km/h in {timeDiffSeconds:F3}s");
                return true;
            }

            return false;
        }

        public static (bool isSlipping, int direction, double intensity) AnalyzeSlip(FeedbackData feedback, double slipThreshold = 0.2)
        {
            double lateralVelocity = feedback.LocalVelocity.Lateral;
            double intensity = Math.Abs(lateralVelocity);

            if (intensity < slipThreshold)
            {
                return (false, 0, 0);
            }

            var direction = lateralVelocity < 0 ? 1 : 2;
            return (true, direction, intensity);
        }
    }
}
