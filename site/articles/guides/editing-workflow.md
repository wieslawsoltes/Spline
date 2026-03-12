---
title: Editing Workflow
description: Editing commands and interaction model in DemoSpline.
---

# Editing Workflow

`DemoSpline` is designed as the interactive validation host for `Spline`.

## Core editing gestures

- Click to add a new corner point.
- `Alt+Click` or click-drag to add a smooth point.
- `Shift+Click` to extend the current point selection.
- Double-click to toggle a point between smooth and corner behavior.
- Drag tangent handles to set explicit tangents.
- `Shift+Drag` on a tangent handle to constrain it to axis-aligned directions.
- Press `Delete` or `Backspace` to remove selected points.
- Use arrow keys to nudge points by 1 pixel, or `Shift+Arrow` for 10 pixels.

## Tool modes

The sample exposes several tool modes through the menu:

- Edit spline
- Trace from freehand
- Delete point tool
- Glyph editing
- Curve tuner

## View and rendering controls

The main window also exposes:

- grid visibility
- zoom reset, fit, in, and out
- raw cubic rendering toggle
- knot reduction
- curvature fairing
- span straightening

These controls make it practical to compare interactive edits with the
underlying library output.
