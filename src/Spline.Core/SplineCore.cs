using System;
using System.Collections.Generic;
using System.Globalization;
using static Spline.Core.MathUtils;

namespace Spline.Core
{
    public abstract class TwoParamCurve
    {
        public abstract IReadOnlyList<Vec2> Render(double th0, double th1);

        public virtual (double ak0, double ak1) ComputeCurvature(double th0, double th1)
        {
            var cb = new CubicBezier(MyCubic(th0, th1));
            double Curv(double t, double th)
            {
                double c = Math.Cos(th);
                double s = Math.Sin(th);
                var d2 = cb.Deriv2(t);
                double d2cross = d2.Y * c - d2.X * s;
                var d = cb.Deriv(t);
                double ddot = d.X * c + d.Y * s;
                return Math.Atan2(d2cross, ddot * Math.Abs(ddot));
            }
            double ak0 = Curv(0, th0);
            double ak1 = Curv(1, -th1);
            return (ak0, ak1);
        }

        public virtual (double dak0dth0, double dak1dth0, double dak0dth1, double dak1dth1) ComputeCurvatureDerivs(double th0, double th1)
        {
            double epsilon = 1e-6;
            double scale = 2.0 / epsilon;
            var k0p = ComputeCurvature(th0 + epsilon, th1);
            var k0m = ComputeCurvature(th0 - epsilon, th1);
            var k1p = ComputeCurvature(th0, th1 + epsilon);
            var k1m = ComputeCurvature(th0, th1 - epsilon);
            double dak0dth0 = scale * (k0p.ak0 - k0m.ak0);
            double dak1dth0 = scale * (k0p.ak1 - k0m.ak1);
            double dak0dth1 = scale * (k1p.ak0 - k1m.ak0);
            double dak1dth1 = scale * (k1p.ak1 - k1m.ak1);
            return (dak0dth0, dak1dth0, dak0dth1, dak1dth1);
        }

        public abstract double EndpointTangent(double th);

        // JS myCubic helper
        protected static double[] MyCubic(double th0, double th1)
        {
            double MyCubicLen(double a0, double a1)
            {
                double offset = 0.3 * Math.Sin(a1 * 2 - 0.4 * Math.Sin(a1 * 2));
                double scale = 1.0 / (3 * 0.8);
                double len = scale * (Math.Cos(a0 - offset) - 0.2 * Math.Cos(3 * (a0 - offset)));
                return len;
            }

            var coords = new double[8];
            double len0 = MyCubicLen(th0, th1);
            coords[2] = Math.Cos(th0) * len0;
            coords[3] = Math.Sin(th0) * len0;
            double len1 = MyCubicLen(th1, th0);
            coords[4] = 1 - Math.Cos(th1) * len1;
            coords[5] = Math.Sin(th1) * len1;
            coords[6] = 1;
            // x0,y0 default to 0,0
            return coords;
        }
    }

    public sealed class MyCurve : TwoParamCurve
    {
        public override IReadOnlyList<Vec2> Render(double th0, double th1)
        {
            var c = MyCubic(th0, th1);
            return new[] { new Vec2(c[2], c[3]), new Vec2(c[4], c[5]) };
        }

        public override double EndpointTangent(double th)
        {
            return 0.5 * Math.Sin(2 * th);
        }

        private CubicBezier ConvCubic(IReadOnlyList<Vec2> pts)
        {
            var coords = new double[8];
            coords[2] = pts[0].X;
            coords[3] = pts[0].Y;
            coords[4] = pts[1].X;
            coords[5] = pts[1].Y;
            coords[6] = 1;
            return new CubicBezier(coords);
        }

        private IReadOnlyList<Vec2> Render4Cubic(double th0, double th1, double? k0, double? k1)
        {
            var cb = new CubicBezier(MyCubic(th0, th1));
            var result = new List<Vec2>(2);
            double DerivScale(double t, double th, double? k)
            {
                if (k == null) return 1.0 / 3.0;
                double c = Math.Cos(th);
                double s = Math.Sin(th);
                var d = cb.Deriv(t);
                var d2 = cb.Deriv2(t);
                double d2cross = d2.Y * c - d2.X * s;
                double ddot = d.X * c + d.Y * s;
                double denom = ddot * ddot;
                double oldK = d2cross / denom;
                if (Math.Abs(oldK) < 1e-6) oldK = 1e-6; // fudge like demo
                double ratio = k.Value / oldK;
                double scale = 1 / (2 + ratio);
                return scale;
            }
            double scale0 = DerivScale(0, th0, k0);
            var d0 = cb.Deriv(0);
            result.Add(new Vec2(d0.X * scale0, d0.Y * scale0));
            var d1 = cb.Deriv(1);
            double scale1 = DerivScale(1, -th1, k1);
            result.Add(new Vec2(1 - d1.X * scale1, -d1.Y * scale1));
            return result;
        }

        private IReadOnlyList<Vec2> Render4Quintic(double th0, double th1, double? k0, double? k1)
        {
            var cb = ConvCubic(Render4Cubic(th0, th1, k0, k1));
            Vec2 CurvAdjust(double t, double th, double? k)
            {
                if (k == null) return new Vec2(0, 0);
                double c = Math.Cos(th);
                double s = Math.Sin(th);
                var d2 = cb.Deriv2(t);
                double d2cross = d2.Y * c - d2.X * s;
                var d = cb.Deriv(t);
                double ddot = d.X * c + d.Y * s;
                // Follow demo exactly; if ddot=0 this will be handled by later clamping/shape
                double oldK = d2cross / (ddot * ddot);
                if (double.IsNaN(oldK) || double.IsInfinity(oldK)) return new Vec2(0, 0);
                double kAdjust = k.Value - oldK;
                double aAdjust = kAdjust * (ddot * ddot);
                return new Vec2(-s * aAdjust, c * aAdjust);
            }
            var a0 = CurvAdjust(0, th0, k0);
            var a1 = CurvAdjust(1, -th1, k1);
            var hx = MathUtils.Hermite5(0, 0, 0, 0, a0.X, a1.X);
            var hy = MathUtils.Hermite5(0, 0, 0, 0, a0.Y, a1.Y);
            var hxd = hx.Deriv();
            var hyd = hy.Deriv();
            var c0 = cb.LeftHalf();
            var c1 = cb.RightHalf();
            var cs = new[] { c0.LeftHalf(), c0.RightHalf(), c1.LeftHalf(), c1.RightHalf() };
            var result = new List<Vec2>(3 * 4 - 1);
            double scale = 1.0 / 12.0;
            for (int i = 0; i < 4; i++)
            {
                double t = 0.25 * i;
                double t1 = t + 0.25;
                var c = cs[i].C;
                double x0 = hx.Eval(t);
                double y0 = hy.Eval(t);
                double x1 = x0 + scale * hxd.Eval(t);
                double y1 = y0 + scale * hyd.Eval(t);
                double x3 = hx.Eval(t1);
                double y3 = hy.Eval(t1);
                double x2 = x3 - scale * hxd.Eval(t1);
                double y2 = y3 - scale * hyd.Eval(t1);
                if (i != 0)
                {
                    result.Add(new Vec2(c[0] + x0, c[1] + y0));
                }
                result.Add(new Vec2(c[2] + x1, c[3] + y1));
                result.Add(new Vec2(c[4] + x2, c[5] + y2));
            }
            return result;
        }

        public IReadOnlyList<Vec2> Render4(double th0, double th1, double? k0, double? k1)
        {
            if (k0 == null && k1 == null) return Render(th0, th1);
            return Render4Quintic(th0, th1, k0, k1);
        }
    }

    public sealed class TwoParamSpline
    {
        private readonly TwoParamCurve _curve;
        private readonly List<Vec2> _ctrlPts;
        public double? StartTh { get; set; }
        public double? EndTh { get; set; }
        public double[] Ths { get; private set; } = Array.Empty<double>();

        public TwoParamSpline(TwoParamCurve curve, IReadOnlyList<Vec2> ctrlPts)
        {
            _curve = curve;
            _ctrlPts = new List<Vec2>(ctrlPts);
        }

        public double[] InitialThs()
        {
            int n = _ctrlPts.Count;
            var ths = new double[n];
            for (int i = 1; i < n - 1; i++)
            {
                double dx0 = _ctrlPts[i].X - _ctrlPts[i - 1].X;
                double dy0 = _ctrlPts[i].Y - _ctrlPts[i - 1].Y;
                double l0 = MathUtils.Hypot(dx0, dy0);
                double dx1 = _ctrlPts[i + 1].X - _ctrlPts[i].X;
                double dy1 = _ctrlPts[i + 1].Y - _ctrlPts[i].Y;
                double l1 = MathUtils.Hypot(dx1, dy1);
                double th0 = Math.Atan2(dy0, dx0);
                double th1 = Math.Atan2(dy1, dx1);
                double bend = Mod2Pi(th1 - th0);
                double th = Mod2Pi(th0 + bend * l0 / (l0 + l1));
                ths[i] = th;
                if (i == 1) ths[0] = th0;
                if (i == n - 2) ths[i + 1] = th1;
            }
            // Normalize endpoints as well
            ths[0] = Mod2Pi(ths[0]);
            ths[n - 1] = Mod2Pi(ths[n - 1]);
            if (StartTh.HasValue) ths[0] = StartTh.Value;
            if (EndTh.HasValue) ths[n - 1] = EndTh.Value;
            Ths = ths;
            return ths;
        }

        private (double th0, double th1, double chord) GetThs(int i)
        {
            double dx = _ctrlPts[i + 1].X - _ctrlPts[i].X;
            double dy = _ctrlPts[i + 1].Y - _ctrlPts[i].Y;
            double th = Math.Atan2(dy, dx);
            double th0 = Mod2Pi(Ths[i] - th);
            double th1 = Mod2Pi(th - Ths[i + 1]);
            double chord = MathUtils.Hypot(dx, dy);
            return (th0, th1, chord);
        }

        public double IterDumb(int iter)
        {
            static double ComputeErr((double th0, double th1, double chord) ths0, (double ak0, double ak1) ak0,
                                     (double th0, double th1, double chord) ths1, (double ak0, double ak1) ak1)
            {
                double ch0 = Math.Sqrt(ths0.chord);
                double ch1 = Math.Sqrt(ths1.chord);
                double a0 = Math.Atan2(Math.Sin(ak0.ak1) * ch1, Math.Cos(ak0.ak1) * ch0);
                double a1 = Math.Atan2(Math.Sin(ak1.ak0) * ch0, Math.Cos(ak1.ak0) * ch1);
                return a0 - a1;
            }

            int n = _ctrlPts.Count;
            if (StartTh == null)
            {
                var ths0 = GetThs(0);
                Ths[0] += _curve.EndpointTangent(ths0.th1) - ths0.th0;
            }
            if (EndTh == null)
            {
                var ths0 = GetThs(n - 2);
                Ths[n - 1] -= _curve.EndpointTangent(ths0.th0) - ths0.th1;
            }
            if (n < 3) return 0;

            double absErr = 0;
            var x = new double[n - 2];
            var thsA = GetThs(0);
            var akA = _curve.ComputeCurvature(thsA.th0, thsA.th1);
            for (int i = 0; i < n - 2; i++)
            {
                var thsB = GetThs(i + 1);
                var akB = _curve.ComputeCurvature(thsB.th0, thsB.th1);
                double err = ComputeErr(thsA, akA, thsB, akB);
                absErr += Math.Abs(err);
                double epsilon = 1e-3;
                var ak0p = _curve.ComputeCurvature(thsA.th0, thsA.th1 + epsilon);
                var ak1p = _curve.ComputeCurvature(thsB.th0 - epsilon, thsB.th1);
                double errp = ComputeErr(thsA, ak0p, thsB, ak1p);
                double derr = (errp - err) * (1.0 / epsilon);
                x[i] = err / derr;
                thsA = thsB;
                akA = akB;
            }
            for (int i = 0; i < n - 2; i++)
            {
                double scale = Math.Tanh(0.25 * (iter + 1));
                // Limit per-iteration change to avoid overshoot
                double delta = scale * x[i];
                double maxStep = 0.5; // radians (~28.6°)
                if (delta > maxStep) delta = maxStep;
                if (delta < -maxStep) delta = -maxStep;
                Ths[i + 1] = Mod2Pi(Ths[i + 1] + delta);
            }
            return absErr;
        }

        public string RenderSvg()
        {
            var c = _ctrlPts;
            if (c.Count == 0) return string.Empty;
            string path = string.Create(CultureInfo.InvariantCulture, $"M{c[0].X} {c[0].Y}");
            string cmd = " C";
            for (int i = 0; i < c.Count - 1; i++)
            {
                var ths = GetThs(i);
                var render = _curve.Render(ths.th0, ths.th1);
                double dx = c[i + 1].X - c[i].X;
                double dy = c[i + 1].Y - c[i].Y;
                for (int j = 0; j < render.Count; j++)
                {
                    var pt = render[j];
                    double x = c[i].X + dx * pt.X - dy * pt.Y;
                    double y = c[i].Y + dy * pt.X + dx * pt.Y;
                    path += string.Create(CultureInfo.InvariantCulture, $"{cmd}{x} {y}");
                    cmd = " ";
                }
                path += string.Create(CultureInfo.InvariantCulture, $" {c[i + 1].X} {c[i + 1].Y}");
            }
            return path;
        }
    }
}
