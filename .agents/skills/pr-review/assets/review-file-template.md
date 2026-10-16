# Review File Template

> **Ownership**: `comment-drafter` is the ONLY agent that reads this file and renders `CANONICAL_BODY` from it.
> It receives `VERIFICATION_CHECKS` and renders `## Verification` directly into `CANONICAL_BODY`.
> `review-writer` saves the resulting bytes verbatim (byte-for-byte) and never re-renders, appends to,
> or mutates `CANONICAL_BODY`. `review-verifier` validates the body. `review-poster` posts the body
> exactly as written. No other subagent reads this file.
>
> Do NOT render internal debugging mechanics (internal line-metadata dumps, dedup markers, draft-comment wrappers) — keep the document high-signal and readable.
>
> `Posting status` does NOT belong in the canonical Markdown review body. Posting status and related runtime
> metadata (effective event, body hash, PR head, preview timestamp, posted review ID) are maintained exclusively
> in the metadata sidecar (`pr-<number>-review.meta.json`). After posting, `pr-<number>-review.md` is never edited.

The review file must stand alone without chat context. It is findings first, concise, and explicit about dimensions reviewed and residual risks.

## Format 1: First Review

````markdown
# PR #<number> Review: <title>

## <🔴 BLOCK | 🟡 PASS WITH NOTES | 🟢 PASS>

<blocker_count> BLOCKER · <important_count> IMPORTANT · <suggestion_count> SUGGESTION

- **PR**: <PR_URL>
- **Reviewed**: base `<base_sha>` → head `<head_sha>`
- **Dimensions**: <comma-separated dimension names>
- **Mode**: `<normal | strict>`

---

## Verification

| Check | Target | Status | Notes |
|---|---|---|---|
| <check name> | <target path or command> | <PASS | FAIL | NOT RUN | REPRODUCED> | <notes or reason> |

---

## Must Fix (BLOCKER)

### 1. [<path>:<line>] <finding title>

<!-- agent-pr-finding fingerprint: <fingerprint> severity: BLOCKER path: <path> line: <line> title: <finding title> -->
- **Issue**: <concise description of defect and failure scenario>
- **Impact**: <why this matters>
- **Fix direction**: <concrete resolution guidance>
- **Sources**: <URL or omitted if internal>

(or: *None*)

---

## Important

### 1. [<path>:<line>] <finding title>

<!-- agent-pr-finding fingerprint: <fingerprint> severity: IMPORTANT path: <path> line: <line> title: <finding title> -->
- **Issue**: <concise description of issue>
- **Impact**: <why this matters>
- **Fix direction**: <concrete resolution guidance>
- **Sources**: <URL or omitted if internal>

(or: *None*)

---

## Suggestions

- <!-- agent-pr-finding fingerprint: <fingerprint> severity: SUGGESTION path: <path> line: <line> title: <finding title> -->**[<path>:<line>]**: <concise non-blocking suggestion and rationale>

(or: *None*)

---

## Confirmed Good

- <Specific boundary, security control, contract, or test verified as sound>

(or: *None specifically recorded.*)

---

## Final Decision

### <🔴 BLOCK | 🟡 PASS WITH NOTES | 🟢 PASS>

<Short summary sentence explaining the final decision and recommended GitHub review action: REQUEST_CHANGES | COMMENT | APPROVE>.

<!-- agent-pr-review reviewed-head: <full HEAD_SHA> findings: <comma-separated list of fingerprints> -->
````

## Format 2: Incremental Re-review

````markdown
# PR #<number> Re-review: <title>

## <🔴 BLOCK | 🟡 PASS WITH NOTES | 🟢 PASS>

<blocker_count> BLOCKER · <important_count> IMPORTANT · <suggestion_count> SUGGESTION

- **PR**: <PR_URL>
- **Reviewed incremental delta**: previous `<previous_head_sha>` → current `<head_sha>`
- **Dimensions**: <comma-separated dimension names>
- **Mode**: `<normal | strict>`

---

## Verification

| Check | Target | Status | Notes |
|---|---|---|---|
| <check name> | <target path or command> | <PASS | FAIL | NOT RUN | REPRODUCED> | <notes or reason> |

---

## Previous Findings

- <!-- agent-pr-finding fingerprint: <fingerprint> severity: <severity> lifecycle: RESOLVED path: <path> line: <line> title: <finding title> -->✅ **RESOLVED** [`<fingerprint>`]: <description> (verified in `<short_sha>`)
- <!-- agent-pr-finding fingerprint: <fingerprint> severity: <severity> lifecycle: STILL_OPEN path: <path> line: <line> title: <finding title> -->⚠️ **STILL OPEN** [`<fingerprint>`]: <description> (still unresolved because `<evidence>`)
- <!-- agent-pr-finding fingerprint: <fingerprint> severity: <severity> lifecycle: WITHDRAWN path: <path> line: <line> title: <finding title> -->↩️ **WITHDRAWN** [`<fingerprint>`]: <description> (withdrawn: <reason>)
- <!-- agent-pr-finding fingerprint: <fingerprint> severity: <severity> lifecycle: OBSOLETE path: <path> line: <line> title: <finding title> -->🗑️ **OBSOLETE** [`<fingerprint>`]: <description> (code refactored/removed)

(or: *None*)

---

## New Findings in Delta

### 1. [<path>:<line>] <finding title>

<!-- agent-pr-finding fingerprint: <fingerprint> severity: <BLOCKER | IMPORTANT> lifecycle: NEW path: <path> line: <line> title: <finding title> -->
- **Issue**: <concise description>
- **Impact**: <why this matters>
- **Fix direction**: <guidance>
- **Sources**: <URL or omitted if internal>

(or: *No new regressions found in incremental delta*)

---

## Suggestions

- <!-- agent-pr-finding fingerprint: <fingerprint> severity: SUGGESTION path: <path> line: <line> title: <finding title> -->**[<path>:<line>]**: <concise non-blocking suggestion>

(or: *None*)

---

## Confirmed Good

- <Specific verified boundary or regression check in delta>

(or: *None specifically recorded.*)

---

## Final Decision

### <🔴 BLOCK | 🟡 PASS WITH NOTES | 🟢 PASS>

<Short summary sentence explaining the final decision and recommended GitHub review action: REQUEST_CHANGES | COMMENT | APPROVE>.

<!-- agent-pr-review reviewed-head: <full HEAD_SHA> findings: <comma-separated list of fingerprints> -->
````

## Required Sections Checklist (for `comment-drafter` and `review-verifier`)

`comment-drafter` must produce a `CANONICAL_BODY` that contains all of these sections:
- Top verdict header (`## <verdict emoji>`)
- Overview metadata block (PR URL, reviewed SHAs, dimensions, mode)
- `## Verification` (rendered directly in `CANONICAL_BODY` from `VERIFICATION_CHECKS`)
- Findings sections (Format 1: `## Must Fix (BLOCKER)` / `## Important`; Format 2: `## Previous Findings` / `## New Findings in Delta`)
- `## Suggestions`
- `## Confirmed Good` (disciplined: only verified sound items, never contradicted by active findings)
- `## Final Decision` with bottom verdict
- Global tracking marker at the very end
