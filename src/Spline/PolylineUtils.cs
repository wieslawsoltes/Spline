using System;
using System.Collections.Generic;

namespace Spline.Core
{
    /// <summary>
    /// Provides headless polyline simplification, corner detection, and resampling helpers.
    /// </summary>
    public static class PolylineUtils
    {
        public static List<Vec2> RamerDouglasPeucker(IReadOnlyList<Vec2> points, double epsilon)
        {
            if (points == null || points.Count == 0) return new List<Vec2>();
            if (points.Count <= 2) return new List<Vec2>(points);

            var keep = new bool[points.Count];
            for (int i = 0; i < keep.Length; i++) keep[i] = false;
            keep[0] = true;
            keep[points.Count - 1] = true;

            var stack = new Stack<(int start, int end)>();
            stack.Push((0, points.Count - 1));

            while (stack.Count > 0)
            {
                var (start, end) = stack.Pop();
                if (end <= start + 1) continue;

                double maxDist = -1;
                int index = -1;
                var a = points[start];
                var b = points[end];
                double ax = a.X, ay = a.Y;
                double bx = b.X, by = b.Y;
                double dx = bx - ax, dy = by - ay;
                double denom = dx * dx + dy * dy;
                for (int i = start + 1; i < end; i++)
                {
                    var p = points[i];
                    double px = p.X, py = p.Y;
                    double t = denom > 0 ? ((px - ax) * dx + (py - ay) * dy) / denom : 0;
                    t = Math.Clamp(t, 0, 1);
                    double projx = ax + t * dx;
                    double projy = ay + t * dy;
                    double dist = Math.Sqrt((px - projx) * (px - projx) + (py - projy) * (py - projy));
                    if (dist > maxDist)
                    {
                        maxDist = dist;
                        index = i;
                    }
                }

                if (maxDist > epsilon && index >= 0)
                {
                    keep[index] = true;
                    stack.Push((start, index));
                    stack.Push((index, end));
                }
            }

            var result = new List<Vec2>();
            for (int i = 0; i < points.Count; i++)
            {
                if (keep[i]) result.Add(points[i]);
            }
            return result;
        }

        public static HashSet<int> DetectCorners(IReadOnlyList<Vec2> polyline, double angleThresholdRadians)
        {
            var corners = new HashSet<int>();
            if (polyline == null || polyline.Count == 0) return corners;
            if (polyline.Count <= 2)
            {
                corners.Add(0);
                if (polyline.Count == 2) corners.Add(1);
                return corners;
            }

            corners.Add(0);
            corners.Add(polyline.Count - 1);

            for (int i = 1; i < polyline.Count - 1; i++)
            {
                var p0 = polyline[i - 1];
                var p1 = polyline[i];
                var p2 = polyline[i + 1];
                double v0x = p1.X - p0.X;
                double v0y = p1.Y - p0.Y;
                double v1x = p2.X - p1.X;
                double v1y = p2.Y - p1.Y;

                double n0 = Math.Sqrt(v0x * v0x + v0y * v0y);
                double n1 = Math.Sqrt(v1x * v1x + v1y * v1y);
                if (n0 < 1e-6 || n1 < 1e-6) continue;
                v0x /= n0; v0y /= n0; v1x /= n1; v1y /= n1;
                double dot = v0x * v1x + v0y * v1y;
                dot = Math.Max(-1, Math.Min(1, dot));
                double angle = Math.Acos(dot);
                if (angle >= angleThresholdRadians)
                {
                    corners.Add(i);
                }
            }

            return corners;
        }

        public static double ComputePolylineLength(IReadOnlyList<Vec2> polyline, int startIndex, int endIndex)
        {
            if (polyline == null || polyline.Count == 0) return 0;
            startIndex = Math.Max(0, Math.Min(startIndex, polyline.Count - 1));
            endIndex = Math.Max(0, Math.Min(endIndex, polyline.Count - 1));
            if (endIndex <= startIndex) return 0;
            double length = 0;
            var last = polyline[startIndex];
            for (int i = startIndex + 1; i <= endIndex; i++)
            {
                var p = polyline[i];
                double dx = p.X - last.X;
                double dy = p.Y - last.Y;
                length += Math.Sqrt(dx * dx + dy * dy);
                last = p;
            }
            return length;
        }

        public static List<Vec2> ResampleBySpacing(IReadOnlyList<Vec2> polyline, int startIndex, int endIndex, double spacing)
        {
            var result = new List<Vec2>();
            if (polyline == null || polyline.Count == 0) return result;
            startIndex = Math.Max(0, Math.Min(startIndex, polyline.Count - 1));
            endIndex = Math.Max(0, Math.Min(endIndex, polyline.Count - 1));
            if (endIndex < startIndex) (startIndex, endIndex) = (endIndex, startIndex);
            if (endIndex == startIndex)
            {
                result.Add(polyline[startIndex]);
                return result;
            }

            double remaining = 0;
            var last = polyline[startIndex];
            result.Add(last);
            for (int i = startIndex + 1; i <= endIndex; i++)
            {
                var p = polyline[i];
                double segDx = p.X - last.X;
                double segDy = p.Y - last.Y;
                double segLen = Math.Sqrt(segDx * segDx + segDy * segDy);
                if (segLen <= 1e-6)
                {
                    last = p;
                    continue;
                }
                double dist = remaining + segLen;
                while (dist >= spacing)
                {
                    double t = (spacing - remaining) / segLen;
                    var nx = last.X + t * segDx;
                    var ny = last.Y + t * segDy;
                    var np = new Vec2(nx, ny);
                    result.Add(np);
                    // advance along segment
                    last = np;
                    segDx = p.X - last.X;
                    segDy = p.Y - last.Y;
                    segLen = Math.Sqrt(segDx * segDx + segDy * segDy);
                    if (segLen <= 1e-6) break;
                    dist -= spacing;
                    remaining = 0;
                }
                remaining += segLen;
                last = p;
            }
            // Ensure endpoint included
            var end = polyline[endIndex];
            if (result.Count == 0 || result[^1].X != end.X || result[^1].Y != end.Y) result.Add(end);
            return result;
        }
    }
}
