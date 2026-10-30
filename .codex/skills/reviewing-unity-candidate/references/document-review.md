# Document-Based Candidate Review

Use the categories and acceptance conditions in root's approved scope. A pinned request is needed only for a selected pinned workflow. Route through PublicMD/ProjectStructure.md and each folder README to the owning Systems leaf; read its minimum scope and only directly affected dependencies.

CodeConvention supplies C# rules; ARCHITECTURE supplies cross-feature responsibility/dependency rules when applicable. Do not read CLAUDE.md or .claude. Check actual source and serialized state, not the author's claimed compliance.

## Required Code Categories

Every C# candidate, including temporary helpers, needs:

- **Conventions:** changed declarations, applicable local-style precedence, naming and serialization rules.
- **Ownership:** changed classes remain within documented responsibilities and their owning Systems leaves.
- **Dependencies:** actual changed references/calls and permitted direction, not folder placement alone. Identify unapproved responsibility moves, shared contracts or extra layers.

Inspect lifecycle, pooling, transactions, null handling and serialized wiring where affected. Compilation does not prove functional behavior. Check whether current flow/ownership agrees with the owning documentation; updating a document does not legitimize an unauthorized design change. For an authorized normative-rule change, use the approved prior rule and authorization supplied by root.

For a concrete NPC domain concern, the relevant portion of [review-workflow.md](../../reviewing-npc-work-code/references/review-workflow.md) may supply a checklist. It is not an instruction to run a full audit, add a reviewer or update the project-wide report.

## Evidence and Coverage

One concise row per applicable category is enough when satisfied. Cite meaningful code locations and rule sections; "read the document" is not evidence. Expand actual violations or uncertainty, not every convention sentence.

Distinguish introduced defects from pre-existing debt. Existing issues explicitly included in acceptance still matter; unrelated cleanup is outside the review. When a material rule or condition is genuinely ambiguous, report missing required coverage rather than inventing a rule.

Use SKILL.md's ordinary verdict and record rules. Record absent bespoke automated coverage and excluded automatic play/screen QA in remainingRisks without demanding new test tooling. Keep missing mandatory compile/scope/reference evidence and actual defects blocking under the applicable verdict.

For selected strict/pinned review, use its exact coverage and result contract through [selected-gate-review.md](selected-gate-review.md). Do not apply ordinary optional-coverage exceptions to required gates or pinned rule rows.

Summarize responsibilities, dependency direction and remaining risk once. Use "unchanged" when supported. No full-project scan, per-line compliance essay or parallel convention/architecture reviews merely to populate a record.
