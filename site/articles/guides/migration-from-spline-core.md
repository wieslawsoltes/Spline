---
title: Migration from Spline.Core
description: What changed when the project and NuGet package were renamed to Spline.
---

# Migration from Spline.Core

The package and project identity were simplified from `Spline.Core` to
`Spline`.

## What changed

- NuGet package ID changed from `Spline.Core` to `Spline`
- project path changed from `src/Spline.Core/Spline.Core.csproj` to
  `src/Spline/Spline.csproj`
- solution and workflow references were updated to the new project path

## What stayed the same

- public namespace: `Spline.Core`
- sample app name: `DemoSpline`
- repository name: `Spline`

## Consumer update checklist

1. Change package references to `Spline`.
2. Keep `using Spline.Core;` in application code.
3. Update local project references if you consume the source tree directly.
4. If you have scripts that pack or restore the project by path, update them to
   `src/Spline/Spline.csproj`.
