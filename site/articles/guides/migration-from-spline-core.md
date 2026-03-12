---
title: Migration from Spline.Core
description: What changed when the project, package, and namespace moved from Spline.Core to Spline.
---

# Migration from Spline.Core

The package, project, and public namespace were simplified from `Spline.Core`
to `Spline`.

## What changed

- NuGet package ID changed from `Spline.Core` to `Spline`
- project path changed from `src/Spline.Core/Spline.Core.csproj` to
  `src/Spline/Spline.csproj`
- public namespace changed from `Spline.Core` to `Spline`
- solution and workflow references were updated to the new project path

## What stayed the same

- sample app name: `DemoSpline`
- repository name: `Spline`

## Consumer update checklist

1. Change package references to `Spline`.
2. Change `using Spline.Core;` to `using Spline;`.
3. If you frequently construct the `Spline` type, add
   `using SplinePath = global::Spline.Spline;`.
4. Update local project references if you consume the source tree directly.
5. If you have scripts that pack or restore the project by path, update them to
   `src/Spline/Spline.csproj`.
