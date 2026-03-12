---
title: Geometry Primitives
description: The low-level geometry types that support the public spline workflow.
---

# Geometry Primitives

The reusable package is built on a small set of geometry and utility types.

## `Vec2`

`Vec2` is the package's 2D vector type. It provides:

- length through `Norm()`
- scalar products through `Dot()`
- oriented area / turning information through `Cross()`
- arithmetic operators for simple vector math

## `CubicBezier`

`CubicBezier` evaluates and differentiates cubic segments in normalized local
space. It is used for:

- shape evaluation and derivative estimation
- curvature estimation
- subdivision into left and right halves

## `BezierPath`

`BezierPath` collects commands in a transport-friendly form:

- `MoveTo`
- `LineTo`
- `CurveTo`
- `ClosePath`
- `Mark`

The same structure is used for SVG serialization and approximate hit testing.

## `MathUtils` and `Polynomial`

`MathUtils` centralizes low-level numerical helpers such as:

- angle normalization with `Mod2Pi`
- Hermite quintic coefficient generation
- hypotenuse calculation
- tridiagonal solving for parity with the original research code

`Polynomial` is a small coefficient-based helper used in the curvature-adjusted
rendering path.
