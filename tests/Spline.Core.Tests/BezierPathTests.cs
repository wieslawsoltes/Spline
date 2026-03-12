using Xunit;

namespace Spline.Core.Tests;

public sealed class BezierPathTests
{
    [Fact]
    public void ToSvgPathMatchesExpectedCommands()
    {
        var path = new BezierPath();
        path.MoveTo(0, 0);
        path.LineTo(4, 0);
        path.CurveTo(4.5, 0, 5, 0.5, 5, 1);
        path.ClosePath();

        Assert.Equal("M0 0L4 0C4.5 0 5 0.5 5 1Z", path.ToSvgPath());
    }

    [Fact]
    public void HitTestTracksClosestMarkedSegment()
    {
        var path = new BezierPath();
        path.MoveTo(0, 0);
        path.Mark(10);
        path.LineTo(4, 0);
        path.Mark(20);
        path.LineTo(4, 4);

        var first = path.HitTest(2, 0.5);
        var second = path.HitTest(3.5, 3);

        Assert.Equal(10, first.BestMark);
        TestAssert.EqualWithin(0.5, first.BestDist, 1e-12, "first dist");

        Assert.Equal(20, second.BestMark);
        TestAssert.EqualWithin(0.5, second.BestDist, 1e-12, "second dist");
    }
}
