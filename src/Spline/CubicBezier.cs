using System;

namespace Spline
{
    /// <summary>
    /// Evaluates and differentiates a cubic Bezier segment in normalized local space.
    /// </summary>
    public sealed class CubicBezier
    {
        // Coordinates: [x0,y0,x1,y1,x2,y2,x3,y3]
        public double[] C { get; }

        public CubicBezier(double[] coords)
        {
            if (coords == null || coords.Length != 8) throw new ArgumentException("coords must have length 8");
            C = coords;
        }

        private Vec2 WeightSum(double c0, double c1, double c2, double c3)
        {
            double x = c0 * C[0] + c1 * C[2] + c2 * C[4] + c3 * C[6];
            double y = c0 * C[1] + c1 * C[3] + c2 * C[5] + c3 * C[7];
            return new Vec2(x, y);
        }

        public Vec2 Eval(double t)
        {
            double mt = 1 - t;
            double c0 = mt * mt * mt;
            double c1 = 3 * mt * mt * t;
            double c2 = 3 * mt * t * t;
            double c3 = t * t * t;
            return WeightSum(c0, c1, c2, c3);
        }

        public Vec2 Deriv(double t)
        {
            double mt = 1 - t;
            double c0 = -3 * mt * mt;
            double c3 = 3 * t * t;
            double c1 = -6 * t * mt - c0;
            double c2 = 6 * t * mt - c3;
            return WeightSum(c0, c1, c2, c3);
        }

        public Vec2 Deriv2(double t)
        {
            double mt = 1 - t;
            double c0 = 6 * mt;
            double c3 = 6 * t;
            double c1 = 6 - 18 * mt;
            double c2 = 6 - 18 * t;
            return WeightSum(c0, c1, c2, c3);
        }

        public double Curvature(double t)
        {
            var d = Deriv(t);
            var d2 = Deriv2(t);
            double denom = Math.Pow(d.Norm(), 3);
            if (denom == 0) return 0;
            return d.Cross(d2) / denom;
        }

        public double AtanCurvature(double t)
        {
            var d = Deriv(t);
            var d2 = Deriv2(t);
            double denom = Math.Pow(d.Norm(), 3);
            return Math.Atan2(d.Cross(d2), denom);
        }

        public CubicBezier LeftHalf()
        {
            var c = new double[8];
            c[0] = C[0];
            c[1] = C[1];
            c[2] = 0.5 * (C[0] + C[2]);
            c[3] = 0.5 * (C[1] + C[3]);
            c[4] = 0.25 * (C[0] + 2 * C[2] + C[4]);
            c[5] = 0.25 * (C[1] + 2 * C[3] + C[5]);
            c[6] = 0.125 * (C[0] + 3 * (C[2] + C[4]) + C[6]);
            c[7] = 0.125 * (C[1] + 3 * (C[3] + C[5]) + C[7]);
            return new CubicBezier(c);
        }

        public CubicBezier RightHalf()
        {
            var c = new double[8];
            c[0] = 0.125 * (C[0] + 3 * (C[2] + C[4]) + C[6]);
            c[1] = 0.125 * (C[1] + 3 * (C[3] + C[5]) + C[7]);
            c[2] = 0.25 * (C[2] + 2 * C[4] + C[6]);
            c[3] = 0.25 * (C[3] + 2 * C[5] + C[7]);
            c[4] = 0.5 * (C[4] + C[6]);
            c[5] = 0.5 * (C[5] + C[7]);
            c[6] = C[6];
            c[7] = C[7];
            return new CubicBezier(c);
        }
    }
}
