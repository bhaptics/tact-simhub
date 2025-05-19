using System;
using System.Collections.ObjectModel;

namespace bHapticsSimHub
{
    public static class Utils
    {
        public static int[] GetSleeveLMotorValue(ObservableCollection<MotorInfo> motorInfo)
        {
            var res = new int[3];

            for (var i = 0; i < 3; i++)
            {
                if (motorInfo[i].IsSelected)
                {
                    res[i] = motorInfo[i].Intensity;
                }
            }

            return res;
        }

        public static int[] GetSleeveRMotorValue(ObservableCollection<MotorInfo> motorInfo)
        {
            var res = new int[3];

            for (var i = 3; i < motorInfo.Count; i++)
            {
                if (motorInfo[i].IsSelected)
                {
                    res[i - 3] = motorInfo[i].Intensity;
                }
            }

            return res;
        }

        public static int[] GetMotorValue(ObservableCollection<MotorInfo> motorInfo)
        {
            var res = new int[32];

            for (var i = 0; i < motorInfo.Count; i++)
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
                        res[i + 8] = motorInfo[i].Intensity;
                        res[i + 4 + 8] = motorInfo[i].Intensity;
                    }
                }
                else
                {
                    if (motorInfo[i].IsSelected)
                    {
                        res[i + 12] = motorInfo[i].Intensity;
                        res[i + 4 + 12] = motorInfo[i].Intensity;
                    }
                }
            }
            return res;
        }

        public static int[] MotorAdd(int[] a, double b)
        {
            var res = new int[a.Length];

            for (var i = 0; i < a.Length; i++)
            {
                res[i] = (int)(a[i] * b);
            }

            return res;
        }

        public static int[] MotorInsert(int[] a, double b)
        {
            var res = new int[a.Length];

            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] != 0)
                {
                    res[i] = (int)(b);
                }
            }

            return res;
        }

        public static int[] MotorAdd(int[] a, int[] b)
        {
            var res = new int[a.Length];

            for (var i = 0; i < a.Length; i++)
            {
                res[i] = MotorAdd(a[i], b[i]);
            }

            return res;
        }

        public static int MotorAdd(int a, int b)
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
    }
}
