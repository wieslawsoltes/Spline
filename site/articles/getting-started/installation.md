---
title: Installation
description: Prerequisites and local setup for the Spline repository.
---

# Installation

## Prerequisites

- .NET SDK 9.0 or newer for the solution build
- a shell environment capable of running the repository scripts
- network access for NuGet restore and Lunet template/tool resolution

## Clone and build

```bash
git clone https://github.com/wieslawsoltes/Spline.git
cd Spline
dotnet restore Spline.slnx
dotnet build Spline.slnx
```

## Create a local package

```bash
dotnet pack src/Spline.Core/Spline.Core.csproj -c Release -o artifacts/packages
```

This produces both `.nupkg` and `.snupkg` outputs under `artifacts/packages`.
