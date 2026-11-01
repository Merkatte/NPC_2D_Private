# Small pond native-pixel trial — 2026-11-02

Status: **root accepted the bounded 64px material/connectivity trial after revision 1**, pending user judgment of style. This is not final user style approval, full-scene readiness or coverage of other topology. Assignment `pond-pixel-trial-20261102`, Graphics (`create-project-sprites` + `create-terrain-tiles`), baseline `8d6edf5`. Only this output folder is owned. Existing terrain skill/memory edits and earlier ArtDirection folders are preserved. Staging only: no Unity import applies.

## Direction and scope

Atmosphere reference: `../TerrainDualConcept20261102/B-quiet/scene-B-v2.png`. It supplies olive turf, warm earth and quiet teal water, not verified pixel geometry or connectors. No previous flattened B artwork is reused. No imagegen, Python, quantization, image downsampling, randomness, executable helper or new validator is used.

Authoring uses explicit placed clusters in retained ImageMagick MVG draw inputs at a true **64×64** grid, with antialiasing disabled. This scale allows narrow grass blades, layered earth and restrained water marks at distinct logical pixels. It is a candidate rendering style, not a claim of Aseprite or human authorship. Preview enlargement is exact nearest-neighbor 6× only. Palette: olive grass approximately `#7e8d49`, warm ochre earth `#aa7947`, teal water `#185f68`; no tile-wide gradient, stone or flower family.

| ID | Native source / output | Occupancy and profiles |
|---|---|---|
| W0 | `source/water-base.mvg` → `raster/W0.png` | Same-depth water everywhere. All four outermost edges are base teal; low-contrast manually positioned interior marks. Intended self-repeat and future compatible variants, not yet tested. |
| N | W0 then `source/shore-n-overlay.mvg` → `raster/N.png` | North shore: land above, water below. x right/y down. Irregular grass edge around y21–28; layered earth and water lip below. This sample resolves material/style only; left/right profiles are not yet fixed as a functional connector. |

The table above records the initial style sample. Root approved its material style for the next phase: deliberate pixel steps, clustered turf and layered bank improved on the failed flattened B style. The completed inventory and profile contract are below. No concave corners, depth changes or additional terrain families were produced.

## Retained production inputs and commands

Tool: existing `C:/Users/Merkatte/AppData/Local/Temp/terrain-imagemagick-nzh4p2yt/magick.exe`, ImageMagick 7.1.2-35 Q8. `$trial` denotes this folder. Inputs are native draw data rather than executable authoring programs.

```powershell
& $magick -size 64x64 xc:'#185f68' +antialias -draw "@$trial/source/water-base.mvg" "$trial/raster/W0.png"
& $magick "$trial/raster/W0.png" +antialias -draw "@$trial/source/shore-n-overlay.mvg" "$trial/raster/N.png"
& $magick "$trial/raster/W0.png" "$trial/raster/N.png" +append "$trial/style-native.png"
& $magick "$trial/style-native.png" -filter point -resize 600% "$trial/style-6x.png"
& $magick ../TerrainDualConcept20261102/B-quiet/scene-B-v2.png -crop 384x384+1080+160 +repage "$trial/reference-crop.png"
& $magick "$trial/raster/N.png" -filter point -resize 600% "$trial/N-6x.png"
& $magick "$trial/reference-crop.png" "$trial/N-6x.png" +append "$trial/style-reference-comparison.png"
& $magick identify -format '%f %wx%h %[channels] %k colors\n' "$trial/raster/W0.png" "$trial/raster/N.png" "$trial/style-6x.png"
```

Reference crop command above is relative to this ArtDirection folder when run; actual execution used the complete repository-relative reference path. Comparison left = unscaled reference crop; right = candidate N enlarged 6×. Different reference pixel scale is intentional and is not a geometry comparison.

## Checks and budget

Measured: W0 64×64 RGB, 6 colors; N 64×64 RGB, 28 colors; paired enlargement 768×384 RGB, 28 colors. Opaque terrain is intended; transparency is not requested. Native `style-native.png` and 6× preview retain integer pixel clusters. Author visually inspected the delivered 6× preview: crisp grass blades and stepped bank, quiet water; no smoothed contours or broad gradient. The sample has fewer decorative elements than the full atmosphere scene by assigned scope. Actual visual quality remains root judgment, and connectivity/repeat behavior remains NOT_VERIFIED.

## Completed native set and connection contract

Native atlas: `pond-tiles-native.png`, **320×128**, 5×2 cells, **64×64**, no padding. Labels are outside cells in `pond-tiles-labeled-4x.png` (1280×576). Every individual file is under `raster/`.

| ID | Atlas column,row | Occupancy | Joining source strips |
|---|---|---|---|
| W0 | 0,0 | Same-depth water base | All four edges base teal; W0/W1 mix compatible |
| N | 1,0 | Land north, water south | `source/N-left.png` at x0–3; `N-right.png` at x60–63 |
| E | 2,0 | Land east, water west | `source/E-top.png` at y0–3; `E-bottom.png` at y60–63 |
| S | 3,0 | Land south, water north | `source/S-left.png` at x0–3; `S-right.png` at x60–63 |
| W | 4,0 | Land west, water east | `source/W-top.png` at y0–3; `W-bottom.png` at y60–63 |
| W1 | 0,1 | Same-depth water mark variant | W0/W1 share full outer edge colors; different interior mark placement |
| NW | 1,1 | Water southeast; land north/west | Right 4 columns from N-right, bottom 4 rows from W-bottom |
| NE | 2,1 | Water southwest; land north/east | Left 4 columns from N-left, bottom 4 rows from E-bottom |
| SE | 3,1 | Water northwest; land south/east | Left 4 columns from S-left, top 4 rows from E-top |
| SW | 4,1 | Water northeast; land south/west | Right 4 columns from S-right, top 4 rows from W-top |

These are pond outer corners, not concave pond notches. Source strips retain actual complete pixel profiles: grass, turf edge, earth face and lip, submerged band, then water. Crossings are around the middle of each cell but the retained pixels, not a midpoint assertion, are authoritative. N/S connect horizontally; E/W vertically. The water-facing edges meet W0/W1. Outer grass edges are not a separately tested grass family.

Exact-copy evidence: `water-repeat-native.png` (3×2 W0), `water-mix-native.png` (3×2 alternating W0/W1), paired `water-evidence-4x.png`; `pond-native.png` (4×4 / 256×256) and `pond-4x.png` use rows `NW,N,N,NE / W,W0,W1,E / W,W1,W0,E / SW,S,S,SE`.

## Expanded authoring operations

The editable MVG sources and all masks/connector strips are retained under `source/`. `grass-detail.mvg` preserves the initial sample's upright upper-turf marks; `grass-lower-detail.mvg` explicitly places the remaining upright patches and blades. `grass-material.png` is their 64px render. `north-bank-geometry.mvg` and `corner-nw-geometry.mvg` are the initial native bank drawings. Only these **geometry/material layers** were rotated for initial orientation construction: 90° E/NE, 180° S/SE, 270° W/SW. Finished tiles were not rotated or resized to pack. Lighting is deliberately diffuse/overhead, with consistent material layers rather than a sunlit rim. Upright grass and horizontal water marks were composited separately after orientation, preventing sideways grass or vertical ripples.

```powershell
# $trial is the complete output directory; $magick is the tool path above.
& $magick -size 64x64 xc:'#7e8d49' +antialias -draw "@$trial/source/grass-detail.mvg" -draw "@$trial/source/grass-lower-detail.mvg" "$trial/source/grass-material.png"
# Draw native bank geometry to an opaque 64px image. Rotate initial geometry only.
& $magick -size 64x64 xc:'#185f68' +antialias -draw "@$trial/source/revision1-north-bank-geometry.mvg" "$trial/source/N-r1-geometry.png"
# For each oriented geometry, select only its flat grass/water area by exact color:
& $magick $geometry -fill white -opaque '#7e8d49' -fill black +opaque white $grassMask
& $magick "$trial/source/grass-material.png" $grassMask -alpha off -compose CopyOpacity -composite $grassOverlay
& $magick $geometry -fill white -opaque '#185f68' -fill black +opaque white $waterMask
& $magick "$trial/raster/W0.png" $waterMask -alpha off -compose CopyOpacity -composite $waterOverlay
& $magick $geometry $grassOverlay -compose over -composite $waterOverlay -compose over -composite $tileOutput
# Restore the corresponding retained four-pixel strips at exact coordinates.
& $magick $tileOutput $leftStrip -geometry +0+0 -compose over -composite $rightStrip -geometry +60+0 -compose over -composite $tileOutput
# E/W use top/bottom strips at +0+0 and +0+60. Corners use the table above.
# Assemble input files in the documented order, with no resampling:
& $magick montage $orderedTilePaths -tile 5x2 -geometry +0+0 "$trial/pond-tiles-native.png"
```

For initial profile construction N/S's first 4 columns were copied into the last 4 columns, then the outermost column was copied from x0 to x63. E/W used the analogous rows. Both actual edge strips were then extracted. Corners inherited the corresponding strips from the table. Revision 1 restores both ends from those same saved strips. Initial individual straights remain in `raster/revision0/`; initial geometry sources are retained. `style-sample-N.png` is the first pre-functional style sample.

## Final checks, judgments and corrective budget

Existing ImageMagick readback only, no new checker/helper: [pixel-readback.txt](pixel-readback.txt) records **24/24 assembled adjacent edge comparisons AE=0**, **8/8 preserved straight strips AE=0**, and **10/10 atlas-cell/source comparisons AE=0**. This proves those exact pixel comparisons, not aesthetic quality. Commands use `-crop`, `-write mpr:a +delete`, the corresponding second image/crop, and `mpr:a -metric AE -compare -format '%[distortion]' info:`. Horizontal joins compare 1×64 at x63 and x0; vertical joins compare 64×1 at y63 and y0. Atlas comparisons use the table's 64×64 rectangles against complete source tiles.

Final measured properties: all ten tiles 64×64 RGB, fully opaque; W0/W1 6 colors each, N/E/W 25, S 23, NW 21, NE 23, SE 24, SW 22. Atlas 320×128 RGB, fully opaque; pond 256×256 RGB, fully opaque. Author inspected final native pond and labeled 4× preview; upright foliage, crisp stair steps, layered warm bank and low-contrast horizontal water marks remain. No resampling noise/soft contour was observed. Labels are presentation-only.

Root judgments: water evidence shows no obvious cell boundary/halo; short mark rhythm remains visible but acceptable for this test. First pond retained crisp material quality and coherent corners but failed natural-shore quality because similarly sized humps formed periodic decorative trim. **Revision 1** replaced straight interior contours with one broader asymmetric meander and a smaller notch while preserving the first/last 4px, palette, corner pixels and both water tiles. Before images: `pond-before-revision1-native.png` / `pond-before-revision1-4x.png`. Root inspected revised pond and accepted the bounded test: smaller regular scallop rhythm reduced, layered bank and grass preserved. The broader shape and turf still visibly repeat per tile; this remains a limitation, not a claim of no repetition. Water evidence was reused because both water source tiles were unchanged.

Compared with the atmosphere reference, this test preserves its olive/ochre/teal treatment and layered shore with deliberate native clusters, but omits reeds, rocks, lilies, flowers and full-scene composition by scope. It does not establish their art quality or future topology. No Unity import/runtime or gameplay validation applies; no user final style approval has been recorded.

Shared corrective revision budget: maximum **2**, **used 1** (periodic shore scallops). Root instructed no further revision or expansion after accepting this bounded trial. Root owns persistent production-memory updates. This record is the single task source/inspection record; images and raw comparison output support it.
