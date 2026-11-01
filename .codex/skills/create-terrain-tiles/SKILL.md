---
name: create-terrain-tiles
description: Create or edit NPC_Work_2D pixel-art terrain tiles and tilesheets with explicit tile inventory, compatible edges, approved pixel style and preserved atlas assembly. Use for ground, shore, corner and interior variants; not UI art, Unity Tilemap wiring or navigation rules.
---

# Create Terrain Tiles

Specialize the existing Graphics role. Use [create-project-sprites](../create-project-sprites/SKILL.md) once for assignment, write boundaries, reference/import handling, ordinary checks, shared correction budget and reporting. Apply this skill for terrain-specific decisions; do not create another worker just because both skills apply. Root may keep roleSkill `create-project-sprites` and include `create-terrain-tiles` as the specialization in requiredContext.

If invoked without a scoped assignment, root uses [orchestrate-unity-work](../orchestrate-unity-work/SKILL.md) to establish it and delegate the graphics role. This skill creates the artwork; Unity import, Tile assets, RuleTile configuration and scene painting belong to assembly. Do not modify navigation/game rules or acquire final acceptance authority.

## 1. Resolve the Tile Contract

Before production, use [tile-contract.md](references/tile-contract.md) to record the needed inventory, directions, connectors, permitted variation and output layout in the existing assignment/task record. A concise table is enough; no separate plan, serialized schema or per-tile evidence document is required.

Inspect the actual approved reference images and relevant existing source/sheet. Reuse established decisions; ask root only about unresolved material choices. Explicit user values override reference defaults. Do not universally prescribe 32/64-pixel cells, midpoint connectors, an 8-pixel protected strip, a fixed palette count or a 16/47-tile family.

Distinguish a visual concept request from a functional tile deliverable. A concept need not cover an unrequested complete tileset, but must not be described as ready-to-connect production tiles. For a functional request, missing requested interiors/corners/variants and incompatible connectors are failures, not optional polish.

## 2. Preserve Structure While Adding Variation

Map each requested tile/variant to a stable ID and sheet cell. Define corner material occupancy as well as direction, using the contract's coordinate convention.

For compatible tiles, preserve the shared edge profile: material arrangement, crossing positions, bank/lip/shadow width, base color and alpha where relevant. Exact midpoint crossings alone do not guarantee compatibility. Variation may change interior contours and decoration without breaking these agreed connections.

“More variety” defaults to decorative/texture variation within the same connectivity class. Do not introduce diagonal connections, new topology, different material coverage or extra transition families without a request. Diagonal visual contours inside an approved corner are not themselves a new diagonal connection.

When flowers, stones or grass are requested inside a tile, deliver composed terrain tiles. Separate decoration layers may be used internally; a loose decoration sheet or “place these yourself” instruction is not an equivalent deliverable unless the user requested overlays.

## 3. Separate New Art from Preservation and Assembly

Use the available imagegen skill/tool for new generated artwork and generative edits. Inspect reference/edit targets first and follow its input, transparency and provider rules. A generated sheet is a draft until actual cells satisfy the inventory and connectors; repeated prompt assertions are not geometric evidence.

The user-approved Terrain workflow also permits existing native pixel-editor/Aseprite operations for exact pixel editing, connector repair and atlas composition. Preserve approved tiles when packing or editing bounded regions. Packing is coordinate placement of the selected pixels, not a new image-generation request. Do not redraw, rescale, recolor, rotate or mirror approved tiles merely to make them fit the sheet. A requested recolor/resize is a separate explicit transformation; inspect its consequences rather than treating it as packing.

Choose available existing tools for the operation. Do not silently replace a missing native tool with whole-sheet regeneration, Python image editing, new API tooling or a simplified procedural art style. Report the concrete capability gap to root. New executable authoring helpers, if actually needed, require root coordination under the project's code-role rules; never create verification-only tools as a workaround.

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
3. **Connections:** assemble a small representative mosaic from the exact delivered tiles. Show the requested corner-to-straight joins, turns, repeating interiors and relevant decorative variants; use a compact contact strip for required variants not present in the mosaic. Include requested concave/convex families and directional joins when in scope.
4. **Pixel style:** inspect that same candidate at native/integer scale for visual treatment, seams, repetitions and unintended artifacts. Compare with approved references; do not use a separately generated landscape as connection evidence.
5. **Handoff:** retain the common measured-file/alpha and proposed-import checks. Report tested combinations and gaps; a representative mosaic is not an exhaustive arbitrary-map or Unity runtime certification.

An exact tile-copy mosaic is part of terrain authoring and inspection, not automatic Play Mode or game-screen QA. It does not authorize new test harnesses or scene execution. For an isolated tile request, use existing approved neighbors in the demonstration where needed; do not manufacture an entire unrequested tileset.

Known missing tiles, bad joins or failed required style remain failures. If a required check cannot be performed, identify the gap and do not claim a production-ready complete set. Root retains independent visual acceptance under create-project-sprites.

## 6. Correct Locally and Report

Preserve satisfactory tiles and approved originals. Correct failed IDs and the directly affected connections only; missing center variants call for those variants, not a new whole atlas. Needed edits outside assigned ownership are reported to root before writing. Recheck changed content, affected joins, style and resulting atlas placement; reuse unaffected evidence.

Use the existing task-wide correction budget, shared across workers and failure types. Do not start a new per-tile/style/generation allowance. When the budget or repeated-cause stopping rule is reached, report completed items, failed/missing IDs, actual evidence and the concrete remaining choice/capability gap. Do not silently lower quality or keep regenerating.

Return one ordinary graphics WorkerReport: selected output/source paths, tile ID/layout mapping, references, generation/edit method and prompt location when used, proposed importSpec, observed inventory/connection/style results and remaining limitations. Keep IDs/layout in the existing report or actual consuming manifest; do not duplicate detailed evidence. Candidate submission is not final acceptance.
