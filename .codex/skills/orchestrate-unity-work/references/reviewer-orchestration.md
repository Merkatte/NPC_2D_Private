# Reviewer Orchestration

Use the policy during scoping, and invoke review after current required checks succeed. This reference owns reviewer selection, primary-evidence handoff and verdict routing; reviewing-unity-candidate owns review judgment and its result contract.

## Review Policy

Record the policy and reason:

- `RequiredByRequestOrSkill`: user, applicable skill or project policy requires review. This includes every C# change, even temporary helpers.
- `RequiredByRisk`: structural, integration, serialization, lifecycle, persistence, transaction or material uncovered acceptance risk requires review.
- `NotRequired`: low-risk non-code work is covered by relevant checks and no overriding requirement calls for review.

Do not downgrade policy to avoid review after implementation. Upgrade when material risk is discovered. Missing bespoke automation or excluded automatic play/screen QA alone does not require extra reviewers or verification development.

Use a few applicable categories, always including conventions, ownership and dependency direction for C#. Add cross-feature architecture when relevant. One reviewer covers these categories; do not create an agent for every rule.

Unrequested responsibility moves, new layers or shared-contract changes need user direction before implementation. Updating current-state documentation does not authorize weakening normative rules. For an authorized rule change, preserve the approved prior rule and authorization in scope evidence.

## Independent Reviewer and Evidence

Create a separate read-only collaboration agent with `fork_turns="none"`, explicitly using $reviewing-unity-candidate. Confirm its identity against collaboration records: it must not be any candidate implementation/remediation worker. Wait for a free slot instead of repurposing an author. Keep it read-only throughout the candidate lifecycle and await its actual result; launching a review is not completion.

Give only primary evidence:

1. original user request and confirmed scope, exclusions and acceptance conditions;
2. relevant project rules/documents;
3. root-inspected actual diff, including relevant untracked files;
4. current successful required check results, raw compile logs for C#, scope/reference evidence and their input/coverage boundaries;
5. freshness evidence tying those results to the current candidate.

Ordinary freshness uses Git baseline/diff, observed changes and corroborated check inputs/environment; per-file hashes are not mandatory. Do not pass WorkerReports, implementation summaries/reasoning, claims of completion, suspected findings, expected verdicts or preferred fixes.

## Ordinary Result Intake

Require one compact ordinary record from reviewing-unity-candidate. Confirm reviewer identity, candidateBasis/current diff, reviewed files, required ruleCoverage, findings/severity and verdict. Check that evidence covers the approved conditions and belongs to the current candidate.

Missing, malformed, stale, contradictory or self-reviewing results mean InsufficientEvidence. Do not require GateResult IDs or accept-review for ordinary review. Only a selected extended workflow uses the contracts in [extended-workflows.md](extended-workflows.md).

## Verdict Routing

- **Approve:** satisfies review for the unchanged reviewed candidate with current successful required checks. Report Minor findings as non-blocking. Approval cannot replace a required check or overrule a failure.
- **ChangesRequested:** use evidenced blocking Critical/Major findings for the smallest authorized same-role correction under [remediation-loop.md](remediation-loop.md). Root and reviewer do not patch implementation. Refresh affected ordinary checks and actual diff, then obtain fresh independent review after successful verification. Selected extended workflows retain their stricter refresh rules.
- **InsufficientEvidence:** do not complete. Repair the evidence gap within current authority and resubmit primary evidence against still-current checks. Candidate changes require affected checks to be refreshed first. Evidence-only repair does not consume candidate correction budget; stop rather than loop if the same gap recurs after one correction.

When a blocker requires new authority or an unavailable external action, report it and seek only the needed decision. Do not commission extra reviews or essays solely for reporting.
