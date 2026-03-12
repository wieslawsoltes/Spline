---
title: Overview
description: What the repository contains and how to approach it.
---

# Overview

This repository has three distinct audiences:

## Package consumers

Use the `Spline` NuGet package if you need reusable geometry, spline solving,
Bezier path output, or headless polyline processing in another .NET project.

## Repository contributors

Work in the source tree if you need to:

- change numerical behavior in the reusable library
- run the xUnit parity tests
- validate changes with the Avalonia sample app
- rebuild and publish the docs site

## Research and tuning users

Use `samples/DemoSpline` when you need the editing UI, the freehand tracing
pipeline, or the curve-grid tuning workflow.

## Identity summary

- Package ID: `Spline`
- Assembly: `Spline.dll`
- Public namespace: `Spline.Core`
- Sample app: `samples/DemoSpline`
- API docs: `site/.lunet/build/www/api/`
