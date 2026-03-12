using System;

namespace Spline.Core
{
    /// <summary>
    /// Provides low-level numerical helpers used by the spline implementation.
    /// </summary>
    public static class MathUtils
    {
        public static double Mod2Pi(double th)
        {
            double twoPi = Math.PI * 2;
            double frac = th / twoPi;
            return twoPi * (frac - Math.Round(frac));
        }

        public static Polynomial Hermite5(double x0, double x1, double v0, double v1, double a0, double a1)
        {
            // Matches JS hermite5 coefficients
            return new Polynomial(new double[]
            {
                x0,
                v0,
                0.5 * a0,
                -10 * x0 + 10 * x1 - 6 * v0 - 4 * v1 - 1.5 * a0 + 0.5 * a1,
                15 * x0 - 15 * x1 + 8 * v0 + 7 * v1 + 1.5 * a0 - a1,
                -6 * x0 + 6 * x1 - 3 * v0 - 3 * v1 - 0.5 * a0 + 0.5 * a1
            });
        }

        public static double Hypot(double x, double y)
        {
            return Math.Sqrt(x * x + y * y);
        }

        /// <summary>
        /// Solve a tridiagonal matrix system in place using the Thomas algorithm.
        /// </summary>
        /// <remarks>
        /// This matches the upstream JS implementation and mutates <paramref name="b"/> and
        /// <paramref name="d"/> while writing the solution into <paramref name="x"/>.
        /// </remarks>
        public static void SolveTridiagonal(double[] a, double[] b, double[] c, double[] d, double[] x)
        {
            ArgumentNullException.ThrowIfNull(a);
            ArgumentNullException.ThrowIfNull(b);
            ArgumentNullException.ThrowIfNull(c);
            ArgumentNullException.ThrowIfNull(d);
            ArgumentNullException.ThrowIfNull(x);

            int n = x.Length;
            if (n == 0)
            {
                throw new ArgumentException("x must not be empty.", nameof(x));
            }

            if (a.Length != n || b.Length != n || c.Length != n || d.Length != n)
            {
                throw new ArgumentException("All tridiagonal vectors must have the same length as x.");
            }

            for (int i = 1; i < n; i++)
            {
                double m = a[i] / b[i - 1];
                b[i] -= m * c[i - 1];
                d[i] -= m * d[i - 1];
            }

            x[n - 1] = d[n - 1] / b[n - 1];
            for (int i = n - 2; i >= 0; i--)
            {
                x[i] = (d[i] - c[i] * x[i + 1]) / b[i];
            }
        }
    }
}
