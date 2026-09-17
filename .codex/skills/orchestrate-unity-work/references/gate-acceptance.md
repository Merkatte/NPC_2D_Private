# Gate Acceptance

Use this reference when selecting a deterministic gate or deciding whether its evidence accepts a candidate.

## Project Contract

The serialized contract is `Tools/NpcHarness/Schemas/gate-result.schema.json`. The Unity types and builder are under `Assets/Editor/NpcHarness/Core`.

A result must identify at least:

- `schemaVersion`, `runId`, `profile`, and `profileVersion`;
- top-level `status` and `success`;
- each check's stable ID, status, expected value, actual value, and message;
- artifacts, changed files, and start/finish timestamps.

Current HarnessBeacon profiles demonstrate the contract, but they do not validate unrelated Unity work. `HarnessBeacon.Structure` checks scene structure and `HarnessBeacon.PlayMode` checks runtime behavior. The cross-platform runner exposes only profiles explicitly registered in `Tools/NpcHarness`; a new task type needs its own independently tested profile before its candidate can be accepted.

## Acceptance Decision

Accept only when all conditions hold:

1. The result exists and conforms to the schema.
2. Its run, profile, and version are the ones selected during scoping.
3. `status` is `Pass` and `success` is `true`.
4. Every required check is present and has `Pass` status.
5. The process exit code, when a process was used, agrees with the serialized result.
6. Required logs and artifacts exist and belong to this run.
7. `changedFiles` and the actual worktree contain no unauthorized or unexplained mutation.
8. The candidate has not changed since the gate executed.

Treat a missing result, malformed result, empty required check set, exit/result mismatch, or absent evidence as not accepted. Never infer success from console wording alone.

## Status Meaning

- `Pass`: the selected deterministic checks accepted this exact candidate.
- `Fail`: at least one candidate property did not meet the gate expectation.
- `InfrastructureError`: the gate could not make a trustworthy candidate judgment because its environment or execution failed.

An infrastructure error is a blocker. Do not rewrite it as candidate failure, and do not treat a failed check as infrastructure trouble merely to avoid rejection.

## Independence and Freshness

The author of a candidate must not alter its accepting profile, validator, expected values, or fixtures in the same acceptance run. Harness changes require their own prior baseline or independent self-test.

Any edit to code, assets, project settings, packages, gate inputs, or relevant environment state after the gate run expires the result. Run every affected gate again against the new candidate.
