---
name: author-unity-code
description: Implement a bounded Unity C# candidate according to NPC_Work_2D document routing, code conventions, ownership boundaries, and an orchestrator-supplied WorkerAssignment. Use for delegated code creation, refactoring, or bug fixes; do not use for sprite production, direct scene or prefab editing, accepting-gate authorship, or independent review.
---

# Author Unity Code

Act as a code worker. Implement only the assigned slice and submit the candidate with evidence for root inspection. Root owns acceptance and obtains independent code review; the author does neither.

## 1. Confirm the Assignment

Use the ordinary WorkerAssignment and role/handoff rules in [worker-coordination.md](../orchestrate-unity-work/references/worker-coordination.md). That document owns the common assignment and report fields. Confirm the objective, acceptance slice, exact source/document/meta ownership, baseline and prior changes, relevant context, existing checks and artifact paths before writing. A concise root message is sufficient for ordinary work.

If safe ownership or source paths are missing, an unapproved product decision is required, or existing changes cannot be preserved, report Blocked with the concrete reason to root. Do not broaden scope or take another role's files. Unrequested responsibility moves or shared-contract changes need root/user direction; changing documentation does not authorize weakening project rules.

Use [SceneWork.md](../../../Tools/NpcHarness/SceneWork.md) for ordinary verification limits. Do not create or extend verification-only runners, eval helpers, validators, tests or harness adapters without a separate explicit user request. Automatic Play Mode and game-screen verification, including retries and dedicated checks, also require a separate explicit request. Generic QA items in plans do not grant either authorization.

For an explicitly selected extended workflow, follow root's selected protocol under [extended-workflows.md](../orchestrate-unity-work/references/extended-workflows.md). Read [legacy-workflow.md](references/legacy-workflow.md) only when root explicitly selects WorkerAssignment v1. Running an existing checker does not select legacy mode. Supplied assignments, review requests and snapshots remain root-owned evidence; never modify them to make the candidate pass.

## 2. Read the Relevant Project Context

1. Use PublicMD/ProjectStructure.md to find the owning Systems feature.
2. Follow each folder's README recursively until reaching the relevant leaf. Read its minimum change scope before opening additional code or Unity assets.
3. Read PublicMD/CodeConvention.md before creating or modifying C#.
4. Read PublicMD/Game_Plan.md and PublicMD/SPEC.md when gameplay rules or player experience change; leave unresolved rules as TBD.
5. Read PublicMD/PLAN.md and its linked active detailed plan when beginning a planned feature or depending on that plan. Preserve specialized feature planning and approval.
6. Read PublicMD/ARCHITECTURE.md when responsibilities, shared interfaces, dependency direction or runtime ownership cross feature boundaries.

Do not read or modify CLAUDE.md or .claude. Actual code and serialized assets establish current facts when documents are stale. Correct the owning Systems leaf within assigned document ownership; otherwise report the needed correction to root.

## 3. Implement the Assigned Code

- Edit only assigned C#, new companion .meta files and explicitly assigned owning documents. Preserve unrelated and pre-existing changes; do not reset, reformat or clean outside the assignment.
- Keep WorkerNPC, selector, action, provider, NPCComponent, manager, runtime-state and shared-definition responsibilities consistent with the owning Systems documents. Use their placement rules and ProjectStructure.md; do not introduce unrequested future architecture.
- Preserve serialized enum values, field migration and existing .meta GUIDs. For new C# assets, prefer Unity import for .meta creation. If the assignment requires creating a new companion .meta without an importer, generate a unique GUID, verify it is unused and never replace an existing asset's GUID.
- Assigned implementation/assembly helper C# may be outside Assets. Writing that logic belongs to code; executing it to mutate Unity assets belongs to assembly. Verification-only helpers still require the separate authorization above.
- Do not edit scenes, prefabs, SO assets, materials, textures, animation assets, ProjectSettings, accepting gates or fixtures. Report cross-role needs to root for a separate assembly/graphics slice. An assignment cannot override role boundaries without an explicit user exception.
- Coordinate imports and compilation-triggering operations with root under worker-coordination.md; do not independently mutate the same Editor while another role is using it.

## 4. Check the Current Candidate

Provide the five code evidence categories required by worker-coordination.md: compilation, conventions, ownership, dependency direction and scope. Scope them to the actual diff and necessary dependencies. Include actual command/results/logs and concrete code/document locations; reading rules or passing compilation alone does not establish structural compliance.

Use existing relevant compile/check commands and direct code inspection. Run additional existing non-Play-Mode tests only when they address a concrete changed behavior or risk. Do not broaden into unrelated regression sweeps or rerun successful checks merely to fill a report.

### Reuse and Invalidation

Follow the common freshness rules in [orchestrate-unity-work](../orchestrate-unity-work/SKILL.md). Reuse a successful result only when its actual inputs, relevant dependencies, command/configuration and relevant environment are unchanged and it covers the current candidate. Inspect raw evidence; an assertion of success is insufficient. Who executed the check is not a reason to repeat it.

- A task-report wording change alone does not invalidate compilation.
- Changed source or compilation dependencies/configuration require the affected compile check again.
- Changed requirements may invalidate review or check coverage even without a code change; report that impact to root.
- If input relevance or freshness cannot be established, rerun the affected check or report its blocker. Do not assume stale evidence remains valid.

After a correction, refresh affected checks and reuse unaffected evidence. An explicitly selected strict protocol keeps its full-profile or snapshot rerun requirements; ordinary reuse rules do not weaken it.

Normal compiler output under Library, Temp or assigned temporary directories is verification output, not permission to edit unrelated sources. Keep persisted logs within assigned artifact paths and generated output out of Git.

Record unavailable behavior coverage as NOT_VERIFIED; compilation does not prove wiring or runtime behavior. Missing bespoke automation alone does not block a candidate. Missing required compilation, failed existing checks and actual defects remain blockers; do not relabel them as optional unverified coverage.

For failures or review-requested corrections, follow [remediation-loop.md](../orchestrate-unity-work/references/remediation-loop.md) under root coordination. Corrections consume the existing task-wide budget, not a new allowance for this worker. Report the failure before further corrective revisions so root can bound/account for them. Stop at the shared limit or repeated cause; do not weaken checks or write a replacement runner for an infrastructure blocker.

## 5. Return the Candidate

Return one concise ordinary WorkerReport under worker-coordination.md, referencing existing raw logs and the five code evidence categories. Identify reused results and why they still cover the candidate, plus deviations and unresolved items. Use the common status/reason fields; report verification failure or blockage truthfully.

Do not fill unused assignment-hash, scope-gate or acceptance-profile fields for ordinary work, or create duplicate report/evidence files. Selected legacy reporting is defined only in legacy-workflow.md.

Candidate means submitted for root inspection, not Accepted. Every C# change, including helpers, still requires an independent read-only reviewer and its received result before overall completion. Do not review your own work or run root-owned acceptance; root decides which current checks can be reused and passes primary evidence to the reviewer.
