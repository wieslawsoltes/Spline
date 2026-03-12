[![Build](https://github.com/wieslawsoltes/Spline/actions/workflows/build.yml/badge.svg)](https://github.com/wieslawsoltes/Spline/actions/workflows/build.yml)
[![Release](https://github.com/wieslawsoltes/Spline/actions/workflows/release.yml/badge.svg)](https://github.com/wieslawsoltes/Spline/actions/workflows/release.yml)
[![Docs](https://github.com/wieslawsoltes/Spline/actions/workflows/docs.yml/badge.svg)](https://github.com/wieslawsoltes/Spline/actions/workflows/docs.yml)

# Spline

Spline is a .NET repository for reusable spline geometry and an Avalonia-based
reference application. The codebase is structured so the spline engine can be
shipped independently as a NuGet package, while `DemoSpline` remains a desktop
application for interactive exploration, tuning, and validation.

The NuGet package ID and public namespace are both `Spline`.

## NuGet Packages

| Package | Description | NuGet | Downloads |
| --- | --- | --- | --- |
| [`Spline`](https://www.nuget.org/packages/Spline/) | Reusable spline geometry, Bezier path generation, curvature helpers, curve-grid interpolation, and headless polyline simplification utilities. | [![NuGet](https://img.shields.io/nuget/v/Spline?logo=nuget)](https://www.nuget.org/packages/Spline/) | [![NuGet Downloads](https://img.shields.io/nuget/dt/Spline?logo=nuget&label=downloads)](https://www.nuget.org/packages/Spline/) |

## Highlights

- `Spline` contains publishable, non-UI spline and Bezier functionality.
- `DemoSpline` provides an Avalonia desktop front end for editing, visualization, and experimentation.
- GitHub Actions workflows are included for CI validation, NuGet publishing, and GitHub release creation.
- A Lunet-based documentation site is included for conceptual docs, workflow docs, and generated API reference.
- NuGet symbol packages are produced alongside the main package for debugging support.

## Repository Layout

- `Spline.slnx`: solution entry point for the repository
- `src/Spline/`: reusable and packable spline library
- `samples/DemoSpline/`: Avalonia desktop application built on top of `Spline`
- `.github/workflows/`: CI and release automation
- `site/`: Lunet documentation site content and navigation

## Getting Started

### Install the package

```bash
dotnet add package Spline
```

Use `using Spline;` in consumer code. Because the main spline type is also
named `Spline`, `using SplinePath = global::Spline.Spline;` is a convenient
alias when you need that type often.

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
dotnet pack src/Spline/Spline.csproj -o artifacts/packages
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

- `build.yml` restores, builds, and packs `Spline` on pushes and pull requests.
- `release.yml` builds a tagged release, publishes NuGet packages, and creates a GitHub release with package artifacts attached.
- `docs.yml` builds the Lunet site and deploys it to GitHub Pages.

The docs site includes article-based documentation and generated API docs for
the `Spline` namespace.

For NuGet publishing, configure the `NUGET_API_KEY` secret in the `nuget`
GitHub environment.

## Upstream and Credits

This repository is an Avalonia/.NET port and packaging of the spline research
work originally published by [Raph Levien](https://levien.com) in
[`raphlinus/spline-research`](https://github.com/raphlinus/spline-research).

Credit for the original spline research, algorithms, and reference
implementation belongs to Raph Levien. This repository adapts that work into a
reusable .NET library, an Avalonia sample application, NuGet packaging, and
project documentation.

## License

This repository is licensed under the MIT License. Additional retained upstream
license texts are included where required by the migrated source material.
