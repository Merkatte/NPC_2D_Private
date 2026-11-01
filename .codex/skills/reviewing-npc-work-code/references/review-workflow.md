# Shared Code Assessment Criteria

This reference owns common assessment criteria, document routing and evidence limits. Domain skills add domain questions without copying this checklist. A bounded candidate reviewer may consult these criteria while retaining its own scope, verdict and record contract; this reference does not launch an audit or require an extra report.

## Establish the Evidence

- Use PublicMD/ProjectStructure.md and recursive Systems READMEs to reach the relevant owning leaves and their minimum code/asset scope.
- Read PublicMD/CodeConvention.md for C# assessment. Read PublicMD/ARCHITECTURE.md when judging cross-feature ownership or dependency direction.
- Consult Game_Plan.md/SPEC.md only when a finding depends on intended game rules. Keep unconfirmed rules TBD; do not invent a rule to declare a defect.
- Inspect actual current code, necessary callers, consumers and comparable implementations. For change reviews, establish the actual diff/baseline and distinguish introduced issues from existing debt. Preserve unrelated dirty changes.
- Open scene/prefab/SO serialized data when wiring or configured values affect the finding. Code/assets establish current behavior; document disagreement is a reportable finding, not permission to edit the source or owning document.
- Do not read CLAUDE.md or .claude. Do not begin by reading all PublicMD or all code for a scoped task.

## Common Questions

Apply relevant criteria, not a per-file essay or a quota of findings:

- **Structure and responsibility:** Does each component have a coherent responsibility and appropriate runtime/data owner? Are decisions, execution, transactions and presentation mixed or duplicated? Do interfaces/layers have actual consumers and useful contracts? Documented ownership is a routing baseline, not proof that the design is good.
- **Consistency and existing capability:** Compare relevant implementations before recommending another mechanism. Identify duplicated behavior and overlapping authority. Similar syntax alone does not justify merging different rules; consistency with a flawed existing pattern does not justify that pattern.
- **Dependencies and contracts:** Examine actual calls/data flow, permitted dependency direction, lifetime, failures/cancellation and boundary assumptions. A folder name or interface count alone proves neither compliance nor poor architecture.
- **Conventions and maintainability:** Apply current conventions/local-style precedence to naming, serialized fields, null handling, comments, class organization, enums and data contracts. Identify dead APIs or stale assumptions from actual usage, not a search miss alone.
- **Data and tuning:** Look for unexplained constants, duplicated tuning and misplaced mutable scene state/shared definitions. Recommend a data abstraction only when the actual responsibility warrants it.
- **Unity behavior:** Where relevant, inspect lifecycle subscriptions, pooling/reset, Unity-object liveness behind interfaces, serialized references and transaction cleanup. Assess expensive lookups/allocations from their real use; do not claim measured performance without measurement.

Record justified strengths as well as actionable problems. A convention or ownership violation with concrete rule/code evidence can be a confirmed issue without a runtime crash. Distinguish confirmed issues, design proposals and questions requiring more evidence using the reporting contract.

## Bounded Verification

Use direct code/serialized inspection, targeted searches and relevant existing checks. Reuse successful check evidence only when covered inputs, relevant dependencies, configuration and environment remain applicable; who ran it is not a reason to repeat it. If freshness cannot be established, do not claim it as current.

Do not unconditionally run a build or full regression suite for every assessment or each domain worker. Coordinate any necessary existing compile/check execution once for shared inputs. A new or affected required check still needs its actual result; do not hide failures, waive candidate acceptance requirements or call unavailable checks successful. Record commands/results or reused log locations and verification gaps.

Do not create or expand verification-only runners, tests, validators or harness code without a separate explicit request. Automatic Play Mode, game-screen capture/interpretation and their retries also require a separate explicit request. Existing generic QA plans do not supply that authorization. Missing bespoke automation alone does not require new tools or prevent an honest bounded assessment.

Inspection may establish a conditional failure path; distinguish that from an observed play-session failure. Compile success does not establish runtime correctness or architectural quality. Report evaluation findings and limitations instead of starting corrective loops.
