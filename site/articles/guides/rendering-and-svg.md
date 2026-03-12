---
title: Rendering and SVG Export
description: Solve a spline, render a BezierPath, and serialize SVG path data.
---

# Rendering and SVG Export

The main package workflow ends in either `BezierPath` output or SVG path text.

## Basic flow

1. Import `using Spline;` and optionally `using SplinePath = global::Spline.Spline;`.
2. Create `SplinePath.CP` control points.
3. Construct a `SplinePath`.
4. Call `Solve()`.
5. Optionally call `ComputeCurvatureBlending()`.
6. Render with `Render()` or `RenderSvg()`.

## Object output

`Render()` returns a `BezierPath`, which is useful when you need:

- path command inspection
- hit testing
- segment marks
- a deferred conversion step

## Text output

`RenderSvg()` calls `Render()` and serializes the resulting command stream into
SVG path data using invariant-culture formatting.

## Raw cubic mode

The sample application also exposes a raw cubic rendering mode that skips the
curvature-adjusted path and uses the baseline two-control representation from
`MyCurve.Render()`.
