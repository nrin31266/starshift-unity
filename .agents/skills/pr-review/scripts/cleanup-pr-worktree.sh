#!/usr/bin/env bash
# Cleanup an isolated Git worktree and verify original workspace integrity.
# Usage: cleanup-pr-worktree.sh <worktree_path> [snapshot_file]
#
# Safety contract: NEVER delete a directory unless git confirms it is a registered
# LINKED worktree belonging to this repository. The main repository root must NEVER
# be treated as a managed worktree. If removal fails, STOP and preserve the path.
set -euo pipefail

if [ "$#" -lt 1 ]; then
  printf '%s\n' "usage: $0 <worktree_path> [snapshot_file]" >&2
  exit 64
fi

worktree_path="$1"
snapshot_file="${2:-${worktree_path}.snapshot}"

original_root=""
expected_branch=""
expected_head=""

if [ -f "$snapshot_file" ]; then
  original_root="$(grep '^ORIGINAL_ROOT=' "$snapshot_file" | cut -d= -f2- || true)"
  expected_branch="$(grep '^ORIGINAL_BRANCH=' "$snapshot_file" | cut -d= -f2- || true)"
  expected_head="$(grep '^ORIGINAL_HEAD=' "$snapshot_file" | cut -d= -f2- || true)"
fi

if [ -z "$original_root" ]; then
  original_root="$(git rev-parse --show-toplevel 2>/dev/null || true)"
fi

if [ -z "$original_root" ] || [ ! -d "$original_root" ]; then
  printf 'SAFETY: Cannot determine valid repository root — skipping worktree removal.\n' >&2
  printf '%s\n' "WORKTREE_CLEANUP: SAFETY_ERROR (repository root undetermined)" >&2
  exit 2
fi

# ---------------------------------------------------------------------------
# SAFETY: Prove that worktree_path is an exact registered LINKED worktree
# of this repository before touching it.
#
# Constraints:
#   1. Candidate must exist and resolve canonically.
#   2. Candidate != repository root.
#   3. Candidate != original caller workspace from snapshot.
#   4. Candidate != /
#   5. Candidate != $HOME
#   6. Candidate != caller PWD when that is repo root.
#   7. Candidate .git must be a regular file (linked worktree signature).
#   8. Candidate must be an exact match in git worktree list (excluding main).
#   9. When snapshot exists, ORIGINAL_ROOT must agree with current repository.
# ---------------------------------------------------------------------------

# Verify snapshot repository identity if snapshot exists
if [ -f "$snapshot_file" ]; then
  real_snapshot_root="$(realpath "$original_root" 2>/dev/null || true)"
  current_repo_root="$(git -C "$original_root" rev-parse --show-toplevel 2>/dev/null || true)"
  real_current_root="$(realpath "$current_repo_root" 2>/dev/null || true)"

  if [ -z "$real_snapshot_root" ] || [ "$real_snapshot_root" != "$real_current_root" ]; then
    printf 'SAFETY: Snapshot ORIGINAL_ROOT ("%s") does not agree with current repository identity ("%s") — refusing cleanup.\n' \
      "$original_root" "$current_repo_root" >&2
    printf '%s\n' "WORKTREE_CLEANUP: SAFETY_ERROR (snapshot repository mismatch)" >&2
    exit 2
  fi
fi

is_registered_linked_worktree() {
  local candidate="$1"
  local git_root="$2"

  # 1. Candidate must exist and resolve canonically
  local real_candidate
  real_candidate="$(realpath "$candidate" 2>/dev/null || true)"
  if [ -z "$real_candidate" ] || [ ! -d "$real_candidate" ]; then
    return 1
  fi

  local real_git_root
  real_git_root="$(realpath "$git_root" 2>/dev/null || true)"
  if [ -z "$real_git_root" ] || [ ! -d "$real_git_root" ]; then
    return 1
  fi

  # 2. Candidate != repository root
  if [ "$real_candidate" = "$real_git_root" ]; then
    return 1
  fi

  # 3. Candidate != /
  if [ "$real_candidate" = "/" ]; then
    return 1
  fi

  # 4. Candidate != $HOME
  if [ -n "${HOME:-}" ]; then
    local real_home
    real_home="$(realpath "$HOME" 2>/dev/null || true)"
    if [ -n "$real_home" ] && [ "$real_candidate" = "$real_home" ]; then
      return 1
    fi
  fi

  # 5. Candidate != current caller PWD when that is repo root
  local caller_pwd
  caller_pwd="$(pwd -P 2>/dev/null || pwd)"
  if [ "$caller_pwd" = "$real_git_root" ] && [ "$real_candidate" = "$caller_pwd" ]; then
    return 1
  fi

  # 6. Candidate .git must be a regular file (linked worktree pointer), not a dir
  if [ ! -f "$real_candidate/.git" ]; then
    return 1
  fi

  # 7. Candidate must match a linked worktree entry in git worktree list --porcelain
  local worktree_list
  worktree_list="$(git -C "$git_root" worktree list --porcelain 2>/dev/null)" || return 1

  local is_first=true
  local found_linked_match=false
  while IFS= read -r line; do
    if [[ "$line" =~ ^worktree[[:space:]]+(.+)$ ]]; then
      local wt_path="${BASH_REMATCH[1]}"
      local real_wt
      real_wt="$(realpath "$wt_path" 2>/dev/null || echo "$wt_path")"
      if [ "$is_first" = "true" ]; then
        is_first=false
        # First entry in porcelain list is always the main worktree; skip it
        continue
      fi
      if [ "$real_candidate" = "$real_wt" ]; then
        found_linked_match=true
        break
      fi
    fi
  done <<< "$worktree_list"

  [ "$found_linked_match" = "true" ] || return 1
  return 0
}

cleanup_ok=true

if [ -e "$worktree_path" ] || [ -L "$worktree_path" ]; then
  if [ -d "$worktree_path" ] && [ ! -L "$worktree_path" ] && is_registered_linked_worktree "$worktree_path" "$original_root"; then
    # Preferred removal: let git remove and unregister the worktree
    if git -C "$original_root" worktree remove --force "$worktree_path"; then
      git -C "$original_root" worktree prune --expire now 2>/dev/null || git -C "$original_root" worktree prune 2>/dev/null || true
    else
      printf 'SAFETY_ERROR: "git worktree remove --force" failed for "%s" — preserving path and stopping cleanup.\n' \
        "$worktree_path" >&2
      cleanup_ok=false
    fi
  else
    printf 'SAFETY: "%s" is not a registered linked git worktree directory for repo at "%s" — refusing to delete.\n' \
      "$worktree_path" "$original_root" >&2
    printf 'SAFETY: Main repository root, regular files, symlinks, and unmanaged paths are strictly protected.\n' >&2
    cleanup_ok=false
  fi
fi

if [ "$cleanup_ok" = "false" ]; then
  printf '%s\n' "WORKTREE_CLEANUP: SAFETY_ERROR (refused to delete unverified path or removal failed — see stderr for details)" >&2
  exit 2
fi

# In stale-recovery mode (used when preparing a new worktree to clean up interrupted runs),
# the linked worktree has been safely removed and verified; no snapshot integrity check is needed.
if [ "$snapshot_file" = "--stale-recovery" ]; then
  if [ -e "$worktree_path" ] || [ -L "$worktree_path" ]; then
    printf 'SAFETY: Target path "%s" still exists after recovery attempt — refusing to continue.\n' "$worktree_path" >&2
    printf '%s\n' "WORKTREE_CLEANUP: SAFETY_ERROR (path still exists)" >&2
    exit 2
  fi
  rm -f "${worktree_path}.snapshot"
  printf '%s\n' "WORKTREE_CLEANUP: PASS (stale linked worktree recovered)"
  exit 0
fi

# Verify original workspace integrity
integrity_status="PASS"
if [ -n "$original_root" ] && [ -f "$snapshot_file" ]; then
  current_branch="$(git -C "$original_root" branch --show-current 2>/dev/null || true)"
  current_head="$(git -C "$original_root" rev-parse HEAD 2>/dev/null || true)"

  if [ "$current_branch" != "$expected_branch" ]; then
    printf 'WORKSPACE_INTEGRITY_MISMATCH: Branch changed! Expected "%s", got "%s"\n' "$expected_branch" "$current_branch" >&2
    integrity_status="MISMATCH"
  fi

  if [ "$current_head" != "$expected_head" ]; then
    printf 'WORKSPACE_INTEGRITY_MISMATCH: HEAD changed! Expected "%s", got "%s"\n' "$expected_head" "$current_head" >&2
    integrity_status="MISMATCH"
  fi

  # Extract expected status from snapshot file
  expected_status="$(awk '/=== STATUS_START ===/{flag=1;next}/=== STATUS_END ===/{flag=0}flag' "$snapshot_file")"
  current_status="$(git -C "$original_root" status --porcelain)"

  if [ "$current_status" != "$expected_status" ]; then
    printf 'WORKSPACE_INTEGRITY_MISMATCH: Git status modified!\n' >&2
    printf '%s\n%s\n' "--- Expected status ---" "$expected_status" >&2
    printf '%s\n%s\n' "--- Current status ---" "$current_status" >&2
    integrity_status="MISMATCH"
  fi
elif [ -n "$original_root" ]; then
  # Snapshot file was not found — cannot verify workspace integrity.
  printf 'WORKSPACE_INTEGRITY_UNVERIFIED: snapshot file not found at "%s" — cannot confirm workspace is unchanged.\n' "$snapshot_file" >&2
  integrity_status="UNVERIFIED"
fi

case "$integrity_status" in
  PASS)
    rm -f "$snapshot_file"
    printf '%s\n' "WORKTREE_CLEANUP: PASS (workspace integrity verified)"
    exit 0
    ;;
  MISMATCH)
    printf '%s\n' "WORKTREE_CLEANUP: INTEGRITY_MISMATCH (preserving original state without hard reset)" >&2
    exit 1
    ;;
  UNVERIFIED)
    printf '%s\n' "WORKTREE_CLEANUP: INTEGRITY_UNVERIFIED (snapshot missing — workspace change cannot be confirmed)" >&2
    exit 3
    ;;
esac
