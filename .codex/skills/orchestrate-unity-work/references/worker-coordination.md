# Worker Coordination

Read this reference before delegating implementation or investigation to any worker.

## Selection

Prefer a deterministic tool, direct root work, or one worker. Use multiple workers only when the request contains at least two required workstreams that can finish independently and parallel execution has a concrete benefit.

Read-only investigations may overlap in what they inspect. Parallel writers must have non-overlapping writable ownership known before they start. When ownership cannot be made disjoint, run the work sequentially.

Use native collaboration subagents: create one subagent per assignment with `spawn_agent`, communicate with `send_message` or `followup_task`, and collect lifecycle results with `wait_agent`; use `interrupt_agent` only when work must be stopped. The root retains orchestration. Do not require tmux, pane layouts, terminal keystrokes, or pane output scraping.

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

Use `$author-unity-code` for C#, `$create-project-sprites` for raster art, and `$assemble-unity-objects` for supported Tool-only scene object work. Do not use a role label as a permission grant: the assignment and harness policy may only narrow what the Skill could otherwise do.

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

`Candidate` means the worker submitted its slice for root inspection. It never means `Accepted`, even if every worker reports `Candidate` or a worker-local test passes.

## Writable Ownership

Canonicalize paths and reject overlapping file or directory ownership before spawning parallel writers. Treat nested paths as overlap.

Treat coupled Unity data as one ownership unit:

- a scene, prefab, ScriptableObject, material, animation asset, or other serialized asset and its `.meta` file;
- all changes within the same `.unity`, prefab, or serialized asset, even when workers target different GameObjects or fields;
- a script and a newly generated companion `.meta` file;
- shared project, package, assembly definition, registry, manifest, or settings files.

Do not run concurrent Unity Editor mutations, imports, scene saves, compilation-triggering asset operations, or Play Mode sessions in the same project. Workers may prepare disjoint text changes, but the root sequences Unity import, serialization, and final verification.

When a combined request needs code or art before Unity assembly, establish those candidates first. Run `$assemble-unity-objects` only after its referenced code and art inputs exist and their paths are stable. A graphics worker returns an import specification; it does not wire its own sprite into a scene.

## Shared Worktree Protocol

All collaboration workers share the same worktree. Parallel delegation is not isolation.

1. Before spawning, the root records the repository baseline and an assignment-to-path ownership map.
2. While workers run, the root does not edit their owned paths or assign them elsewhere.
3. After all required reports arrive, the root reads the actual status and diff rather than concatenating reports or trusting claimed file lists.
4. Compare actual paths against the baseline and ownership map. Flag any path reported by multiple workers, changed outside the ownership union, omitted from reports, or coupled to another worker's Unity asset.
5. On conflict, stop new writes and do not run the final gate. Preserve unrelated and pre-existing work. Invalidate affected reports, then choose one sequential owner to reconcile within the original scope or request user direction when safe reconciliation needs broader authority.

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

After deterministic acceptance, follow the scoped review policy. No implementation or remediation worker may serve as the independent `$reviewing-unity-candidate` reviewer for that candidate.
