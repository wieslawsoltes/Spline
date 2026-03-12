---
title: Quick Start
description: Basic package consumption and demo app usage.
---

# Quick Start

## Add the package

```bash
dotnet add package Spline.Core
```

## Run the demo application

```bash
dotnet run --project samples/DemoSpline/DemoSpline.csproj
```

## Minimal code example

```csharp
using Spline.Core;

var controlPoints = new[]
{
    new Spline.CP(new Vec2(0, 0), "corner", null, null),
    new Spline.CP(new Vec2(50, 20), "smooth", null, null),
    new Spline.CP(new Vec2(100, 0), "corner", null, null),
};

var spline = new Spline(controlPoints, isClosed: false);
spline.Solve();
spline.ComputeCurvatureBlending();

string svgPath = spline.RenderSvg();
```

The resulting SVG path data can be used for rendering, export, diagnostics, or
further geometry processing.
