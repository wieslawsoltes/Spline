---
title: Rendering and Hit Testing
description: Path generation, SVG export, and hit testing in Spline.Core.
---

# Rendering and Hit Testing

After solving, `Spline.Render()` converts each logical segment into a sequence
of path commands stored in `BezierPath`.

## Path generation

For each segment, the renderer:

1. computes the local chord direction
2. converts solved tangent angles into segment-local `th0` and `th1` values
3. requests control geometry from `MyCurve`
4. transforms local control points back into world coordinates
5. appends cubic Bezier commands to the output path

`RenderSvg()` is a convenience wrapper that serializes the final path to SVG
path data.

## Segment marks

`BezierPath.Mark(int)` stores the logical segment index before subsequent path
commands. This is used by the sample application to map a hit-tested path point
back to the originating spline span.

## Hit testing

`BezierPath.HitTest()` performs approximate nearest-distance queries against the
path.

- lines are checked analytically
- curves are subdivided into line segments
- the best distance and the best marked segment index are tracked

This is sufficient for editing behaviors such as inserting knots, measuring fit
error, and refining traced input in `DemoSpline`.
