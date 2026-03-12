# Spline

Reusable spline geometry and fitting primitives extracted from the `DemoSpline`
Avalonia application and published as the `Spline` NuGet package.

## Included

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

## Consumer example

The main spline type is also named `Spline`, so many consumers alias it as
`SplinePath`:

```csharp
using Spline;
using SplinePath = global::Spline.Spline;

var spline = new SplinePath(controlPoints, isClosed: false);
spline.Solve();
string svg = spline.RenderSvg();
```
