using Xunit;

namespace Spline.Core.Tests;

public sealed class PolylineUtilsTests
{
    [Fact]
    public void RamerDouglasPeuckerPreservesEndpointsAndCriticalDeviation()
    {
        var points = new[]
        {
            new Vec2(0, 0),
            new Vec2(1, 0.01),
            new Vec2(2, -0.01),
            new Vec2(3, 1),
            new Vec2(4, 2),
        };

        var simplified = PolylineUtils.RamerDouglasPeucker(points, 0.1);

        Assert.Equal(3, simplified.Count);
        TestAssert.VecEqualWithin(points[0], simplified[0], 1e-12);
        TestAssert.VecEqualWithin(points[2], simplified[1], 1e-12);
        TestAssert.VecEqualWithin(points[4], simplified[2], 1e-12);
    }

    [Fact]
    public void DetectCornersLengthAndResamplingBehaveConsistently()
    {
        var polyline = new[]
        {
            new Vec2(0, 0),
            new Vec2(1, 0),
            new Vec2(1, 1),
            new Vec2(2, 1),
        };

        var corners = PolylineUtils.DetectCorners(polyline, Math.PI / 4);
        Assert.Equal([0, 1, 2, 3], corners.OrderBy(x => x).ToArray());

        TestAssert.EqualWithin(3, PolylineUtils.ComputePolylineLength(polyline, 0, 3), 1e-12, "length");

        var resampled = PolylineUtils.ResampleBySpacing(polyline, 0, 3, 0.5);
        Assert.True(resampled.Count >= 6);
        TestAssert.VecEqualWithin(polyline[0], resampled[0], 1e-12, "resample start");
        TestAssert.VecEqualWithin(polyline[^1], resampled[^1], 1e-12, "resample end");
    }
}
