using System;
using System.Collections.Generic;
using System.Linq;

namespace Spline.Core
{
    public sealed class Spline
    {
        public sealed class CP
        {
            public Vec2 Pt;
            public string Ty; // "corner" or "smooth"
            public double? LTh;
            public double? RTh;

            // Computed during solve
            public double LThComputed;
            public double RThComputed;
            public double LAk;
            public double RAk;
            public double? KBlend;

            public CP(Vec2 pt, string ty, double? lth, double? rth)
            {
                Pt = pt;
                Ty = ty;
                LTh = lth;
                RTh = rth;
            }
        }

        public List<CP> CtrlPts { get; }
        public bool IsClosed { get; }
        private readonly MyCurve _curve = new MyCurve();

        public Spline(IEnumerable<CP> ctrlPts, bool isClosed)
        {
            CtrlPts = ctrlPts.ToList();
            IsClosed = isClosed;
        }

        private CP Pt(int i, int start)
        {
            int len = CtrlPts.Count;
            return CtrlPts[(i + start + len) % len];
        }

        private int StartIx()
        {
            if (!IsClosed) return 0;
            for (int i = 0; i < CtrlPts.Count; i++)
            {
                var pt = CtrlPts[i];
                if (pt.Ty == "corner" || pt.LTh != null) return i;
            }
            return 0;
        }

        public void Solve()
        {
            int start = StartIx();
            int length = CtrlPts.Count - (IsClosed ? 0 : 1);
            int i = 0;
            while (i < length)
            {
                var ptI = Pt(i, start);
                var ptI1 = Pt(i + 1, start);
                if ((i + 1 == length || ptI1.Ty == "corner") && ptI.RTh == null && ptI1.LTh == null)
                {
                    double dx = ptI1.Pt.X - ptI.Pt.X;
                    double dy = ptI1.Pt.Y - ptI.Pt.Y;
                    double th = Math.Atan2(dy, dx);
                    ptI.RThComputed = th;
                    ptI1.LThComputed = th;
                    i += 1;
                }
                else
                {
                    var innerPts = new List<Vec2> { ptI.Pt };
                    int j = i + 1;
                    while (j < length + 1)
                    {
                        var ptJ = Pt(j, start);
                        innerPts.Add(ptJ.Pt);
                        j += 1;
                        if (ptJ.Ty == "corner" || ptJ.LTh != null) break;
                    }
                var inner = new TwoParamSpline(_curve, innerPts);
                    inner.StartTh = Pt(i, start).RTh;
                    inner.EndTh = Pt(j - 1, start).LTh;
                inner.InitialThs();
                int nIter = 10;
                for (int k = 0; k < nIter; k++) inner.IterDumb(k);
                    for (int k = i; k + 1 < j; k++)
                    {
                        Pt(k, start).RThComputed = inner.Ths[k - i];
                        Pt(k + 1, start).LThComputed = inner.Ths[k + 1 - i];
                    }

                    // Match the upstream solver: record endpoint curvature in the local chord frame.
                    for (int k = i; k + 1 < j; k++)
                    {
                        double dx = Pt(k + 1, start).Pt.X - Pt(k, start).Pt.X;
                        double dy = Pt(k + 1, start).Pt.Y - Pt(k, start).Pt.Y;
                        double chth = Math.Atan2(dy, dx);
                        double th0 = MathUtils.Mod2Pi(Pt(k, start).RThComputed - chth);
                        double th1 = MathUtils.Mod2Pi(chth - Pt(k + 1, start).LThComputed);
                        var aks = _curve.ComputeCurvature(th0, th1);
                        Pt(k, start).RAk = aks.ak0;
                        Pt(k + 1, start).LAk = aks.ak1;
                    }

                    i = j - 1;
                }
            }
        }

        private double ChordLen(int i)
        {
            var a = Pt(i, 0).Pt;
            var b = Pt(i + 1, 0).Pt;
            return MathUtils.Hypot(b.X - a.X, b.Y - a.Y);
        }

        public void ComputeCurvatureBlending()
        {
            static double MyTan(double th)
            {
                if (th > Math.PI / 2) return Math.Tan(Math.PI - th);
                if (th < -Math.PI / 2) return Math.Tan(-Math.PI - th);
                return Math.Tan(th);
            }
            foreach (var pt in CtrlPts) pt.KBlend = null;
            int length = CtrlPts.Count - (IsClosed ? 0 : 1);
            // Recompute ak at joints from final framed th0/th1 to match demo
            for (int i = 0; i < length; i++)
            {
                var joint = Pt(i, 0);
                if (!(joint.Ty == "smooth" && joint.LTh != null)) continue;

                // Previous segment (i-1 -> i)
                var a = Pt(i - 1, 0); var b = Pt(i, 0);
                double dxPrev = b.Pt.X - a.Pt.X; double dyPrev = b.Pt.Y - a.Pt.Y;
                double chthPrev = Math.Atan2(dyPrev, dxPrev);
                double th0Prev = MathUtils.Mod2Pi(a.RThComputed - chthPrev);
                double th1Prev = MathUtils.Mod2Pi(chthPrev - b.LThComputed);
                var akPrev = _curve.ComputeCurvature(th0Prev, th1Prev);
                double akRight = akPrev.ak1; // curvature at right end of prev seg

                // Next segment (i -> i+1)
                var c = Pt(i + 1, 0);
                double dxNext = c.Pt.X - b.Pt.X; double dyNext = c.Pt.Y - b.Pt.Y;
                double chthNext = Math.Atan2(dyNext, dxNext);
                double th0Next = MathUtils.Mod2Pi(b.RThComputed - chthNext);
                double th1Next = MathUtils.Mod2Pi(chthNext - c.LThComputed);
                var akNext = _curve.ComputeCurvature(th0Next, th1Next);
                double akLeft = akNext.ak0; // curvature at left end of next seg

                // Preserve the upstream naming: LAk is the incoming curvature,
                // RAk is the outgoing curvature at the joint.
                joint.LAk = akRight;
                joint.RAk = akLeft;

                if (Math.Sign(akRight) != Math.Sign(akLeft))
                {
                    joint.KBlend = 0;
                }
                else
                {
                    double rK = MyTan(akRight) / ChordLen(i - 1);
                    double lK = MyTan(akLeft) / ChordLen(i);
                    joint.KBlend = 2 / (1 / rK + 1 / lK);
                }
            }
        }

        public BezierPath Render(bool useRawCubic = false)
        {
            var path = new BezierPath();
            if (CtrlPts.Count == 0) return path;
            var pt0 = CtrlPts[0];
            path.MoveTo(pt0.Pt.X, pt0.Pt.Y);
            int length = CtrlPts.Count - (IsClosed ? 0 : 1);
            for (int i = 0; i < length; i++)
            {
                path.Mark(i);
                var ptI = Pt(i, 0);
                var ptI1 = Pt(i + 1, 0);
                double dx = ptI1.Pt.X - ptI.Pt.X;
                double dy = ptI1.Pt.Y - ptI.Pt.Y;
                double chth = Math.Atan2(dy, dx);
                double chord = MathUtils.Hypot(dy, dx);
                // Use computed rTh/lTh from solver (demo uses these, explicit tangents only set boundary conditions)
                double th0 = MathUtils.Mod2Pi(ptI.RThComputed - chth);
                double th1 = MathUtils.Mod2Pi(chth - ptI1.LThComputed);
                double? k0 = ptI.KBlend.HasValue ? ptI.KBlend.Value * chord : (double?)null;
                double? k1 = ptI1.KBlend.HasValue ? ptI1.KBlend.Value * chord : (double?)null;

                IReadOnlyList<Vec2> pts;
                if (useRawCubic)
                {
                    // Equivalent to demo’s base cubic (no curvature adjust)
                    pts = ((MyCurve)_curve).Render(th0, th1);
                }
                else
                {
                    // Render with curvature adjustment (quintic)
                    pts = ((MyCurve)_curve).Render4(th0, th1, k0, k1);
                }
                var c = new List<double>(6);
                for (int j = 0; j < pts.Count; j++)
                {
                    var p = pts[j];
                    c.Add(ptI.Pt.X + dx * p.X - dy * p.Y);
                    c.Add(ptI.Pt.Y + dy * p.X + dx * p.Y);
                }
                c.Add(ptI1.Pt.X);
                c.Add(ptI1.Pt.Y);
                for (int j = 0; j < c.Count; j += 6)
                {
                    path.CurveTo(c[j], c[j + 1], c[j + 2], c[j + 3], c[j + 4], c[j + 5]);
                }
            }
            if (IsClosed) path.ClosePath();
            return path;
        }

        public string RenderSvg() => Render().ToSvgPath();
    }
}
