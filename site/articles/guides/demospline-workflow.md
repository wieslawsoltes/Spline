---
title: DemoSpline Workflow
description: How the Avalonia demo app relates to the reusable package.
---

# DemoSpline Workflow

`DemoSpline` is the reference application for the repository. It lives under
`samples/DemoSpline`, depends on `Spline` from `src/Spline`, and
provides an interactive UI for editing, loading, saving, tuning, and
visualizing spline data.

## Typical workflow

1. Modify reusable geometry or spline logic in `Spline`.
2. Build the full solution with `dotnet build Spline.slnx`.
3. Run `DemoSpline` to validate the runtime behavior interactively.
4. Pack `Spline` once the reusable surface is ready to publish.

## Why keep the demo separate

Keeping `DemoSpline` outside the package surface avoids shipping desktop UI
dependencies to library consumers while preserving a fast feedback loop for
curve behavior changes.
