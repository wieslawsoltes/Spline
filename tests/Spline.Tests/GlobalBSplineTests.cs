using System;
using Xunit;

namespace Spline.Tests;

public sealed class GlobalBSplineTests
{
    [Fact]
    public void InterpolateMatchesReferenceControlNetAndFitPoints()
    {
        var fitPoints = new[]
        {
            new Vec2(0, 0),
            new Vec2(3, 4),
            new Vec2(-1, 4),
            new Vec2(-4, 0),
            new Vec2(-4, -3),
        };

        var result = GlobalBSpline.InterpolateDetailed(
            fitPoints,
            new Vec2(1, 0),
            new Vec2(0, -1));

        var curve = result.Curve;
        Assert.Equal(3, curve.Degree);
        Assert.Equal(7, curve.ControlPoints.Count);
        Assert.Equal(11, curve.Knots.Count);
        Assert.False(curve.IsRational);

        var expectedParameters = new[]
        {
            0.0,
            5.0 / 17.0,
            9.0 / 17.0,
            14.0 / 17.0,
            1.0,
        };

        for (int i = 0; i < expectedParameters.Length; i++)
        {
            TestAssert.EqualWithin(expectedParameters[i], result.FitParameters[i], 1e-14, $"u[{i}]");
            TestAssert.VecEqualWithin(fitPoints[i], curve.Evaluate(result.FitParameters[i]), 1e-11, $"fit[{i}]");
        }

        var expectedControlPoints = new[]
        {
            new Vec2(0.0, 0.0),
            new Vec2(1.6666666666666667, 0.0),
            new Vec2(5.11009924109749, 5.181436077057794),
            new Vec2(-2.0966725043782835, 4.385405720957385),
            new Vec2(-4.346059544658495, 0.7178050204319895),
            new Vec2(-4.0, -1.9999999999999998),
            new Vec2(-4.0, -3.0),
        };

        for (int i = 0; i < expectedControlPoints.Length; i++)
            TestAssert.VecEqualWithin(expectedControlPoints[i], curve.ControlPoints[i], 1e-11, $"P[{i}]");

        TestAssert.VecEqualWithin(new Vec2(17, 0), curve.EvaluateDerivative(0), 1e-10, "start derivative");
        TestAssert.VecEqualWithin(new Vec2(0, -17), curve.EvaluateDerivative(1), 1e-10, "end derivative");
    }

    [Fact]
    public void DerivativeModePreservesProvidedDerivativeVectors()
    {
        var fitPoints = new[]
        {
            new Vec2(0, 0),
            new Vec2(2, 3),
            new Vec2(5, 1),
            new Vec2(8, 2),
        };
        var startDerivative = new Vec2(4.5, 1.25);
        var endDerivative = new Vec2(3.0, -2.5);

        var result = GlobalBSpline.InterpolateDetailed(
            fitPoints,
            startDerivative,
            endDerivative,
            options: new GlobalBSplineOptions
            {
                TangentMode = BSplineTangentMode.Derivative,
            });

        TestAssert.VecEqualWithin(startDerivative, result.StartDerivative, 1e-13);
        TestAssert.VecEqualWithin(endDerivative, result.EndDerivative, 1e-13);
        TestAssert.VecEqualWithin(startDerivative, result.Curve.EvaluateDerivative(0), 1e-11);
        TestAssert.VecEqualWithin(endDerivative, result.Curve.EvaluateDerivative(1), 1e-11);
    }

    [Fact]
    public void DirectionModeUsesTotalChordLengthAndOptionalScale()
    {
        var fitPoints = new[]
        {
            new Vec2(0, 0),
            new Vec2(3, 4),
            new Vec2(6, 4),
        };

        var result = GlobalBSpline.InterpolateDetailed(
            fitPoints,
            new Vec2(10, 0),
            new Vec2(0, -5),
            options: new GlobalBSplineOptions
            {
                StartTangentScale = 0.5,
                EndTangentScale = 2.0,
            });

        // Chord length is 5 + 3 = 8. Direction mode normalizes the supplied tangent vectors.
        TestAssert.VecEqualWithin(new Vec2(4, 0), result.StartDerivative, 1e-13);
        TestAssert.VecEqualWithin(new Vec2(0, -16), result.EndDerivative, 1e-13);
        TestAssert.VecEqualWithin(result.StartDerivative, result.Curve.EvaluateDerivative(0), 1e-11);
        TestAssert.VecEqualWithin(result.EndDerivative, result.Curve.EvaluateDerivative(1), 1e-11);
    }

    [Fact]
    public void AllParameterizationsInterpolateExactly()
    {
        var fitPoints = new[]
        {
            new Vec2(0, 0),
            new Vec2(0.5, 3),
            new Vec2(4, 4),
            new Vec2(4.5, 0.5),
            new Vec2(10, 0),
        };

        foreach (var parameterization in new[]
                 {
                     BSplineParameterization.ChordLength,
                     BSplineParameterization.Centripetal,
                     BSplineParameterization.Uniform,
                 })
        {
            var result = GlobalBSpline.InterpolateDetailed(
                fitPoints,
                new Vec2(1, 1),
                new Vec2(1, -0.25),
                options: new GlobalBSplineOptions
                {
                    Parameterization = parameterization,
                });

            for (int i = 0; i < fitPoints.Length; i++)
                TestAssert.VecEqualWithin(fitPoints[i], result.Curve.Evaluate(result.FitParameters[i]), 1e-10, $"{parameterization}[{i}]");
        }
    }

    [Fact]
    public void TwoPointInterpolationIsAClampedCubicHermiteCurve()
    {
        var p0 = new Vec2(1, 2);
        var p1 = new Vec2(6, 5);
        var d0 = new Vec2(2, 4);
        var d1 = new Vec2(3, -1);

        var curve = GlobalBSpline.Interpolate(
            new[] { p0, p1 },
            d0,
            d1,
            options: new GlobalBSplineOptions
            {
                TangentMode = BSplineTangentMode.Derivative,
            });

        Assert.Equal(4, curve.ControlPoints.Count);
        Assert.Equal(8, curve.Knots.Count);
        TestAssert.VecEqualWithin(p0, curve.Evaluate(0), 1e-13);
        TestAssert.VecEqualWithin(p1, curve.Evaluate(1), 1e-13);
        TestAssert.VecEqualWithin(d0, curve.EvaluateDerivative(0), 1e-12);
        TestAssert.VecEqualWithin(d1, curve.EvaluateDerivative(1), 1e-12);
    }

    [Fact]
    public void BezierConversionMatchesCubicBSplineOnEverySpan()
    {
        var result = GlobalBSpline.InterpolateDetailed(
            new[]
            {
                new Vec2(0, 0),
                new Vec2(2, 4),
                new Vec2(5, 5),
                new Vec2(8, 1),
                new Vec2(11, 2),
            },
            new Vec2(1, 0.5),
            new Vec2(1, 0));

        var segments = result.Curve.ToBezierSegments();
        Assert.Equal(result.FitParameters.Count - 1, segments.Count);

        var sampleTs = new[] { 0.0, 0.125, 0.5, 0.875, 1.0 };
        for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
        {
            double start = result.FitParameters[segmentIndex];
            double end = result.FitParameters[segmentIndex + 1];
            foreach (double t in sampleTs)
            {
                double u = start + (end - start) * t;
                TestAssert.VecEqualWithin(
                    result.Curve.Evaluate(u),
                    segments[segmentIndex].Eval(t),
                    2e-10,
                    $"segment {segmentIndex} at {t}");
            }
        }

        Assert.StartsWith("M", result.Curve.ToSvgPath());
    }

    [Fact]
    public void GeneratedCurveIsC2AcrossInteriorFitKnots()
    {
        var result = GlobalBSpline.InterpolateDetailed(
            new[]
            {
                new Vec2(0, 0),
                new Vec2(1, 3),
                new Vec2(4, 5),
                new Vec2(8, 2),
                new Vec2(12, 4),
                new Vec2(15, 0),
            },
            new Vec2(1, 0.4),
            new Vec2(1, -0.5));

        var segments = result.Curve.ToBezierSegments();
        for (int i = 1; i < segments.Count; i++)
        {
            double previousSpan = result.FitParameters[i] - result.FitParameters[i - 1];
            double nextSpan = result.FitParameters[i + 1] - result.FitParameters[i];

            var previousFirst = segments[i - 1].Deriv(1) / previousSpan;
            var nextFirst = segments[i].Deriv(0) / nextSpan;
            TestAssert.VecEqualWithin(previousFirst, nextFirst, 2e-9, $"C1 at knot {i}");

            var previousSecond = segments[i - 1].Deriv2(1) / (previousSpan * previousSpan);
            var nextSecond = segments[i].Deriv2(0) / (nextSpan * nextSpan);
            TestAssert.VecEqualWithin(previousSecond, nextSecond, 2e-8, $"C2 at knot {i}");
        }
    }

    [Fact]
    public void LargeInputUsesLinearControlNetAndRemainsInterpolating()
    {
        const int count = 4096;
        var fitPoints = new Vec2[count];
        for (int i = 0; i < count; i++)
        {
            double x = i * 0.01;
            fitPoints[i] = new Vec2(x, Math.Sin(x) + 0.1 * Math.Sin(7 * x));
        }

        var startTangent = fitPoints[1] - fitPoints[0];
        var endTangent = fitPoints[^1] - fitPoints[^2];
        var result = GlobalBSpline.InterpolateDetailed(fitPoints, startTangent, endTangent);

        Assert.Equal(count + 2, result.Curve.ControlPoints.Count);
        Assert.Equal(count + 6, result.Curve.Knots.Count);

        for (int i = 0; i < count; i += 257)
            TestAssert.VecEqualWithin(fitPoints[i], result.Curve.Evaluate(result.FitParameters[i]), 2e-9, $"large fit[{i}]");

        TestAssert.VecEqualWithin(fitPoints[^1], result.Curve.Evaluate(1), 2e-9, "large last fit");
    }

    [Fact]
    public void InvalidInputsAreRejectedExplicitly()
    {
        var points = new[]
        {
            new Vec2(0, 0),
            new Vec2(1, 1),
            new Vec2(2, 0),
        };

        Assert.Throws<NotSupportedException>(() => GlobalBSpline.Interpolate(points, new Vec2(1, 0), new Vec2(1, 0), degree: 2));
        Assert.Throws<ArgumentException>(() => GlobalBSpline.Interpolate(new[] { new Vec2(0, 0) }, new Vec2(1, 0), new Vec2(1, 0)));
        Assert.Throws<ArgumentException>(() => GlobalBSpline.Interpolate(points, default, new Vec2(1, 0)));
        Assert.Throws<ArgumentException>(() => GlobalBSpline.Interpolate(
            new[] { new Vec2(0, 0), new Vec2(0, 0), new Vec2(1, 0) },
            new Vec2(1, 0),
            new Vec2(1, 0)));
    }
}
