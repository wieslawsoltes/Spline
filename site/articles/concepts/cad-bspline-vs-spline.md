---
title: Spline Solver vs CAD B-Spline
description: Architectural and mathematical comparison between the existing curvature-fair Spline solver and the CAD-oriented GlobalBSpline interpolator.
---

# Spline Solver vs CAD B-Spline

The package now contains two deliberately different spline models. They solve
different problems and should not be treated as interchangeable representations.

## Existing `Spline` solver

`Spline` is based on the Levien spline-research model already used throughout
`DemoSpline`. Its `Spline.CP` points are interpolation points: rendered spans end
at those points, so the curve passes through them. The solver determines tangent
angles and curvature behavior across smooth runs, then emits cubic or
curvature-adjusted Bezier spans.

This model is particularly useful when the primary objective is visually fair
shape construction, curvature behavior, interactive editing, and compatibility
with the existing DemoSpline workflow.

The model does **not** expose a canonical CAD B-spline control polygon, degree,
knot vector, or rational weights.

## `GlobalBSpline`

`GlobalBSpline` solves a classical open cubic B-spline interpolation problem.
Input fit points are mapped to normalized parameters, endpoint tangent constraints
are applied, and a global control polygon is solved so that

```text
C(u_k) = Q_k
```

for every fit point `Q_k`.

The output is a `BSplineCurve` with explicit:

- degree (`3`)
- B-spline control points
- clamped knot vector
- unit weights for NURBS/CAD interchange
- parameter-domain evaluation
- derivative evaluation
- exact cubic Bezier decomposition

The resulting curve is non-rational, but a non-rational B-spline is also a NURBS
curve whose weights are all `1.0`.

## Terminology

The distinction between fit points and control points is important:

```text
Existing Spline
---------------
Spline.CP points           curve passes through these points
computed tangent data      solved internally
Bezier handles             generated during rendering

GlobalBSpline
-------------
fit points Q[k]            curve passes through these points
control points P[i]        generally NOT on the curve
knots U[i]                 define B-spline basis support
weights W[i]               all 1.0 in the current implementation
```

For CAD import/export code, `BSplineCurve.ControlPoints` should be called control
points. Input passed to `GlobalBSpline.Interpolate` should be called fit points.

## Mathematical model

For the CAD-oriented curve,

```text
C(u) = sum(N[i,3](u) * P[i])
```

where `N[i,3]` are cubic B-spline basis functions and `P[i]` are the solved
control points.

The implementation uses a clamped cubic knot vector. After parameterizing the
fit points,

```text
u[0] = 0
u[n] = 1
```

the interior fit parameters become simple interior knots:

```text
U = [0, 0, 0, 0, u[1], ..., u[n-1], 1, 1, 1, 1]
```

This makes every interior fit point a simple cubic knot and produces a `C2`
curve across non-degenerate interior spans.

Endpoint first derivatives determine the two inner endpoint control points:

```text
P[0]     = Q[0]
P[1]     = P[0] + U[4] / 3 * D0

P[n+2]   = Q[n]
P[n+1]   = P[n+2] - (1 - U[n+2]) / 3 * Dn
```

The remaining unknown control points form a tridiagonal system.

## Why the solver is O(n)

At an interior simple cubic knot only three basis functions contribute to the
fit-point equation. After the endpoint-adjacent control points are known, each
remaining equation touches only the previous, current, and next unknown control
point. The coefficient matrix is therefore tridiagonal.

Construction uses a Thomas-style LU elimination specialized for this matrix:

```text
parameterization      O(n)
knot generation       O(n)
matrix assembly       O(n)
tridiagonal solve     O(n)
control-net storage   O(n)
```

There is no dense matrix allocation and no general-purpose linear algebra
package in the hot path.

## Shape behavior

| Property | `Spline` | `GlobalBSpline` |
| --- | --- | --- |
| Passes through supplied points | Yes | Yes |
| Interior shape objective | curvature-oriented fair solve | cubic B-spline interpolation |
| Endpoint constraints | tangent angles | tangent directions or derivatives |
| Canonical degree | no B-spline degree | cubic (`3`) |
| Knot vector | no | yes |
| B-spline control polygon | no | yes |
| Rational weights | no | unit weights exposed |
| Typical rendering | Bezier path | de Boor evaluation or exact Bezier spans |
| Construction complexity | iterative curvature solve | O(n) tridiagonal solve |
| CAD interchange semantics | indirect | direct B-spline representation |

Neither model is universally better. Use the model whose invariants match the
consumer.

## CAD and DXF expectations

`BSplineCurve` provides the core data needed by CAD-style spline formats:
`Degree`, `ControlPoints`, `Knots`, and `Weights`. Fit points and the effective
start/end derivatives are available from `GlobalBSplineResult`.

This should be described as **CAD-compatible spline mathematics**, not as a claim
of shape-identical reconstruction for every CAD product. A particular CAD
application may apply implementation-specific rules for fit tolerance,
parameterization, tangent magnitude estimation, periodic closure, rational
weights, or fit-data reconstruction. Preserve imported control-point/knot data
when exact round-trip identity is required; use `GlobalBSpline` when constructing
a new curve from fit data.

## Continuity

With distinct fit parameters and simple interior knots, a cubic B-spline has
`C2` continuity at each interior knot. Endpoint tangent constraints establish
first derivative values, but they do not by themselves force a particular
second derivative at the endpoints.

The existing `Spline` solver uses a different curvature matching strategy and
therefore should not be judged by B-spline knot-continuity rules.

## Rendering bridge

CAD data does not force the application renderer to implement B-spline rasterization.
For cubic curves:

```csharp
BSplineCurve curve = GlobalBSpline.Interpolate(
    fitPoints,
    startTangent,
    endTangent);

BezierPath path = curve.ToBezierPath();
string svg = curve.ToSvgPath();
```

`ToBezierSegments()` creates one mathematically equivalent cubic Bezier per
non-empty knot span. This gives the package a clean separation between canonical
CAD geometry and the existing Bezier rendering pipeline.

## References

The global cubic interpolation formulation follows the standard CAGD approach
described by Les Piegl and Wayne Tiller in *The NURBS Book*, particularly the
global interpolation material in Chapter 9. The implementation is written for
this package and does not copy an external solver or depend on a third-party
numerics library.
