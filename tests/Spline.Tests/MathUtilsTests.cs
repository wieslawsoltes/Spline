using Xunit;

namespace Spline.Tests;

public sealed class MathUtilsTests
{
    [Theory]
    [InlineData(4)]
    [InlineData(10)]
    public void SolveTridiagonalMatchesUpstreamResidualCheck(int n)
    {
        var random = new Random(1234 + n);
        var a = new double[n];
        var b = new double[n];
        var c = new double[n];
        var d = new double[n];
        var x = new double[n];

        for (int i = 0; i < n; i++)
        {
            a[i] = random.NextDouble();
            b[i] = 2 + random.NextDouble();
            c[i] = random.NextDouble();
            d[i] = random.NextDouble();
            x[i] = random.NextDouble();
        }

        var bSaved = (double[])b.Clone();
        var dSaved = (double[])d.Clone();

        MathUtils.SolveTridiagonal(a, b, c, d, x);

        TestAssert.EqualWithin(0, bSaved[0] * x[0] + c[0] * x[1] - dSaved[0], 1e-12, "row 0");
        for (int i = 1; i < n - 1; i++)
        {
            double residual = a[i] * x[i - 1] + bSaved[i] * x[i] + c[i] * x[i + 1] - dSaved[i];
            TestAssert.EqualWithin(0, residual, 1e-12, $"row {i}");
        }

        TestAssert.EqualWithin(0, a[n - 1] * x[n - 2] + bSaved[n - 1] * x[n - 1] - dSaved[n - 1], 1e-12, $"row {n - 1}");
    }

    [Fact]
    public void SolveTridiagonalRejectsMismatchedLengths()
    {
        Assert.Throws<ArgumentException>(() =>
            MathUtils.SolveTridiagonal(new double[2], new double[2], new double[2], new double[2], new double[3]));
    }

    [Fact]
    public void Hermite5SatisfiesBoundaryConditions()
    {
        var p = MathUtils.Hermite5(1.2, -0.75, 0.5, -0.25, 0.9, -1.1);
        var d1 = p.Deriv();
        var d2 = d1.Deriv();

        TestAssert.EqualWithin(1.2, p.Eval(0), 1e-12, "x0");
        TestAssert.EqualWithin(-0.75, p.Eval(1), 1e-12, "x1");
        TestAssert.EqualWithin(0.5, d1.Eval(0), 1e-12, "v0");
        TestAssert.EqualWithin(-0.25, d1.Eval(1), 1e-12, "v1");
        TestAssert.EqualWithin(0.9, d2.Eval(0), 1e-12, "a0");
        TestAssert.EqualWithin(-1.1, d2.Eval(1), 1e-12, "a1");
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(2 * Math.PI, 0.0)]
    [InlineData(3 * Math.PI / 2, -Math.PI / 2)]
    [InlineData(-3 * Math.PI / 2, Math.PI / 2)]
    public void Mod2PiMatchesExpectedRange(double input, double expected)
    {
        TestAssert.EqualWithin(expected, MathUtils.Mod2Pi(input), 1e-12);
    }
}
