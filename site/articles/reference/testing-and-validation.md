---
title: Testing and Validation
description: Test project layout, validation commands, and what they protect.
---

# Testing and Validation

## Test project

- Path: `tests/Spline.Tests/Spline.Tests.csproj`
- Framework: xUnit v3
- Scope: parity, geometry, fair-spline solving, CAD B-spline interpolation, and utility behavior

## Validation commands

```bash
dotnet test Spline.slnx
dotnet build Spline.slnx
bash ./check-docs.sh
```

The Build GitHub Actions workflow also executes the test project before package
creation, so pull requests cannot rely on compilation alone.

## What each command protects

- `dotnet test` protects numerical behavior, translated upstream parity, global B-spline interpolation, endpoint derivative constraints, and exact Bezier conversion
- `dotnet build` catches solution-level integration regressions, including both sample applications
- `check-docs.sh` protects article generation, generated API pages (including the B-spline API), and routing

## Global B-spline validation

`GlobalBSplineTests` includes checks for:

- known reference fit parameters and control points
- exact reconstruction of every fit point
- endpoint derivative reproduction
- direction-mode derivative magnitude estimation
- explicit derivative mode
- chord-length, centripetal, and uniform parameterization
- two-point Hermite reduction
- B-spline/Bezier span equivalence
- a 4,096-fit-point solve to exercise linear-size behavior
- invalid and degenerate input rejection

The large-input test intentionally avoids wall-clock assertions. CI timing is
noisy; the test instead validates the structural O(n) invariants and numerical
accuracy on a data set large enough to expose accidental dense-matrix behavior.

## Manual validation

Not every regression is purely numerical. Use `DemoSpline` for:

- interaction checks
- trace fitting behavior
- tuner workflows
- render and hit-test sanity checks

Use `GlobalBSplineSample` to inspect solved CAD control points, knots, fit-point
reconstruction, and SVG output without UI dependencies.
