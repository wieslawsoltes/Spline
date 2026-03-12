---
title: Using Spline.Core
description: Overview of the reusable package surface and intended responsibilities.
---

# Using Spline.Core

`Spline.Core` is the reusable library in this repository. It is designed to
hold non-UI spline functionality so consumers can use the geometry engine
without taking an Avalonia dependency.

## Included responsibilities

- vector math through `Vec2`
- Bezier helpers such as `BezierPath` and `CubicBezier`
- spline solving through `Spline`, `TwoParamCurve`, and `TwoParamSpline`
- curve-grid interpolation through `CurveGrid` and `TwoCubics`
- headless polyline simplification with `PolylineUtils`

## Excluded responsibilities

- application settings persistence
- Avalonia windows, views, and interaction logic
- demo-specific editing and visualization behavior

## Packaging model

The package is built from `src/Spline.Core/Spline.Core.csproj` and includes both a
NuGet package and a symbol package during `dotnet pack`.
