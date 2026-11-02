# Native Aseprite leaf-wind authoring

Run from the repository root after the graphics author creates an RGBA source, JSON config and output directory:

```sh
'/Users/minbh/Library/Application Support/Steam/steamapps/common/Aseprite/Aseprite.app/Contents/MacOS/aseprite' -b --script-param input='SOURCE.png' --script-param outdir='OUTPUT_DIRECTORY' --script-param config='CONFIG.json' --script output/ArtDirection/TreeLeafWindWarm20261102/authoring/build-leafwind.lua
```

JSON shape (example values only; graphics owns the actual polygons and colors):

```json
{
  "name": "tree-leafwind",
  "base_inset": 1,
  "wood_colors": [[100, 70, 40]],
  "lobes": [{"name": "crown", "polygon": [[20, 5], [80, 5], [95, 65], [10, 65]]}],
  "frames": [
    {"duration": 0.12, "offsets": {"crown": [0, 0]}},
    {"duration": 0.12, "offsets": {"crown": [1, 0]}}
  ]
}
```

Optional `wood_mask` is a same-canvas RGBA PNG: nonzero alpha protects that coordinate. A path is resolved from the working directory first, then relative to the config. RGB selections and mask combine as a union. At least one selection must be supplied and match visible source pixels. Include all visible trunk, forks and roots. Colors match exact RGB regardless of alpha; do not select colors shared with moving leaves unless those leaves should remain fixed.

Coordinates are zero-based; polygons test pixel centers. Lobes can overlap and later array entries render above earlier ones. Source wood pixels are excluded from every lobe; protected destination coordinates reject moving pixels. The topmost fixed wood layer restores original exposed wood exactly. `base_inset` defaults to 1: each pass erodes the mapped foliage alpha silhouette by four neighbors. Interior source pixels and all unmapped foliage remain fixed under the moving lobes. This prevents retaining the complete stationary outer silhouette while retaining interior coverage. Moving silhouettes must be covered by polygons. Configured offsets must fit the canvas; cropping raises an error before export.

Durations are seconds. Offsets are integer native pixels, omitted names default to `[0,0]`. Source dimensions, RGBA values and pixel scale are preserved. Output is `NAME.aseprite` with fixed base, named foliage and fixed wood layers, plus `NAME-frame-01.png` and subsequent frames. Existing names are overwritten; use a fresh output directory for a new candidate. GIF and sheet exports are separate standard Aseprite CLI operations by the graphics author.

Uses the documented [Image](https://aseprite.org/api/image), [Sprite](https://aseprite.org/api/sprite), [Frame](https://aseprite.org/api/frame), and [JSON](https://aseprite.org/api/json) APIs. No Unity or C# operations are involved.
