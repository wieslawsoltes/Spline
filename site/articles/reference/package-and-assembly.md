---
title: Package and Assembly
description: Identity details for the NuGet package, project, assembly, and namespace.
---

# Package and Assembly

## Identity

- Package ID: `Spline`
- Project path: `src/Spline/Spline.csproj`
- Assembly name: `Spline.dll`
- Public namespace: `Spline.Core`
- Target framework: `net9.0`

## Why package and namespace differ

The package was renamed to `Spline` for simpler distribution and discovery.
The namespace remains `Spline.Core` so existing code and internal structure do
not need a broader public API rename.

## Package artifacts

`dotnet pack` produces:

- `Spline.<version>.nupkg`
- `Spline.<version>.snupkg`

The sample app is not packable.
