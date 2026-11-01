---
name: create-project-sprites
description: Create or edit bounded raster sprite candidates for NPC_Work_2D using approved project reference assets, family-specific visual constraints, and an orchestrator-supplied WorkerAssignment. Use for characters, buildings, props, crops, UI sprites, or tiles; do not use for code, scene or prefab wiring, direct importer YAML edits, or final art acceptance.
---

# Create Project Sprites

Act as the assigned graphics worker. Produce a raster candidate, proposed import settings and evidence for root inspection. Unity import/wiring belongs to assembly; final acceptance and independent visual review remain outside the author's role.

For terrain tiles (including shores, corners, interior variants and terrain atlas assembly), also apply [create-terrain-tiles](../create-terrain-tiles/SKILL.md) within this same Graphics assignment. This file owns common role/verification/handoff rules; that specialization owns tile inventory, connectors, pixel-art quality and preservation during assembly. Read each once; this is not a request for another worker or recursive dispatch.

## 1. Confirm the Assignment

Use the ordinary WorkerAssignment and role/handoff rules in [worker-coordination.md](../orchestrate-unity-work/references/worker-coordination.md). A concise root message is sufficient; do not require a per-image gate, assignment JSON or hash manifest for ordinary work.

Confirm the exact output/staging path, intended use and asset family, approved references or authority to select them, dimensions/transparency, and applicable framing, frames, slicing and import requirements. Preserve baseline/prior changes and assigned ownership. Missing safe writable paths or essential requirements mean Blocked/AssignmentIncomplete; unresolved competing visual families mean Blocked/ArtDirectionAmbiguous. Report the gap rather than inventing a new art direction.

Follow [SceneWork.md](../../../Tools/NpcHarness/SceneWork.md)'s verification limits: use existing tools and do not create/extend verification-only runners, validators or tests without a separate explicit request. Inspecting source/output images is part of this role; automatic game-screen/Play Mode checks require a separate explicit request.

For an explicitly selected extended workflow, follow root's protocol under [extended-workflows.md](../orchestrate-unity-work/references/extended-workflows.md). Read [legacy-workflow.md](references/legacy-workflow.md) only when root explicitly selects WorkerAssignment v1. An existing checker alone does not select that route.

## 2. Establish References and Target Settings

Read [project-art-routing.md](references/project-art-routing.md). Inspect the approved images visually and the actual same-purpose reference .meta settings. The documented families are starting points, not a global art bible; preserve differences in view, pixel scale, framing, PPU, pivot, filtering and slicing.

Use the assignment's explicit authorized values first. Use reference values only for unspecified fields. Compare the candidate and proposed importSpec against these resolved targets, not unconditional equality with the reference. Keep a short note of explicit overrides and their source in the report; do not add provenance fields to an existing serialized schema.

A requested bottom-center pivot is valid even when the reference uses a centered pivot. Contradictory requirements, unsupported settings or unresolved family choices go to root for resolution; do not silently substitute another value. Proposed settings do not authorize changing the reference asset.

## 3. Produce the Candidate

- Use the available $imagegen skill for new raster generation and image editing, following its input and transparency rules. For the explicitly agreed Terrain workflow, use existing native pixel-editor/Aseprite operations for pixel-preserving edits and atlas composition under create-terrain-tiles; do not regenerate approved artwork merely to pack it.
- Base the prompt on approved references, intended view, silhouette, palette, material language and exact deliverable constraints.
- Generate the smallest useful number of variants. Promote only the selected candidate into the assigned output path. Preserve other provider/run-artifact variants unless cleanup is explicitly authorized.
- Preserve requested transparency and keep unrequested surrounding pixels, text, shadows and presentation mockups out of production sprites.
- Write only assigned raster output and explicitly assigned source/manifest files. Do not edit C#, scenes, prefabs, SO assets, materials, animation controllers, ProjectSettings, accepting gates or fixtures.
- Do not hand-edit Unity .meta or apply importer settings. Return importSpec for the assembly worker. Keep files in assigned staging/run paths unless writing directly under Assets is explicitly authorized; coordinate any import-triggering writes with root.

## 4. Check the Output

Check the actual delivered candidate, not a previous variant or the generation prompt. Keep the following evidence distinct.

### Measured File Properties

Use existing raster-inspection tools to confirm file type, pixel dimensions, applicable frame/sheet dimensions, output filename/path and alpha-channel presence. When a transparent background is requested, also inspect actual alpha values for transparent pixels: an alpha channel with every pixel fully opaque does not satisfy that request.

Some transparent pixels alone do not prove correct background removal or clean edges. Report the values/observations actually obtained and the tool used; do not substitute checkerboard appearance or a filename for alpha evidence. Content hashes are needed only by a selected protocol or explicit assignment.

### Visual Inspection

Open the delivered image and inspect silhouette, framing, unintended cropping, transparent-edge halos/contamination and unwanted background details. Compare style, view, palette and rendering density against the approved family.

Report these as visual observations/judgments, with specific concerns where present. Dimensions and alpha checks do not establish visual quality; visual similarity is not a deterministic pass. The author's inspection supports the candidate and does not replace independent visual acceptance.

### Import Settings Comparison

Check proposed importSpec against explicit assigned targets for overridden fields and the inspected same-purpose reference for the remaining fields. Use the field guidance in project-art-routing.md. Do not reject an authorized override merely because it differs from the reference, or claim that proposed settings have already been applied in Unity.

If a check cannot be performed with available tools, state precisely what remains NOT_VERIFIED. An observed defect or unmet required property remains a failure, not optional missing coverage. Report failures to root and follow the shared [remediation-loop.md](../orchestrate-unity-work/references/remediation-loop.md); do not start unlimited regeneration or grant this worker a separate correction budget.

## 5. Return the Candidate and Import Handoff

Use one concise ordinary WorkerReport from worker-coordination.md, referencing existing evidence. Include referenceAssets, the generation prompt used (or its existing artifact location), and importSpec needed by assembly. Separate measured results, visual observations, explicit setting overrides and unverified items in that same report.

Do not fill unused assignment-hash or gate fields, or create duplicate evidence summaries. Persist an importSpec artifact when the actual handoff requires it; ordinary reporting alone does not require the legacy schema or another report file.

Candidate means submitted for root inspection and independent visual review, never Accepted. Root verifies actual scope/evidence and arranges acceptance; the graphics worker does not execute root-owned acceptance gates or take over Unity wiring.
