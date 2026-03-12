using System;
using System.Collections.Generic;
using System.Globalization;

namespace Spline.Core
{
    public enum PathOp { MoveTo, LineTo, CurveTo, ClosePath, Mark }

    public sealed class BezierPath
    {
        public readonly struct Command
        {
            public readonly PathOp Op;
            public readonly double[] Args;
            public Command(PathOp op, params double[] args)
            {
                Op = op;
                Args = args;
            }
        }

        private readonly List<Command> _cmds = new List<Command>();

        public void MoveTo(double x, double y) => _cmds.Add(new Command(PathOp.MoveTo, x, y));
        public void LineTo(double x, double y) => _cmds.Add(new Command(PathOp.LineTo, x, y));
        public void CurveTo(double x1, double y1, double x2, double y2, double x3, double y3)
            => _cmds.Add(new Command(PathOp.CurveTo, x1, y1, x2, y2, x3, y3));
        public void ClosePath() => _cmds.Add(new Command(PathOp.ClosePath));
        public void Mark(int i) => _cmds.Add(new Command(PathOp.Mark, i));

        public string ToSvgPath()
        {
            var parts = new List<string>(_cmds.Count);
            foreach (var cmd in _cmds)
            {
                switch (cmd.Op)
                {
                    case PathOp.MoveTo:
                        parts.Add(string.Create(CultureInfo.InvariantCulture, $"M{cmd.Args[0]} {cmd.Args[1]}"));
                        break;
                    case PathOp.LineTo:
                        parts.Add(string.Create(CultureInfo.InvariantCulture, $"L{cmd.Args[0]} {cmd.Args[1]}"));
                        break;
                    case PathOp.CurveTo:
                        parts.Add(string.Create(CultureInfo.InvariantCulture, $"C{cmd.Args[0]} {cmd.Args[1]} {cmd.Args[2]} {cmd.Args[3]} {cmd.Args[4]} {cmd.Args[5]}"));
                        break;
                    case PathOp.ClosePath:
                        parts.Add("Z");
                        break;
                }
            }
            return string.Join(string.Empty, parts);
        }

        public HitTestResult HitTest(double x, double y)
        {
            var result = new HitTestResult(x, y);
            double curX = 0, curY = 0;
            int? curMark = null;
            foreach (var cmd in _cmds)
            {
                switch (cmd.Op)
                {
                    case PathOp.MoveTo:
                        curX = cmd.Args[0];
                        curY = cmd.Args[1];
                        break;
                    case PathOp.LineTo:
                        result.AccumLine(curX, curY, cmd.Args[0], cmd.Args[1], curMark);
                        curX = cmd.Args[0];
                        curY = cmd.Args[1];
                        break;
                    case PathOp.CurveTo:
                        result.AccumCurve(curX, curY, cmd.Args[0], cmd.Args[1], cmd.Args[2], cmd.Args[3], cmd.Args[4], cmd.Args[5], curMark);
                        curX = cmd.Args[4];
                        curY = cmd.Args[5];
                        break;
                    case PathOp.Mark:
                        curMark = (int)cmd.Args[0];
                        break;
                }
            }
            return result;
        }
    }

    public sealed class HitTestResult
    {
        public readonly double X;
        public readonly double Y;
        public double BestDist { get; private set; }
        public int? BestMark { get; private set; }

        public HitTestResult(double x, double y)
        {
            X = x;
            Y = y;
            BestDist = 1e12;
            BestMark = null;
        }

        public void AccumLine(double x0, double y0, double x1, double y1, int? mark)
        {
            double dx = x1 - x0;
            double dy = y1 - y0;
            double dotp = (X - x0) * dx + (Y - y0) * dy;
            double linDotp = dx * dx + dy * dy;
            double rMin = MathUtils.Hypot(X - x0, Y - y0);
            rMin = Math.Min(rMin, MathUtils.Hypot(X - x1, Y - y1));
            if (dotp > 0 && dotp < linDotp)
            {
                double norm = (X - x0) * dy - (Y - y0) * dx;
                double r = Math.Abs(norm / Math.Sqrt(linDotp));
                rMin = Math.Min(rMin, r);
            }
            if (rMin < BestDist)
            {
                BestDist = rMin;
                BestMark = mark;
            }
        }

        public void AccumCurve(double x0, double y0, double x1, double y1, double x2, double y2, double x3, double y3, int? mark)
        {
            int n = 32;
            double dt = 1.0 / n;
            double lastX = x0;
            double lastY = y0;
            for (int i = 0; i < n; i++)
            {
                double t = (i + 1) * dt;
                double mt = 1 - t;
                double x = (x0 * mt * mt + 3 * (x1 * mt * t + x2 * t * t)) * mt + x3 * t * t * t;
                double y = (y0 * mt * mt + 3 * (y1 * mt * t + y2 * t * t)) * mt + y3 * t * t * t;
                AccumLine(lastX, lastY, x, y, mark);
                lastX = x;
                lastY = y;
            }
        }
    }
}

