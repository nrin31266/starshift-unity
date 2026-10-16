# Review Workflow

> Read this file once, after input normalization, before dispatching any subagent. This is the single authoritative description of phase order, routing, repair, and terminal behavior. Keep only status summaries in the orchestrator context; raw diffs, command output, API payloads, and fetched web pages stay inside the subagent that produced them.

## Phase Sequence

| Phase | Owner | Continue on |
| --- | --- | --- |
| 1. Intake | Inline (orchestrator) | Inputs normalized; one PR chosen |
| 2. Worktree Setup | Inline (`prepare-pr-worktree.sh`) | `WORKTREE: PASS` (isolated worktree created & snapshot recorded) |
| 3. Context | `pr-context-collector` | `CONTEXT: PASS` (with history digest & dimension proposal) |
| 4. Chunk review | One `chunk-reviewer` per dimension | Every chunk returns `CHUNK: PASS` or `CHUNK: NO_FINDINGS` |
| 5. Adjudication | `finding-adjudicator` | `ADJUDICATE: PASS` or `ADJUDICATE: NO_FINDINGS` |
| 6. Targeted Verification | Inline (in `WORKTREE_PATH`) | Targeted checks executed or marked `NOT RUN` → `VERIFICATION_CHECKS` |
| 7. Comment Drafting | `comment-drafter` | ALWAYS dispatched → `COMMENTS: PASS` (renders `CANONICAL_BODY` with `## Verification`) |
| 8. Review Verification | `review-verifier` | `VERIFY: PASS` (quality gate; single `Fix target` on repair) |
| 9. Review Writing | `review-writer` | `WRITE: PASS` (saves exact body byte-for-byte & writes `.meta.json` sidecar) |
| 10. Exact Preview & Authorization | Inline (orchestrator) | Effective event resolved; authorized via human preview approval or verified-auto |
| 11. Review Posting | `review-poster` | `POST: PASS` (1 review event, 0 inline comments, 0 thread replies; skipped in `draft-only`) |
| 12. Artifact Update | `review-writer` (update mode) | `WRITE: PASS` (updates `.meta.json` sidecar only; `.md` file is never edited) |
| 13. Worktree Cleanup | Inline (`cleanup-pr-worktree.sh`) | Linked worktree pruned & caller workspace integrity verified (on ALL exit paths) |

## State Envelope

Carry this compact state between phases:

```text
Inputs: PR_URL, OUTPUT_FILE, POSTING_MODE, REVIEW_MODE, LANGUAGE_STYLE, REVIEW_FOCUS
Worktree: WORKTREE_PATH, SNAPSHOT_FILE, PR_HEAD_SHA
Dimensions: <1–3 dimension names (normal) or 2–4 (strict) from CONTEXT: PASS>
Existing-comment digest: <summary reference, held by adjudicator inputs>
Latest status: <CONTEXT | CHUNK | ADJUDICATE | COMMENTS | VERIFY | WRITE | POST block>
Review verdict (post-verify): 🔴 BLOCK | 🟡 PASS WITH NOTES | 🟢 PASS
Review decision (post-verify): comment | request changes | approve
Effective GitHub event: REQUEST_CHANGES | COMMENT | APPROVE (resolved before preview/posting)
Post authorization: human-approved | verified-auto | none
Authorized body SHA-256: <hash or none>
Authorized head SHA: <sha or none>
Posting status: draft | posted | cancelled | failed | stale_head
Exported artifact path: <persistent path in ~/.local/state/...>
Sidecar path: <persistent path to .meta.json>
Repair cycles: <0–2>
Narrow-context retries used: <0–1>
Status-parse retries used per dispatch: <0–1>
```

`🔴 BLOCK` / `request changes` appears only after verified BLOCKER findings exist, via `comment-drafter` and `review-verifier`.

## Fail-Closed Status Handling

Every subagent must return its documented status block. When a reply is missing its status line or the status cannot be parsed, re-dispatch that subagent once with the same inputs and a note that the status block was malformed. If the second reply is also unparseable, treat it as that phase's `ERROR` status and route accordingly. Never guess a status.

## Execution Rules

### 1. Intake & Worktree Setup

1. Require exactly one parseable GitHub PR URL, valid `POSTING_MODE` (`post-after-confirmation` [generic safe fallback], `draft-only`, or `auto-post-verified`), `REVIEW_MODE` (`normal` [default] or `strict`), `REVIEW_FOCUS` values, and a safe workspace-relative Markdown `OUTPUT_FILE` (relative, `.md`, no `..`, not under `.git/`, resolves inside the workspace).
   - Resolve `POSTING_MODE` in order: 1. explicit invocation input -> 2. project profile default (`references/project-profile.md`) -> 3. generic safe fallback (`post-after-confirmation`).
   - If multiple PR URLs are present, run `HUMAN_GATE_CHOOSE_ONE_PR`; if a valid single PR is not chosen, stop with `PR_REVIEW: NEEDS_CONTEXT`.
2. Run `../scripts/prepare-pr-worktree.sh <PR_URL>` to create an isolated detached worktree (`WORKTREE_PATH`) and capture an immutable snapshot of the original workspace (`ORIGINAL_ROOT`, `ORIGINAL_BRANCH`, `ORIGINAL_HEAD`, `git status --porcelain`). If the worktree path already exists from an interrupted run, it safely recovers it if Git confirms it is a linked worktree of this repository; otherwise it refuses unverified paths. If worktree creation fails, stop with `PR_REVIEW: NEEDS_CONTEXT`. All subsequent file reads, diff inspections, and verification checks run inside `WORKTREE_PATH`; never modify or check out PR files in the caller's working tree.

### 2. Context

3. Dispatch `pr-context-collector` with `PR_URL`, `WORKTREE_PATH`, `OUTPUT_FILE`, `REVIEW_MODE`, and `REVIEW_FOCUS`. It reads `../references/project-profile.md` for project-routing hints and language default, executes `collect-pr-review-history.sh` (parses `<!-- agent-pr-review ... -->` and legacy `<!-- lyreo-review ... -->` markers), and proposes 1–3 dimensions (normal) or 2–4 (strict). Detects incremental re-review when a previous reviewed HEAD is found in markers.
4. Route context statuses exactly: `CONTEXT: AUTH` → `PR_REVIEW: AUTH`; `CONTEXT: NOT_FOUND` → `PR_REVIEW: NOT_FOUND`; `CONTEXT: ERROR` → `PR_REVIEW: REVIEW_ERROR`.
5. `CONTEXT: NEEDS_CONTEXT` is a narrow request the orchestrator may be able to satisfy. Satisfy it inline and re-dispatch the collector at most once per run; if the need persists or cannot be satisfied, stop with `PR_REVIEW: NEEDS_CONTEXT`.

### 3. Chunk Review

6. Dispatch one `chunk-reviewer` per dimension, each with `WORKTREE_PATH`, the context summary, its assigned dimension, `DIMENSION_FILES`, `REVIEW_MODE`, `REVIEW_FOCUS`, and `LANGUAGE_STYLE`. Dispatch concurrently when the runtime supports it; otherwise serially in dimension order. All code and diff inspections run inside `WORKTREE_PATH`. Chunk reviewers enforce 3-tier severity (`BLOCKER`, `IMPORTANT`, `SUGGESTION`) and assign stable semantic fingerprints (`domain:behavioral-defect`). Never let a chunk reviewer dispatch other subagents.
7. Route `CHUNK: ERROR` for any dimension to `PR_REVIEW: REVIEW_ERROR`. On `CHUNK: NEEDS_CONTEXT`, dispatch `pr-context-collector` once with the narrow request (this consumes the single narrow-context retry), then re-dispatch only that chunk reviewer. A second `CHUNK: NEEDS_CONTEXT` from any reviewer stops with `PR_REVIEW: NEEDS_CONTEXT`.
8. Proceed when every chunk returns `CHUNK: PASS` or `CHUNK: NO_FINDINGS`.

### 4. Adjudication

9. Dispatch `finding-adjudicator` with `WORKTREE_PATH`, all chunk findings, and the existing history digest. It confirms, severity-adjusts, or drops each candidate with a written reason; merges cross-dimension duplicates by fingerprint; reconciles lifecycle states (`NEW`, `STILL_OPEN`, `RESOLVED`, `WITHDRAWN`, `OBSOLETE`); and sets posting dispositions. All findings are consolidated entirely into the single review body — there are zero inline comments and zero thread replies:
   - `NEW` BLOCKER/IMPORTANT → `BODY_SECTION` (described in dedicated sections of `CANONICAL_BODY`)
   - `STILL_OPEN`/`RESOLVED`/`WITHDRAWN`/`OBSOLETE` → `BODY_SECTION` (rendered in `## Previous Findings` section of `CANONICAL_BODY`)
   - All `SUGGESTION` → `SUMMARY_ONLY` (cheap suggestions policy; bullet list in `## Suggestions`, no dedicated section)
10. Route `ADJUDICATE: ERROR` to `PR_REVIEW: REVIEW_ERROR`. On `ADJUDICATE: NO_FINDINGS` (nothing survived, no previous findings owed), proceed to Targeted Verification and always dispatch `comment-drafter`. Never skip `comment-drafter`.

### 5. Targeted Verification

11. In `WORKTREE_PATH`, execute targeted verification commands. Discover commands from CI config, build metadata, and documented repo instructions — do not hardcode project-specific commands. Distinguish check types: `TEST`, `BUILD`, `TYPECHECK`, `LINT`, `GUARDRAIL`, `SYNTAX`, `REPRODUCTION`. Record each check in `VERIFICATION_CHECKS` table (Check name, Target, Status `PASS`/`FAIL`/`NOT RUN — <reason>`/`REPRODUCED`, Notes). Bug reproduction is `REPRODUCED`, never `PASS`. Never run mutating commands in the caller's original workspace.

### 6. Comment Drafting (Always Dispatched)

12. ALWAYS dispatch `comment-drafter` with `PR_URL`, `HEAD_SHA`, `CONTEXT_SUMMARY`, `ADJUDICATED_FINDINGS`, `VERIFICATION_CHECKS`, and `LANGUAGE_STYLE`. It loads `../assets/review-file-template.md` and produces ONE `CANONICAL_BODY` Markdown string — the exact text that will be posted to GitHub — containing: verdict header, overview metadata, `## Verification` table (from `VERIFICATION_CHECKS`), findings in appropriate template sections (with hidden per-finding markers), suggestion bullets, Confirmed Good items, Final Decision, and the global tracking marker `<!-- agent-pr-review reviewed-head: ... findings: ... -->` at the end.
    - Zero inline comment threads and zero thread replies are produced.
    - On the zero-findings path (0 BLOCKER, 0 IMPORTANT, 0 SUGGESTION), `comment-drafter` renders `🟢 PASS` with overview, `## Verification` table, Confirmed Good items, Final Decision (`APPROVE`), and global tracking marker. Downstream phases always require `CANONICAL_BODY`.
    - `Posting status` is NOT included in `CANONICAL_BODY` (it belongs strictly in the metadata sidecar).
    - Confirmed Good discipline: only include positives verified and not contradicted by active findings.
13. Route `COMMENTS: ERROR` to `PR_REVIEW: REVIEW_ERROR`. On `COMMENTS: NEEDS_METADATA`, collect only the requested context inline and retry drafting once; a repeated `NEEDS_METADATA` or `ERROR` stops with `PR_REVIEW: REVIEW_ERROR`.

### 7. Review Verification

14. Dispatch `review-verifier` with `PR_URL`, `WORKTREE_PATH`, `CONTEXT_SUMMARY`, `REVIEW_PACKAGE`, and `VERIFICATION_CHECKS`. It validates evidence, severity vocabulary, deterministic verdict, single verification table ownership, Confirmed Good discipline, hidden markers, zero inline comments/thread replies, and byte parity readiness.
    On `VERIFY: FAIL`, the verifier names exactly one `Fix target`; increment the repair-cycle counter on each `VERIFY: FAIL` and stop with `PR_REVIEW: VERIFY_FAIL` when a third failure would begin. Route repairs:
    - `pr-context-collector`: repair the context packet, then re-run `finding-adjudicator` (with prior chunk findings), `comment-drafter`, and `review-verifier`. Do not re-run chunk reviewers during repair.
    - `finding-adjudicator`: repair the named adjudication defect, then re-run `comment-drafter` and `review-verifier`.
    - `comment-drafter`: repair the named comments, then re-run `review-verifier`.
15. Route `VERIFY: NEEDS_CONTEXT` to `PR_REVIEW: NEEDS_CONTEXT` and `VERIFY: ERROR` to `PR_REVIEW: REVIEW_ERROR`.

### 8. Review Writing

16. Dispatch `review-writer` in write mode only after `VERIFY: PASS`, with posting status `draft`. It writes `OUTPUT_FILE` in `WORKTREE_PATH` and exports a persistent copy to `${XDG_STATE_HOME:-$HOME/.local/state}/pr-review/<owner>-<repo>/pr-<number>-review.md`. Both files contain the EXACT `CANONICAL_BODY` byte-for-byte (no wrappers, no header comments, no appended sections). It also creates the sidecar `pr-<number>-review.meta.json` containing runtime metadata (posting status, effective event, body SHA-256, PR head, timestamp).
    Route `WRITE: ERROR` to `PR_REVIEW: WRITE_ERROR`.
17. In `draft-only` mode, run cleanup and finish with `PR_REVIEW: VERIFIED_DRAFT_SAVED`.

### 9. Exact Preview Gate & Posting Authorization

18. Posting authorization is resolved strictly based on `POSTING_MODE`:
    - **`draft-only`**:
      Skip preview and posting entirely. Run worktree cleanup and finish with `PR_REVIEW: VERIFIED_DRAFT_SAVED`.
    - **`post-after-confirmation`**:
      Resolve the authenticated GitHub user vs PR author BEFORE showing the preview:
      - If self-review (same user), set `EFFECTIVE_EVENT` to `COMMENT` regardless of internal verdict; record this in the preview. Never preview `REQUEST_CHANGES` when self-review would cause a GitHub 422 error.
      - Compute SHA-256 hash of `pr-<number>-review.md`.
      - Display the exact verified preview:
        ```text
        GitHub Posting Preview

        Internal verdict: <🔴 BLOCK | 🟡 PASS WITH NOTES | 🟢 PASS>
        Effective GitHub event: <REQUEST_CHANGES | COMMENT | APPROVE>
        Body SHA-256: <computed SHA-256 hash>

        --- EXACT BODY START ---
        <exact contents of pr-N-review.md>
        --- EXACT BODY END ---

        Post this exact review?
        ```
      - Run `HUMAN_GATE_FINAL_PREVIEW_APPROVAL`. User approval binds strictly to:
        1. Exact body contents and body SHA-256
        2. Effective GitHub event
        3. PR number and current PR head SHA
      - If any of these change before posting, the approval is void — re-preview and ask again.
      - On approval, record `POST_AUTHORIZATION=human-approved`, `AUTHORIZED_BODY_SHA256=<hash>`, and `AUTHORIZED_HEAD_SHA=<head_sha>`.
      - If the user declines, dispatch `review-writer` in update mode to update the sidecar `.meta.json` status to `cancelled` (do not edit `pr-N-review.md`), run cleanup, then finish with `PR_REVIEW: VERIFIED_DRAFT_SAVED_POSTING_CANCELLED`.
    - **`auto-post-verified`**:
      - Authorized ONLY after `review-verifier` returns `VERIFY: PASS` (all quality, lifecycle, marker, and verification gates passed).
      - If `review-verifier` returned `VERIFY: FAIL` or repair cycles were exhausted, abort posting immediately: preserve draft artifact in worktree/persistent storage and stop with `PR_REVIEW: VERIFY_FAIL`. Never auto-post an unverified review.
      - Resolve the authenticated GitHub user vs PR author (self-review -> set `EFFECTIVE_EVENT` to `COMMENT`).
      - Compute SHA-256 hash of `pr-<number>-review.md`.
      - Set `POST_AUTHORIZATION=verified-auto`, `AUTHORIZED_BODY_SHA256=<hash>`, and `AUTHORIZED_HEAD_SHA=<head_sha>`.
      - Do NOT prompt the user with `HUMAN_GATE_FINAL_PREVIEW_APPROVAL`.
      - Proceed immediately to Review Posting.

### 10. Review Posting & Read-Back Verification

19. On authorization (`human-approved` or `verified-auto`), dispatch `review-poster` with:
    `PR_URL`, `BODY_FILE="${WORKTREE_PATH}/${OUTPUT_FILE}"` (absolute path to exact review file), `BODY_SHA256`, `EFFECTIVE_EVENT`, `POST_AUTHORIZATION`, `AUTHORIZED_BODY_SHA256`, `AUTHORIZED_HEAD_SHA`, and `COMMIT_ID="${PR_HEAD_SHA}"`.
    - `review-poster` reads `BODY_FILE` directly via its absolute path; it does not need `WORKTREE_PATH`.
    - Before posting, `review-poster` verifies:
      1. `POST_AUTHORIZATION` is valid (`human-approved` or `verified-auto`).
      2. `sha256(BODY_FILE) == AUTHORIZED_BODY_SHA256`.
      3. Current PR head fetched from GitHub matches `AUTHORIZED_HEAD_SHA` exactly. If developer pushed new commits, abort posting with `POST: STALE_HEAD`.
      4. Effective event matches self-review rules.
    - If current head changed (`POST: STALE_HEAD`), dispatch `review-writer` in update mode with `POSTING_STATUS=stale_head`, run cleanup, and stop with `PR_REVIEW: STALE_HEAD` (Reason: "PR head changed during review; re-review required"). The review draft artifact is preserved; no stale review is posted to GitHub.
    - If hash or authorization check fails, stop with `PR_REVIEW: POST_ERROR`.
    - Posts ONE atomic review event bound to reviewed commit: exact `BODY_FILE` as review body, `commit_id: COMMIT_ID`, `comments: []` (empty array, zero inline comments), and zero thread replies.
    - Reads back the created review to verify:
      1. Complete body integrity: full returned body matches `BODY_FILE` exactly (normalizing `\r\n` to `\n`). No spot-check line matching.
      2. Commit matches reviewed `AUTHORIZED_HEAD_SHA`.
      3. 0 inline comments created, 0 thread replies created.
    - Route `POST: PASS` to artifact update. Route `POST: AUTH`, `POST: METADATA_INVALID`, and `POST: ERROR` to `PR_REVIEW: POST_ERROR` with reason and next step, and update sidecar status to `failed`.

### 11. Artifact Update

20. After `POST: PASS`, dispatch `review-writer` in update mode with `POSTING_STATUS=posted` and `POSTED_REVIEW_ID`.
    - `review-writer` updates ONLY the `.meta.json` sidecar. It NEVER edits `pr-<number>-review.md`.
    - Finish with `PR_REVIEW: VERIFIED_REVIEW_POSTED`.

### 12. Re-review Lifecycle

A re-review is triggered when `pr-context-collector` detects a previous reviewed HEAD (from markers `<!-- agent-pr-review ... -->` or legacy `<!-- lyreo-review ... -->`).

In a re-review:
- Provide incremental diff (`previous_head...current_head`) to chunk reviewers.
- Each surviving previous finding carries a lifecycle state:
  - `RESOLVED`: valid defect fixed by new code in delta. Only used when original defect was real.
  - `STILL_OPEN`: defect still unaddressed or fix incomplete.
  - `WITHDRAWN`: withdrawn by reviewer because initial evidence was invalid, misunderstood, or false-positive (e.g. `--worktree` flag support). Never mark false positives as `RESOLVED`.
  - `OBSOLETE`: targeted code/feature was removed/refactored out.
- `finding-adjudicator` verifies developer-claimed fixes against current code in `WORKTREE_PATH` before marking `RESOLVED`; developer replies are evidence to verify, not ground truth.
- `comment-drafter` produces one `CANONICAL_BODY` using **Format 2 (Incremental Re-review)** from `review-file-template.md`: containing `## Previous Findings` (with per-finding markers including `lifecycle`), `## New Findings in Delta` (with per-finding markers including `lifecycle: NEW`), `## Suggestions`, `## Confirmed Good`, `## Final Decision`, and the global tracking marker. This ensures every re-review is a complete machine-readable snapshot and multi-turn chains (round 3+) preserve all history.
- Post ONE review event: `CANONICAL_BODY` as review body, `comments[]` always empty, zero thread replies. Existing GitHub inline threads are strictly read-only history.

### 13. Worktree Cleanup & Caller Integrity Check

22. On EVERY terminal outcome (success or failure), run `../scripts/cleanup-pr-worktree.sh <WORKTREE_PATH> <SNAPSHOT_FILE>`.
    - Verifies candidate is a registered linked worktree of this repository and NOT the main repo root or caller workspace.
    - Refuses to touch main repository root, caller workspace, `/`, `$HOME`, or unmanaged paths.
    - Uses `git worktree remove --force` followed by `git worktree prune`. If removal fails, STOP and preserve path; never fallback to `rm -rf`.
    - Verifies original branch, HEAD, and git status match snapshot.
    - Exit codes: 0 (PASS), 1 (INTEGRITY_MISMATCH), 2 (SAFETY_ERROR), 3 (INTEGRITY_UNVERIFIED).

### 14. Resume / Interruption Behavior

If workflow execution is interrupted after user approval but before confirmed successful posting:
- Do NOT assume the previous approval remains valid.
- Do NOT parse review artifacts using fixed line offsets.
- Always reload `pr-N-review.md` directly (since it is the exact canonical body).
- Recompute SHA-256 hash, re-verify current PR head, re-resolve effective event, display the exact preview again, and request fresh user approval before posting.

## Terminal Outcomes

Success:

```text
PR_REVIEW: VERIFIED_DRAFT_SAVED
PR_REVIEW: VERIFIED_DRAFT_SAVED_POSTING_CANCELLED
PR_REVIEW: VERIFIED_REVIEW_POSTED
```

Failure envelope:

```text
PR_REVIEW: AUTH | NOT_FOUND | NEEDS_CONTEXT | REVIEW_ERROR | VERIFY_FAIL | WRITE_ERROR | POST_ERROR | STALE_HEAD
Reason: <one line>
Next step: <one clear action>
```

## Final Output Contract

Final success replies include:

```text
Review verdict: <🔴 BLOCK | 🟡 PASS WITH NOTES | 🟢 PASS>
Review file: <EXPORTED_PERSISTENT_FILE_PATH>
Workspace integrity: verified unchanged (branch <branch>, HEAD <sha>)
Findings: <count or 0>
- BLOCKER: <count>
- IMPORTANT: <count>
- SUGGESTION: <count>
New inline comments: 0
Thread replies: 0
Review decision: <comment | request changes | approve>
Posting: <skipped | posted | cancelled>
Notes: <one-line residual risk or none>
```

## Dispatch Example

<example>
Re-review on a previously reviewed PR, `POSTING_MODE=post-after-confirmation`, `REVIEW_MODE=normal`:

1. Worktree created at `/tmp/pr-worktree-abc12345-1020`.
2. `pr-context-collector` → `CONTEXT: PASS`; dimensions `security`, `tests`; compact history digest shows 3 prior findings (1 resolved), previous reviewed HEAD `a1b2c3d`.
3. Two `chunk-reviewer` dispatches → 5 candidate findings.
4. `finding-adjudicator` → `ADJUDICATE: PASS`: 2 BLOCKER (STILL_OPEN BODY_SECTION), 1 IMPORTANT (NEW BODY_SECTION), 1 SUGGESTION (SUMMARY_ONLY), 1 RESOLVED (BODY_SECTION); 1 dropped.
5. Worktree verification: `NOT RUN — no discoverable test command`.
6. `comment-drafter` → `COMMENTS: PASS` with verdict `🔴 BLOCK`; `CANONICAL_BODY` rendered from Format 2 of `review-file-template.md` containing `## Previous Findings` (2 STILL_OPEN, 1 RESOLVED), `## New Findings in Delta` (1 IMPORTANT), `## Suggestions` (1 bullet), `## Confirmed Good`, and global tracking marker; zero inline comments and zero thread replies.
7. `review-verifier` → `VERIFY: PASS`.
8. `review-writer` → `WRITE: PASS` (draft); `CANONICAL_BODY` saved verbatim byte-for-byte; exported to `~/.local/state/pr-review/org-repo/pr-1020-review.md`; writes `pr-1020-review.meta.json`.
9. Resolve auth user vs PR author → not self-review; effective event `REQUEST_CHANGES`.
10. Show exact preview with SHA-256 and `--- EXACT BODY START ---` / `--- EXACT BODY END ---`; user approves.
11. `review-poster` verifies approval, hash, and head → `POST: PASS` (1 atomic review, `comments[]` empty, 0 thread replies); read-back verified.
12. `review-writer` update mode → updates sidecar status to `posted` (does not touch `.md`); `PR_REVIEW: VERIFIED_REVIEW_POSTED`.
13. `cleanup-pr-worktree.sh` → `WORKTREE_CLEANUP: PASS (workspace integrity verified)`.
</example>
