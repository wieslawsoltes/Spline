---
title: CI and Releases
description: GitHub Actions workflows used by the repository.
---

# CI and Releases

## Build workflow

The repository `build.yml` workflow restores the solution, builds in `Release`,
runs `Spline.Tests`, packs `Spline`, and uploads package artifacts for inspection.
The test step is explicit so compilation alone cannot make a pull request green.

## Release workflow

The `release.yml` workflow is triggered by `v*` tags or manual dispatch. It:

- determines the release version
- builds the solution in `Release`
- runs the complete test project against the release build
- packs `Spline` only after tests succeed
- publishes packages to NuGet
- creates a GitHub release with package artifacts attached

NuGet publishing requires the `NUGET_API_KEY` secret in the `nuget`
environment.

## Docs workflow

The `docs.yml` workflow restores local tools and runs `check-docs.sh`, which builds
and validates the Lunet articles plus generated API reference.

Documentation validation runs on pull requests targeting `main`/`master`. Pull
requests do **not** deploy. Pushes to `main`/`master` and manual workflow runs
perform the same validation and then publish `site/.lunet/build/www` to GitHub
Pages.
