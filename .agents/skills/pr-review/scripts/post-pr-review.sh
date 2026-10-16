#!/usr/bin/env bash
# Post a summary-only PR review via gh. Caller must already hold verified or preview
# authorization for this exact body.
# Usage: post-pr-review.sh <owner>/<repo> <number> <event> <body-file> [commit_id]
#   event: APPROVE | REQUEST_CHANGES | COMMENT
set -euo pipefail
if [ "$#" -lt 4 ] || [ "$#" -gt 5 ]; then
  printf '%s\n' "usage: $0 <owner>/<repo> <number> <APPROVE|REQUEST_CHANGES|COMMENT> <body-file> [commit_id]" >&2
  exit 64
fi
repo_path="$1"
number="$2"
event="$3"
body_file="$4"
commit_id="${5:-}"
case "$event" in
APPROVE | REQUEST_CHANGES | COMMENT) ;;
*)
  printf '%s\n' "invalid event: $event (expected APPROVE|REQUEST_CHANGES|COMMENT)" >&2
  exit 65
  ;;
esac
if [ ! -f "$body_file" ]; then
  printf '%s\n' "body file not found: $body_file" >&2
  exit 66
fi
if ! command -v gh >/dev/null 2>&1; then
  printf '%s\n' "gh CLI required" >&2
  exit 2
fi

api_args=(
  api --method POST "repos/${repo_path}/pulls/${number}/reviews"
  -f event="$event"
  -F body=@"$body_file"
)
if [ -n "$commit_id" ]; then
  api_args+=(-f commit_id="$commit_id")
fi

gh "${api_args[@]}"
