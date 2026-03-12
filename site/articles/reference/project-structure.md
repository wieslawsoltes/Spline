---
title: Project Structure
description: Repository layout and responsibility boundaries.
---

# Project Structure

The repository is split into three main areas.

## Source

- `src/Spline.Core/` contains the reusable package code.

## Samples

- `samples/DemoSpline/` contains the Avalonia reference application.

## Documentation and automation

- `site/` contains the Lunet documentation site.
- `.github/workflows/` contains build, release, and docs deployment workflows.
- `Directory.Build.props` centralizes common MSBuild settings.
- `Directory.Packages.props` centralizes NuGet package versions.

## Build entry points

- solution: `Spline.slnx`
- package: `src/Spline.Core/Spline.Core.csproj`
- sample app: `samples/DemoSpline/DemoSpline.csproj`
