---
title: Curve Grid and Tuning
description: Advanced notes on CurveGrid, TwoCubics, and tuning behavior.
---

# Curve Grid and Tuning

`CurveGrid` is the advanced interpolation structure behind the tuner workflow.

## Data model

A grid stores:

- `n`, which defines the angular sampling resolution
- a flat list of `TwoCubics` masters

`GetMaster()` applies symmetry rules so a smaller set of stored masters can
represent a larger effective angle domain.

## Interpolation

`GetInterp(th0, th1)` maps angles into the grid, locates the surrounding master
entries, and bilinearly interpolates the six stored `TwoCubics` coefficients.

## Tuning loop

The tuner workflow uses that interpolation in three ways:

- immediate preview of the interpolated curve under the pointer
- direct editing of selected master curves
- curvature-map generation across the sampled angle domain

This makes the tuner a research and calibration tool rather than part of the
basic package-consumer workflow.
