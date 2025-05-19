using GameReaderCommon.Feedback;
using System;
using System.Windows.Markup;

namespace bHapticsSimHub
{
    public static class SimhubUtils
    {
        private const int UPDATE_INTERVAL_MS = 15;
        private const double COLLISION_THRESHOLD = -35;
        private const int COLLISION_CHECK_FRAMES = 4;
        private const double MIN_SPEED_DIFF = 10; // 최소 속도 차이 (노이즈 필터링)

        private static string _previousGear = null;
        private static double _previousSpeed = 0;
        private static int currentIndex = 0;
        private static bool bufferFilled = false;

        private static double[] speedBuffer = new double[COLLISION_CHECK_FRAMES];

        public static void WriteLog(string message)
        {
            SimHub.Logging.Current.Info($"[bHaptics] {message}");
        }

        public static void WriteErrorLog(string message)
        {
            SimHub.Logging.Current.Error($"[bHaptics] {message}");
        }

        public static double CalculatePercentageIncrease(double previousValue, double currentValue)
        {
            if (previousValue == 0)
            {
                // Cannot calculate percentage increase from zero
                //return double.PositiveInfinity; // Or could return a special value like -1
                return currentValue;
            }
            else
            {
                // Calculate the percentage increase
                return ((currentValue - previousValue) / previousValue) * 100;
            }
        }

        public static double CalculateRpmOutput(double currentRpm, double maxRpm)
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

        public static bool DetermineSurface(double slip, double rumble)
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

        public static double CalculateSpeedOutput(double currentSpeed, double maxSpeed, double maxSpeedPercent)
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

        public static bool CheckGearChange(string currentGear)
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

        public static bool CheckSuddenSpeedDrop(double currentSpeed)
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

        public static (bool isSlipping, int direction, double intensity) AnalyzeSlip(FeedbackData feedback, double slipThreshold = 0.3)
        {
            WriteLog($"DataUpdate LocalVelocity.Lateral: {feedback.LocalVelocity.Lateral}");
            WriteLog($"DataUpdate LocalVelocity.Forward: {feedback.LocalVelocity.Forward}");
            WriteLog($"DataUpdate LocalVelocity.Upward: {feedback.LocalVelocity.Upward}");

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
