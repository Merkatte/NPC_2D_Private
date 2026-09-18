# ReviewResult Contract

## Version selection

A pinned review request plus candidate snapshot selects compact v2, whose exact fields
and consistency rules are in `Tools/NpcHarness/ReviewEvidence.md`. Do not emit both
formats or append a narrative report. Use the v1 format below only for legacy callers
without that envelope; v1 does not satisfy `accept-review`.

Return a single JSON object. Keep field names and enum values exact so the orchestrator can consume the result without interpreting prose.

```json
{
  "schemaVersion": 1,
  "reviewId": "review-unique-id",
  "candidateRunId": "run-id-from-gate",
  "reviewerRole": "independent-read-only",
  "verdict": "Approve | ChangesRequested | InsufficientEvidence",
  "summary": "Concise evidence-based decision",
  "deterministicGate": {
    "profile": "profile name",
    "profileVersion": 1,
    "status": "Pass",
    "preserved": true,
    "evidence": "GateResult path or other unambiguous identifier"
  },
  "freshnessEvidence": "Candidate digest, or the exact fallback evidence used to establish that the candidate did not change after gating",
  "findings": [
    {
      "id": "REVIEW-001",
      "severity": "Critical | Major | Minor",
      "blocking": true,
      "title": "Short defect statement",
      "evidence": "Specific observed fact and why it matters",
      "file": "repo/relative/path",
      "line": 123,
      "recommendation": "Smallest concrete corrective direction"
    }
  ],
  "acceptanceCoverage": [
    {
      "condition": "Confirmed acceptance condition",
      "evidence": "Gate check ID, diff location, log, or direct observation",
      "status": "Satisfied | Violated | NotCovered"
    }
  ],
  "reviewedFiles": ["repo/relative/path"],
  "reviewedDocuments": ["repo/relative/project-document.md"],
  "unverifiedRisks": ["Specific remaining uncertainty"]
}
```

## Invariants

- `candidateRunId` must equal the supplied gate run ID. Never invent one.
- `deterministicGate.status` records the gate verbatim. This review requires `Pass`; otherwise use `InsufficientEvidence` and preserve the actual supplied status rather than fabricating `Pass`.
- `preserved` is `true` only when the review did not mutate the candidate or reinterpret the gate.
- `Critical` and `Major` findings always use `blocking: true` and require `ChangesRequested`.
- `Minor` findings always use `blocking: false` and cannot alone prevent `Approve`.
- `InsufficientEvidence` may list evidence gaps in `unverifiedRisks`; do not manufacture a candidate defect to explain missing evidence.
- Use a repository-relative `file`. Use `line: null` only when the evidence is inherently file-wide or a serialized object without a stable line.
- Each finding describes one defect. Evidence states what was observed; recommendation states what should change. Do not combine them into vague commentary.
- Every confirmed acceptance condition appears once in `acceptanceCoverage`. `Violated` requires a blocking finding and `ChangesRequested`; `NotCovered` prevents `Approve` and yields `InsufficientEvidence` unless another blocking defect already requires changes.
- `freshnessEvidence` states the supplied candidate digest when available. Without a digest, record the run ID, changed-file match, timestamps, logs, and unchanged-worktree evidence actually checked; do not imply cryptographic identity.
- `reviewedFiles` lists candidate or dependency files actually inspected. `reviewedDocuments` separately lists project requirements and architecture documents inspected.

## Severity

- `Critical`: likely data loss, security/permission breach, broken project loading/build, or corruption of serialized state.
- `Major`: violates confirmed scope or acceptance, causes a likely runtime/Editor defect, breaks a required integration, or leaves a material gate coverage hole.
- `Minor`: localized maintainability, clarity, or low-impact robustness issue with no demonstrated acceptance failure.
