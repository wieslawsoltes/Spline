---
title: Using Spline
description: Package identity, namespace, responsibilities, and consumer expectations.
---

# Using Spline

`Spline` is the NuGet package published from this repository. It exposes the
reusable spline engine without bringing the Avalonia UI layer along with it.

## Package and namespace

- NuGet package ID: `Spline`
- Assembly: `Spline.dll`
- Public namespace: `Spline`

The package, assembly, and public namespace are aligned as `Spline`. Because
the namespace and the main spline type now share the same identifier, an alias
such as `using SplinePath = global::Spline.Spline;` keeps consumer code clear.

## Included responsibilities

- vector math through `Vec2`
- Bezier helpers such as `BezierPath` and `CubicBezier`
- curvature-oriented fair-spline solving through `Spline`, `TwoParamCurve`, and `TwoParamSpline`
- CAD-style global cubic B-spline interpolation through `GlobalBSpline`
- canonical B-spline geometry/evaluation through `BSplineCurve`
- curve-grid interpolation through `CurveGrid` and `TwoCubics`
- headless polyline simplification with `PolylineUtils`

## Excluded responsibilities

- application settings persistence
- Avalonia windows, views, and interaction logic
- demo-specific editing and visualization behavior
- product-specific CAD file parsing/serialization

## Packaging model

The package is built from `src/Spline/Spline.csproj` and includes both a
NuGet package and a symbol package during `dotnet pack`.

## Fair-spline consumer flow

```csharp
using Spline;
using SplinePath = global::Spline.Spline;

var controlPoints = new[]
{
    new SplinePath.CP(new Vec2(0, 0), "corner", null, null),
    new SplinePath.CP(new Vec2(60, 20), "smooth", null, null),
    new SplinePath.CP(new Vec2(120, 0), "corner", null, null),
};

var spline = new SplinePath(controlPoints, isClosed: false);
spline.Solve();
spline.ComputeCurvatureBlending();

BezierPath path = spline.Render();
string svg = path.ToSvgPath();
```

## CAD-style B-spline consumer flow

```csharp
using Spline;

var fitPoints = new[]
{
    new Vec2(0, 0),
    new Vec2(60, 40),
    new Vec2(120, 10),
    new Vec2(180, 0),
};

var result = GlobalBSpline.InterpolateDetailed(
    fitPoints,
    startTangent: new Vec2(1, 0.2),
    endTangent: new Vec2(1, -0.1));

BSplineCurve curve = result.Curve;

IReadOnlyList<Vec2> cadControlPoints = curve.ControlPoints;
IReadOnlyList<double> knots = curve.Knots;
IReadOnlyList<double> weights = curve.Weights;

string svg = curve.ToSvgPath();
```

Use `GlobalBSpline` when fit-point interpolation must produce explicit
B-spline degree/control-point/knot semantics. Use the existing `Spline` solver
when its curvature-oriented fairing behavior and editing model are the desired
contract.

## When to use DemoSpline instead

Reach for `samples/DemoSpline` when you need:

- interactive editing and tangent manipulation
- freehand tracing and curve fitting validation
- tuner-based exploration of `CurveGrid` and `TwoCubics`
- live confirmation that a package change still behaves as expected

For a focused headless example of CAD-style interpolation, use
`samples/GlobalBSplineSample`.
