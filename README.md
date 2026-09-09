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
| [`Spline`](https://www.nuget.org/packages/Spline/) | Reusable fair-spline geometry, CAD-style global cubic B-spline interpolation, Bezier path generation, curvature helpers, curve-grid interpolation, and polyline utilities. | [![NuGet](https://img.shields.io/nuget/v/Spline?logo=nuget)](https://www.nuget.org/packages/Spline/) | [![NuGet Downloads](https://img.shields.io/nuget/dt/Spline?logo=nuget&label=downloads)](https://www.nuget.org/packages/Spline/) |

## Highlights

- `Spline` contains publishable, non-UI spline and Bezier functionality.
- `GlobalBSpline` constructs a true open cubic B-spline from fit points plus endpoint tangents in O(n) time.
- `BSplineCurve` exposes degree, control points, knots, unit weights, de Boor evaluation, derivatives, and exact cubic Bezier conversion.
- The existing Levien-derived `Spline` solver remains available for curvature-oriented fair spline construction.
- `DemoSpline` provides an Avalonia desktop front end for editing, visualization, and experimentation.
- `samples/GlobalBSplineSample` demonstrates CAD-style interpolation, control-net inspection, knot output, and SVG conversion.
- GitHub Actions workflows are included for CI validation, documentation validation/deployment, NuGet publishing, and GitHub release creation.
- A Lunet-based documentation site is included for conceptual docs, workflow docs, and generated API reference.
- NuGet symbol packages are produced alongside the main package for debugging support.

## Repository Layout

- `Spline.slnx`: solution entry point for the repository
- `src/Spline/`: reusable and packable spline library
- `samples/DemoSpline/`: Avalonia desktop application built on top of `Spline`
- `samples/GlobalBSplineSample/`: console sample for global cubic B-spline interpolation
- `tests/Spline.Tests/`: numerical, parity, and geometry tests
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

### CAD-style global B-spline from fit points

```csharp
using Spline;

var fitPoints = new[]
{
    new Vec2(0, 0),
    new Vec2(40, 70),
    new Vec2(90, 100),
    new Vec2(150, 75),
    new Vec2(210, 20),
    new Vec2(280, 0),
};

BSplineCurve curve = GlobalBSpline.Interpolate(
    fitPoints,
    startTangent: new Vec2(1, 0.35),
    endTangent: new Vec2(1, -0.20));

Vec2 p = curve.Evaluate(0.5);
string svg = curve.ToSvgPath();
```

The default tangent mode uses only direction and estimates endpoint derivative
magnitudes from total chord length. Use `BSplineTangentMode.Derivative` when the
input vectors already carry meaningful derivative magnitude.

### Build the repository

```bash
dotnet build Spline.slnx
```

### Run the demo application

```bash
dotnet run --project samples/DemoSpline/DemoSpline.csproj
```

### Run the global B-spline sample

```bash
dotnet run --project samples/GlobalBSplineSample/GlobalBSplineSample.csproj
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

## Two spline models

The package intentionally exposes two different constructions:

- `Spline`: the existing Levien-derived fair-spline solver. Its `Spline.CP` points are interpolation points and the renderer emits Bezier spans after solving tangent/curvature behavior.
- `GlobalBSpline`: a CAD-oriented cubic B-spline interpolator. It solves an explicit B-spline control polygon and clamped knot vector from fit points and endpoint tangent constraints.

Use `GlobalBSpline` when degree/control-point/knot semantics or CAD interchange
matter. Use `Spline` when the existing curvature-oriented editing and rendering
behavior is the desired model. The documentation contains a detailed comparison.

## CI and Release

The repository includes three GitHub Actions workflows:

- `build.yml` restores, builds, tests, and packs `Spline` on pushes and pull requests.
- `release.yml` builds, tests, and packs a tagged release, publishes NuGet packages, and creates a GitHub release with package artifacts attached.
- `docs.yml` validates documentation on pull requests and builds/deploys the Lunet site to GitHub Pages after changes land on `main`/`master`.

The docs site includes article-based documentation and generated API docs for
the `Spline` namespace.

For NuGet publishing, configure the `NUGET_API_KEY` secret in the `nuget`
GitHub environment.

## Upstream and Credits

This repository is an Avalonia/.NET port and packaging of the spline research
work originally published by [Raph Levien](https://levien.com) in
[`raphlinus/spline-research`](https://github.com/raphlinus/spline-research).

Credit for the original fair-spline research, algorithms, and reference
implementation belongs to Raph Levien. This repository adapts that work into a
reusable .NET library, an Avalonia sample application, NuGet packaging, and
project documentation.

The CAD-style global cubic B-spline interpolator is a separate implementation
based on standard CAGD B-spline interpolation mathematics. Its formulation is
consistent with the global cubic interpolation material described by Les Piegl
and Wayne Tiller in *The NURBS Book*.

## License

This repository is licensed under the MIT License. Additional retained upstream
license texts are included where required by the migrated source material.
