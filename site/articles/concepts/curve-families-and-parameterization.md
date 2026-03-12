---
title: Curve Families and Parameterization
description: How local chord frames and two-parameter curve families shape the spline output.
---

# Curve Families and Parameterization

Spline solving and rendering are expressed in a local frame for each segment.

## Local chord frame

Each segment is normalized into a chord-aligned coordinate system:

- the segment start is `(0, 0)`
- the segment end is `(1, 0)`
- tangent angles are represented relative to the chord direction

This makes the curve family reusable across arbitrary world-space geometry.

## `TwoParamCurve`

`TwoParamCurve` is the abstraction for families defined by two endpoint tangent
angles:

- `Render(th0, th1)` returns local control geometry
- `ComputeCurvature(th0, th1)` estimates endpoint curvature
- `EndpointTangent(th)` provides the default boundary tangent model

## `MyCurve`

`MyCurve` is the concrete family used by the package and sample. It provides:

- baseline cubic rendering
- curvature-aware `Render4(...)` output for smoother joints
- the endpoint tangent rule used by the global solver

## `TwoCubics` and `CurveGrid`

The tuner workflow stores samples of a curve family using `TwoCubics`. Those
masters are arranged in `CurveGrid`, which:

- exploits symmetry to reduce stored data
- interpolates between sampled masters
- supports JSON serialization and tuning-oriented experimentation
