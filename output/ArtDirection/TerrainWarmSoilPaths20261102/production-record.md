# Warm soil fields and paths — 2026-11-02

Assignment `warm-soil-paths-20261102`, Graphics. Baseline `8d6edf5`; own only this new output folder. Existing terrain outputs/dirty policies are preserved. Native 64px opaque tiles; no Unity import. New task corrective budget 2 total, used 0 during initial production.

User explicitly requests **multiple terrain tiles forming a wide path**, not a whole road inside one tile. Deliver full soil interiors, one-sided grass/soil edges, convex soil corners and concave soil corners. No road T/cross/turn/end-cap sprites. The scene must expose one or more complete interior cells between opposite boundary tiles, including wider stretches and a clearing.

Style reference: actual `../TerrainWarmHabitat20261102/scene-2x.png`, and its G0/G2/GF/GR. Those four complete tiles are copied pixel-identically into `raster/`; source images remain unchanged. Warm subdued 64px pixel treatment follows that family. New soil base `#ceb383`, compressed-earth patches `#c9ad7e`, highlights `#d3b98a`, small grain `#c1a476/#dbc293`. Grass/soil border uses only `#b8ae78/#9fa76e` around original grass `#a5ae73`: a flat narrow transition, no water lip or cliff shadow.

Initial material candidate: `style-native.png` / `style-6x.png` (D0, D1, N); exact base-repeat and mixed 3×2 mosaics in `soil-repeat-native.png`, `soil-mix-native.png`, paired `soil-evidence-4x.png`. Author inspected native logical clusters and enlarged repeat: no tile-local gradient/rim; sparse mark rhythm remains observable.

Initial [pixel-readback.txt](pixel-readback.txt): 8 D0/D1 ordered horizontal/vertical comparisons, 4 copied-ground full-image comparisons, N self horizontal join and G0-to-N grass join, all AE=0 (14 checks). This is only initial material/profile evidence; corner coverage is pending.

## Source operations and contract

Editable sources are ImageMagick MVG in `source/`: `soil-base.mvg`, `soil-wear.mvg`, `edge-n-geometry.mvg`. Existing tool `C:/Users/Merkatte/AppData/Local/Temp/terrain-imagemagick-nzh4p2yt/magick.exe` (7.1.2-35 Q8), exact native drawing with `+antialias -draw @source`. No imagegen, Python editing, quantization, new helper/validator or high-resolution generated atlas.

```powershell
# $soil is this folder; $magick is the binary above.
& $magick -size 64x64 xc:'#ceb383' +antialias -draw "@$soil/source/soil-base.mvg" "$soil/raster/D0.png"
& $magick -size 64x64 xc:'#ceb383' +antialias -draw "@$soil/source/soil-wear.mvg" "$soil/raster/D1.png"
& $magick -size 64x64 xc:'#ceb383' +antialias -draw "@$soil/source/edge-n-geometry.mvg" "$soil/source/N-geometry.png"
# Exact-color area masks permit native G0/D0 pixels to fill their respective areas.
& $magick $geometry -fill white -opaque '#a5ae73' -fill black +opaque white $grassMask
& $magick "$soil/raster/G0.png" $grassMask -alpha off -compose CopyOpacity -composite $grassOverlay
& $magick $geometry -fill white -opaque '#ceb383' -fill black +opaque white $soilMask
& $magick "$soil/raster/D0.png" $soilMask -alpha off -compose CopyOpacity -composite $soilOverlay
& $magick $geometry $grassOverlay -compose over -composite $soilOverlay -compose over -composite $tile
```

x increases right, y down. N/E/S/W name the **grass side**, with soil opposite; each is a single material boundary. Full D0/D1 occupy soil everywhere. Ground fills retain actual opposite-edge profiles, not a broad blank border. D0/D1 soil outermost edges are the same base material color. New soil corners must inherit both corresponding straight source profiles; corner meanings/coordinates and completed 20-tile inventory will be recorded after style inspection and minimal assembly.

Root inspected the style strip and soil repeat/mix, accepting warm flat dirt and the grass transition for expansion. Recognizable small repeating marks remain an explicit limited-variant caveat. The four convex and four concave corners were then authored, tested in the closed region below, and inspected by root before the larger scene.

## Final inventory and exact profiles

All individual tiles are `raster/<ID>.png`, native 64×64 opaque terrain. Atlas [tiles-native.png](tiles-native.png) is **320×256**, 5×4 cells with no padding; `source/atlas-order.txt` stores cell order. [tiles-labeled-4x.png](tiles-labeled-4x.png) is 1360×1236: each image cell is exactly 256×256, with labels/margins outside the cells.

| Atlas row | IDs left to right | Contents |
|---|---|---|
| 0 | D0, D1, D2, D3, G0 | Full soil, alternate soil marks, baked small pebbles, baked leaves, copied meadow |
| 1 | N, E, S, W, G2 | One-sided grass/soil edges; copied quiet meadow |
| 2 | CNW, CNE, CSE, CSW, GF | Four convex soil corners; copied flowers on complete ground |
| 3 | INW, INE, ISE, ISW, GR | Four concave soil corners; copied rocks on complete ground |

N means grass north/soil south; E grass east/soil west; S grass south/soil north; W grass west/soil east. These tiles contain one boundary, never the entire width of a road. Their actual four-pixel end strips are retained as `source/N-left.png`, `N-right.png`, `S-left.png`, `S-right.png`, `E-top.png`, `E-bottom.png`, `W-top.png`, `W-bottom.png`. N/S strips cover x0–3 or x60–63, E/W y0–3 or y60–63; each strip spans the entire other dimension. Straight endpoints were already matching in the authored native geometry. Corners receive both full source strips by exact copying.

| Corner | Material occupancy | First matching strip | Second matching strip |
|---|---|---|---|
| CNW | Soil southeast quarter; grass around north/west | Right = N-right | Bottom = W-bottom |
| CNE | Soil southwest quarter; grass around north/east | Left = N-left | Bottom = E-bottom |
| CSE | Soil northwest quarter; grass around south/east | Left = S-left | Top = E-top |
| CSW | Soil northeast quarter; grass around south/west | Right = S-right | Top = W-top |
| INW | Grass southeast quarter; soil everywhere else | Right = S-right | Bottom = E-bottom |
| INE | Grass southwest quarter; soil everywhere else | Left = S-left | Bottom = W-bottom |
| ISE | Grass northwest quarter; soil everywhere else | Left = N-left | Top = W-top |
| ISW | Grass northeast quarter; soil everywhere else | Right = N-right | Top = E-top |

C is convex **soil**; I is concave **soil**. The occupancy table is authoritative rather than assuming an atlas naming convention. Native outer/inner geometry sources are `convex-nw-geometry.mvg` and `concave-nw-geometry.mvg`. Geometry layers alone turn 90/180/270° for the other orientations; unchanged G0 and D0 are separately composited through the appropriate native masks so material marks remain upright. The corners inherit the strips above after material composition. D2 uses `soil-pebbles.mvg` over D0; D3 uses `soil-leaves.mvg` over D1. Decorations are complete baked terrain, not placement assets.

## Actual compositions

[minimal-2x.png](minimal-2x.png) / `minimal-native.png`: 8×8 cells, a closed soil region surrounding a grass opening. `source/minimal-layout.txt` contains all IDs. This provides every convex/concave orientation and both intended straight joins, not a separately generated landscape. Root inspected and accepted its continuous flat field treatment before scene assembly.

[scene-2x.png](scene-2x.png) / `scene-native.png`: **16×10 tiles**, native **1024×640**, exact nearest 2× preview **2048×1280**. `source/scene-layout.txt` retains all placement decisions. A broad horizontal approach turns/branches north and opens into a wider clearing at right, with sparse baked pebbles/leaves and unchanged meadow/flower/rock context. The shape is assembled entirely from the terrain inventory.

Width evidence, using zero-based cell coordinates:

- Horizontal approach: full soil rows **4 and 5** between N at row 3 and S at row 6 (e.g. columns 0–3).
- North branch: full soil columns **5 and 6** between W at column 4 and E at column 7 (rows 0–2).
- Clearing: full soil columns **10–13**, four complete interior cells across, in rows 3–7, surrounded by independent boundary/corner pieces.

[multitile-width-explained.png](multitile-width-explained.png) is an optional presentation crop of the actual scene's cells x0–3/y3–6, enlarged 4× with a thin grid and row labels. It demonstrates two separate full-interior rows between two boundary rows. Grid/labels appear only in this explanatory image; clean scene and tile pixels are unchanged.

```powershell
# Native composition and preservation-only packing, using retained layout/order lists:
& $magick montage $minimalFiles -tile 8x8 -geometry +0+0 "$soil/minimal-native.png"
& $magick montage $sceneFiles -tile 16x10 -geometry +0+0 "$soil/scene-native.png"
& $magick "$soil/scene-native.png" -filter point -resize 200% "$soil/scene-2x.png"
& $magick montage $atlasFiles -tile 5x4 -geometry +0+0 "$soil/tiles-native.png"
& $magick montage -filter point -font Arial -pointsize 18 -fill '#f7ead1' -background '#596452' -label '%t' $atlasFiles -tile 5x4 -geometry 256x256+8+14 "$soil/tiles-labeled-4x.png"
# Example actual corner strip placement:
& $magick "$soil/raster/INW.png" "$soil/source/S-right.png" -geometry +60+0 -compose over -composite "$soil/source/E-bottom.png" -geometry +0+60 -compose over -composite "$soil/raster/INW.png"
```

## Checked coverage, judgment and limits

All executed comparisons in [pixel-readback.txt](pixel-readback.txt) returned **AE=0**:

- Initial 14: D0/D1 self/mixes in both axes (8), copied grass entire images (4), N self join and G0–N (2).
- Minimal assembly: **112 adjacent edges**, covering all 4 convex and all 4 concave corners on both joins, straight runs and adjacent fills.
- Final scene: **53 new ordered adjacency classes** absent from the minimal assembly; unchanged prior classes reused rather than rerun. Together these cover the final scene's actual grass/soil/fill transitions and junctions.
- New baked soil variants: **8 edge-preservation comparisons** against their D0/D1 base.
- Atlas: **20 full 64×64 cell comparisons** against the delivered individual tiles, confirming unchanged packing.

Checks use existing ImageMagick cropping and comparison only: horizontal `1x64+63+0` versus `1x64+0+0`; vertical `64x1+0+63` versus `64x1+0+0`; corresponding edges for variant preservation; atlas rectangle at `(column*64,row*64)` versus the complete original. Each uses `-write mpr:a +delete`, the second image/crop and `mpr:a -metric AE -compare -format '%[distortion]' info:`. Scene dimensions and actual opacity were confirmed with `identify`. No executable validator/harness was created. All copied WarmHabitat grass images are unchanged; their unrelated water checks were not rerun.

Author inspected native and integer enlargements: clean stepped pixel boundaries, subdued grain, flat-level grass/soil contact, continuous broad interiors and restrained ecological decoration. **Small soil marks and the reused flower/rock motifs still repeat.** This is a bounded concept set, not a claim of invisible repetition, every possible autotile combination, Unity behavior, collision/navigation rules or complete world art.

Root inspected the actual final scene and labeled sheet, accepting requested concept delivery: coherent WarmHabitat palette, continuous soil field, two full branch columns, two full horizontal approach rows, wider clearing and visually coherent convex/concave joins. This is root visual acceptance for the requested scene/sheet, not user approval of a new final style or certification for all maps.

Corrective revisions **0 of 2**. No artwork corrections or further scene changes were requested. Only this task's new output folder was authored; prior outputs and existing policy changes remain preserved. Staging only: importSpec/Unity runtime not applicable.
