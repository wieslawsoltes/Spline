---
title: Quick Start
description: Basic package consumption and demo app usage.
---

# Quick Start

## Add the package

```bash
dotnet add package Spline
```

## Run the demo application

```bash
dotnet run --project samples/DemoSpline/DemoSpline.csproj
```

## Minimal code example

```csharp
using Spline;
using SplinePath = global::Spline.Spline;

var controlPoints = new[]
{
    new SplinePath.CP(new Vec2(0, 0), "corner", null, null),
    new SplinePath.CP(new Vec2(50, 20), "smooth", null, null),
    new SplinePath.CP(new Vec2(100, 0), "corner", null, null),
};

var spline = new SplinePath(controlPoints, isClosed: false);
spline.Solve();
spline.ComputeCurvatureBlending();

string svgPath = spline.RenderSvg();
```

The resulting SVG path data can be used for rendering, export, diagnostics, or
further geometry processing.

For a broader package walkthrough, continue with
[Using Spline](/articles/guides/using-spline/).
