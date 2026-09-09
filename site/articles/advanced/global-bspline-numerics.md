---
title: Global B-Spline Numerics and Performance
description: Solver structure, complexity, numerical safeguards, and production behavior of GlobalBSpline.
---

# Global B-Spline Numerics and Performance

`GlobalBSpline` is intentionally specialized around the open cubic interpolation
case because that case admits a compact, deterministic, linear-time solver.

## Pipeline

For `n + 1` fit points:

1. validate finite geometry and non-degenerate adjacent chords
2. compute normalized fit parameters
3. build a clamped cubic knot vector
4. resolve start/end derivative vectors
5. compute the two endpoint-adjacent control points directly
6. assemble the tridiagonal interior system
7. solve it in place
8. construct immutable `BSplineCurve` state

No iterative optimization is required.

## Knot structure

The output has `n + 3` control points and `n + 7` knots:

```text
fit points      Q0 ... Qn
control points  P0 ... P(n+2)
knots           0 0 0 0 u1 ... u(n-1) 1 1 1 1
```

The interior knots are simple, so a non-degenerate cubic curve is `C2` across
those knots.

## Endpoint derivative equations

Clamping makes endpoint derivatives local:

```text
C'(0) = 3 / U4 * (P1 - P0)
C'(1) = 3 / (1 - U(n+2)) * (P(n+2) - P(n+1))
```

Therefore `P1` and `P(n+1)` do not belong to the global linear solve.

## Tridiagonal interior system

At an interior fit parameter `u[k]`, which is also a simple cubic knot, the
fit-point equation has three non-zero basis coefficients:

```text
Q[k] = a[k] P[k] + b[k] P[k+1] + c[k] P[k+2]
```

After subtracting the already-known endpoint-adjacent terms, the unknowns are
`P2 ... Pn`. Every row touches at most the previous, current, and next unknown,
so the matrix is tridiagonal.

The implementation performs forward elimination and backward substitution in
place on three scalar diagonals and a `Vec2` right-hand side.

## Complexity

| Operation | Time | Additional memory |
| --- | ---: | ---: |
| Distance/parameter generation | O(n) | O(n) |
| Knot construction | O(n) | O(n) |
| Tridiagonal assembly | O(n) | O(n) |
| Tridiagonal solve | O(n) | O(n) |
| One de Boor evaluation | O(log n + p^2) | O(p) |
| Cubic Bezier conversion | O(n) | O(n) output |

For the interpolator `p = 3`, so per-evaluation polynomial work is constant.

## Allocation strategy

The global solve uses flat arrays only. Basis-function scratch storage is a
four-element stack span. No dense `double[,]` matrix is allocated.

`BSplineCurve.Evaluate` uses stack storage for normal spline degrees. Derivative
evaluation derives the control polygon into stack storage for small curves and
uses `ArrayPool<Vec2>` for large control nets to avoid persistent large temporary
allocations.

## Stable length calculation

Geometry validation and tangent normalization use a scaled hypotenuse calculation
rather than directly evaluating `sqrt(x*x + y*y)`. This avoids unnecessary
overflow or underflow when coordinate components have very different magnitudes.

## Degenerate fit data

Exact global interpolation is poorly conditioned when distinct fit constraints
collapse to almost the same parameter. The solver rejects consecutive chords
whose length is below:

```text
RelativePointTolerance * totalChordLength
```

The default is `1e-12`.

This is scale-relative: a model expressed in micrometres and the same model
expressed in metres receive equivalent geometric treatment.

## Pivot checks

The cubic interpolation matrix is expected to be well behaved for valid strictly
increasing parameters, but production code should not silently divide by a tiny
or non-finite pivot. The solver checks each eliminated diagonal against
`SolverPivotTolerance` and reports an explicit failure if the system becomes
numerically unsafe.

## Parameterization and conditioning

Chord-length and centripetal parameterization usually produce better geometric
behavior than uniform parameters for irregular point spacing. Centripetal
parameterization is particularly useful when one or two chords are much longer
than their neighbors.

Parameterization affects shape and matrix coefficients. It does not change the
interpolation invariant:

```text
C(u[k]) = Q[k]
```

## Tangent magnitude

When only tangent direction is known, `Direction` mode normalizes each input
vector and uses total chord length as the default derivative magnitude. This is a
shape heuristic, not an intrinsic property of B-splines.

For CAD data that already provides a derivative vector with meaningful magnitude,
use `BSplineTangentMode.Derivative`.

## Exact rendering conversion

A cubic B-spline is piecewise cubic polynomial. `ToBezierSegments()` samples each
knot span at four exact parameter values and inverts the cubic Bernstein basis.
No flattening tolerance or iterative fitting is involved, and the conversion also
works when a directly constructed cubic curve contains repeated interior knots.

## Production test coverage

The test suite covers:

- known control-net reference data
- fit-point interpolation error
- start/end derivative constraints
- direction and derivative tangent modes
- chord-length, centripetal, and uniform parameterization
- two-point cubic Hermite reduction
- exact B-spline-to-Bezier equivalence across every span
- large multi-thousand-point interpolation
- invalid and degenerate input rejection

The large-input test validates the linear control-net and knot-size invariants
without relying on fragile wall-clock timing assertions.
