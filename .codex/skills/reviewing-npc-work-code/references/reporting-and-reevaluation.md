# Reporting and Reevaluation

## One Maintained Result

Use PublicMD/Status/Code_Evaluation_Result.md by default, or the user's requested artifact location. Read the relevant previous record before assessing and before updating it; preserve unrelated user edits. Domain workers return evidence to the coordinator, which is the single writer. Do not maintain copies of the same detailed findings in several reports.

Scale the report to the request. Keep:
- assessment date, requested scope and depth, actual inspected coverage and exclusions;
- concise priority judgment and evidence-backed strengths where present;
- findings with identity, type, impact/urgency, file/function evidence, consequence and recommended direction;
- checks actually performed/reused with outcomes/log locations, and remaining questions/limitations;
- reassessment outcomes and preserved previous findings not checked this time.

Keep each detailed finding in one place. Summaries or cross-domain indexes can refer to its ID; do not repeat the full text in severity, file and cross-cutting sections. Do not require empty severity headings, positive findings or an issue quota. Never interpret overview coverage, compilation or evaluation completion as a detailed quality pass.

## Finding Type and Importance

Use:
- **확인된 문제 / Confirmed issue:** code, serialized state and applicable rules establish the violation or conditional failure path. Includes evidenced convention/ownership violations.
- **설계 개선 제안 / Design improvement:** explain the expected benefit and change cost/tradeoff; do not present preference or mere resemblance as a defect.
- **추가 확인 필요 / Needs confirmation:** identify the specific missing evidence and why it matters.

Keep type separate from severity and reassessment status. Preserve existing Critical/High/Medium/Low labels when updating the existing report; justify importance from actual impact and urgency rather than introducing a new scoring system. Uncertain issues must not be counted as confirmed severe defects. Distinguish source-supported conditional behavior from an actually reproduced failure.

Retain existing finding IDs (for example F01 or C01) when referring to the same issue. Give new findings unused IDs so later requests such as “check issue 2 again” remain traceable. Do not renumber earlier findings to match a new severity order. If identity is genuinely ambiguous, clarify the target rather than silently checking a different issue.

## Reevaluation Procedure

For a domain/feature reassessment, examine its previous findings and current structure, including new problems. For “only check whether issue F02 was fixed,” inspect that issue and its affected path, not the whole domain.

1. Read the previous claim, evidence, date and triggering conditions.
2. Compare current code/serialized state to the original cause and failure path. A change or a worker's “fixed” report is not proof of resolution.
3. Inspect relevant effects of the correction. If preventing duplicate subscriptions now prevents resubscription after reopening, record the old issue's outcome and the new issue separately.
4. Update the finding's result, evidence, last checked date, method and limits.

| Result | Meaning |
|---|---|
| 해결 확인 / Resolved | Evidence shows the original cause or failure path is removed |
| 일부 해결 / Partially resolved | Some paths are corrected but affected conditions remain |
| 미해결 / Unresolved | The previous issue still holds |
| 확인 보류 / Inconclusive | Inspection was attempted but evidence is insufficient |
| 미재평가 / Not reassessed | Outside this run's scope and not inspected |
| 지적 철회 / Withdrawn | Evidence shows the earlier claim itself was mistaken; retain the reason |

“Resolved” states how it was established: code/serialized inspection, relevant existing checks, or explicitly authorized runtime checks. Do not claim unperformed play verification. If a scoped item could not be investigated or completed, record that coverage gap and use Inconclusive where a conclusion is lacking; do not imply it was intentionally outside scope.

## Preserve History Without Repeating the Audit

- Keep the previous assessment date, last known finding result and evidence for untouched items. Mark their current-run coverage Not reassessed; do not replace their last known outcome with a fresh judgment.
- Partial reviews update only inspected findings/coverage. Do not relabel an older whole-project conclusion with today's date. Retain it as dated historical context, clearly separate from the current scope.
- Resolved/withdrawn items remain traceable with a short reason and date. Do not delete stale-looking findings without checking them, invent an earlier date, or silently rewrite the historical claim.
- When an old report lacks these fields, retain its IDs/date/text and add the minimum current status or dated section. Do not re-evaluate unrelated findings just to migrate its format.
- Evaluation complete, no confirmed defects, fixes implemented, and resolution verified are different claims. Report only the claims supported by current evidence.

Reevaluation does not authorize unrelated scans, full regression reruns, new verification code or implementation. Use the shared evidence limits in [review-workflow.md](review-workflow.md). If fixes were requested, the root hands the recorded assessment and authorized scope to orchestrate-unity-work after the assessment is finished.
