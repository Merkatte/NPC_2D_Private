# Evaluation Coordination

## Decide Whether to Split

Evaluate these together:

1. Can each evaluator receive an independent question and primary responsibility?
2. Is there enough investigation for each to justify assignment/report/integration cost?
3. Will splitting avoid, rather than mostly duplicate, tracing the same code and execution flow?

Keep small fit checks, narrow symptom traces and ordinary overview prioritization with one evaluator using relevant domain skills. Split large independently assessable domains; a large single domain can be split by independent subfeatures. Whole-project detailed assessment normally benefits from domain division, without requiring all agents to run simultaneously.

Do not impose a fixed file/line threshold, agent count or a separate agent for every skill. If scope grows during investigation, hand over already inspected evidence and unresolved questions rather than restarting the assessment.

## Assign Ownership and Boundaries

Use Systems leaf ownership to choose a primary evaluator for each functional scope. Source directories, UI appearance or the presence of NPCs do not determine ownership. The ownership map itself may be criticized with evidence.

For example, recruitment is led by Settlement. UI may examine common input/subscription/window lifecycle; NPC may examine actor initialization/registration/pooling. A small recruitment review can use one evaluator for all three perspectives. Economy owns shared resource storage/contracts, while the consumer domain owns recruitment/construction cost rules.

Give each evaluator a concise assignment containing:
- original user request, scope/depth and exclusions;
- primary functional responsibility, independent questions and assigned cross-boundary questions;
- relevant domain skill, document/code entrypoints and prior finding IDs/records;
- existing check evidence and what has already been inspected;
- read-only code/asset authority and a return destination for findings/coverage, not shared-report editing.

Start delegated evaluators with fresh contexts (`fork_turns="none"`) and the bounded assignment, not the entire conversation. Prior findings are reassessment inputs, not expected conclusions. Do not route assessment workers through implementation WorkerAssignment/gate schemas. The separate candidate reviewer keeps its own independence/evidence contract.

External code can be read as needed. If an evaluator finds a concrete issue outside its primary responsibility, pass the evidence to the primary evaluator/coordinator without silently expanding to a full assessment of that domain.

Assign an owner to each material connection question, such as recruitment result delivery to UI. The primary evaluator ensures the connection was assessed and may give the specialist the actual question. Do not assume that separate internal reviews cover the interface between them.

## Integrate Once

Return actual inspected scope, findings with evidence, relevant existing issue outcomes, boundary questions and gaps. Domain workers do not concurrently edit the consolidated report or start their own implementation/reviewer chains.

The coordinator:
- merges the same defect and preserves supporting evidence; identical symptoms with different causes remain separate findings;
- reconciles advice against actual responsibility, callers and common rules; unresolved design choices remain explicit rather than being settled by preference;
- verifies material evidence and coverage, including boundary gaps, without repeating all domain investigations;
- commissions further investigation only for a concrete unanswered question or conflicting evidence.

Run any justified shared checks once and share applicable results. Do not rerun the entire assessment after each finding, ask every domain to run the same build, or recursively spawn more evaluators merely to fill a report.
