---
title: Verification
description: Commands used to validate the package, tests, sample app, and docs.
---

# Verification

Use these commands to verify the repository end to end.

## Build the solution

```bash
dotnet build Spline.slnx
```

## Run the unit tests

```bash
dotnet test Spline.slnx
```

## Produce package artifacts

```bash
dotnet pack src/Spline/Spline.csproj -c Release -o artifacts/packages
```

## Build and validate docs

```bash
bash ./check-docs.sh
```

## Manual sample validation

```bash
dotnet run --project samples/DemoSpline/DemoSpline.csproj
```

Use the sample app to check editing gestures, freehand tracing, rendering, and
tuner behavior against your library changes.
