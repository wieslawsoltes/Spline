---
title: CI and Releases
description: GitHub Actions workflows used by the repository.
---

# CI and Releases

## Build workflow

The repository `build.yml` workflow restores the solution, builds in `Release`,
packs `Spline`, runs the test project as part of the solution build, and uploads
package artifacts for inspection.

## Release workflow

The `release.yml` workflow is triggered by `v*` tags or manual dispatch. It:

- determines the release version
- builds the solution in `Release`
- packs `Spline`
- publishes packages to NuGet
- creates a GitHub release with package artifacts attached

NuGet publishing requires the `NUGET_API_KEY` secret in the `nuget`
environment.

## Docs workflow

The `docs.yml` workflow restores local tools, builds the Lunet articles and the
generated API reference, and publishes `site/.lunet/build/www` to GitHub Pages.
