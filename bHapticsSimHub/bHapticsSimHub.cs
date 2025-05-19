using GameReaderCommon;
using Newtonsoft.Json;
using SimHub.Plugins;
using SimHub.Plugins.DataPlugins.ShakeItV3.UI.Effects;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace bHapticsSimHub
{
    [PluginDescription("bHaptics SimHub Plugin")]
    [PluginAuthor("bHaptics")]
    [PluginName("bHaptics SimHub Plugin")]
    public class bHapticsSimHub : IPlugin, IDataPlugin, IWPFSettingsV2
    {
        private int _playCounterLeft = 0;
        private int _playCounterRight = 0;
        private int _playCounterLeftHigh = 0;
        private int _playCounterRightHigh = 0;

        private double _cardamage1;
        private double _cardamage2;
        private double _cardamage3;
        private double _cardamage4;
        private double _lastHapticIntensity;

        private BhapticsWebsocket _websocket;
        public PluginManager PluginManager { get; set; }
        string IWPFSettingsV2.LeftMenuTitle => "bHaptics SimHub Plugin";

        public ImageSource PictureIcon => new BitmapImage(new Uri("pack://application:,,,/bHapticsSimHub;component/bhaptics_icon.ico"));

        public void Init(PluginManager pluginManager)
        {
            this.PluginManager = pluginManager;
            _websocket = new BhapticsWebsocket("67637d8ee5f9a222c0470965", "yMMWXvSZ6Usw5GvR6Tks");
            SimhubUtils.WriteLog($"Plugin Initialized!");
        }

        public void End(PluginManager pluginManager)
        {
        }

        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            return new SettingView(this);
        }

        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            if (data == null)
            {
                return;
            }

            if (data.NewData == null)
            {
                return;
            }

            if (data.GameRunning)
            {
                //SimhubUtils.WriteLog($"DataUpdate NewData: {JsonConvert.SerializeObject(data.NewData, Formatting.Indented)}");

                var values = new int[32];
                var sleeves_l_values = new int[3];
                var sleeves_r_values = new int[3];

                if (data.NewData.ABSActive > 0)
                {
                    _websocket.Play(Utils.GetMotorValue(SimhubViewModel.Instance.Items[0].MotorInfos));

                    _websocket.PlaySleeves(Utils.GetSleeveLMotorValue(SimhubViewModel.Instance.SleevesItems[0].MotorInfos));
                    _websocket.PlaySleeves(Utils.GetSleeveRMotorValue(SimhubViewModel.Instance.SleevesItems[0].MotorInfos), false);
                }

                if (data.NewData.TCActive > 0)
                {
                    _websocket.Play(Utils.GetMotorValue(SimhubViewModel.Instance.Items[3].MotorInfos));

                    _websocket.PlaySleeves(Utils.GetSleeveLMotorValue(SimhubViewModel.Instance.SleevesItems[3].MotorInfos));
                    _websocket.PlaySleeves(Utils.GetSleeveRMotorValue(SimhubViewModel.Instance.SleevesItems[3].MotorInfos), false);
                }

                if (SimhubUtils.CheckGearChange(data.NewData.Gear))
                {
                    _websocket.Play(Utils.GetMotorValue(SimhubViewModel.Instance.Items[4].MotorInfos));

                    _websocket.PlaySleeves(Utils.GetSleeveLMotorValue(SimhubViewModel.Instance.SleevesItems[4].MotorInfos));
                    _websocket.PlaySleeves(Utils.GetSleeveRMotorValue(SimhubViewModel.Instance.SleevesItems[4].MotorInfos), false);
                }

                if (data.NewData.Rpms > 0)
                {
                    var rpm = SimhubUtils.CalculateRpmOutput(data.NewData.Rpms, data.NewData.MaxRpm);
                    ApplyEffect("RPM_RATIO", rpm);
                    SimhubUtils.WriteLog($"DataUpdate Rpms: {data.NewData.Rpms},  MaxRpm: {data.NewData.MaxRpm}, rpm ratio: {rpm} ");

                    var rpmArr = Utils.GetMotorValue(SimhubViewModel.Instance.Items[1].MotorInfos);
                    var rpmLArr = Utils.GetSleeveLMotorValue(SimhubViewModel.Instance.SleevesItems[1].MotorInfos);
                    var rpmRArr = Utils.GetSleeveRMotorValue(SimhubViewModel.Instance.SleevesItems[1].MotorInfos);

                    values = Utils.MotorAdd(values, Utils.MotorAdd(rpmArr, rpm));
                    sleeves_l_values = Utils.MotorAdd(sleeves_l_values, Utils.MotorAdd(rpmLArr, rpm));
                    sleeves_r_values = Utils.MotorAdd(sleeves_r_values, Utils.MotorAdd(rpmRArr, rpm));
                }

                var speed = data.NewData.SpeedKmh;
                var maxSpeed = data.NewData.MaxSpeedKmh;
                var speedRatio = SimhubUtils.CalculateSpeedOutput(speed, maxSpeed, SimhubViewModel.Instance.HighForceMaxSpeed);
                if (data.NewData.SpeedKmh > SimhubViewModel.Instance.LowForceSpeed)
                {
                    ApplyEffect("SPEED_RATIO", speedRatio);
                    SimhubUtils.WriteLog($"DataUpdate SpeedKmh: {speed},  MaxSpeedKmh: {maxSpeed}, SPEED_RATIO: {speedRatio}, duration: {20 / speedRatio} ");

                    var speedArr = Utils.GetMotorValue(SimhubViewModel.Instance.Items[2].MotorInfos);
                    var speedLArr = Utils.GetSleeveLMotorValue(SimhubViewModel.Instance.SleevesItems[2].MotorInfos);
                    var speedRArr = Utils.GetSleeveRMotorValue(SimhubViewModel.Instance.SleevesItems[2].MotorInfos);

                    values = Utils.MotorAdd(values, Utils.MotorAdd(speedArr, speedRatio));
                    sleeves_l_values = Utils.MotorAdd(sleeves_l_values, Utils.MotorAdd(speedLArr, speedRatio));
                    sleeves_r_values = Utils.MotorAdd(sleeves_r_values, Utils.MotorAdd(speedRArr, speedRatio));
                }

                /*
                if(data.NewData.SpeedKmh > 10 && data.NewData.SpeedKmh < 50 && data.NewData.Throttle >= 100)
                {
                    _websocket.PlayEvent("simhub_initial_acceleration", 1.0f);
                }
                */

                var brake = data.NewData.Brake;
                var internalDurationBrake = 240;
                if (brake > 0 && speed > 10)
                {   
                    /*
                    if (SimhubViewModel.Instance.EventItems[0].MotorInfos[0].IsSelected)
                    {
                        _websocket.PlayEvent("break", (float)(data.NewData.Brake / 100.0f));
                    }
                    */

                    var brakeArr = Utils.GetMotorValue(SimhubViewModel.Instance.Items[15].MotorInfos);
                    var brakeLArr = Utils.GetSleeveLMotorValue(SimhubViewModel.Instance.SleevesItems[12].MotorInfos);
                    var brakeRArr = Utils.GetSleeveRMotorValue(SimhubViewModel.Instance.SleevesItems[12].MotorInfos);

                    var baseIntensity = (speed / maxSpeed) * (brake / 100f);
                    var smoothedIntensity = _lastHapticIntensity + (baseIntensity - _lastHapticIntensity) * 0.5f;
                    _lastHapticIntensity = smoothedIntensity;

                    SimhubUtils.WriteLog($"DataUpdate brake: {brake}, baseIntensity: {baseIntensity}, smoothedIntensity: {smoothedIntensity}");

                    _websocket.Play(Utils.MotorAdd(brakeArr, smoothedIntensity), internalDurationBrake);
                    _websocket.PlaySleeves(Utils.MotorAdd(brakeLArr, smoothedIntensity), true, internalDurationBrake);
                    _websocket.PlaySleeves(Utils.MotorAdd(brakeRArr, smoothedIntensity), false, internalDurationBrake);
                }

                var internalDurationSpeed = 20;
                var rumbleRatio = 1.20;

                if (data.NewData.FeedbackData.FrontLeftWheelRumble >= 1)
                {
                    var res = Utils.GetMotorValue(SimhubViewModel.Instance.Items[5].MotorInfos);
                    var resL = Utils.GetSleeveLMotorValue(SimhubViewModel.Instance.SleevesItems[5].MotorInfos);
                    var resR = Utils.GetSleeveLMotorValue(SimhubViewModel.Instance.SleevesItems[5].MotorInfos);

                    var wheelRumble = data.NewData.FeedbackData.FrontLeftWheelRumble * rumbleRatio;
                    var allamplitude = data.NewData.FeedbackData.overallamplitude[0];

                    if (data.NewData.SpeedKmh < 50)
                    {
                        allamplitude *= speedRatio;
                    }

                    ApplyEffect("FrontLeftWheelRumble SuspensionVelocity 0: ", data.NewData.FeedbackData.SuspensionVelocity[0]);
                    ApplyEffect("FrontLeftWheelRumble RATIO", wheelRumble);
                    ApplyEffect("FrontLeftWheelRumble * allamplitude RATIO", wheelRumble * allamplitude);

                    _websocket.Play(Utils.MotorAdd(res, wheelRumble * allamplitude), internalDurationSpeed);
                    _websocket.PlaySleeves(Utils.MotorAdd(resL, wheelRumble * allamplitude), true, internalDurationSpeed);
                    _websocket.PlaySleeves(Utils.MotorAdd(resR, wheelRumble * allamplitude), false, internalDurationSpeed);
                }
                if (data.NewData.FeedbackData.FrontRightWheelRumble >= 1)
                {
                    var res = Utils.GetMotorValue(SimhubViewModel.Instance.Items[6].MotorInfos);
                    var resL = Utils.GetSleeveLMotorValue(SimhubViewModel.Instance.SleevesItems[6].MotorInfos);
                    var resR = Utils.GetSleeveLMotorValue(SimhubViewModel.Instance.SleevesItems[6].MotorInfos);

                    var wheelRumble = data.NewData.FeedbackData.FrontRightWheelRumble * rumbleRatio;
                    var allamplitude = data.NewData.FeedbackData.overallamplitude[1];
                    if (data.NewData.SpeedKmh < 50)
                    {
                        allamplitude *= speedRatio;
                    }

                    ApplyEffect("FrontRightWheelRumble SuspensionVelocity 1: ", data.NewData.FeedbackData.SuspensionVelocity[1]);
                    ApplyEffect("FrontRightWheelRumble RATIO", wheelRumble);
                    ApplyEffect("FrontRightWheelRumble * allamplitude RATIO", wheelRumble * allamplitude);

                    _websocket.Play(Utils.MotorAdd(res, wheelRumble * allamplitude), internalDurationSpeed); // * speedRatio
                    _websocket.PlaySleeves(Utils.MotorAdd(resL, wheelRumble * allamplitude), true, internalDurationSpeed);
                    _websocket.PlaySleeves(Utils.MotorAdd(resR, wheelRumble * allamplitude), false, internalDurationSpeed);
                }
                if (data.NewData.FeedbackData.RearLeftWheelRumble >= 1)
                {
                    var res = Utils.GetMotorValue(SimhubViewModel.Instance.Items[7].MotorInfos);
                    var resL = Utils.GetSleeveLMotorValue(SimhubViewModel.Instance.SleevesItems[7].MotorInfos);
                    var resR = Utils.GetSleeveLMotorValue(SimhubViewModel.Instance.SleevesItems[7].MotorInfos);

                    var wheelRumble = data.NewData.FeedbackData.RearLeftWheelRumble * rumbleRatio;
                    var allamplitude = data.NewData.FeedbackData.overallamplitude[2];
                    if (data.NewData.SpeedKmh < 50)
                    {
                        allamplitude *= speedRatio;
                    }

                    ApplyEffect("RearLeftWheelRumble SuspensionVelocity 2: ", data.NewData.FeedbackData.SuspensionVelocity[2]);
                    ApplyEffect("RearLeftWheelRumble RATIO", wheelRumble);
                    ApplyEffect("RearLeftWheelRumble * allamplitude RATIO", wheelRumble * allamplitude);

                    _websocket.Play(Utils.MotorAdd(res, wheelRumble * allamplitude), internalDurationSpeed); // * speedRatio
                    _websocket.PlaySleeves(Utils.MotorAdd(resL, wheelRumble * allamplitude), true, internalDurationSpeed);
                    _websocket.PlaySleeves(Utils.MotorAdd(resR, wheelRumble * allamplitude), false, internalDurationSpeed);
                }

                if (data.NewData.FeedbackData.RearRightWheelRumble >= 1)
                {
                    var res = Utils.GetMotorValue(SimhubViewModel.Instance.Items[8].MotorInfos);
                    var resL = Utils.GetSleeveLMotorValue(SimhubViewModel.Instance.SleevesItems[8].MotorInfos);
                    var resR = Utils.GetSleeveLMotorValue(SimhubViewModel.Instance.SleevesItems[8].MotorInfos);

                    var wheelRumble = data.NewData.FeedbackData.RearRightWheelRumble * rumbleRatio;
                    var allamplitude = data.NewData.FeedbackData.overallamplitude[3];
                    if (data.NewData.SpeedKmh < 50)
                    {
                        allamplitude *= speedRatio;
                    }

                    ApplyEffect("RearRightWheelRumble SuspensionVelocity 3: ", data.NewData.FeedbackData.SuspensionVelocity[3]);
                    ApplyEffect("RearRightWheelRumble RATIO", wheelRumble);
                    ApplyEffect("RearRightWheelRumble * allamplitude RATIO", wheelRumble * allamplitude);

                    _websocket.Play(Utils.MotorAdd(res, wheelRumble * allamplitude), internalDurationSpeed); // * speedRatio
                    _websocket.PlaySleeves(Utils.MotorAdd(resL, wheelRumble * allamplitude), true, internalDurationSpeed);
                    _websocket.PlaySleeves(Utils.MotorAdd(resR, wheelRumble * allamplitude), false, internalDurationSpeed);
                }

                var slipinfo = SimhubUtils.AnalyzeSlip(data.NewData.FeedbackData);

                var yaw = slipinfo.direction;
                SimhubUtils.WriteLog($"slipinfo direction: {slipinfo.direction}, intensity: {slipinfo.intensity}, {slipinfo.isSlipping}");

                if (slipinfo.isSlipping)
                {
                    if (yaw == 1)
                    {
                        _websocket.Play(Utils.MotorAdd(Utils.GetMotorValue(SimhubViewModel.Instance.Items[10].MotorInfos), slipinfo.intensity), 20);

                        _websocket.PlaySleeves(Utils.MotorAdd(Utils.GetSleeveLMotorValue(SimhubViewModel.Instance.SleevesItems[10].MotorInfos), slipinfo.intensity), true, 20);
                        _websocket.PlaySleeves(Utils.MotorAdd(Utils.GetSleeveRMotorValue(SimhubViewModel.Instance.SleevesItems[10].MotorInfos), slipinfo.intensity), false, 20);

                    }
                    else if (yaw == 2)
                    {
                        _websocket.Play(Utils.MotorAdd(Utils.GetMotorValue(SimhubViewModel.Instance.Items[9].MotorInfos), slipinfo.intensity), 20);
                        _websocket.PlaySleeves(Utils.MotorAdd(Utils.GetSleeveLMotorValue(SimhubViewModel.Instance.SleevesItems[9].MotorInfos), slipinfo.intensity), true, 20);
                        _websocket.PlaySleeves(Utils.MotorAdd(Utils.GetSleeveRMotorValue(SimhubViewModel.Instance.SleevesItems[9].MotorInfos), slipinfo.intensity), false, 20);
                    }
                }
                /*
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

                if (SimhubUtils.CheckSuddenSpeedDrop(data.NewData.SpeedKmh))
                {
                    SimhubUtils.WriteLog("CheckSuddenSpeedDrop");

                    if (SimhubViewModel.Instance.EventItems[1].MotorInfos[0].IsSelected)
                    {
                        //_websocket.PlayEvent("car_crash", 1.0f);
                    }
                }
                */

                var cardamaged1 = SimhubUtils.CalculatePercentageIncrease(_cardamage1, data.NewData.CarDamage1);
                var cardamaged2 = SimhubUtils.CalculatePercentageIncrease(_cardamage2, data.NewData.CarDamage2);
                var cardamaged3 = SimhubUtils.CalculatePercentageIncrease(_cardamage3, data.NewData.CarDamage3);
                var cardamaged4 = SimhubUtils.CalculatePercentageIncrease(_cardamage4, data.NewData.CarDamage4);

                SimhubUtils.WriteLog($"cardamaged1: {cardamaged1}, cardamaged2: {cardamaged2}, " +
                    $"cardamaged3: {cardamaged3}, cardamaged4: {cardamaged4}, ");

                var damageRatio = Math.Max(cardamaged1, Math.Max(cardamaged2, Math.Max(cardamaged3, cardamaged4)));
                var damageDuration = 160;

                if (cardamaged1 > 0)
                {
                    _websocket.Play(Utils.MotorAdd(Utils.GetMotorValue(SimhubViewModel.Instance.Items[11].MotorInfos), cardamaged1), damageDuration);
                }

                if (cardamaged2 > 0)
                {
                    _websocket.Play(Utils.MotorAdd(Utils.GetMotorValue(SimhubViewModel.Instance.Items[12].MotorInfos), cardamaged2), damageDuration);
                }

                if (cardamaged3 > 0)
                {
                    _websocket.Play(Utils.MotorAdd(Utils.GetMotorValue(SimhubViewModel.Instance.Items[13].MotorInfos), cardamaged3), damageDuration);
                }

                if (cardamaged4 > 0)
                {
                    _websocket.Play(Utils.MotorAdd(Utils.GetMotorValue(SimhubViewModel.Instance.Items[14].MotorInfos), cardamaged4), damageDuration);
                }

                if (damageRatio > 0) 
                {
                    _websocket.PlaySleeves(Utils.MotorAdd(Utils.GetSleeveLMotorValue(SimhubViewModel.Instance.SleevesItems[11].MotorInfos), damageRatio), true, damageDuration);
                    _websocket.PlaySleeves(Utils.MotorAdd(Utils.GetSleeveRMotorValue(SimhubViewModel.Instance.SleevesItems[11].MotorInfos), damageRatio), false, damageDuration);
                }

                _cardamage1 = data.NewData.CarDamage1;
                _cardamage2 = data.NewData.CarDamage2;
                _cardamage3 = data.NewData.CarDamage3;
                _cardamage4 = data.NewData.CarDamage4;

                if (data.NewData.IsInPitLane == 1)
                {
                    values = Utils.MotorAdd(values, 0.1);
                    sleeves_l_values = Utils.MotorAdd(sleeves_l_values, 0.1);
                    sleeves_r_values = Utils.MotorAdd(sleeves_r_values, 0.1);
                }

                _websocket.Play(values);
                _websocket.PlaySleeves(sleeves_l_values);
                _websocket.PlaySleeves(sleeves_r_values, false);
            }
            else
            {
                SimhubUtils.WriteLog("Game is not running");
            }
        }
        private void ApplyEffect(string effectName, double intensity)
        {
            // Implementation would connect to your force feedback system
            // This is a placeholder for the actual force feedback implementation
            SimhubUtils.WriteLog($"Applying effect {effectName} with intensity {intensity}");
        }
    }
}