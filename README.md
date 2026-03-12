[![Build](https://github.com/wieslawsoltes/Spline/actions/workflows/build.yml/badge.svg)](https://github.com/wieslawsoltes/Spline/actions/workflows/build.yml)
[![Release](https://github.com/wieslawsoltes/Spline/actions/workflows/release.yml/badge.svg)](https://github.com/wieslawsoltes/Spline/actions/workflows/release.yml)
[![Docs](https://github.com/wieslawsoltes/Spline/actions/workflows/docs.yml/badge.svg)](https://github.com/wieslawsoltes/Spline/actions/workflows/docs.yml)

# Spline

Spline is a .NET repository for reusable spline geometry and an Avalonia-based
reference application. The codebase is structured so the spline engine can be
shipped independently as a NuGet package, while `DemoSpline` remains a desktop
application for interactive exploration, tuning, and validation.

## NuGet Packages

| Package | Description | NuGet | Downloads |
| --- | --- | --- | --- |
| [`Spline.Core`](https://www.nuget.org/packages/Spline.Core/) | Reusable spline geometry, Bezier path generation, curvature helpers, curve-grid interpolation, and headless polyline simplification utilities. | [![NuGet](https://img.shields.io/nuget/v/Spline.Core?logo=nuget)](https://www.nuget.org/packages/Spline.Core/) | [![NuGet Downloads](https://img.shields.io/nuget/dt/Spline.Core?logo=nuget&label=downloads)](https://www.nuget.org/packages/Spline.Core/) |

## Highlights

- `Spline.Core` contains publishable, non-UI spline and Bezier functionality.
- `DemoSpline` provides an Avalonia desktop front end for editing, visualization, and experimentation.
- GitHub Actions workflows are included for CI validation, NuGet publishing, and GitHub release creation.
- A Lunet-based documentation site is included for project, package, and workflow documentation.
- NuGet symbol packages are produced alongside the main package for debugging support.

## Repository Layout

- `Spline.slnx`: solution entry point for the repository
- `src/Spline.Core/`: reusable and packable spline library
- `samples/DemoSpline/`: Avalonia desktop application built on top of `Spline.Core`
- `.github/workflows/`: CI and release automation
- `site/`: Lunet documentation site content and navigation

## Getting Started

### Install the package

```bash
dotnet add package Spline.Core
```

### Build the repository

```bash
dotnet build Spline.slnx
```

### Run the demo application

```bash
dotnet run --project samples/DemoSpline/DemoSpline.csproj
```

### Create local packages

```bash
dotnet pack src/Spline.Core/Spline.Core.csproj -o artifacts/packages
```

### Build documentation locally

```bash
bash ./check-docs.sh
```

### Serve documentation locally

```bash
bash ./serve-docs.sh
```

## CI and Release

The repository includes two GitHub Actions workflows:

- `build.yml` restores, builds, and packs `Spline.Core` on pushes and pull requests.
- `release.yml` builds a tagged release, publishes NuGet packages, and creates a GitHub release with package artifacts attached.
- `docs.yml` builds the Lunet site and deploys it to GitHub Pages.

For NuGet publishing, configure the `NUGET_API_KEY` secret in the `nuget`
GitHub environment.

## License

This repository is licensed under the MIT License. Additional retained upstream
license texts are included where required by the migrated source material.
