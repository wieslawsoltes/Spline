using Xunit;

namespace Spline.Core.Tests;

public sealed class SplineCoreParityTests
{
    [Fact]
    public void MyCurveMatchesUpstreamReferenceData()
    {
        var curve = new MyCurve();

        var rendered = curve.Render(0.35, -0.55).ToArray();
        TestAssert.VecEqualWithin(new Vec2(0.33996491468802964, 0.12409688110372516), rendered[0], 1e-12, "render[0]");
        TestAssert.VecEqualWithin(new Vec2(0.6916152785964466, -0.18907228039092797), rendered[1], 1e-12, "render[1]");

        var render4 = curve.Render4(0.35, -0.55, 0.12, -0.09).ToArray();
        var expectedRender4 = new[]
        {
            new Vec2(0.13121609949163424, 0.04789761529495048),
            new Vec2(0.2220179895171856, 0.07672024972460563),
            new Vec2(0.30376369682803883, 0.06778908961764189),
            new Vec2(0.38550940413889206, 0.058857929510678156),
            new Vec2(0.4565921145813146, 0.015549550160052356),
            new Vec2(0.5257264999005974, -0.028010920311147172),
            new Vec2(0.5948608852198803, -0.0715713907823467),
            new Vec2(0.660440131262291, -0.11200737708116322),
            new Vec2(0.732637767073015, -0.11303843868444997),
            new Vec2(0.8048354028837392, -0.11406950028773673),
            new Vec2(0.8820446143090442, -0.0723190619025372),
        };

        for (int i = 0; i < expectedRender4.Length; i++)
        {
            TestAssert.VecEqualWithin(expectedRender4[i], render4[i], 1e-12, $"render4[{i}]");
        }

        var curvature = curve.ComputeCurvature(0.35, -0.55);
        TestAssert.EqualWithin(-1.1284265839609793, curvature.ak0, 1e-12, "ak0");
        TestAssert.EqualWithin(1.1601474446635685, curvature.ak1, 1e-12, "ak1");

        var derivs = curve.ComputeCurvatureDerivs(0.35, -0.55);
        TestAssert.EqualWithin(-1.9136795033070086, derivs.dak0dth0, 1e-9, "dak0dth0");
        TestAssert.EqualWithin(1.2481645934592223, derivs.dak1dth0, 1e-9, "dak1dth0");
        TestAssert.EqualWithin(1.2013791375053984, derivs.dak0dth1, 1e-9, "dak0dth1");
        TestAssert.EqualWithin(-1.9171144267637885, derivs.dak1dth1, 1e-9, "dak1dth1");
        TestAssert.EqualWithin(0.3221088436188455, curve.EndpointTangent(0.35), 1e-12, "endpoint tangent");
    }

    [Fact]
    public void TwoParamSplineMatchesUpstreamReferenceData()
    {
        var curve = new MyCurve();
        var spline = new TwoParamSpline(curve, new[]
        {
            new Vec2(0, 0),
            new Vec2(1.1, 0.25),
            new Vec2(2.1, 1.05),
            new Vec2(3.0, 0.9),
        });

        var initial = spline.InitialThs();
        TestAssert.ArrayEqualWithin(new[]
        {
            0.223476601140633, 0.43481649225419877, 0.18428755552754528, -0.16514867741462688
        }, initial, 1e-12, "initial");

        var expectedErrors = new[]
        {
            0.8811640720287979, 0.7139861947668854, 0.43980591393630597, 0.18047828450586356,
            0.04376287257267597, 0.015343274503285986, 0.006341572627434289, 0.002618948876121907,
            0.001051492888278438, 0.0004131415642930625
        };

        for (int i = 0; i < expectedErrors.Length; i++)
        {
            TestAssert.EqualWithin(expectedErrors[i], spline.IterDumb(i), 1e-10, $"iter error {i}");
        }

        TestAssert.ArrayEqualWithin(new[]
        {
            -0.132759563897988, 0.6200515793107222, 0.28813230311819327, -0.5588789596227831
        }, spline.Ths, 1e-10, "final");

        TestAssert.SvgPathEqualWithin(
            "M0 0 C0.380379257567663 -0.05079777425521821 0.7843501561830156 0.024630153767994906 1.1 0.25 1.4537372570544 0.5025637594647583 1.6653189956766263 0.921169286721252 2.1 1.05 2.4034408894884765 1.1399337809610286 2.736054431799355 1.065068641309515 3 0.9",
            spline.RenderSvg(),
            1e-12,
            "two-param render");
    }
}
