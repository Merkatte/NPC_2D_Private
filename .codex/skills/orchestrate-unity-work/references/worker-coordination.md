# Worker Coordination

This reference owns assignment/report contents, role boundaries and shared-worktree scheduling. Read it before delegation. Use the ordinary message contracts here; serialized legacy contracts are conditional under [extended-workflows.md](extended-workflows.md).

## Roles and Context

Assign every required implementation role to a separate worker, including small edits and existing authoring-tool execution. Root coordinates, checks evidence and maintains shared/status/workflow documents; it does not implement code, graphics or wiring. Delegation failure requires same-role reassignment or reporting the missing capability, not root implementation. Only an explicit user exception changes this boundary.

| Role | Owns | Reports to root instead of editing |
|---|---|---|
| Code — author-unity-code | Assigned C#, including authorized helpers outside Assets; assigned new script meta and code-owner docs | Scenes, prefabs, SO assets, import changes and graphics |
| Graphics — create-project-sprites | Assigned raster files, import specification and art evidence | C#, Unity import/meta and wiring |
| Assembly — assemble-unity-objects | Assigned scene/prefab/SO/import operations and readback through permitted YAML, existing tools or supplied helpers | C# authoring/modification, including temporary helper logic; source image generation |
| Reviewer — reviewing-unity-candidate | Read-only candidate assessment | All implementation fixes |
| Root | Scope/contracts, scheduling, checks/evidence, shared/status/workflow documents, final decision | Code/art/wiring implementation and remediation |

Passing arguments to an existing helper is execution; authoring new C# logic is code work. These roles do not authorize verification-tool development or unrestricted YAML changes. Apply SceneWork.md's tool, serialization and verification limits.

Use native collaboration subagents, not terminal panes, scraping or a worker-managed control plane. Start each worker/reviewer with `fork_turns="none"`. Supply only relevant requirements/approvals, role skill, documents, exact paths, input artifacts and evidence requirements. Workers may inspect directly relevant dependencies under project routing.

Reuse an agent only for the same role and related scope. A candidate worker never becomes its independent reviewer; a reviewer never becomes a remediation worker. Root transmits agreed class/interface/serialized-field names, asset paths, import requirements and readiness between roles, not another worker's reasoning.

## WorkerAssignment — Ordinary Work

Give a concise message containing the following information; no serialized v1 file is required:

- identity: `assignmentId` and matching `roleSkill`;
- outcome: `objective` and observable `acceptanceSlice`;
- context: `requiredContext`, relevant approvals and stable handoff contracts/dependency readiness;
- ownership: exact files or narrow `writablePaths`, `forbiddenPaths` and starting `baseline` commit/dirty paths (explicitly empty when none overlap);
- execution: `allowedTools`, `forbiddenOperations` and separate temporary/ignored `artifactPaths` for logs or outputs;
- evidence: `preliminaryChecks`, any reusable current results and `reportRequirements` below.

Acceptance may consist of common checks and requested changed values; it does not require a new functional gate. An assignment narrows a role and grants no additional user authority. Artifact paths grant no source-edit rights. Required out-of-scope changes are reported to root; add/reassign the appropriate role rather than letting a worker cross roles.

Tell workers the remaining task-wide correction budget. Do not grant independent retry allowances per worker; root accounts for corrections under [remediation-loop.md](remediation-loop.md).

## WorkerReport — Ordinary Work

Return one concise collaboration message; link raw logs rather than writing duplicate report files:

- `assignmentId`, `roleSkill`, `status` (Candidate, Blocked or Failed) and `reasonCode` (None for Candidate, otherwise the concrete blocker/failure);
- `changedFiles` intentionally edited;
- `checksRun` with actual commands/outcomes, and `evidence` with logs, relevant inputs/freshness and observations;
- `unresolved` work/uncertainty and `deviations` from assigned scope or expected worktree state.

Code evidence covers five categories: actual compile command/result/log; conventions with rule/code locations; ownership with class responsibility and owning Systems leaf; changed dependencies and permitted direction; actual diff/scope with prior edits and diff check. Include relevant forbidden-reference/serialization inspection. A short row per category is enough; "read the rules" or "build passed" alone does not prove architecture compliance.

Candidate submits a slice for root inspection, not acceptance. A successful worker check may be reused under SKILL.md's freshness/coverage rules; it is neither automatically discarded nor accepted from the report alone.

## Ownership and Scheduling

Create only needed workers. Run independent code/art work concurrently. Assembly can inspect/prepare concurrently, but waits for stable input code/art and successful compilation before dependent writes/imports.

Canonicalize paths before parallel writes, including nested-directory overlaps. Shared worktrees and role prompts are not filesystem isolation. Treat a serialized asset and its meta, every edit within one scene/prefab/SO, and a new script with its meta as coupled ownership units. Assign shared settings, package, assembly-definition, registry or manifest files to one owner.

Sequence Unity mutations, imports, compilation-triggering operations and saves on the same Editor. If live auto-import prevents safe parallel writes, assign staging paths outside Assets and track target paths. Source promotion stays with its authoring role; assembly owns import/serialized changes. Do not change auto-refresh settings or save user state to force concurrency. Preserve existing scene structure and SceneWork.md's unsaved-state safeguards.

## Reconciliation and Partial Failure

1. Root records baseline and assignment-to-path ownership before workers start.
2. While workers run, root does not edit or reassign their writable paths.
3. After required reports, inspect actual status/diff including relevant untracked files and serialized state against baseline, ownership and reports.
4. Flag overlaps, unreported or unauthorized changes and partial output from blocked/failed workers.
5. On conflict, stop new writes and final verification. Preserve all prior/unrelated work, invalidate affected reports and assign one same-role owner at a time to reconcile. Seek user direction only if resolution requires broader authority or risks unrelated work.

Never reset/discard a conflicted worktree automatically. A required Blocked/Failed workstream makes the combined candidate incomplete; preserve successful slices and do not silently remove the failed requirement. Correct within authorized scope and the shared budget, or report the partial diff and blocker.

After reconciliation, follow SKILL.md's common checks and required independent review. For an explicitly selected strict workflow, also follow extended-workflows.md; worker success does not replace its required root-owned acceptance.
