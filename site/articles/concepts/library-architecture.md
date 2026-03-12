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
- `Spline`, `TwoParamCurve`, `MyCurve`, and `TwoParamSpline` form the spline
  solving and rendering pipeline.
- `CurveGrid` stores an interpolated grid of curve masters for the tuner
  workflow.
- `PolylineUtils` provides headless simplification and resampling utilities for
  converting traced input into spline control points.

## Separation of concerns

The package intentionally excludes:

- windowing and view logic
- app settings persistence
- input handling and editing UX
- file-picker and rendering-surface integration

Those responsibilities stay in `samples/DemoSpline`, which acts as the
interactive host for the library.

## Typical consumer flow

Most consumers only need to:

1. create `Spline.CP` control points
2. construct a `Spline`
3. call `Solve()`
4. optionally call `ComputeCurvatureBlending()`
5. render via `Render()` or `RenderSvg()`
