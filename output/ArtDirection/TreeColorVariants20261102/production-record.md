# Warm tree shape and color variants — 2026-11-02

Status: complete — three variants and the combined preview produced and accepted by root as the requested art candidates. Baseline `7740ed62acb070bb956b1a415557fd4a7a6af674`. Pre-existing untracked `output/ArtDirection/TreeLeafWindWarm20261102/` is a read-only reference and preserved.

User outcome: trees matching the accepted warm pixel-art style but with distinct silhouettes and non-green foliage, similar foliage-only animation, and a smooth last-to-first loop, suitable for mixed placement.

Scope: three staged art variants — apricot broad canopy, golden tall canopy, rose rounded canopy — plus a combined placement/animation preview. Graphics owns each exclusive variant folder; the apricot worker also owns `preview/`. Root owns this one record. No Unity import, gameplay code, original art changes, new authoring helpers or verification helpers.

Reference: `../TreeLeafWindWarm20261102/source/tree-warm-clean.png` and its beige enlargement. Reuse the corrected native Aseprite helper `../TreeLeafWindWarm20261102/authoring/build-leafwind.lua` unchanged; parameters/masks/timing are graphics-owned. New static art uses built-in imagegen; animation reuses each selected source consistently.

Targets: 112×144 native RGBA canvas, similar logical pixel scale, fixed exposed wood, 12-frame 2.04-second phase-offset foliage loop. Normalize ground anchor consistently where possible and record measured geometry. Distinct silhouettes, branch layouts and warm palettes are required; color swaps of the same tree are insufficient. No detached particles. Inspect 12→1→2 in addition to the full cycle. Combined placement uses the actual delivered sprites, never a separately generated forest image.

Checks: measured dimensions/alpha/timing, actual frame and loop-wrap inspection, exposed wood stability, clear canvas edges, consistent style and differentiated silhouettes. Root independently inspects candidates and evidence. No C# changes; no separate code review or Unity runtime check required for staging-only art.

Shared corrective budget: 2 candidate revisions across all workers; 0 used. Evidence/export-only repairs do not authorize source changes. Each worker submits commands, exact prompt and primary logs to root; this is the single production record.

## Produced variants

| Variant | Shape and palette | Native source bounds | Protected wood | Main files |
|---|---|---|---|---|
| Apricot | Low broad asymmetric umbrella, short forked trunk; cream/apricot/peach/coral foliage | x5–98, y61–129 | 518 pixels | `apricot/apricot.gif`, `apricot/apricot-sheet.png`, `apricot/frames/apricot.aseprite` |
| Gold | Tall narrow crown with staggered broadleaf clusters and exposed branches; honey/gold/cream foliage | x22–89, y5–129 | 964 pixels | `gold/tree-gold-leafwind.gif`, `gold/tree-gold-leafwind-sheet.png`, `gold/frames/tree-gold-leafwind.aseprite` |
| Rose | Medium rounded asymmetrical two-lobed crown and curved fork; blush/dusty rose/mauve foliage | x6–106, y34–129 | 812 pixels | `rose/tree-rose-leafwind.gif`, `rose/tree-rose-leafwind-sheet.png`, `rose/frames/tree-rose-leafwind.aseprite` |

All sources and PNG frames are 112×144 RGBA, with ground anchor (56,130). The different tree heights are intentional silhouette/size variation, not differently scaled display previews. Horizontal sheets are 1344×144, 12 cells without padding, with accompanying JSON. Each variant includes its transparent GIF, a beige 4× GIF, native editable Aseprite animation and PNG frames. Existing green-tree assets are reused read-only in the combined comparison.

Each variant used one built-in imagegen static generation using the prior tree as a style reference, then existing Aseprite/FFmpeg native pixel preparation. The generated source is retained; generation did not directly produce the final low-resolution animation. Native reduction, binary-alpha threshold and limited no-dither palette are recorded as transformations, not proof of manual pixel authorship. Root inspected actual native-scale enlargements against the reference before accepting animation production. Apricot was aspect-preservingly reduced to 96×135 and padded, then aligned (−4,+1); Gold was aligned upward 5 pixels; Rose was padded/aligned downward 16 pixels. These were initial canvas-placement operations, not artistic corrections.

Exact generation prompts: [apricot](apricot/source/prompt.txt), [gold](gold/source/prompt.txt), [rose](rose/source/imagegen-prompt.txt). Native layer polygons, protected wood selections and timings are in each `source/leafwind-config.json`. Apricot additionally uses `source/wood-highlight-mask.png` for wood highlight colors also found in leaves, preserving the full fork without freezing scattered foliage. The existing corrected `build-leafwind.lua` was executed unchanged; no new or modified executable authoring/verification code was needed.

## Animation and performed checks

Every variant uses five phased foliage groups and static exposed wood. Durations are `200,160,160,200,160,160,160,200,160,160,160,160` ms, totaling 2.04 seconds. All three GIFs have 12 distinct poses and infinite looping. Native offsets remain within ±1 pixel. Frame 12 has only the lower-right group at −1 horizontal pixel, frame 1 is neutral, and frame 2 moves the crown +1 pixel; the wrap is an ordinary small step, without a duplicated final-neutral frame or large reset.

Workers inspected their full cycle and 12→1→2 contacts. Root independently inspected all three wrap contacts, Gold/Rose full contacts, and the three static candidates. Root accepted the shared warm pixel treatment, distinct silhouettes and non-green foliage, stable visible wood and lack of obvious horizontal cuts, doubled contours or wrap jumps. These are visual judgments; actual UI playback and Unity runtime were not performed.

Workers used existing FFmpeg/ffprobe/Aseprite readbacks for binary alpha, transparent borders, dimensions/timing and full exposed-wood stability. Root inspected all three `source/rendered-wood.framemd5` logs: each variant's protected wood is identical across all 12 decoded GIF frames. Root independently ran ffprobe on all three native GIFs and obtained 112×144, 12 frames, 2.040000 seconds. Alpha logs contain only 0/255; every canvas edge remains transparent. Raw evidence remains beside the relevant source: `alpha-readback.txt`, `edge-alpha*.txt`, `gif-readback.*`, `rendered-wood.framemd5`; Gold/Rose also retain full-frame and source/frame-01 comparison evidence. Reused helper behavior was not re-tested with new validator code.

Future import proposal only: Sprite/Multiple, 112×144 cells, Point, uncompressed, sRGB, no mipmaps, Clamp, FullRect, alpha transparency and pivot (0.5,14/144); PPU remains undecided. No import settings were applied. Remaining limits: crisp integer-pixel motion rather than continuous deformation; human in-game scale/placement judgment and Unity operation remain unverified. Root art acceptance does not claim user final style approval.

## Mixed placement and final acceptance

- [Animated mixed grove](preview/mixed-grove.gif): 784×520 at 2× native scale, 12 frames, 2.04 seconds. Seven actual delivered tree instances in two staggered rows, with the existing green tree included read-only. Beige background is a simple fill, not generated scenery.
- [Lineup](preview/lineup-3x.png): green reference, short apricot, tall gold, medium rose; native comparison is `preview/lineup.png` at 480×160. Root independently inspected both the lineup and mixed placement for consistent pixel scale, shared rendering style and differentiated shapes.
- `preview/layout.json` records exact positions and cyclic frame phase offsets (0/3/6/9). The preview uses the shared duration schedule, while each standalone animation remains unchanged. Root inspected `preview/mixed-grove-wrap-12-01-02.png` and confirmed no obvious reset or placement jump. Root also independently confirmed the mixed GIF dimensions, 12 frames and 2.04-second duration with ffprobe.
- [Download bundle](preview/tree-variants.zip): exactly 12 files — the three new variants' native GIF, sheet PNG/JSON and editable Aseprite file. Root inspected the archive listing. Large generated/intermediate art and the previous green tree are excluded.

Workers: `/root/tree_graphics` (apricot and mixed previews), `/root/tree_gold_graphics`, `/root/tree_rose_graphics`, each Graphics with separate writable folders. Root owns this single record and independently inspected source candidates, wrap/full-cycle contacts, raw wood-preservation evidence, metadata and final mixed previews. No implementation code changed; the existing Lua authoring helper and its correction were reused as-is. No C# compilation or independent code reviewer was applicable. Final scope inspection shows only the new output folder in addition to the preserved pre-existing untracked green-tree output. No Unity assets, settings or original references changed. Corrections used: **0/2**.
