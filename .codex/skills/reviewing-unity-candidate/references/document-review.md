# Document-based review without a full audit

Use the categories selected in the pinned request. Route via ProjectStructure to only
the owning Systems leaves and directly affected dependencies. CodeConvention supplies
general C# rules; ARCHITECTURE supplies cross-feature responsibility/dependency rules.
The domain audit categories in `../../reviewing-npc-work-code/references/review-workflow.md`
are a checklist reference, not an instruction to run another review or overwrite its
project-wide report.

Every C# candidate, including temporary helpers, must cover conventions, ownership
and dependency direction. In ordinary review use the same categories with current
common-check evidence and current Git baseline/diff; a pinned request is not mandatory unless
that workflow was selected. Check the actual diff and source, not a worker's claimed
compliance. One independent reviewer covers these categories; do not spawn one per rule.

- Conventions: changed declarations, local-style precedence, serialization and naming.
- Ownership: changed classes remain within documented responsibilities and owning leaves.
- Dependencies: actual new references/calls, not just folder placement; flag unapproved
  responsibility moves, shared contracts or additional layers.
- Relevant runtime risks: lifecycle, pooling, transaction, null and serialized wiring
  only where affected. Functional gate coverage is not presumed from a compilation pass.
- Documentation: actual flow and ownership agree with owning docs; changing docs does
  not legitimize an unauthorized design deviation.

One short evidence row per requested category is enough when satisfied. Cite meaningful
code locations and source sections; do not merely assert that a document was read.
Expand actual violations/uncertainties only. NotApplicable requires both prior permission
in the request and an explanation. Never use it to hide missing required verification.
Distinguish introduced defects from pre-existing debt; do not request unrelated cleanup.
For genuine ambiguity, report NotCovered rather than inventing a rule or claiming proof.
Under the ordinary SceneWork route, missing bespoke automated coverage belongs in
remainingRisks and does not itself require InsufficientEvidence or new test tooling.
Required compile/scope/reference evidence and actual defects remain subject to review.

Summarize responsibilities, dependencies and remaining risk once in the compact record.
Use "unchanged" when supported. No full-project scan, per-line compliance essay, second
reviewer, or parallel convention/architecture reviews merely to populate the record.
