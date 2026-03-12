---
title: JSON Format
description: JSON serialization format used by DemoSpline.
---

# JSON Format

`DemoSpline` stores drawings as a top-level `subpaths` array. Each subpath
contains a `closed` flag and a `pts` array.

## Shape

```json
{
  "subpaths": [
    {
      "closed": false,
      "pts": [
        { "x": 0.0, "y": 0.0, "c": 0 },
        { "x": 50.0, "y": 20.0, "c": 1, "t": 0.35 },
        { "x": 100.0, "y": 0.0, "c": 0, "l": 2.8, "r": 0.1 }
      ]
    }
  ]
}
```

## Point fields

- `x`, `y`: point coordinates
- `c`: point kind, where `1` means smooth and `0` means corner
- `t`: shared tangent for smooth points
- `l`, `r`: explicit left and right tangents for corner points

## Notes

- Coordinates are rounded to two decimal places during save.
- Tangent values are rounded to three decimal places.
- Smooth points deserialize `t` into both `LTh` and `RTh`.
- Corner points may omit `l` or `r` independently.

This format is specific to the sample application rather than the reusable
library package.
