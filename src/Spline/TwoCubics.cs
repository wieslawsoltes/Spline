using System;
using System.Collections.Generic;

namespace Spline.Core
{
    /// <summary>
    /// Stores a two-cubic curve family sample used by the curve-grid tuner workflow.
    /// </summary>
    public sealed class TwoCubics
    {
        // a[0] and a[5] are arm lengths; a[1..4] are interior points coords
        public double[] A { get; }

        public TwoCubics(double[] a)
        {
            if (a == null || a.Length != 6) throw new ArgumentException("a must have length 6");
            A = a;
        }

        public Vec2 GetCenterPt(double th0, double th1)
        {
            double a0 = A[0];
            double a1 = A[1];
            double a2 = A[2];
            double a3 = A[3];
            double a4 = A[4];
            double a5 = A[5];
            var p1 = new Vec2(a0 * Math.Cos(th0), a0 * Math.Sin(th0));
            var p2 = new Vec2(a1, a2);
            var p4 = new Vec2(a3, a4);
            var p5 = new Vec2(1 - a5 * Math.Cos(th1), a5 * Math.Sin(th1));
            // choose midpoint along the interior segment (stable)
            double t = 0.5;
            return new Vec2(a1 + t * (a3 - a1), a2 + t * (a4 - a2));
        }

        public IReadOnlyList<Vec2> Render(double th0, double th1)
        {
            double a0 = A[0];
            double a1 = A[1];
            double a2 = A[2];
            double a3 = A[3];
            double a4 = A[4];
            double a5 = A[5];
            var p1 = new Vec2(a0 * Math.Cos(th0), a0 * Math.Sin(th0));
            var p2 = new Vec2(a1, a2);
            var p4 = new Vec2(a3, a4);
            var p5 = new Vec2(1 - a5 * Math.Cos(th1), a5 * Math.Sin(th1));
            var p3 = GetCenterPt(th0, th1);
            return new[] { p1, p2, p3, p4, p5 };
        }

        public double AtanCurvature(double th0, double th1)
        {
            var coords = new double[8];
            double a0 = A[0];
            var p1 = new Vec2(a0 * Math.Cos(th0), a0 * Math.Sin(th0));
            var p3 = GetCenterPt(th0, th1);
            coords[2] = p1.X;
            coords[3] = p1.Y;
            coords[4] = A[1];
            coords[5] = A[2];
            coords[6] = p3.X;
            coords[7] = p3.Y;
            var cb = new CubicBezier(coords);
            var d = cb.Deriv(0);
            var d2 = cb.Deriv2(0);
            double denom = Math.Pow(d.Norm(), 3);
            if (denom < 1e-12) return 0;
            return Math.Atan2(d.Cross(d2), denom);
        }

        public static TwoCubics Raise(object cbInput)
        {
            CubicBezier cb;
            if (cbInput is CubicBezier c)
            {
                cb = c;
            }
            else if (cbInput is IReadOnlyList<Vec2> pts && pts.Count >= 2)
            {
                var coords = new double[8];
                coords[2] = pts[0].X;
                coords[3] = pts[0].Y;
                coords[4] = pts[1].X;
                coords[5] = pts[1].Y;
                coords[6] = 1;
                cb = new CubicBezier(coords);
            }
            else
            {
                throw new ArgumentException("Unsupported input to Raise");
            }
            var l = cb.LeftHalf();
            var r = cb.RightHalf();
            var a = new double[6];
            a[0] = MathUtils.Hypot(l.C[2], l.C[3]);
            a[1] = l.C[4];
            a[2] = l.C[5];
            a[3] = r.C[2];
            a[4] = r.C[3];
            a[5] = MathUtils.Hypot(1 - r.C[4], r.C[5]);
            return new TwoCubics(a);
        }

        public static TwoCubics Default()
        {
            var a = new double[6];
            a[0] = 1.0 / 6;
            a[1] = 1.0 / 3;
            a[2] = 0;
            a[3] = 2.0 / 3;
            a[4] = 0;
            a[5] = 1.0 / 6;
            return new TwoCubics(a);
        }

        public TwoCubics Turn()
        {
            var a = new double[6];
            a[0] = A[5];
            a[1] = 1 - A[3];
            a[2] = -A[4];
            a[3] = 1 - A[1];
            a[4] = -A[2];
            a[5] = A[0];
            return new TwoCubics(a);
        }

        public TwoCubics FlipHoriz()
        {
            var a = new double[6];
            a[0] = A[5];
            a[1] = 1 - A[3];
            a[2] = A[4];
            a[3] = 1 - A[1];
            a[4] = A[2];
            a[5] = A[0];
            return new TwoCubics(a);
        }

        public TwoCubics FlipVert()
        {
            var a = (double[])A.Clone();
            a[2] = -a[2];
            a[4] = -a[4];
            return new TwoCubics(a);
        }
    }
}
