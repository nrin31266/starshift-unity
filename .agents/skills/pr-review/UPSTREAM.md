# Upstream Skill Provenance

- **Source repository**: `https://github.com/b-mendoza/agent-skills`
- **Source skill path**: `skills/review-pull-request/`
- **Upstream commit SHA**: `14d79d993b448aac50f2f7493e0903e629350155`
- **Vendor date**: 2026-09-19
- **Baseline**: fresh upstream clone; custom applied selectively on top

## Customizations Over Upstream

1. **Isolated PR worktree lifecycle**: Review execution and verification run inside a temporary detached Git worktree; the original working tree is treated as immutable and strictly protected. Scripts: `prepare-pr-worktree.sh`, `cleanup-pr-worktree.sh`.
2. **Persistent external artifact storage**: Review artifacts are stored outside the Git repository in `${XDG_STATE_HOME:-$HOME/.local/state}/pr-review/<owner>-<repo>/` to prevent untracked file pollution.
3. **3-tier severity model**: Replaced upstream `nit`/`blocking` vocabulary with `BLOCKER`, `IMPORTANT`, and `SUGGESTION`. Suggestions are summary-only bullets — no individual inline GitHub threads (cheap suggestions policy).
4. **Deterministic top/bottom verdicts**: `🔴 BLOCK`, `🟡 PASS WITH NOTES`, `🟢 PASS` mapped to GitHub review actions with a mandatory human preview gate.
5. **Review modes (`normal` / `strict`)**: `normal` (default, 1–3 dimensions) vs `strict` (2–4 dimensions, deeper failure/boundary inspection).
6. **Re-review continuity and finding lifecycle**: Extended finding lifecycle (`NEW`, `STILL_OPEN`, `RESOLVED`, `WITHDRAWN`, `OBSOLETE`), stable semantic fingerprints (`domain:behavioral-defect`), hidden HTML markers for thread tracking, and incremental diff analysis (`previous_head...current_head`). Script: `collect-pr-review-history.sh`.
7. **Generic project-profile routing**: Context collector reads `references/project-profile.md` for project identity, default language, and context-routing entry points. Profile is thin — no architecture rules embedded. Core files remain project-agnostic.
8. **One canonical review body**: Review posts as one GitHub review event (one body, zero per-suggestion inline threads). Path/line evidence remains in Markdown. Self-review detection before preview (COMMENT event fallback).
9. **Generic verification**: Discovers commands from CI config and repo instructions — does not hardcode project-specific commands. Distinguishes TEST/BUILD/TYPECHECK/LINT/GUARDRAIL/SYNTAX/REPRODUCTION and PASS/FAIL/NOT RUN/REPRODUCED.
10. **Generic tracking markers**: New reviews embed a global tracking marker `<!-- agent-pr-review reviewed-head: <sha> findings: <fingerprints> -->` at the end of `CANONICAL_BODY`, and per-finding markers `<!-- agent-pr-finding fingerprint: <fp> severity: <sev> lifecycle: <lc> path: <path> line: <line> title: <title> -->` across both previous and new findings in re-reviews to guarantee complete machine-readable snapshots across multi-turn review chains. Legacy `<!-- lyreo-review ... -->` markers are recognized on read for backward compatibility; new reviews never create new `lyreo-*` markers.

## Tracking Marker

New reviews:
- Global marker (end of body): `<!-- agent-pr-review reviewed-head: <sha> findings: <fingerprints> -->`
- Per-finding marker: `<!-- agent-pr-finding fingerprint: <fp> severity: <sev> lifecycle: <lc> path: <path> line: <line> title: <title> -->`

Legacy (read-only): `<!-- lyreo-review ... -->`

## Portability Answer

> If I copy this skill folder to another project, what must I edit?

**Only `references/project-profile.md`** (and optionally the skill `name`/`description` in `SKILL.md` frontmatter). Nothing else in core subagents or scripts.
