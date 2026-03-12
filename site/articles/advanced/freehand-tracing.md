---
title: Freehand Tracing
description: How DemoSpline converts traced input into spline control points.
---

# Freehand Tracing

The freehand trace mode converts a raw pointer polyline into a spline using a
small staged pipeline.

## Current parameters

- simplification tolerance: `2.0` pixels
- corner threshold: `35` degrees
- target spacing in smooth spans: `20.0` pixels
- fit tolerance: `2.5` pixels
- refinement iterations: `2`

## Pipeline

1. The raw pointer polyline is simplified with
   `PolylineUtils.RamerDouglasPeucker`.
2. Corners are detected with `PolylineUtils.DetectCorners`.
3. Smooth spans are resampled with `PolylineUtils.ResampleBySpacing`.
4. New spline control points are created as `corner` or `smooth`.
5. A local spline is solved and rendered.
6. If the rendered path deviates too far from the simplified trace, a new smooth
   knot is inserted at the worst sampled point and the refinement repeats.

## Why this exists

This mode uses the library’s own simplification, rendering, and hit-testing
primitives to bootstrap a spline from approximate user input without introducing
UI-only geometry logic.
