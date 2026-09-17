---
name: create-project-sprites
description: Create or edit bounded raster sprite candidates for NPC_Work_2D using approved project reference assets, family-specific visual constraints, and an orchestrator-supplied WorkerAssignment. Use for characters, buildings, props, crops, UI sprites, or tiles; do not use for code, scene or prefab wiring, direct importer YAML edits, or final art acceptance.
---

# Create Project Sprites

Act as a candidate-producing graphics worker, not as the root orchestrator or final art reviewer. Produce the requested raster artifact and evidence without wiring it into a Unity scene.

## Required Assignment

Require a bounded assignment containing:

- `assignmentId`, `runId`, `roleSkill`, `policyVersion`, objective, intended in-game use, and asset family;
- approved reference assets or authority to select the nearest existing family references;
- `requiredContext`, output path, exact `writablePaths`, and explicit `forbiddenPaths`;
- `allowedTools`, `forbiddenOperations`, root-recorded `baselineCommit`, and `baselineDirtyFiles`;
- worker-result and log `artifactPaths` under the assigned run;
- required dimensions, transparency, framing, animation frames, slicing, and naming when applicable;
- expected Unity import settings or a reference `.meta` from which to derive them;
- observable `acceptanceConditions`, `preliminaryChecks`, and `reportRequirements`.

The root must serialize this assignment as WorkerAssignment v1 under
`.harness-runs/<runId>/assignments/`, select
`Tools/NpcHarness/SkillPolicies/create-project-sprites.json` v1, and set
`execution.kind` to `raster-art`. The execution contract records the asset family,
intended use, exact PNG output, approved references, paired import-reference `.meta`,
import-spec artifact path, dimensions, alpha requirement, and frame grid. Read
`Tools/NpcHarness/Schemas/worker-assignment.schema.json` for the assignment contract and
`Tools/NpcHarness/Schemas/sprite-import-spec.schema.json` for the persisted import specification.
The root records the assignment SHA-256 before delegation and supplies it separately.
Do not edit the assignment or recompute a replacement digest.

If required assignment fields such as the exact staging filename or writable path are absent, return `status: Blocked` with `reasonCode: AssignmentIncomplete`. If several existing families conflict and the assignment does not identify the intended one, use `reasonCode: ArtDirectionAmbiguous`. Do not merge unrelated styles or declare a new global art direction.

## Establish the Visual Contract

Read [references/project-art-routing.md](references/project-art-routing.md). Inspect the approved reference images visually and read their actual `.meta` settings. Treat the closest approved reference family as the contract; filenames or this Skill's prose do not override the assets.

There is currently no single authoritative project-wide art bible. Preserve family-specific differences in pixel scale, filtering, pivot, PPU, slicing, and framing. Do not infer UI settings from a character, tile settings from a building, or vice versa.

Explicit assignment constraints take precedence over reference dimensions and importer values. References define the intended visual family and supply defaults only for fields the assignment leaves open. Report material conflicts instead of silently ignoring either source.

## Produce the Candidate

- Use the available `$imagegen` Skill for new raster generation or image editing, and follow its image-input rules.
- Base prompts on the approved references, intended camera/view, silhouette, palette, material language, transparency, and exact deliverable constraints.
- Generate the smallest useful number of variants. Promote only the selected candidate into the assigned output path. Leave provider-generated or run-artifact variants untouched unless their cleanup is explicitly authorized.
- Preserve transparent background requirements and keep unrelated surrounding pixels, text, shadows, or presentation mockups out of production sprites unless requested.
- Write only assigned raster output and explicitly assigned source or manifest files. Do not modify C#, scenes, prefabs, materials, animation controllers, ScriptableObjects, ProjectSettings, or accepting gates.
- Do not hand-edit Unity `.meta` YAML. Return an `importSpec`; Unity import and scene wiring belong to a later root-owned or object-assembly step.
- If placing a raster directly under `Assets` is not explicitly authorized, keep it in the assigned staging or run-artifact path.

## Verify Observable Properties

Check properties that can be measured deterministically:

- file type, pixel dimensions, and alpha-channel presence;
- expected frame or sheet dimensions;
- transparent-edge contamination and obvious cropping;
- output filename, path, and content hash;
- agreement between proposed `importSpec` and the selected reference `.meta`.

Visual style similarity remains probabilistic. Report it as review-required rather than calling it a deterministic pass.

After file verification, return the candidate, import specification, and evidence to the root. Do not run the trust-boundary Scope Gate yourself. The root must run:

`./run-harness.sh verify-scope --policy Tools/NpcHarness/SkillPolicies/create-project-sprites.json --assignment <assignment-path> --assignment-sha256 <pre-delegation-hash> --run-id <runId>`

The root then runs the separate task-specific acceptance and visual review. Never modify either gate to fit the candidate.

## Return a WorkerReport

Return one structured report with:

```json
{
  "assignmentId": "assignment-id",
  "roleSkill": "create-project-sprites",
  "status": "Candidate | Blocked | Failed",
  "reasonCode": "None | AssignmentIncomplete | ArtDirectionAmbiguous | GenerationFailure | VerificationFailure",
  "assignmentSha256": "root-supplied pre-delegation digest",
  "scopeGateResultPath": "pending root verification",
  "acceptanceGate": {
    "manifestPath": "",
    "profile": "",
    "profileVersion": 1
  },
  "changedFiles": [],
  "checksRun": [],
  "evidence": [],
  "referenceAssets": [],
  "generationPrompt": "",
  "importSpec": {},
  "unresolved": [],
  "deviations": []
}
```

`Candidate` is an art proposal ready for deterministic file/import checks and independent visual review. It is not final acceptance.
