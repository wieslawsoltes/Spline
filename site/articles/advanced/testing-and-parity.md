---
title: Testing and Parity
description: How the xUnit suite validates behavior against the original spline-research code.
---

# Testing and Parity

The repository includes a dedicated xUnit project at
`tests/Spline.Tests/Spline.Tests.csproj`.

## What is covered

- direct ports of the explicit upstream numerical self-checks
- parity fixtures for cubic Bezier evaluation and subdivision
- parity coverage for `MyCurve`, `TwoParamSpline`, `TwoCubics`, `CurveGrid`,
  and `Spline`
- behavior tests for `BezierPath` and `PolylineUtils`

## Why parity tests matter

The original `spline-research` repository did not ship as a conventional .NET
test project. The current suite captures the translated behavior in a form that
can run in CI and protect the package surface against accidental drift.

## Run the suite

```bash
dotnet test Spline.slnx
```

When you change solver or rendering behavior, validate both:

- the xUnit suite
- the interactive result in `DemoSpline`
