using System;

namespace Spline
{
    /// <summary>
    /// Represents a 2D vector used throughout the spline geometry pipeline.
    /// </summary>
    public readonly struct Vec2
    {
        public readonly double X;
        public readonly double Y;

        public Vec2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double Norm()
        {
            return Math.Sqrt(X * X + Y * Y);
        }

        public double Dot(in Vec2 other)
        {
            return X * other.X + Y * other.Y;
        }

        public double Cross(in Vec2 other)
        {
            return X * other.Y - Y * other.X;
        }

        public static Vec2 operator +(in Vec2 a, in Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(in Vec2 a, in Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator *(in Vec2 a, double s) => new Vec2(a.X * s, a.Y * s);
        public static Vec2 operator /(in Vec2 a, double s) => new Vec2(a.X / s, a.Y / s);
    }
}
