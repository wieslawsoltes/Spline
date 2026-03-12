using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Spline.Core
{
    public sealed class CurveGrid
    {
        public int N { get; }
        public List<TwoCubics> Masters { get; }

        public CurveGrid(int n, List<TwoCubics> masters)
        {
            N = n;
            Masters = masters;
        }

        private static int Mod(int a, int b)
        {
            int r = a % b;
            return r < 0 ? r + b : r;
        }

        private TwoCubics GetMasterCore(int i, int j)
        {
            int ix = i * i + i + j;
            return Masters[ix];
        }

        public TwoCubics GetMaster(int i, int j)
        {
            i = Mod(i + N - 1, N * 2) - N + 1;
            j = Mod(j + N - 1, N * 2) - N + 1;
            if (i >= 0 && -i <= j && j <= i)
            {
                return GetMasterCore(i, j);
            }
            else if (j >= 0 && -j <= i && i <= j)
            {
                return GetMasterCore(j, i).FlipHoriz();
            }
            else if (i <= 0 && i <= j && j <= -i)
            {
                return GetMasterCore(-i, -j).FlipVert();
            }
            else
            {
                return GetMasterCore(-j, -i).Turn();
            }
        }

        public TwoCubics GetInterp(double th0, double th1)
        {
            double i = th0 * 2 * N / Math.PI;
            double j = th1 * 2 * N / Math.PI;
            int iInt = (int)Math.Floor(i);
            int jInt = (int)Math.Floor(j);
            double iFrac = i - iInt;
            double jFrac = j - jInt;
            var m00 = GetMaster(iInt, jInt);
            var m01 = GetMaster(iInt + 1, jInt);
            var m10 = GetMaster(iInt, jInt + 1);
            var m11 = GetMaster(iInt + 1, jInt + 1);
            var a = new double[6];
            for (int k = 0; k < 6; k++)
            {
                double a00 = m00.A[k];
                double a01 = m01.A[k];
                double a10 = m10.A[k];
                double a11 = m11.A[k];
                double a0 = a00 + iFrac * (a01 - a00);
                double a1 = a10 + iFrac * (a11 - a10);
                a[k] = a0 + jFrac * (a1 - a0);
            }
            return new TwoCubics(a);
        }

        public string ToJson()
        {
            var masters = new List<double[]>(Masters.Count);
            foreach (var m in Masters) masters.Add((double[])m.A.Clone());
            var obj = new Dictionary<string, object?>
            {
                ["n"] = N,
                ["masters"] = masters
            };
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
        }

        public static CurveGrid FromJson(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            int n = root.GetProperty("n").GetInt32();
            var masters = new List<TwoCubics>();
            foreach (var m in root.GetProperty("masters").EnumerateArray())
            {
                var a = new double[6];
                int idx = 0;
                foreach (var v in m.EnumerateArray()) a[idx++] = v.GetDouble();
                masters.Add(new TwoCubics(a));
            }
            return new CurveGrid(n, masters);
        }
    }
}

