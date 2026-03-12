---
title: Package and Assembly
description: Identity details for the NuGet package, project, assembly, and namespace.
---

# Package and Assembly

## Identity

- Package ID: `Spline`
- Project path: `src/Spline/Spline.csproj`
- Assembly name: `Spline.dll`
- Public namespace: `Spline`
- Target framework: `net9.0`

## Namespace and type naming

The package, assembly, and public namespace are all `Spline`. The primary spline
type is also named `Spline`, so many consumers use an alias such as
`using SplinePath = global::Spline.Spline;` to keep code readable.

## Package artifacts

`dotnet pack` produces:

- `Spline.<version>.nupkg`
- `Spline.<version>.snupkg`

The sample app is not packable.
