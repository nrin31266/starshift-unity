# Project Review Profile

> This is the ONLY file to edit when copying this skill to another project.
> Core skill files (SKILL.md, subagents, scripts, other references) are project-agnostic.
> Read this file once in `pr-context-collector`, before proposing dimensions.

## Default Language

Natural Vietnamese. Use if `LANGUAGE_STYLE` is not explicitly provided.

## Default Posting Mode

`auto-post-verified`. Use if `POSTING_MODE` is not explicitly provided.
(Resolution order: 1. explicit invocation input -> 2. project-profile default -> 3. generic safe fallback `post-after-confirmation`).

## Context Entry Points

Read these files as routing indexes to find owner documents:

1. `AGENTS.md` — engineering contract and mandatory prohibitions (project root)
2. `docs/README.md` — routing index to domain owner documents

If an entry point does not exist in the current project, skip gracefully.

## StarShift Routing

- Unity project root: `app/`; documentation: `docs/`; offline Editor verification helpers: `tools/unity/`.
- Route player/input/combat changes through the player combat owner; camera/flight changes through levels/encounters; enemy changes through enemies/bosses. Resolve exact paths and responsibility boundaries from `docs/README.md`, rather than copying contracts here.
- For C# runtime and serialized asset changes, inspect the connected prefab/scene wiring and `.meta` GUID references. Read Unity YAML as evidence; make native asset changes through Unity MCP/Editor.
- Discover verification commands from `tools/unity/README.md`, project metadata and any CI configuration. Keep static checks distinct from Unity runtime evidence.
- A running Unity Editor belongs to its open project path. Do not use an Editor attached to the caller checkout to claim verification of the isolated review worktree. Record unavailable Unity checks as `NOT RUN` with the reason; prior author-reported Play Mode results remain separate context.
- Do not upgrade Unity/packages, implement deferred gameplay, build Android, or alter assets merely to perform a review. Follow repository instructions and the requested scope.

## Review Posture

- Production-minded project: prioritize correctness, regressions, contracts, security when relevant, and maintainability.
- Avoid unnecessary enterprise over-engineering in findings.
- Useful verification: discover commands from CI config and build metadata — do not hardcode.
