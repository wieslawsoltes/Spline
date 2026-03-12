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
- spline solving through `Spline`, `TwoParamCurve`, and `TwoParamSpline`
- curve-grid interpolation through `CurveGrid` and `TwoCubics`
- headless polyline simplification with `PolylineUtils`

## Excluded responsibilities

- application settings persistence
- Avalonia windows, views, and interaction logic
- demo-specific editing and visualization behavior

## Packaging model

The package is built from `src/Spline/Spline.csproj` and includes both a
NuGet package and a symbol package during `dotnet pack`.

## Typical consumer flow

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

## When to use DemoSpline instead

Reach for `samples/DemoSpline` when you need:

- interactive editing and tangent manipulation
- freehand tracing and curve fitting validation
- tuner-based exploration of `CurveGrid` and `TwoCubics`
- live confirmation that a package change still behaves as expected
