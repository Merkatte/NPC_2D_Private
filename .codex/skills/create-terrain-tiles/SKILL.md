---
name: create-terrain-tiles
description: Create or edit NPC_Work_2D pixel-art terrain tiles and tilesheets with explicit tile inventory, compatible edges, approved pixel style and preserved atlas assembly. Use for ground, shore, corner and interior variants; not UI art, Unity Tilemap wiring or navigation rules.
---

# Create Terrain Tiles

Specialize the existing Graphics role. Use [create-project-sprites](../create-project-sprites/SKILL.md) once for assignment, write boundaries, reference/import handling, ordinary checks, shared correction budget and reporting. Apply this skill for terrain-specific decisions; do not create another worker just because both skills apply. Root may keep roleSkill `create-project-sprites` and include `create-terrain-tiles` as the specialization in requiredContext.

If invoked without a scoped assignment, root uses [orchestrate-unity-work](../orchestrate-unity-work/SKILL.md) to establish it and delegate the graphics role. This skill creates the artwork; Unity import, Tile assets, RuleTile configuration and scene painting belong to assembly. Do not modify navigation/game rules or acquire final acceptance authority.

## 1. Resolve the Tile Contract

Before production, use [tile-contract.md](references/tile-contract.md) to record the needed inventory, directions, connectors, permitted variation and output layout in the existing assignment/task record. A concise table is enough; no separate plan, serialized schema or per-tile evidence document is required.

Consult the short [production memory](references/production-memory.md) before selecting a reusable terrain sample or repeating a production method. Open only the matching record/artifacts, distinguish proposals and failed trials from accepted samples, and check that the recorded scope/style still fits the request. At handoff, update that memory with the outcome and links to the existing task record/source/evidence; keep detailed prompts, tile coordinates and commands in that single task record rather than duplicating them. Unchanged, applicable evidence may be reused; changed art or connectors invalidate only their affected checks.

Inspect the actual approved reference images and relevant existing source/sheet. Reuse established decisions; ask root only about unresolved material choices. Explicit user values override reference defaults. Do not universally prescribe 32/64-pixel cells, midpoint connectors, an 8-pixel protected strip, a fixed palette count or a 16/47-tile family.

Distinguish a visual concept request from a functional tile deliverable. A concept need not cover an unrequested complete tileset, but must not be described as ready-to-connect production tiles. Even when connection checks are deferred for a concept, identify visible seams or per-cell shading/repetition defects as correction targets; do not approve affected tiles as a production reference. For a functional request, missing requested interiors/corners/variants and incompatible connectors are failures, not optional polish.

## 2. Preserve Structure While Adding Variation

Map each requested tile/variant to a stable ID and sheet cell. Define corner material occupancy as well as direction, using the contract's coordinate convention.

For every requested boundary family (for example shore, grass-to-dirt, path-to-ground or cliff), establish the shared edge profiles before authoring connecting corners. For compatible tiles, preserve material arrangement, crossing positions, contour, border/lip/shadow width, color and alpha where relevant. Each corner must inherit the profiles of its intended straight neighbors on both joining sides. Exact midpoint crossings alone do not guarantee compatibility. Variation may change interior contours and decoration without breaking these agreed connections.

Continuity applies to interior fills, including water, dirt and grass, as well as borders and corners. Define which fills repeat with themselves, which variants may mix and which materials/depths require transition tiles; universal all-to-all compatibility is not required. Avoid tile-local shading that resets at every cell or produces an unintended grid of bright centers/dark rims even when edge colors match. Express depth, moisture and wear across the intended terrain area using compatible fills and transitions. Gradual changes across tiles are valid when they preserve continuity and the approved pixel style.

“More variety” defaults to decorative/texture variation within the same connectivity class. Do not introduce diagonal connections, new topology, different material coverage or extra transition families without a request. Diagonal visual contours inside an approved corner are not themselves a new diagonal connection.

When flowers, stones or grass are requested inside a tile, deliver composed terrain tiles. Separate decoration layers may be used internally; a loose decoration sheet or “place these yourself” instruction is not an equivalent deliverable unless the user requested overlays.

## 3. Prove a Minimal Set Before Expanding

For functional tiles or a continuity trial, establish an available production method before generating a full sheet. Confirm that the chosen native pixel editor can edit exact logical pixels, retain/reuse connector pixels and assemble unchanged tiles; a crop/packing tool alone does not establish a repair capability. Demonstrate the method through the small actual deliverable below, not a new verification helper. If precise repair is unavailable, report that capability gap before promising functional completion. Explicitly requested visual concepts may still explore appearance, but cannot substitute for this trial or imply working connections.

Use this progression, reusing approved art and still-valid evidence where available:

1. **Style sample:** resolve palette, material treatment and logical pixel scale on a small sample or existing approved reference.
2. **Interior:** make one base fill for the selected family and check actual horizontal/vertical repetition under section 5. Correct seams and cell-local shading before building on it.
3. **Straight boundaries:** establish the required straight tiles against those fills, fix their connector profiles in actual source pixels and check straight runs. Keep stable tile IDs and source edge coordinates for reuse; prompt descriptions alone do not fix a connector.
4. **Corners:** inherit the matching straight profiles on both joining sides. Directly edit failed endpoints/contours in the native pixel editor while retaining the chosen style; check both joins for each required orientation.
5. **Minimal assembly:** use these exact tiles to build a small region or turning route covering the selected family's required connections. Interior, straight, corner and pixel-style checks must all pass together.
6. **Expansion:** only then add requested decorative variants, further families and the full atlas. Preserve established connectors, check variant mixes and validate each added family's actual joins before treating it as compatible.

Choose the smallest meaningful subset of the requested work, not a universal tile count. An isolated repair or variant request reuses existing passing neighbors rather than rebuilding an entire family. Failure in the minimal assembly stops dependent expansion; it calls for local repair under the shared budget, not more full-sheet generations or a separately generated landscape.

Use the available imagegen skill/tool for style exploration, new art candidates and generative edits, inspecting inputs and following its transparency/provider rules. Generated artwork is a candidate until actual tiles satisfy the contract. Generation does not replace precise connector editing, and repeated prompt assertions are not geometric evidence.

The user-approved Terrain workflow also permits existing native pixel-editor/Aseprite operations for exact pixel editing, connector repair and atlas composition. Preserve approved tiles when packing or editing bounded regions. Packing is coordinate placement of the selected pixels, not a new image-generation request. Do not redraw, rescale, recolor, rotate or mirror approved tiles merely to make them fit the sheet. A requested recolor/resize is a separate explicit transformation; inspect its consequences rather than treating it as packing.

Choose available existing tools for the operation. Do not silently replace a missing native editing capability with whole-sheet regeneration, Python image editing, new API tooling or a simplified procedural art style. New executable authoring helpers, if actually needed, require root coordination under the project's code-role rules; never create verification-only tools as a workaround.

Preserve original/editable files and assigned ownership. Any allowed connector adjustment must retain the approved visual treatment. Masks/templates may organize geometry but do not justify flat bands or mechanical detail replacing the requested artwork. Do not assume geometric rotation preserves directional lighting or asymmetric material details.

## 4. Enforce the Pixel-Art Quality Requirement

For requested pixel art, match the approved pixel-authored appearance, not merely a “pixel style” label:

- Work against a resolved logical pixel grid and intended on-screen scale. Final contours, marks and shading clusters must read as intentional decisions on that grid.
- Preserve deliberate stair steps, line weight, material shading and readable clusters. Reject unintended blur, smoothing, continuous painterly gradients, photo texture and scattered resampling noise.
- Match reference detail density and grass/flower/stone scale. Crisp but randomly fragmented pixels, or drastically flattened artwork, do not satisfy the requirement.
- Do not treat shrinking a high-resolution illustration, nearest-neighbor resampling, palette quantization or saving as .aseprite as proof of pixel-art quality.
- Inspect the final deliverable at native size and an integer nearest-neighbor enlargement. A fixed color count or tool filename is not acceptance evidence. Do not claim manual pixel authorship unless it actually occurred.

Composition, connectivity and approved pixel-art quality are simultaneous requirements. Do not trade away one to pass another; unresolved tradeoffs go to root/user rather than an unapproved simplification.

## 5. Check Actual Tiles and Their Connections

Use existing tools and source-image inspection within the ordinary verification limits:

1. **Inventory:** match each required ID, orientation and variant to actual content. Equal-sized cells or similar-looking corners do not prove the requested set exists. Interior fill variants have their own inventory entries.
2. **Preservation/layout:** check final cell size, placement, padding and content against approved source tiles. Where preservation was required, copying into an atlas must not alter those pixels; confirm with available comparison/readback, not just the packer's exit code.
3. **Interior repetition:** assemble compact mosaics from the exact delivered tiles. Check repeatable interior fills tiled both horizontally and vertically, then mix the base and variants within declared compatible families. Inspect both edge discontinuities and conspicuous grid-shaped repetition of shading/marks; matching edge colors alone is insufficient.
4. **Boundary assembly:** for each requested boundary family, check straight-to-straight joins in its requested directions and every requested convex/concave corner orientation against its intended straight neighbors on both joining sides. Include required material/depth transitions. Assemble a small closed region or turning route appropriate to that family from the exact delivered tiles; use additional compact join strips for required connections absent from that assembly. Compare the shared profiles from section 2 at the actual joins. A successful direction or family does not establish the others. Cover requested families/directions and affected joins, not every possible map or unrelated tile combination.
5. **Pixel style:** inspect those same assembled candidates at native/integer scale for visual treatment, seams, repetitions and unintended artifacts. Compare with approved references; do not use a separately generated landscape as connection evidence.
6. **Handoff:** retain the common measured-file/alpha and proposed-import checks. In the existing WorkerReport, distinguish interior repetition, straight joins and corner joins by tested family/direction, identifying failures and untested combinations. Partial success must not be reported as whole-set continuity acceptance; these assemblies are not an exhaustive arbitrary-map or Unity runtime certification.

An exact tile-copy mosaic is part of terrain authoring and inspection, not automatic Play Mode or game-screen QA. It does not authorize new test harnesses or scene execution. For an isolated tile request, use existing approved neighbors in the demonstration where needed; do not manufacture an entire unrequested tileset.

Known missing tiles, bad joins, unintended seams/grid-shaped shading in repeat or mix previews, or failed required style remain failures. Mark the affected IDs and combinations for correction rather than treating an observed defect as merely unverified. If a required check cannot be performed, identify the gap and do not claim a production-ready complete set. Root retains independent visual acceptance under create-project-sprites.

## 6. Correct Locally and Report

Preserve satisfactory tiles and approved originals. Correct failed IDs and the directly affected connections only; missing center variants call for those variants, not a new whole atlas. For connector failures, directly repair the actual source pixels against the established straight profiles rather than repeatedly asking the generator for a new sheet. Needed edits outside assigned ownership are reported to root before writing. Recheck changed content, affected joins, style and resulting atlas placement; reuse unaffected evidence. Resume dependent expansion only after the affected minimal assembly passes.

Use the existing task-wide correction budget, shared across workers and failure types. Do not start a new per-tile/style/generation allowance. When the budget or repeated-cause stopping rule is reached, report completed items, failed/missing IDs, actual evidence and the concrete remaining choice/capability gap. Do not silently lower quality or keep regenerating.

Return one ordinary graphics WorkerReport: selected output/source paths, tile ID/layout mapping, references, generation/edit method and prompt location when used, proposed importSpec, observed inventory/connection/style results and remaining limitations. Keep IDs/layout in the existing report or actual consuming manifest; do not duplicate detailed evidence. Candidate submission is not final acceptance.
