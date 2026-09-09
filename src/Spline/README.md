# Spline

Reusable spline geometry and fitting primitives extracted from the `DemoSpline`
Avalonia application and published as the `Spline` NuGet package.

## Included

- Levien-derived curvature-oriented fair spline solving through `Spline`
- CAD-style global cubic B-spline interpolation through `GlobalBSpline`
- canonical non-rational B-spline representation through `BSplineCurve`
- de Boor evaluation and derivative evaluation
- exact cubic B-spline to Bezier/SVG conversion
- Bezier path and hit-testing helpers
- Cubic and two-parameter spline primitives
- Curve-grid interpolation helpers
- Headless polyline simplification and corner detection

## Identity

- Package ID: `Spline`
- Assembly: `Spline.dll`
- Namespace: `Spline`

## Target

`Spline` currently targets `net9.0`.

## Fair-spline consumer example

The main spline type is also named `Spline`, so many consumers alias it as
`SplinePath`:

```csharp
using Spline;
using SplinePath = global::Spline.Spline;

var spline = new SplinePath(controlPoints, isClosed: false);
spline.Solve();
string svg = spline.RenderSvg();
```

## CAD-style global B-spline example

```csharp
using Spline;

var fitPoints = new[]
{
    new Vec2(0, 0),
    new Vec2(30, 50),
    new Vec2(80, 40),
    new Vec2(120, 0),
};

var result = GlobalBSpline.InterpolateDetailed(
    fitPoints,
    startTangent: new Vec2(1, 0.25),
    endTangent: new Vec2(1, -0.25));

BSplineCurve curve = result.Curve;
Vec2 point = curve.Evaluate(0.5);
Vec2 derivative = curve.EvaluateDerivative(0.5);
string svg = curve.ToSvgPath();
```

`BSplineCurve` exposes `Degree`, `ControlPoints`, `Knots`, and unit `Weights` for
CAD/NURBS-oriented interchange. The input fit points are distinct from the solved
B-spline control points.
