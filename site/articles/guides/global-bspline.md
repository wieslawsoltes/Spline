---
title: Global B-Spline Interpolation
description: Build a CAD-style cubic B-spline from fit points and start/end tangents, inspect its control net and knots, and convert it to Bezier or SVG output.
---

# Global B-Spline Interpolation

Use `GlobalBSpline` when your source data is expressed as fit points plus endpoint
tangents and you need a real cubic B-spline representation rather than only a
Bezier rendering result.

## Minimal usage

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
    startTangent: new Vec2(1.0, 0.35),
    endTangent: new Vec2(1.0, -0.20));
```

The default interpretation is `BSplineTangentMode.Direction`. Tangent vector
length is ignored; only direction is used. The solver estimates both endpoint
derivative magnitudes from total chord length.

## Inspect the CAD representation

```csharp
Console.WriteLine(curve.Degree); // 3

foreach (var controlPoint in curve.ControlPoints)
    Console.WriteLine($"P = {controlPoint.X}, {controlPoint.Y}");

foreach (double knot in curve.Knots)
    Console.WriteLine($"U = {knot}");

foreach (double weight in curve.Weights)
    Console.WriteLine($"W = {weight}"); // always 1.0 today
```

`ControlPoints` are B-spline control points, not fit points. The curve generally
does not pass through them.

## Get fit parameters and effective derivatives

Use `InterpolateDetailed` when import/export, validation, or diagnostics need the
parameter assigned to each fit point:

```csharp
GlobalBSplineResult result = GlobalBSpline.InterpolateDetailed(
    fitPoints,
    new Vec2(1, 0.35),
    new Vec2(1, -0.20));

BSplineCurve curve = result.Curve;

for (int i = 0; i < fitPoints.Length; i++)
{
    double u = result.FitParameters[i];
    Vec2 reconstructed = curve.Evaluate(u);
}

Vec2 actualStartDerivative = result.StartDerivative;
Vec2 actualEndDerivative = result.EndDerivative;
```

For a valid solve, `curve.Evaluate(result.FitParameters[i])` reproduces the input
fit point to floating-point accuracy.

## Tangent direction vs derivative

There are two endpoint modes.

### Direction mode

```csharp
var options = new GlobalBSplineOptions
{
    TangentMode = BSplineTangentMode.Direction,
};
```

The supplied vector is normalized. Its effective derivative magnitude is:

```text
total chord length * tangent scale
```

This follows the standard CAGD recommendation for cases where only tangent
direction is known.

You can shape the ends without changing direction:

```csharp
var options = new GlobalBSplineOptions
{
    TangentMode = BSplineTangentMode.Direction,
    StartTangentScale = 0.75,
    EndTangentScale = 1.25,
};
```

### Derivative mode

If the input already supplies first derivatives with respect to the normalized
parameter `u in [0,1]`, preserve their magnitudes:

```csharp
var options = new GlobalBSplineOptions
{
    TangentMode = BSplineTangentMode.Derivative,
};

var curve = GlobalBSpline.Interpolate(
    fitPoints,
    startDerivative,
    endDerivative,
    options: options);
```

`curve.EvaluateDerivative(0)` and `curve.EvaluateDerivative(1)` will reproduce the
requested derivatives within floating-point accuracy.

## Parameterization

The parameterization changes the control net and therefore the shape between fit
points, while preserving interpolation.

### Chord length

```csharp
Parameterization = BSplineParameterization.ChordLength
```

This is the default and is a strong general-purpose choice for engineering data.
Parameter increments are proportional to Euclidean distance between fit points.

### Centripetal

```csharp
Parameterization = BSplineParameterization.Centripetal
```

Parameter increments are proportional to the square root of chord length. This
can reduce overshoot when spacing changes abruptly or points form sharp turns.

### Uniform

```csharp
Parameterization = BSplineParameterization.Uniform
```

Every adjacent pair receives the same parameter interval regardless of geometric
distance. Use this when index spacing has semantic meaning or when reproducing a
known uniform construction.

## Evaluate the curve

```csharp
Vec2 midpoint = curve.Evaluate(0.5);
Vec2 tangent = curve.EvaluateDerivative(0.5);
Vec2 secondDerivative = curve.EvaluateDerivative(0.5, order: 2);
```

Evaluation uses de Boor's algorithm. Cost depends primarily on spline degree,
not total control-point count after the containing knot span has been found.

## Convert to Bezier and SVG

The package's rendering stack is Bezier-based, so cubic B-splines can be bridged
without approximation:

```csharp
IReadOnlyList<CubicBezier> segments = curve.ToBezierSegments();
BezierPath path = curve.ToBezierPath();
string svg = curve.ToSvgPath();
```

Each non-empty cubic knot span becomes exactly one cubic Bezier segment.

## Example using explicit derivatives

```csharp
var fitPoints = new[]
{
    new Vec2(0, 0),
    new Vec2(20, 40),
    new Vec2(60, 35),
    new Vec2(100, 0),
};

var result = GlobalBSpline.InterpolateDetailed(
    fitPoints,
    startTangent: new Vec2(80, 15),
    endTangent: new Vec2(70, -30),
    options: new GlobalBSplineOptions
    {
        Parameterization = BSplineParameterization.Centripetal,
        TangentMode = BSplineTangentMode.Derivative,
    });

for (int i = 0; i < fitPoints.Length; i++)
{
    var p = result.Curve.Evaluate(result.FitParameters[i]);
    Console.WriteLine($"fit {i}: ({p.X:R}, {p.Y:R})");
}
```

## Validation rules

The production solver rejects data that would make the interpolation numerically
undefined or misleading:

- fewer than two fit points
- non-finite coordinates
- zero or non-finite endpoint tangents
- consecutive points that collapse relative to total chord length
- non-positive tangent scales
- unsupported degree values
- singular or numerically unsafe tridiagonal pivots

The default relative point tolerance is `1e-12` of total chord length. It can be
changed through `GlobalBSplineOptions.RelativePointTolerance` when working at
extreme scales.

## Degree support

`GlobalBSpline` currently implements the specialized production path for cubic
B-splines only:

```csharp
GlobalBSpline.Interpolate(..., degree: 3)
```

Cubic interpolation is intentionally specialized because it produces a
tridiagonal system and therefore gives deterministic O(n) construction with very
small constant factors. `BSplineCurve` itself can represent and evaluate other
non-rational B-spline degrees when constructed directly.

## Complete runnable sample

The repository includes:

```text
samples/GlobalBSplineSample/
```

Run it with:

```bash
dotnet run --project samples/GlobalBSplineSample/GlobalBSplineSample.csproj
```

It prints fit-point reconstruction, the solved control net, knot vector, and SVG
path output.
