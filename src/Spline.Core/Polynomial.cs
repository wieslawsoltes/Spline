using System;

namespace Spline.Core
{
    public sealed class Polynomial
    {
        public double[] C { get; }

        public Polynomial(double[] c)
        {
            C = c ?? throw new ArgumentNullException(nameof(c));
        }

        public double Eval(double x)
        {
            double xi = 1;
            double s = 0;
            for (int i = 0; i < C.Length; i++)
            {
                s += C[i] * xi;
                xi *= x;
            }
            return s;
        }

        public Polynomial Deriv()
        {
            if (C.Length <= 1) return new Polynomial(new double[] { 0 });
            var c = new double[C.Length - 1];
            for (int i = 0; i < c.Length; i++)
            {
                c[i] = (i + 1) * C[i + 1];
            }
            return new Polynomial(c);
        }
    }
}

