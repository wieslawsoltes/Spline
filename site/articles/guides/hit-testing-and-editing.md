---
title: Hit Testing and Editing
description: How BezierPath marks and hit tests support editing workflows.
---

# Hit Testing and Editing

`BezierPath` is more than a serialization container. It also carries enough
information to support editing-oriented lookup.

## Segment marks

Before appending the path data for a logical spline segment, `Spline.Render()`
calls `BezierPath.Mark(i)`.

That means `HitTest()` can report not only a distance, but also the originating
segment index through `BestMark`.

## Approximate hit testing

`BezierPath.HitTest(x, y)`:

- evaluates line segments analytically
- subdivides cubic segments into short line segments
- tracks the best distance and the currently active mark

This approximation is sufficient for:

- selecting nearby spans
- inserting knots into an existing path
- estimating deviation in freehand trace refinement

## DemoSpline usage

`DemoSpline` uses this workflow to decide where pointer interaction should land
on a rendered curve, even though the reusable package itself stays UI-free.
