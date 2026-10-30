# Bounded Remediation

Use after an existing check fails, required review requests changes, or verification is blocked. Ordinary work uses check/finding evidence; only a selected extended workflow needs GateResult fields and full-profile reruns.

## One Shared Budget

The budget set during scoping belongs to the whole task, across roles, worker replacements, check failures and review findings. For example, with two attempts total, a code correction followed by an assembly correction uses both; neither worker receives another two attempts.

One attempt is one bounded corrective candidate revision prompted by failed checks or blocking findings, not each file edit, command or tool call. Root agrees its scope and counts it once when the revision starts changing the candidate, including an unsuccessful partial correction. A coordinated revision may cover several roles only when root bounds it as one correction beforehand; later corrections cannot be bundled retroactively to evade the limit.

Investigation, evidence-only repair and a rerun without candidate mutation do not consume candidate attempts. They still follow the stopping rules below. Never reset the ledger on reassignment or silently increase the budget.

Keep the initial budget, attempts used, failed check/finding evidence, correction/changed paths and outcome in the single task record. Link existing logs; do not create separate retry reports.

## Candidate Defect or Blocking Finding

1. Confirm evidence belongs to the current candidate. Review findings must come from an eligible required reviewer and be evidenced Critical/Major blockers.
2. Identify the cause from the failed condition, expected/actual values, affected files and concrete defect category. Ignore only volatile details such as timestamps or temporary paths.
3. If the same cause remains or returns after a correction, stop even with budget left. Also stop if the budget is exhausted.
4. Assign the smallest justified correction within authorized paths, requirements and permissions to the owning role. Root/reviewer do not implement it. Scope or authority expansion needs the user's decision.
5. Count the correction once, inspect the actual resulting diff and invalidate evidence whose inputs could be affected.
6. Rerun affected ordinary checks; reuse unaffected current evidence. Changed reviewed code/contracts require fresh independent review after current checks succeed. For strict protocols, follow [extended-workflows.md](extended-workflows.md).

Do not weaken expected values, remove checks, change fixtures, edit the accepting validator or omit a review condition to obtain approval. Harness/verification development is a separate explicitly requested scope with independent validation, not candidate remediation.

## Infrastructure and Evidence Blockers

A failed execution environment, missing/malformed evidence or unavailable runner does not prove a candidate defect. Preserve logs, distinguish it from an actual failed check, and do not modify candidate code/assets on that basis.

Correct the environment/evidence only within existing authority; rerun the relevant check after the blocker is resolved. Do not consume candidate budget for this repair, invent a passing result, or write a replacement runner. Stop if the cause remains unknown or the same blocker recurs after one corrective action; new permission or external dependencies require reporting the blocker and the needed decision.

## Freshness

A correction invalidates checks and review whose inputs could be affected, including prior Pass/Approve results. A document/log-only repair does not invalidate unrelated compile evidence, but a changed requirement or dependency can invalidate review/check coverage. Re-establish input relevance and freshness rather than rerunning everything or assuming everything survives.

Changing verification criteria, policy or fixtures never refreshes acceptance for the candidate that motivated that change. Do not report completion while a required current check or review is missing, failed or blocked.
