using System.Linq;
using Xunit;

namespace Spline.Tests;

public sealed class BSplineCurvePathTests
{
    [Fact]
    public void ToBezierPathStartsNewSubpathAtFullyDiscontinuousInteriorKnot()
    {
        var curve = new BSplineCurve(
            degree: 3,
            controlPoints: new[]
            {
                new Vec2(0, 0),
                new Vec2(1, 2),
                new Vec2(2, 2),
                new Vec2(3, 0),
                new Vec2(10, 0),
                new Vec2(11, -2),
                new Vec2(12, -2),
                new Vec2(13, 0),
            },
            knots: new[]
            {
                0.0, 0.0, 0.0, 0.0,
                0.5, 0.5, 0.5, 0.5,
                1.0, 1.0, 1.0, 1.0,
            });

        string svg = curve.ToSvgPath();

        Assert.Equal(2, svg.Count(c => c == 'M'));
        Assert.StartsWith("M0 0C", svg);
        Assert.Contains("M10 0C", svg);
    }

    [Fact]
    public void ToBezierPathKeepsContinuousInteriorKnotsInSingleSubpath()
    {
        var curve = GlobalBSpline.Interpolate(
            new[]
            {
                new Vec2(0, 0),
                new Vec2(1, 2),
                new Vec2(2, 1),
                new Vec2(3, 0),
            },
            startTangent: new Vec2(1, 0),
            endTangent: new Vec2(1, 0));

        string svg = curve.ToSvgPath();

        Assert.Equal(1, svg.Count(c => c == 'M'));
    }
}
