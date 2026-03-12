using System;

namespace Spline.Core
{
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
    }
}
