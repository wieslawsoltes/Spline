---
title: Testing and Validation
description: Test project layout, validation commands, and what they protect.
---

# Testing and Validation

## Test project

- Path: `tests/Spline.Tests/Spline.Tests.csproj`
- Framework: xUnit v3
- Scope: parity, geometry, spline solving, and utility behavior

## Validation commands

```bash
dotnet test Spline.slnx
dotnet build Spline.slnx
bash ./check-docs.sh
```

## What each command protects

- `dotnet test` protects numerical behavior and translated upstream parity
- `dotnet build` catches solution-level integration regressions
- `check-docs.sh` protects article generation, API docs generation, and routing

## Manual validation

Not every regression is purely numerical. Use `DemoSpline` for:

- interaction checks
- trace fitting behavior
- tuner workflows
- render and hit-test sanity checks
