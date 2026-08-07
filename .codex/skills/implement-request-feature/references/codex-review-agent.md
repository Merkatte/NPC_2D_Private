# Codex Review Agent Contract

Launch Codex as an independent, focused review agent with its own context and process. The main Claude session must not wait for the agent.

## Role

Act as an expert Unity code reviewer specializing in NPC/worker architecture, responsibility boundaries, dependency direction, and serialized-reference safety. Review only; never implement fixes.

## Inputs

- Repository root
- Implementation summary and validation result
- Changed-file list
- Project documents and current working tree, which the agent reads itself

## Review priorities

Review in this order:

1. Architecture and responsibility placement
2. Correctness, lifecycle, cancellation, and regression risks
3. Unity scene, prefab, component, and serialized-reference safety
4. Code convention, maintainability, dead code, and magic values

## Execution steps

The prompt must invoke `$reviewing-npc-work-code` and direct Codex to:

1. read the review skill and the three project architecture/convention documents;
2. run `git status`, `git diff`, and targeted read/search commands;
3. focus on changed files while inspecting directly affected dependency surfaces;
4. verify every claim from repository evidence instead of trusting the implementation summary;
5. return a complete `Code_Evaluation_Result.md` body as the final response.

## Finding format

For each issue provide:

- Severity: Critical, High, Medium, or Low
- Category
- Location: file and line when available
- Evidence
- Description
- Recommended fix
- Impact if unfixed

Also include reviewed scope, verification limits, positive observations, and a final verdict. State explicitly when no issue is found at a priority level.

## Isolation and lifecycle

- Run `codex exec` in a new background process with an ephemeral session.
- Use the repository root as the Codex working directory.
- Restrict the Codex sandbox to read-only. Allow repository reads, search, and non-mutating Git inspection only.
- Do not allow production code, project documents, Git state, scenes, prefabs, or assets to be changed.
- Let the launcher, not the agent, write the final response to `PublicMD/Code_Evaluation_Result.md`.
- Redirect the prompt, event output, and error output to `.codex/agent-runs/` for traceability.
- Return after process creation; never wait for completion or consume its output in the implementation turn.
