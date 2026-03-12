using Xunit;

namespace Spline.Core.Tests;

public sealed class TwoCubicsAndCurveGridTests
{
    [Fact]
    public void TwoCubicsMatchesUpstreamReferenceData()
    {
        var twoCubics = new TwoCubics(new[] { 0.21, 0.34, 0.11, 0.71, -0.08, 0.25 });

        TestAssert.VecEqualWithin(new Vec2(0.525, 0.015), twoCubics.GetCenterPt(0.4, -0.3), 1e-12, "center");

        var rendered = twoCubics.Render(0.4, -0.3).ToArray();
        var expectedRendered = new[]
        {
            new Vec2(0.19342280874060586, 0.08177785188481661),
            new Vec2(0.34, 0.11),
            new Vec2(0.525, 0.015),
            new Vec2(0.71, -0.08),
            new Vec2(0.7611658777185986, -0.07388005166533489),
        };

        for (int i = 0; i < expectedRendered.Length; i++)
        {
            TestAssert.VecEqualWithin(expectedRendered[i], rendered[i], 1e-12, $"render[{i}]");
        }

        TestAssert.EqualWithin(-0.43929931047434495, twoCubics.AtanCurvature(0.4, -0.3), 1e-12, "atanCurvature");
        TestAssert.ArrayEqualWithin(new[] { 0.25, 0.29000000000000004, 0.08, 0.6599999999999999, -0.11, 0.21 }, twoCubics.Turn().A, 1e-12, "turn");
        TestAssert.ArrayEqualWithin(new[] { 0.25, 0.29000000000000004, -0.08, 0.6599999999999999, 0.11, 0.21 }, twoCubics.FlipHoriz().A, 1e-12, "flipHoriz");
        TestAssert.ArrayEqualWithin(new[] { 0.21, 0.34, -0.11, 0.71, 0.08, 0.25 }, twoCubics.FlipVert().A, 1e-12, "flipVert");

        var cubic = new CubicBezier(new[] { 0.1, -0.2, 0.3, 0.4, 0.9, -0.1, 1.2, 0.5 });
        TestAssert.ArrayEqualWithin(new[]
        {
            0.223606797749979, 0.4, 0.12500000000000003, 0.825, 0.175, 0.20615528128088303
        }, TwoCubics.Raise(cubic).A, 1e-12, "raise cubic");

        var myCurve = new MyCurve();
        TestAssert.ArrayEqualWithin(new[]
        {
            0.18095315631297468, 0.34288627699312646, 0.014780370454130588,
            0.6807988679702307, -0.0635119199195327, 0.18086560176507654
        }, TwoCubics.Raise(myCurve.Render(0.35, -0.55)).A, 1e-12, "raise points");
    }

    [Fact]
    public void CurveGridInterpolationAndSymmetryMatchUpstreamReferenceData()
    {
        var masters = new List<TwoCubics>
        {
            new(new[] { 1.0 / 6.0, 1.0 / 3.0, 0.0, 2.0 / 3.0, 0.0, 1.0 / 6.0 }),
            new(new[] { 0.18, 0.31, 0.02, 0.68, -0.01, 0.19 }),
            new(new[] { 0.2, 0.34, 0.04, 0.71, -0.03, 0.21 }),
            new(new[] { 0.22, 0.29, 0.08, 0.66, -0.02, 0.24 }),
            new(new[] { 0.25, 0.36, 0.11, 0.74, -0.05, 0.27 }),
            new(new[] { 0.27, 0.39, 0.13, 0.78, -0.07, 0.29 }),
            new(new[] { 0.29, 0.42, 0.16, 0.81, -0.09, 0.31 }),
            new(new[] { 0.31, 0.45, 0.18, 0.84, -0.11, 0.34 }),
            new(new[] { 0.33, 0.48, 0.2, 0.88, -0.12, 0.36 }),
        };

        var grid = new CurveGrid(2, masters);

        TestAssert.ArrayEqualWithin(new[] { 0.2, 0.34, 0.04, 0.71, -0.03, 0.21 }, grid.GetMaster(1, 0).A, 1e-12, "master 1,0");
        TestAssert.ArrayEqualWithin(new[] { 0.21, 0.29000000000000004, -0.03, 0.6599999999999999, 0.04, 0.2 }, grid.GetMaster(0, 1).A, 1e-12, "master 0,1");
        TestAssert.ArrayEqualWithin(new[] { 0.19, 0.31999999999999995, -0.01, 0.69, 0.02, 0.18 }, grid.GetMaster(-1, 1).A, 1e-12, "master -1,1");
        TestAssert.ArrayEqualWithin(new[]
        {
            0.18646416603023472, 0.32742311093241827, 0.021526714231063962,
            0.6840123733643114, -0.017683323990971715, 0.19030755627032697
        }, grid.GetInterp(0.4, -0.2).A, 1e-12, "interp");

        var roundTrip = CurveGrid.FromJson(grid.ToJson());
        Assert.Equal(grid.N, roundTrip.N);
        Assert.Equal(grid.Masters.Count, roundTrip.Masters.Count);
        for (int i = 0; i < grid.Masters.Count; i++)
        {
            TestAssert.ArrayEqualWithin(grid.Masters[i].A, roundTrip.Masters[i].A, 1e-12, $"roundTrip[{i}]");
        }
    }
}
