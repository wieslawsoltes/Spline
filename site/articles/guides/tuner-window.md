---
title: Tuner Window
description: How the DemoSpline tuner works with CurveGrid and TwoCubics.
---

# Tuner Window

The tuner window is a specialized editor for the `CurveGrid`-based interpolation
data used to explore and adjust curve families.

## What it shows

- a lattice of angle-space sample points
- an interpolated curve preview for the current pointer position
- draggable control points for selected master curves
- a curvature plot for the currently rendered curve
- an optional generated curvature map image

## How it is initialized

At startup, the tuner creates a default `CurveGrid` by:

1. sampling `th0` and `th1` over a triangular lattice
2. generating a base cubic with `TwoParamCurve_MyCubic`
3. raising that cubic into `TwoCubics`
4. storing the result as the master data set

## Load and save

The tuner can serialize and deserialize the grid using `CurveGrid.ToJson()` and
`CurveGrid.FromJson()`. This is separate from the main drawing JSON format.

## Editing behavior

When a master lattice point is selected, the tuner exposes four draggable
control handles. Moving those handles updates the underlying `TwoCubics` data,
with extra symmetry logic when the selected point lies on a symmetric or
antisymmetric axis.
