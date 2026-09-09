---
title: Library Architecture
description: High-level structure of the reusable Spline package.
---

# Library Architecture

`Spline` is structured around a small set of geometry responsibilities that
can be reused without any Avalonia dependency.

## Core building blocks

- `Vec2` provides the package's basic 2D vector type.
- `MathUtils` and `Polynomial` support low-level numerical operations.
- `CubicBezier` and `TwoCubics` model Bezier control geometry and segment
  transformations.
- `BezierPath` represents rendered output as commands and also supports
  distance-based hit testing.
- `Spline`, `TwoParamCurve`, `MyCurve`, and `TwoParamSpline` form the existing
  curvature-oriented fair-spline solving and rendering pipeline.
- `GlobalBSpline` is the CAD-oriented interpolation layer. It converts fit
  points plus endpoint tangents into an explicit cubic B-spline control net in
  O(n) time.
- `BSplineCurve` is the canonical non-rational B-spline geometry layer. It owns
  degree, control points, knots, unit weights, de Boor evaluation, derivatives,
  and exact cubic Bezier conversion.
- `CurveGrid` stores an interpolated grid of curve masters for the tuner
  workflow.
- `PolylineUtils` provides headless simplification and resampling utilities for
  converting traced input into spline control points.

## Two independent spline contracts

The fair-spline and CAD B-spline subsystems intentionally share low-level
geometry but not their solver state:

```text
Spline.CP[]
    -> Spline / TwoParamSpline
    -> tangent + curvature solve
    -> BezierPath

fit Vec2[] + endpoint tangents
    -> GlobalBSpline
    -> BSplineCurve { degree, control points, knots, weights }
    -> de Boor evaluation or exact BezierPath conversion
```

This avoids forcing the existing fair-spline solver into B-spline semantics and
avoids reducing CAD geometry to a rendering-only Bezier chain.

## Separation of concerns

The package intentionally excludes:

- windowing and view logic
- app settings persistence
- input handling and editing UX
- file-picker and rendering-surface integration
- product-specific CAD parsers and serializers

Those responsibilities stay in host applications. `samples/DemoSpline` is the
interactive fair-spline host, while `samples/GlobalBSplineSample` is a focused
headless example of the CAD-oriented API.

## Typical fair-spline consumer flow

1. create `Spline.CP` interpolation points
2. construct a `Spline`
3. call `Solve()`
4. optionally call `ComputeCurvatureBlending()`
5. render via `Render()` or `RenderSvg()`

## Typical CAD B-spline consumer flow

1. collect fit points and endpoint tangents
2. call `GlobalBSpline.Interpolate` or `InterpolateDetailed`
3. preserve `Degree`, `ControlPoints`, `Knots`, and `Weights` for CAD geometry
4. evaluate with `BSplineCurve.Evaluate()` or `EvaluateDerivative()`
5. convert to `BezierPath`/SVG only at the rendering boundary when needed
