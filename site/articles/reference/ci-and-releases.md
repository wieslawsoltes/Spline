---
title: CI and Releases
description: GitHub Actions workflows used by the repository.
---

# CI and Releases

## Build workflow

The repository `build.yml` workflow restores the solution, builds in `Release`,
packs `Spline.Core`, and uploads package artifacts for inspection.

## Release workflow

The `release.yml` workflow is triggered by `v*` tags or manual dispatch. It:

- determines the release version
- builds the solution in `Release`
- packs `Spline.Core`
- publishes packages to NuGet
- creates a GitHub release with package artifacts attached

NuGet publishing requires the `NUGET_API_KEY` secret in the `nuget`
environment.
