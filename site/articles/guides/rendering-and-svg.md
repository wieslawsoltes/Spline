---
title: Rendering and SVG Export
description: Solve a spline, render a BezierPath, and serialize SVG path data.
---

# Rendering and SVG Export

The main package workflow ends in either `BezierPath` output or SVG path text.

## Basic flow

1. Create `Spline.Core.Spline.CP` control points.
2. Construct a `Spline.Core.Spline`.
3. Call `Solve()`.
4. Optionally call `ComputeCurvatureBlending()`.
5. Render with `Render()` or `RenderSvg()`.

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
