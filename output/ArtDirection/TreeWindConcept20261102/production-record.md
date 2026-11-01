# Tree wind concept — 2026-11-02

Assignment `tree-wind-concept-20261102`, Graphics. Baseline `8d6edf5`; only this new folder authored. One tree, four animation frames, transparent background. Reference direction: warm soft pixel environment in `../TerrainWarmHabitat20261102/`, especially its shrub palette. Staging only: no Unity/import/code changes. Corrective frame revisions **0/2**.

## Selected source and actual production

The initial native MVG sketch (`source/tree-base.mvg`, `source/tree-base.png`) is retained as an unselected draft. Root corrected the initial overbroad native-production instruction and selected one built-in **imagegen** generation. Its untouched RGBA result is `source/tree-generated-base.png`, 1086×1448, with soft/generated edges. It was not treated as a finished low-resolution sprite.

Root then explicitly requested a production derivative: point sampling to 96×128, alpha threshold 50%, no-dither maximum 32 colors, centered at (+8,+8) on a 112×144 transparent canvas. `source/tree-pixel-96x128.png` and **`source/tree-pixel-base.png`** retain these results. The latter has 28 colors and binary alpha. Root inspected its actual native/5× appearance and approved it as this concept's source. The transformation alone was not claimed to prove pixel-art quality. No separate frame was generated; all frames reuse that exact selected PNG.

Built-in generation prompt (one call; image 1 = native sketch, image 2 = WarmHabitat GB):

> Use case: sketch-to-render. Create ONE single static tree sprite by refining image1, preserving its sturdy warm trunk, fixed root location, three main visible branch forks and single-tree identity. Image2 is warm soft pixel-art palette/material reference only, not a target to reproduce. Improve the crown: asymmetrical rounded deciduous canopy with overlapping leaf-cluster volumes, readable soft shade pockets and warm yellow-green highlights; avoid a flat oval with decorative spots. Keep restrained warm greens and ochre bark, cozy small-game environment aesthetic, no extreme dark outline. Critical medium: deliberate crisp low-resolution pixel art with consistent logical pixel steps and clustered shading, not a smooth illustration or blurry painterly texture. Ideally retain the native 96x128 sprite-grid scale of image1; no resampling noise or antialiased edges. Isolated full tree, genuine fully transparent background, generous clear margin on every side including roots, no grass platform or ground shadow. Tree and roots centered, same view as image1. Only ONE tree and ONE pose, no sheet/animation, no fruit, flowers, detached leaves, particles, extra branches, text or frame. This single finished sprite will later be reused identically for every animation frame; do not invent multiple views.

Existing ImageMagick 7.1.2-35 Q8 at `C:/Users/Merkatte/AppData/Local/Temp/terrain-imagemagick-nzh4p2yt/magick.exe` performed the derivative and exact crops/translations/packing. No Python, executable helper, custom Aseprite encoder or installation was used.

```powershell
# $tree is this directory; $magick is the existing tool above.
& $magick "$tree/source/tree-generated-base.png" -filter point -resize '96x128!' -channel A -threshold '50%' +channel +dither -colors 32 "$tree/source/tree-pixel-96x128.png"
& $magick -size 112x144 xc:none "$tree/source/tree-pixel-96x128.png" -geometry +8+8 -compose over -composite "$tree/source/tree-pixel-base.png"
# Retained exact strips: 112x72+0+0, 112x31+0+72, 112x41+0+103.
# Frame02 composites these at +2+0, +1+72, +0+103; frame04 at -2+0, -1+72, +0+103.
# Frame01 and frame03 are identical copies of the selected source.
& $magick $frames +append "$tree/tree-sheet-1x4.png"
& $magick -delay 20 -dispose Background $frames -loop 0 "$tree/tree-wind.gif"
& $magick -delay 20 -dispose Background $frames -background '#d8d2ba' -alpha remove -alpha off -filter point -resize 400% -loop 0 "$tree/tree-wind-preview-4x.gif"
```

## Frames, timing and anchor

`tree-frame01.png` through `tree-frame04.png` are **112×144 RGBA**. Order: neutral → right → neutral → left → neutral. **01=03 is intentional**, not four distinct tree designs. Every frame lasts **200ms**, total **800ms**, infinitely looping.

| Source rows (y down) | Frame01 | Frame02 | Frame03 | Frame04 |
|---|---:|---:|---:|---:|
| 0–71, upper crown | 0 px | +2 px | 0 px | −2 px |
| 72–102, lower crown/upper trunk | 0 px | +1 px | 0 px | −1 px |
| 103–143, lower trunk/roots | 0 px | 0 px | 0 px | 0 px |

No vertical shift. At every transition, including **04→01**, the same bands move only 2/1/0 pixels. Root ground anchor is fixed at image coordinate **(56,127)**, immediately below the lowest opaque row 126; this corresponds to normalized bottom-origin pivot **(0.5,17/144)** on the padded canvas. A literal canvas-bottom-center anchor would include the same 17px bottom padding in every frame.

Deliverables: raw horizontal sheet `tree-sheet-1x4.png` (**448×144**, four columns/one row, no gaps); `tree-contact-4x.png` (neutral backdrop and labels); `loop-wrap-4x.png` (04→01→02); transparent native `tree-wind.gif`; quiet-backdrop `tree-wind-preview-4x.gif` (**448×576**, four frames).

## Actual verification and review

[pixel-readback.txt](pixel-readback.txt) records equal dimensions, **28 colors**, alpha **0/1 only**, and exactly **5,592 opaque pixels** in every frame. All four canvas-edge alpha maxima are zero for all frames (no clipping). Fixed lower-trunk/root crops match the selected source exactly. Inverse translation of both moving strips on frames 02/04 returns the original source pixels with **AE=0**; no branch/foliage drawing is regenerated or dropped. 01/03 equality, all four sheet cells and all four decoded GIF frames also return **AE=0**.

Native GIF readback: exactly four full 112×144 frames, delay 20 centiseconds each, disposal Background, iterations 0 (infinite). Enlarged preview likewise has four 448×576 frames, 200ms each, infinite. Author inspected actual native/5× source, four-frame contact and 04→01→02 contact; root independently inspected those static contact/wrap images and accepted the concept's identity, small movement and fixed base. **Actual Aseprite playback was not performed.** This is a concept review, not user final art approval or a full skeletal wind simulation.

## Aseprite handoff

Aseprite was not found on PATH or the checked standard standalone/Steam/local installation paths. No native `.aseprite` file is claimed. To play/edit: **File → Open → `tree-frame01.png`**, answer Yes to load the ordered image sequence, select all four frames and set duration to **200ms**, then play the four-frame loop. This sequence-open workflow is documented in [Aseprite Open](https://www.aseprite.org/docs/open/).

Alternatively use **File → Import Sprite Sheet**, choose `tree-sheet-1x4.png`, horizontal order, frame size **112×144**, offset **0,0**, no padding, then set 200ms per frame. See [Aseprite Sprite Sheets](https://www.aseprite.org/docs/sprite-sheet/). The supplied GIF provides an immediate timed preview; PNG sequence/sheet remain the lossless editing handoff.
