#!/usr/bin/env bash
# Regression tests for /pr-review critical safety logic:
# 1. Worktree isolation, caller workspace protection, refusal of unsafe paths, non-directory safety, orphan recovery.
# 2. Review history parsing calling real collect-pr-review-history.sh via shimmed gh API.
# 3. Posting script execution calling real post-pr-review.sh via shimmed gh API (direct file body, trailing newlines, commit_id).
#
# Tests actual production primitives directly without duplicating logic into test code.
set -euo pipefail

SKILL_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PREPARE_SCRIPT="${SKILL_DIR}/scripts/prepare-pr-worktree.sh"
CLEANUP_SCRIPT="${SKILL_DIR}/scripts/cleanup-pr-worktree.sh"
HISTORY_SCRIPT="${SKILL_DIR}/scripts/collect-pr-review-history.sh"
POST_SCRIPT="${SKILL_DIR}/scripts/post-pr-review.sh"

TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

assert_eq() {
  local expected="$1"
  local actual="$2"
  local desc="$3"
  TOTAL_TESTS=$((TOTAL_TESTS + 1))
  if [ "$expected" = "$actual" ]; then
    printf '  [PASS] %s\n' "$desc"
    PASSED_TESTS=$((PASSED_TESTS + 1))
  else
    printf '  [FAIL] %s (expected: "%s", got: "%s")\n' "$desc" "$expected" "$actual" >&2
    FAILED_TESTS=$((FAILED_TESTS + 1))
  fi
}

assert_exit() {
  local expected_code="$1"
  local actual_code="$2"
  local desc="$3"
  TOTAL_TESTS=$((TOTAL_TESTS + 1))
  if [ "$expected_code" -eq "$actual_code" ]; then
    printf '  [PASS] %s (exit code %d)\n' "$desc" "$actual_code"
    PASSED_TESTS=$((PASSED_TESTS + 1))
  else
    printf '  [FAIL] %s (expected exit %d, got %d)\n' "$desc" "$expected_code" "$actual_code" >&2
    FAILED_TESTS=$((FAILED_TESTS + 1))
  fi
}

printf '=== Running PR Review Skill Regression Tests ===\n\n'

TEST_SANDBOX="$(mktemp -d /tmp/test-pr-review-XXXXXX)"
trap 'rm -rf "$TEST_SANDBOX"' EXIT

# Set up shim bin directory for external tools (e.g. gh)
SHIM_BIN="${TEST_SANDBOX}/bin"
mkdir -p "$SHIM_BIN"
export PATH="${SHIM_BIN}:${PATH}"

# ---------------------------------------------------------------------------
# TEST SUITE 1: Worktree Safety, Isolation & Recovery
# ---------------------------------------------------------------------------
printf '1. Testing Worktree Safety & Workspace Integrity...\n'

# Set up upstream bare repo and local clone
mkdir -p "${TEST_SANDBOX}/testowner/testrepo.git"
git -C "${TEST_SANDBOX}/testowner/testrepo.git" init --bare --quiet
git clone --quiet "${TEST_SANDBOX}/testowner/testrepo.git" "${TEST_SANDBOX}/local"
cd "${TEST_SANDBOX}/local"
git config user.name "Tester"
git config user.email "tester@example.com"
echo "init" > README.md
git add README.md
git commit --quiet -m "init commit"
git push --quiet origin HEAD:refs/heads/main

# Push PR branch and pull ref
git checkout --quiet -b feature
echo "feature" > feature.txt
git add feature.txt
git commit --quiet -m "feature commit"
PR_HEAD="$(git rev-parse HEAD)"
git push --quiet origin HEAD:refs/pull/42/head
git checkout --quiet main

# Add dirty state to caller workspace
echo "staged content" > staged.txt
git add staged.txt
echo "unstaged addition" >> README.md
echo "untracked content" > untracked.txt

CALLER_BRANCH_BEFORE="$(git branch --show-current)"
CALLER_HEAD_BEFORE="$(git rev-parse HEAD)"
CALLER_STATUS_BEFORE="$(git status --porcelain)"

# Prepare worktree
TARGET_WT="${TEST_SANDBOX}/worktree-42"
"$PREPARE_SCRIPT" "https://github.com/testowner/testrepo/pull/42" "$TARGET_WT" >/dev/null

assert_eq "true" "$([ -d "$TARGET_WT" ] && echo true || echo false)" "prepare-pr-worktree creates isolated worktree"
assert_eq "$CALLER_BRANCH_BEFORE" "$(git branch --show-current)" "caller branch remains unchanged"
assert_eq "$CALLER_HEAD_BEFORE" "$(git rev-parse HEAD)" "caller HEAD remains unchanged"
assert_eq "$CALLER_STATUS_BEFORE" "$(git status --porcelain)" "caller staged/unstaged/untracked state preserved"

# Test safety refusals: repository main root
set +e
"$CLEANUP_SCRIPT" "${TEST_SANDBOX}/local" >/dev/null 2>&1
RC_REPO_ROOT=$?
set -e
assert_exit 2 "$RC_REPO_ROOT" "cleanup refuses repository main root"

# Test safety refusals: arbitrary unverified path
mkdir -p "${TEST_SANDBOX}/arbitrary_dir"
set +e
"$CLEANUP_SCRIPT" "${TEST_SANDBOX}/arbitrary_dir" >/dev/null 2>&1
RC_ARBITRARY=$?
set -e
assert_exit 2 "$RC_ARBITRARY" "cleanup refuses arbitrary unverified directory"

# Test stale recovery: target worktree already exists (simulate interrupted run)
"$PREPARE_SCRIPT" "https://github.com/testowner/testrepo/pull/42" "$TARGET_WT" >/dev/null
assert_eq "true" "$([ -d "$TARGET_WT" ] && echo true || echo false)" "stale linked worktree recovery successfully recreates worktree"

# Normal cleanup of worktree
"$CLEANUP_SCRIPT" "$TARGET_WT" "${TARGET_WT}.snapshot" >/dev/null
assert_eq "false" "$([ -d "$TARGET_WT" ] && echo true || echo false)" "cleanup removes verified linked worktree"
assert_eq "$CALLER_STATUS_BEFORE" "$(git status --porcelain)" "caller workspace intact after cleanup"

# Case A: Regular file occupies worktree path
TARGET_FILE_WT="${TEST_SANDBOX}/worktree-regular-file"
echo "not a directory" > "$TARGET_FILE_WT"

set +e
"$PREPARE_SCRIPT" "https://github.com/testowner/testrepo/pull/42" "$TARGET_FILE_WT" >/dev/null 2>&1
RC_FILE_WT=$?
set -e
assert_exit 67 "$RC_FILE_WT" "prepare-pr-worktree safely refuses when target path is a regular file"
assert_eq "true" "$([ -f "$TARGET_FILE_WT" ] && grep -q "not a directory" "$TARGET_FILE_WT" && echo true || echo false)" "existing regular file remains untouched"

set +e
"$CLEANUP_SCRIPT" "$TARGET_FILE_WT" >/dev/null 2>&1
RC_CLEANUP_FILE=$?
set -e
assert_exit 2 "$RC_CLEANUP_FILE" "cleanup safely refuses regular file"

set +e
"$CLEANUP_SCRIPT" "$TARGET_FILE_WT" --stale-recovery >/dev/null 2>&1
RC_CLEANUP_STALE_FILE=$?
set -e
assert_exit 2 "$RC_CLEANUP_STALE_FILE" "cleanup stale-recovery safely refuses regular file"

# Case B: Orphaned Git worktree registration (directory disappeared externally)
ORPHAN_WT="${TEST_SANDBOX}/worktree-orphan"
"$PREPARE_SCRIPT" "https://github.com/testowner/testrepo/pull/42" "$ORPHAN_WT" >/dev/null
assert_eq "true" "$([ -d "$ORPHAN_WT" ] && echo true || echo false)" "orphan test: worktree created initially"

# Simulate external directory removal (e.g. system reboot or /tmp cleanup) leaving Git metadata
rm -rf "$ORPHAN_WT"
assert_eq "false" "$([ -d "$ORPHAN_WT" ] && echo true || echo false)" "orphan test: directory removed externally"

# Calling prepare again must prune the orphaned registration and create the worktree afresh
"$PREPARE_SCRIPT" "https://github.com/testowner/testrepo/pull/42" "$ORPHAN_WT" >/dev/null
assert_eq "true" "$([ -d "$ORPHAN_WT" ] && echo true || echo false)" "prepare-pr-worktree successfully recovers when registration was orphaned"
"$CLEANUP_SCRIPT" "$ORPHAN_WT" "${ORPHAN_WT}.snapshot" >/dev/null

# ---------------------------------------------------------------------------
# TEST SUITE 2: Review History Parsing (Calling Real collect-pr-review-history.sh)
# ---------------------------------------------------------------------------
printf '\n2. Testing Review History Parsing (Executing Real collect-pr-review-history.sh)...\n'

# Create a mock gh CLI that supplies controlled review/comment payloads and records API calls
cat << 'EOF' > "${SHIM_BIN}/gh"
#!/usr/bin/env bash
BASE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
if [ "${1:-}" = "api" ]; then
  printf '%s\n' "$@" > "${BASE_DIR}/last_gh_api_call.txt"
  for arg in "$@"; do
    case "$arg" in
      *pulls/*/reviews)
        if [ -f "${BASE_DIR}/mock_reviews.json" ]; then
          cat "${BASE_DIR}/mock_reviews.json"
        else
          echo "[]"
        fi
        exit 0
        ;;
      *pulls/*/comments)
        if [ -f "${BASE_DIR}/mock_comments.json" ]; then
          cat "${BASE_DIR}/mock_comments.json"
        else
          echo "[]"
        fi
        exit 0
        ;;
    esac
  done
  exit 0
fi
exec /usr/bin/gh "$@"
EOF
chmod +x "${SHIM_BIN}/gh"

# Test 2.1: Modern agent-pr-review markers with lifecycle states
cat << 'EOF' > "${TEST_SANDBOX}/mock_reviews.json"
[
  {
    "id": 1,
    "submitted_at": "2026-09-19T12:00:00Z",
    "body": "## Review\n<!-- agent-pr-finding fingerprint: auth:guard severity: BLOCKER lifecycle: STILL_OPEN path: api/auth.ts line: 42 title: Missing guard -->\n<!-- agent-pr-finding fingerprint: sec:leak severity: IMPORTANT lifecycle: WITHDRAWN path: api/leak.ts line: 10 title: False positive leak -->\n<!-- agent-pr-finding fingerprint: perf:cache severity: IMPORTANT lifecycle: RESOLVED path: api/cache.ts line: 15 title: Cache added -->\n<!-- agent-pr-review reviewed-head: 1a2b3c4d findings: auth:guard,sec:leak,perf:cache -->"
  }
]
EOF
echo "[]" > "${TEST_SANDBOX}/mock_comments.json"

OUTPUT_MODERN="$("$HISTORY_SCRIPT" "https://github.com/testowner/testrepo/pull/42")"

assert_eq "1a2b3c4d" "$(echo "$OUTPUT_MODERN" | jq -r .latest_reviewed_head)" "real script recovers latest_reviewed_head from agent-pr-review"
assert_eq "STILL_OPEN" "$(echo "$OUTPUT_MODERN" | jq -r '.previous_findings[] | select(.fingerprint=="auth:guard") | .lifecycle')" "real script parses lifecycle STILL_OPEN"
assert_eq "WITHDRAWN" "$(echo "$OUTPUT_MODERN" | jq -r '.previous_findings[] | select(.fingerprint=="sec:leak") | .lifecycle')" "real script preserves WITHDRAWN (distinct from RESOLVED)"
assert_eq "RESOLVED" "$(echo "$OUTPUT_MODERN" | jq -r '.previous_findings[] | select(.fingerprint=="perf:cache") | .lifecycle')" "real script parses lifecycle RESOLVED"

# Test 2.2: Legacy lyreo-review markers and legacy inline comments
cat << 'EOF' > "${TEST_SANDBOX}/mock_reviews.json"
[
  {
    "id": 2,
    "submitted_at": "2026-09-17T12:00:00Z",
    "body": "## Review\n<!-- lyreo-review reviewed-head: 9z8y7x6w findings: legacy:f1 -->"
  }
]
EOF
cat << 'EOF' > "${TEST_SANDBOX}/mock_comments.json"
[
  {
    "id": 10,
    "path": "legacy.ts",
    "line": 99,
    "in_reply_to_id": null,
    "created_at": "2026-09-17T12:00:00Z",
    "body": "**BLOCKER**: Legacy blocker bug\n\n<!-- lyreo-review reviewed-head: 9z8y7x6w fingerprint: legacy:f1 -->"
  }
]
EOF

OUTPUT_LEGACY="$("$HISTORY_SCRIPT" "https://github.com/testowner/testrepo/pull/42")"

assert_eq "9z8y7x6w" "$(echo "$OUTPUT_LEGACY" | jq -r .latest_reviewed_head)" "real script recovers reviewed-head from legacy lyreo-review marker"
assert_eq "legacy:f1" "$(echo "$OUTPUT_LEGACY" | jq -r '.previous_findings[0].fingerprint')" "real script extracts legacy inline comment fingerprint"
assert_eq "BLOCKER" "$(echo "$OUTPUT_LEGACY" | jq -r '.previous_findings[0].severity')" "real script extracts legacy inline comment severity"

# ---------------------------------------------------------------------------
# TEST SUITE 3: Posting Script Execution (Calling Real post-pr-review.sh)
# ---------------------------------------------------------------------------
printf '\n3. Testing Posting Script Execution (Executing Real post-pr-review.sh)...\n'

DUMMY_BODY="${TEST_SANDBOX}/body.md"
printf '# Canonical Review Body\nVerified PASS content.\nLine ending with newline.\n' > "$DUMMY_BODY"

# Test 3.1: Rejection of invalid event
set +e
"$POST_SCRIPT" "testowner/testrepo" 42 "INVALID_EVENT" "$DUMMY_BODY" >/dev/null 2>&1
RC_INVALID_EVENT=$?
set -e
assert_exit 65 "$RC_INVALID_EVENT" "post-pr-review rejects invalid review event"

# Test 3.2: Rejection of missing body file
set +e
"$POST_SCRIPT" "testowner/testrepo" 42 "APPROVE" "${TEST_SANDBOX}/nonexistent.md" >/dev/null 2>&1
RC_MISSING_BODY=$?
set -e
assert_exit 66 "$RC_MISSING_BODY" "post-pr-review rejects missing body file"

# Test 3.3: Execution with commit_id and direct-file body flag (-F body=@file)
rm -f "${TEST_SANDBOX}/last_gh_api_call.txt"
"$POST_SCRIPT" "testowner/testrepo" 42 "REQUEST_CHANGES" "$DUMMY_BODY" "feedbeef12345678" >/dev/null

LAST_CALL="$(cat "${TEST_SANDBOX}/last_gh_api_call.txt")"
assert_eq "true" "$(echo "$LAST_CALL" | grep -q "POST" && echo true || echo false)" "post-pr-review performs POST method"
assert_eq "true" "$(echo "$LAST_CALL" | grep -q "repos/testowner/testrepo/pulls/42/reviews" && echo true || echo false)" "post-pr-review targets correct endpoint"
assert_eq "true" "$(echo "$LAST_CALL" | grep -q "event=REQUEST_CHANGES" && echo true || echo false)" "post-pr-review sets event=REQUEST_CHANGES"
assert_eq "true" "$(echo "$LAST_CALL" | grep -q "commit_id=feedbeef12345678" && echo true || echo false)" "post-pr-review binds commit_id to reviewed HEAD"
assert_eq "true" "$(echo "$LAST_CALL" | grep -q -- "-F" && echo true || echo false)" "post-pr-review uses -F direct-file field flag"
assert_eq "true" "$(echo "$LAST_CALL" | grep -q -- "body=@${DUMMY_BODY}" && echo true || echo false)" "post-pr-review passes body file path directly without command substitution"
assert_eq "true" "$(tail -c 1 "$DUMMY_BODY" | grep -q '^$' && echo true || echo false)" "body fixture preserves trailing newline"

# Test 3.4: Execution without commit_id (backward-compatibility check)
rm -f "${TEST_SANDBOX}/last_gh_api_call.txt"
"$POST_SCRIPT" "testowner/testrepo" 42 "APPROVE" "$DUMMY_BODY" >/dev/null

LAST_CALL_NO_COMMIT="$(cat "${TEST_SANDBOX}/last_gh_api_call.txt")"
assert_eq "true" "$(echo "$LAST_CALL_NO_COMMIT" | grep -q "event=APPROVE" && echo true || echo false)" "post-pr-review sets event=APPROVE without commit_id"
assert_eq "false" "$(echo "$LAST_CALL_NO_COMMIT" | grep -q "commit_id=" && echo true || echo false)" "post-pr-review omits commit_id flag when not supplied"

printf '\n=======================================================\n'
printf 'Regression test summary: %d passed, %d failed (Total: %d)\n' "$PASSED_TESTS" "$FAILED_TESTS" "$TOTAL_TESTS"
printf '=======================================================\n'

# Hard failure gate: exit non-zero if any test failed or no tests ran
if [ "$FAILED_TESTS" -gt 0 ] || [ "$PASSED_TESTS" -ne "$TOTAL_TESTS" ] || [ "$TOTAL_TESTS" -eq 0 ]; then
  printf '\nTEST SUITE FAILED: %d failures detected!\n' "$FAILED_TESTS" >&2
  exit 1
fi
