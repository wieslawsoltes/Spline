using Xunit;

namespace Spline.Core.Tests;

public sealed class CubicBezierTests
{
    [Fact]
    public void DerivativesMatchFiniteDifferencesAndReferenceValues()
    {
        var coords = new[] { 0.1, -0.2, 0.3, 0.4, 0.9, -0.1, 1.2, 0.5 };
        var cubic = new CubicBezier(coords);
        const double t = 0.375;
        const double epsilon = 1e-6;

        var eval = cubic.Eval(t);
        var deriv = cubic.Deriv(t);
        var deriv2 = cubic.Deriv2(t);

        var evalNext = cubic.Eval(t + epsilon);
        var derivNext = cubic.Deriv(t + epsilon);

        var derivApprox = (evalNext - eval) / epsilon;
        var deriv2Approx = (derivNext - deriv) / epsilon;

        TestAssert.VecEqualWithin(new Vec2(0.4568359375, 0.126953125), eval, 1e-12, "eval");
        TestAssert.VecEqualWithin(new Vec2(1.2046875, 0.253125), deriv, 1e-12, "deriv");
        TestAssert.VecEqualWithin(new Vec2(0.8249999999999997, -1.65), deriv2, 1e-12, "deriv2");
        TestAssert.EqualWithin(-1.1775425577056342, cubic.Curvature(t), 1e-12, "curvature");
        TestAssert.EqualWithin(-0.8667516607588899, cubic.AtanCurvature(t), 1e-12, "atanCurvature");

        TestAssert.VecEqualWithin(deriv, derivApprox, 1e-5, "finite diff deriv");
        TestAssert.VecEqualWithin(deriv2, deriv2Approx, 3e-5, "finite diff deriv2");
    }

    [Fact]
    public void SubdivisionMatchesUpstreamReference()
    {
        var cubic = new CubicBezier(new[] { 0.1, -0.2, 0.3, 0.4, 0.9, -0.1, 1.2, 0.5 });

        var left = cubic.LeftHalf();
        var right = cubic.RightHalf();
        var midpoint = cubic.Eval(0.5);

        TestAssert.ArrayEqualWithin(new[]
        {
            0.1, -0.2, 0.2, 0.1, 0.4, 0.12500000000000003, 0.6124999999999999, 0.15000000000000002
        }, left.C, 1e-12, "left");

        TestAssert.ArrayEqualWithin(new[]
        {
            0.6124999999999999, 0.15000000000000002, 0.825, 0.175, 1.05, 0.2, 1.2, 0.5
        }, right.C, 1e-12, "right");

        TestAssert.VecEqualWithin(midpoint, new Vec2(left.C[6], left.C[7]), 1e-12, "left midpoint");
        TestAssert.VecEqualWithin(midpoint, new Vec2(right.C[0], right.C[1]), 1e-12, "right midpoint");
        TestAssert.VecEqualWithin(left.Eval(1), midpoint, 1e-12, "left eval");
        TestAssert.VecEqualWithin(right.Eval(0), midpoint, 1e-12, "right eval");
    }
}
