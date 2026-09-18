# Lightweight review evidence v1

This is a review-completion gate, not a gameplay validator or an authentication boundary.
The root chooses adequate functional gates and input coverage before work, retains the
request SHA-256 outside worker-controlled files, and verifies the real reviewer identity.
It must not let an implementer change the request, snapshot, gates, or approval evidence.

## Commands and sequence

1. Before implementation, create `.harness-runs/<runId>/review-request.json` and retain
   its SHA-256. `inputRoots` are repository-relative directories or exact files covering
   the candidate, dependencies, validation code, and relevant documents. Do not use the
   repository root. Existing roots must exist when snapshotting. A directory captures
   added/deleted files too. Generated `bin`, `obj`, `Library`, `Temp`, `Logs`, `Build`,
   `Builds`, `UserSettings`, `_Recovery`, `agent-runs` entries are excluded at any depth
   (case-insensitively); use source roots, never output directories. `.claude`,
   `CLAUDE.md`, `.git`, and `.harness-runs` are not candidate inputs. Other encountered
   symlinks/reparse points are rejected, not followed. Every rule document must be captured.
2. After implementation, before functional gates, run `review-snapshot --request <path>
   --request-sha256 <root-retained hash>`. Retain the printed snapshot SHA-256. Snapshot
   creation refuses overwrite. Each changed candidate uses a new run ID/request.
3. Run the required gates on the snapshotted unchanged candidate. Their UTC start times
   must follow the snapshot. This checks sequencing, not cryptographic execution provenance.
4. Give one independent reviewer the request, snapshot hash, actual diff, project documents,
   and passing gates. It returns the compact record below at the request's `reviewPath`.
5. Run `accept-review --request <path> --request-sha256 <hash> --snapshot-sha256 <hash>`.
   It writes `gate-results/review-evidence.json` using GateResult v1. Only Pass/exit 0
   satisfies this review step. This does not replace scope, compile, runtime, or specialized
   workflow gates. Missing/invalid/stale evidence is InfrastructureError/2; an evidenced
   gate/review rejection (including an explicit NotCovered review) is Fail/1. Output is
   replaced on every evaluation with a safe canonical request path, including invalid or
   missing request contents. A command failure is never rescued by an older output file.

## Request (all fields required; no unknown or duplicate properties)

```json
{
  "schemaVersion": 1,
  "runId": "example-review",
  "inputRoots": ["Assets/Scripts", "Tools/NpcHarness", "PublicMD"],
  "authorIds": ["root-agent-id"],
  "requiredGates": [{
    "path": ".harness-runs/example-review/gate-results/function.json",
    "profile": "Example.Function", "version": 1,
    "requiredCheckIds": ["example.behavior"]
  }],
  "rules": [{
    "id": "naming", "documentPath": "PublicMD/CodeConvention.md",
    "section": "2. Naming", "allowNotApplicable": false
  }],
  "reviewPath": ".harness-runs/example-review/review.json"
}
```

All lists are nonempty. IDs and paths must be unique (paths case-insensitively).
Documents must be covered by the snapshot. Required gate and review paths must be
distinct JSON paths in this run; reserved snapshot/request/output paths cannot collide.
Select a few categories, not one record for every convention sentence. C# changes need
conventions and ownership review; cross-feature responsibility/dependency changes need
architecture review. Material unresolved requirements cannot be marked NotApplicable.

The strict executable contract is implemented by `ReviewEvidenceGate` and
`ReviewEvidenceJson`; `ReviewEvidenceSelfTest` has independently authored success and
corruption fixtures. Existing GateResult v1 remains unchanged. Artifact paths recorded
by required gates must be repository-relative existing files under this run; normalize
legacy absolute artifact paths in a separate authorized producer update, never by
rewriting a gate result just to make it pass.

## Compact review record v2

```json
{
  "schemaVersion": 2, "runId": "example-review",
  "requestSha256": "<64 hex>", "snapshotSha256": "<64 hex>",
  "reviewerId": "independent-agent-id", "verdict": "Approve",
  "gateEvidence": [{"path": ".harness-runs/example-review/gate-results/function.json", "sha256": "<64 hex>"}],
  "ruleCoverage": [{"id": "naming", "status": "Satisfied", "evidence": "HarvestAction.cs:12-24; changed declarations follow Naming."}],
  "findings": [],
  "summary": {
    "responsibilities": "Existing owners preserved; implementation changed only.",
    "dependencies": "No new dependency direction.",
    "remainingRisks": "Play Mode behavior is outside this example gate."
  }
}
```

Coverage contains exactly one row per requested rule: `Satisfied`, `Violated`,
`NotCovered`, or explicitly permitted `NotApplicable`, always with nonempty evidence.
Findings use exactly `id`, `severity` (`Critical|Major|Minor`), `ruleId`, `file`,
`evidence`, `recommendation`, `origin` (`Introduced|PreExisting`). Critical/Major always
block, including pre-existing issues explicitly made acceptance conditions. Pre-existing
unrelated debt belongs in remainingRisks, not an excuse for unrelated repairs.
Violated needs a matching blocking finding and ChangesRequested. NotCovered forbids
Approve. Otherwise ChangesRequested requires a blocking finding, InsufficientEvidence
requires NotCovered, and Approve requires neither. Normal evidence stays brief; expand
only findings, exceptions and uncertainty. No separate essay or additional reviewer.

## Limits

Hashes prove byte identity inside selected roots, not that the selected roots or tests
are sufficient, that an AI actually read a file, or that its reasoning is correct.
The root must confirm author/reviewer identities using collaboration records; strings
in JSON cannot authenticate them. Trusted root control of pinned hashes is required.
No sandbox/CI protection against a same-permission actor rewriting the verifier is added.
Snapshot and final comparison do not detect a temporary change restored between checks;
run sequentially in an unchanged/isolated candidate. Out-of-root changes remain the
Scope Gate's responsibility. Review Pass is never blanket acceptance of the whole project.
