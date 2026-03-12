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
- Namespace: `Spline.Core`

## Target

`Spline` currently targets `net9.0`.

## Consumer example

```csharp
using Spline.Core;
using SplinePath = Spline.Core.Spline;

var spline = new SplinePath(controlPoints, isClosed: false);
spline.Solve();
string svg = spline.RenderSvg();
```
