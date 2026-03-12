using Xunit;

namespace Spline.Tests;

public sealed class SplineTests
{
    [Fact]
    public void SolveMatchesUpstreamComputedTangentsAndCurvatures()
    {
        var spline = new SplinePath(new[]
        {
            new SplinePath.CP(new Vec2(0, 0), "smooth", null, null),
            new SplinePath.CP(new Vec2(1.0, 0.2), "smooth", null, null),
            new SplinePath.CP(new Vec2(2.1, 1.1), "smooth", null, null),
            new SplinePath.CP(new Vec2(3.2, 0.85), "smooth", null, null),
        }, false);

        spline.Solve();

        TestAssert.EqualWithin(-0.1491179161682514, spline.CtrlPts[0].RThComputed, 1e-10, "cp0.rTh");
        TestAssert.EqualWithin(0.580340684180249, spline.CtrlPts[1].LThComputed, 1e-10, "cp1.lTh");
        TestAssert.EqualWithin(0.580340684180249, spline.CtrlPts[1].RThComputed, 1e-10, "cp1.rTh");
        TestAssert.EqualWithin(0.2973262512780154, spline.CtrlPts[2].LThComputed, 1e-10, "cp2.lTh");
        TestAssert.EqualWithin(0.2973262512780154, spline.CtrlPts[2].RThComputed, 1e-10, "cp2.rTh");
        TestAssert.EqualWithin(-0.6551286457154758, spline.CtrlPts[3].LThComputed, 1e-10, "cp3.lTh");

        TestAssert.EqualWithin(0.5693516045049913, spline.CtrlPts[0].RAk, 1e-10, "cp0.rAk");
        TestAssert.EqualWithin(0.6945111170013761, spline.CtrlPts[1].LAk, 1e-10, "cp1.lAk");
        TestAssert.EqualWithin(0.859775646784526, spline.CtrlPts[1].RAk, 1e-10, "cp1.rAk");
        TestAssert.EqualWithin(-0.9808909722116603, spline.CtrlPts[2].LAk, 1e-10, "cp2.lAk");
        TestAssert.EqualWithin(-0.8701865477492885, spline.CtrlPts[2].RAk, 1e-10, "cp2.rAk");
        TestAssert.EqualWithin(-0.6468623560678353, spline.CtrlPts[3].LAk, 1e-10, "cp3.lAk");
    }

    [Fact]
    public void CurvatureBlendingAndRenderMatchUpstreamReferenceData()
    {
        var spline = new SplinePath(new[]
        {
            new SplinePath.CP(new Vec2(0, 0), "smooth", null, null),
            new SplinePath.CP(new Vec2(1.0, 0.25), "smooth", 0.8, null),
            new SplinePath.CP(new Vec2(2.0, 1.2), "smooth", null, null),
            new SplinePath.CP(new Vec2(3.0, 1.0), "corner", null, null),
        }, false);

        spline.Solve();
        spline.ComputeCurvatureBlending();

        TestAssert.EqualWithin(-0.20288016699175024, spline.CtrlPts[0].RThComputed, 1e-10, "cp0.rTh");
        TestAssert.EqualWithin(0.8, spline.CtrlPts[1].LTh!.Value, 1e-12, "cp1.explicit lTh");
        TestAssert.EqualWithin(1.2011353026421068, spline.CtrlPts[1].RThComputed, 1e-10, "cp1.rTh");
        TestAssert.EqualWithin(0.2189278774558325, spline.CtrlPts[2].LThComputed, 1e-10, "cp2.lTh");
        TestAssert.EqualWithin(0.2189278774558325, spline.CtrlPts[2].RThComputed, 1e-10, "cp2.rTh");
        TestAssert.EqualWithin(-0.5672520222693629, spline.CtrlPts[3].LThComputed, 1e-10, "cp3.lTh");

        TestAssert.EqualWithin(0.6571424851400094, spline.CtrlPts[0].RAk, 1e-10, "cp0.rAk");
        TestAssert.EqualWithin(0.9063245322441836, spline.CtrlPts[1].LAk, 1e-10, "cp1.lAk");
        TestAssert.EqualWithin(-0.6532346891123275, spline.CtrlPts[1].RAk, 1e-10, "cp1.rAk");
        TestAssert.EqualWithin(0.0, spline.CtrlPts[1].KBlend!.Value, 1e-12, "cp1.kBlend");
        TestAssert.EqualWithin(-0.8916917393310155, spline.CtrlPts[2].LAk, 1e-10, "cp2.lAk");
        TestAssert.EqualWithin(-0.7415706652787324, spline.CtrlPts[2].RAk, 1e-10, "cp2.rAk");
        TestAssert.EqualWithin(-0.5937797179800445, spline.CtrlPts[3].LAk, 1e-10, "cp3.lAk");

        TestAssert.SvgPathEqualWithin(
            "M0 0C0.08639214148509812 -0.01777175441802816 0.16936518323265098 -0.03649597915417046 0.2532341278944505 -0.05047716874238538C0.33710307255624994 -0.0644583583306003 0.421179266983529 -0.0730276827780214 0.503579835489176 -0.0644701666828088C0.5859804039948228 -0.055912650587596174 0.6660166934300706 -0.029559463956883303 0.7459377470004099 0.022291388653970595C0.8258588005707492 0.07414224126482451 0.904975965127413 0.15215958984868605 1 0.25C1.0639465289893664 0.4150346966288427 1.1196828482256094 0.5538690841997053 1.183839027510403 0.6674038345055994C1.2479952067951963 0.7809385848114936 1.3198959486794206 0.8694353575445246 1.4019900765332343 0.939289678031914C1.4840842043870481 1.0091439985193031 1.5756964207613322 1.0606175264531559 1.6752237643315264 1.1016757453133246C1.7747511079017209 1.1427339641734933 1.881518281218706 1.1736385336520831 2 1.2C2.3432838930492124 1.2763785914610433 2.707080625898664 1.1866184645532591 3 1",
            spline.RenderSvg(),
            1e-12,
            "spline render");
    }
}
