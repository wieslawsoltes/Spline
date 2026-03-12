---
title: Curve Grid and Serialization
description: Working with CurveGrid, TwoCubics, and the JSON representation used by the tuner workflow.
---

# Curve Grid and Serialization

`CurveGrid` is the package structure used to store and interpolate a sampled
family of `TwoCubics` masters.

## What it stores

- `N`: the angular sampling density
- `Masters`: the canonical list of stored `TwoCubics` values

## Symmetry handling

The grid does not need to store every angular quadrant explicitly.
`GetMaster(i, j)` maps requests through symmetry operations:

- horizontal flip
- vertical flip
- 180-degree turn

## Interpolation

`GetInterp(th0, th1)`:

1. maps angles to grid coordinates
2. finds the surrounding master samples
3. bilinearly interpolates the six stored coefficients

## JSON round-tripping

The tuner workflow persists grids with:

- `CurveGrid.ToJson()`
- `CurveGrid.FromJson(string json)`

This makes it practical to save calibration data, compare tuned master sets,
and ship interpolated curve families independently of the UI.
