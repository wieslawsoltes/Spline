---
title: Numerical Behavior
description: Notes on iterative solving, normalization, and the package's numerical tradeoffs.
---

# Numerical Behavior

Spline solving relies on lightweight iterative updates instead of a large
general-purpose nonlinear solver.

## Angle normalization

`MathUtils.Mod2Pi()` keeps tangent deltas in a stable range. This matters when
segments rotate across the `-pi` to `pi` boundary.

## Initial tangent seeding

`TwoParamSpline.InitialThs()` estimates starting tangents from neighboring
chords. Good initialization is important because the iterative solve only makes
incremental corrections.

## Iterative refinement

`TwoParamSpline.IterDumb()`:

- updates endpoint tangents when they are unconstrained
- estimates local curvature mismatch between adjacent spans
- applies bounded interior angle corrections

This is simple and robust, but it is intentionally not a full Newton solve.

## Curvature blending

Curvature blending is applied only after the base tangent solve is complete.
When the incoming and outgoing curvature signs disagree, the blend is forced to
zero rather than attempting to smooth through a reversal.
