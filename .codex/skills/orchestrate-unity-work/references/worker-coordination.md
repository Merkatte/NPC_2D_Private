# Worker Coordination

Read this reference before delegating implementation or investigation to any worker.

## Selection

Assign every required implementation role to a separate worker, including small changes and existing authoring-tool execution. The root coordinates, verifies, maintains workflow/status documents and accepts results; it does not produce code, art or wiring. Only an explicit user exception changes that boundary. Delegation failure does not authorize root implementation.

Use only needed roles. Run independent code/art tasks concurrently. Assembly may inspect and prepare concurrently, but waits for stable code/art and successful compilation before writes/imports. A one-role request needs one role worker, not all three.

Read-only investigations may overlap in what they inspect. Parallel writers must have non-overlapping writable ownership known before they start. When ownership cannot be made disjoint, run the work sequentially.

Use native collaboration subagents: create one subagent per assignment with `spawn_agent`, communicate with `send_message` or `followup_task`, and collect lifecycle results with `wait_agent`; use `interrupt_agent` only when work must be stopped. The root retains orchestration. Do not require tmux, pane layouts, terminal keystrokes, or pane output scraping.

## Context and Role Isolation

- Start workers and reviewers with `fork_turns="none"`; do not copy the whole conversation or other workers' reasoning.
- Supply relevant user requirements/approvals, role Skill, objective, required documents/reference assets, exact paths, exclusions and evidence requirements. Workers may read directly relevant dependencies under project routing.
- Maintain a small handoff contract: class/interface/serialized field names, asset paths, import requirements, ownership and dependency readiness. The root transmits facts and artifacts between roles.
- Reuse an agent only for the same role and related scope. A different role/unrelated scope needs a fresh context. A candidate worker never becomes its independent reviewer.
- Shared worktrees are not filesystem isolation. Verify actual diffs and before hashes; a role prompt is not a security sandbox.

| Role | Owns | Reports to root instead of editing |
|---|---|---|
| Code | Assigned C#, including Editor/eval/assembly helpers; assigned new script meta and code-owner docs | Scenes, prefabs, SO assets, importer changes and graphics |
| Graphics | Assigned raster files, import specification and art evidence | C#, Unity import/meta and scene wiring |
| Assembly | Assigned scene/prefab/SO/import changes and readback through existing tools or supplied helpers | C# creation/modification, including temporary helper/eval logic; source image generation |
| Reviewer | Read-only current candidate review and findings | Every implementation fix |
| Root | Scope/contracts, scheduling, checks/evidence, shared/status/workflow docs and final report | Code/art/wiring implementation and remediation |

Passing arguments to an existing helper is execution. Authoring new C# logic, even
outside Assets, is code work. A code worker must not author the gate accepting its
own candidate; accepting-gate changes require their own independent scope/evidence.
Do not build a feature-specific validator just to produce a completion record.

## WorkerAssignment

For the ordinary-work route in `Tools/NpcHarness/SceneWork.md`, these are the fields
of a concise assignment, not a requirement to serialize WorkerAssignment v1. The
legacy v1 schema applies only to explicitly selected legacy role-policy runs.
`acceptanceSlice` can be the shared common checks plus the requested changed values;
it does not require a new functional gate. Root verifies actual evidence and scope.

Give each worker a bounded assignment containing:

- `assignmentId`: stable identifier for reporting and ownership;
- `roleSkill`: the project worker Skill to follow when one matches the slice;
- `objective`: one independently completable outcome;
- `requiredContext`: project documents and candidate inputs to inspect;
- `writablePaths`: exact files or narrow directories exclusively owned by this worker;
- `forbiddenPaths`: accepting gates, fixtures, unrelated user changes, and other workers' ownership;
- `artifactPaths`: separate ignored or temporary paths for Jobs, logs, reports, generated variants, and build evidence;
- `allowedTools` and `forbiddenOperations`: explicit capability boundary for Tool-mediated or high-risk work;
- `baseline`: starting commit plus an explicit pre-existing dirty-path list or hashes, including an explicit empty list when none overlap;
- `acceptanceSlice`: observable contribution expected from this workstream;
- `preliminaryChecks`: checks the worker may run before reporting;
- `reportRequirements`: changed files, checks, evidence, unresolved items, and deviations.

An assignment grants no permission beyond its stated paths and the user's existing authority. `artifactPaths` do not grant source or candidate mutation rights. A worker that discovers a required out-of-scope change must stop and report it instead of editing.

Use `$author-unity-code` for C#, `$create-project-sprites` for raster art, and `$assemble-unity-objects` for ordinary bounded YAML/Editor API or selected legacy Tool work. An assignment narrows its role; it cannot grant another role's files or operations. Add/reassign the required role instead.

## WorkerReport

Require each worker to return:

- `assignmentId` and `status`: `Candidate`, `Blocked`, or `Failed`;
- `reasonCode`: `None` for a candidate or a stable role-specific reason for blocked/failed work;
- `roleSkill`: the role contract actually followed;
- `changedFiles`: only files it intentionally changed;
- `checksRun`: command or gate name with actual outcome;
- `evidence`: logs, result paths, and relevant observations;
- `unresolved`: remaining work, uncertainty, or external dependency;
- `deviations`: unexpected worktree changes or assignment boundary issues.

For code, `checksRun`/`evidence` must contain five categories: actual compile
command/result/log; conventions (rule/document and code location); ownership (class
responsibility and Systems leaf); dependencies (changed calls/references and allowed
direction); and scope (actual diff, prior edits and diff check). Include relevant
forbidden-reference/serialization searches. One short evidence row per category is
enough. "Read CodeConvention" or "build passed" does not establish architecture
compliance. Missing evidence is unverified. Independent code review is still required.

`Candidate` means the worker submitted its slice for root inspection. It never means `Accepted`, even if every worker reports `Candidate` or a worker-local test passes.

## Writable Ownership

Canonicalize paths and reject overlapping file or directory ownership before spawning parallel writers. Treat nested paths as overlap.

Treat coupled Unity data as one ownership unit:

- a scene, prefab, ScriptableObject, material, animation asset, or other serialized asset and its `.meta` file;
- all changes within the same `.unity`, prefab, or serialized asset, even when workers target different GameObjects or fields;
- a script and a newly generated companion `.meta` file;
- shared project, package, assembly definition, registry, manifest, or settings files.

Do not run concurrent Unity Editor mutations, imports, scene saves, compilation-triggering asset operations, or Play Mode sessions in the same project. Workers may prepare disjoint text changes, but the root sequences Unity import, serialization, and final verification.

If an open Editor would auto-import parallel writes, prepare code/art at assigned
staging paths outside Assets until promotion/import is scheduled. Source promotion
remains with its authoring role; assembly owns import/serialized changes. Track
staging and target paths. Do not change auto-refresh settings or save user state to
force concurrency. Freeze shared field names and asset paths before handoff.

Assembly may start read-only preparation early; writes wait for its dependencies.
A graphics worker returns an import specification, not a wired scene.

## Shared Worktree Protocol

All collaboration workers share the same worktree. Parallel delegation is not isolation.

1. Before spawning, the root records the repository baseline and an assignment-to-path ownership map.
2. While workers run, the root does not edit their owned paths or assign them elsewhere.
3. After all required reports arrive, the root reads the actual status and diff rather than concatenating reports or trusting claimed file lists.
4. Compare actual paths against the baseline and ownership map. Flag any path reported by multiple workers, changed outside the ownership union, omitted from reports, or coupled to another worker's Unity asset.
5. On conflict, stop new writes and do not run the final gate. Preserve unrelated and pre-existing work. Invalidate affected reports and assign one same-role owner at a time to reconcile. The root does not patch worker files. Request user direction only when reconciliation needs broader authority.

Do not automatically reset, discard, or overwrite a conflicted shared worktree.

## Partial Failure

A `Blocked` or `Failed` report prevents the combined candidate from reaching final verification when its workstream is required. Successful slices remain unaccepted candidate material; do not discard them automatically and do not silently remove the failed requirement.

Inspect changes left by the unsuccessful worker. If the original scope and remediation budget permit a correction, assign one sequential owner and follow the bounded remediation rules. Otherwise stop with the partial diff, evidence, and unresolved assignment clearly reported.

## Root Aggregation and Acceptance

For ordinary work, the root uses the common checks and evidence record from
`Tools/NpcHarness/SceneWork.md` after reconciliation. Steps 5-6 below describe the
selected gate workflow only; never invent a GateResult or require a new functional
profile solely to serialize an ordinary worker report.

The root owns aggregation:

1. verify actual changes against every assignment and the original scope;
2. resolve all ownership conflicts and required partial failures;
3. inspect the combined diff and Unity serialized state;
4. form one current candidate;
5. run one root-selected deterministic acceptance profile over the complete candidate;
6. accept only its valid `GateResult`.

Worker checks are preliminary evidence. Per-worker success, report consensus, or parallel speed does not replace the final deterministic gate.

After selected checks, require independent review for every C# change and other
required-risk scope. Wait for the actual result and inspect conventions/ownership/
dependencies evidence. Route fixes to the original role. A launch is not completion.
No implementation/remediation worker may be the candidate's independent reviewer.
Wait for a free concurrency slot instead of reusing a worker as reviewer. Ordinary
review uses common evidence; selected GateResult/review-evidence contracts remain
unchanged. SceneWork.md's automatic play/screen exclusions still apply.
