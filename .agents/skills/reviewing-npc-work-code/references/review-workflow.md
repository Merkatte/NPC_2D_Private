# Review Workflow

## Required Inputs

Read these before judging the code:

- `PublicMD/ProjectStructure.md`
- the `PublicMD/Systems` leaf documents that own the requested or changed files
- `PublicMD/CodeConvention.md` when C# is in scope
- `PublicMD/ARCHITECTURE.md` only when judging cross-system ownership or dependency direction
- `PublicMD/Status/Code_Evaluation_Result.md` when it exists
- the changed C# scripts and direct dependency surfaces identified by the selected leaf documents
- Unity scene/prefab YAML only when serialized wiring affects the finding

For an explicitly whole-project audit, read every Systems leaf and inspect all production C#. Do not broaden a focused review into a whole-project audit.

Do not read `CLAUDE.md` or `.claude`.

## Audit Categories

Evaluate each relevant script for:

- responsibility boundaries and whether the class does only its documented role
- dependency direction and whether lower-level code depends on decision/orchestration layers
- decision/selector/action/context/provider/runtime ownership rules from the selected Systems leaf
- `WorkerNPC` remaining queue-lifecycle-only
- action pool ownership and whether pooled action state is fully cleared
- magic numbers, duplicated tuning values, and whether data belongs in ScriptableObject/provider assets
- field naming, serialized field style, null-check style, comments, one-primary-class-per-file
- dead code, stale enums, unused usings, unexplained commented-out code
- Unity lifecycle misuse, repeated expensive lookups, per-frame allocations
- prefab/scene serialized references that contradict code ownership

## Report Format

Use this structure in `PublicMD/Status/Code_Evaluation_Result.md`:

```markdown
# Code Evaluation Result

## Purpose
Short scope and standards used.

## Review Snapshot
- Date:
- Scope:
- Sources:
- Verification:

## Executive Summary
High-signal summary of current quality and risk.

## Improvements Since Previous Review
List resolved or improved items when prior content exists.

## Findings By Severity
### Critical
### High
### Medium
### Low

## Findings By File
File-by-file notes with path, issue, evidence, and recommendation.

## Cross-Cutting Findings
Architecture-wide issues such as namespace policy, ownership drift, repeated magic numbers, or documentation drift.

## Positive Notes
Briefly record patterns that match the conventions well.

## Recommended Next Actions
Ordered, concrete tasks.
```

Use `None found` under a severity heading when there are no findings.

## Finding Style

For each issue, include:

- severity
- file path
- concrete line/function/class reference when possible
- why it violates project convention or architecture
- recommended fix

Prefer concise, actionable findings over broad commentary.

## Validation

Before finalizing:

- Run `dotnet build Assembly-CSharp.csproj --no-restore` when available.
- Run targeted `rg` checks for repeated constants, unused APIs, ownership violations, and stale references.
- If build cannot run, record why in the report.

Do not report speculative runtime behavior as confirmed unless supported by code or serialized data.
