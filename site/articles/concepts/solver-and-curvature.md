---
title: Solver and Curvature
description: How Spline computes tangents and curvature-aware segments.
---

# Solver and Curvature

The `Spline` type works from control points that are marked as either `corner`
or `smooth`. Each point can optionally carry explicit left or right tangent
angles.

## Solve phase

`Spline.Solve()` walks the control-point list segment by segment.

- Straight corner-to-corner spans without explicit tangents fall back to the
  chord direction directly.
- Smooth spans are grouped and solved through `TwoParamSpline`.
- `TwoParamSpline.InitialThs()` seeds tangent angles from neighboring chord
  directions.
- `TwoParamSpline.IterDumb()` refines those angles iteratively so adjacent
  segment curvatures stay coherent.

The result of `Solve()` is a set of computed left and right tangent angles on
the working control points.

## Curvature blending

`Spline.ComputeCurvatureBlending()` adds a second pass for smooth joints.

- It recomputes curvature at the right end of the previous segment and the left
  end of the next segment.
- When both sides have compatible curvature sign, it blends them harmonically.
- When the signs disagree, the blend is forced to zero.

The blended curvature values are later used to choose between a raw cubic shape
and a curvature-adjusted quintic shape.

## Rendering modes

- Raw cubic mode uses `MyCurve.Render()` and preserves the baseline two-control
  representation.
- Curvature-adjusted mode uses `MyCurve.Render4()` and inserts additional
  control points derived from Hermite-based curvature correction.
