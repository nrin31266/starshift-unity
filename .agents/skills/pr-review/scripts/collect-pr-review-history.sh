#!/usr/bin/env bash
# Collect PR review history via gh and output a compact, token-efficient history digest.
# Extracts latest_reviewed_head and structured previous_findings (fingerprint, severity,
# path, line, title, reviewed_head) without dumping full raw history bodies.
#
# Usage: collect-pr-review-history.sh <owner>/<repo>/pull/<number>|PR_URL
set -euo pipefail

if [ "$#" -ne 1 ]; then
  printf '%s\n' "usage: $0 <owner>/<repo>/pull/<number>|PR_URL" >&2
  exit 64
fi

raw="$1"
if [[ "$raw" =~ github\.com/([^/]+)/([^/]+)/pull/([0-9]+) ]]; then
  owner="${BASH_REMATCH[1]}"
  repo="${BASH_REMATCH[2]}"
  number="${BASH_REMATCH[3]}"
elif [[ "$raw" =~ ^([^/]+)/([^/]+)/pull/([0-9]+)$ ]]; then
  owner="${BASH_REMATCH[1]}"
  repo="${BASH_REMATCH[2]}"
  number="${BASH_REMATCH[3]}"
else
  printf '%s\n' "unrecognized PR reference: $raw" >&2
  exit 65
fi

if ! command -v gh > /dev/null 2>&1; then
  printf '%s\n' "gh CLI required" >&2
  exit 2
fi

if ! command -v jq > /dev/null 2>&1; then
  printf '%s\n' "jq required" >&2
  exit 2
fi

# Collect paginated responses. --paginate --slurp with gh api wraps all pages in an array.
# Flatten arrays deterministically; never mix stderr (2>&1) into the JSON stdout pipe.
collect_paginated() {
  local endpoint="$1"
  gh api --paginate --slurp "$endpoint" | jq 'if type == "array" then (if length == 0 then [] elif (.[0] | type) == "array" then (add // []) else . end) else [] end'
}

reviews_json="$(collect_paginated "repos/${owner}/${repo}/pulls/${number}/reviews")"
comments_json="$(collect_paginated "repos/${owner}/${repo}/pulls/${number}/comments")"

# Process into a compact history digest.
# Invariants:
#   1. Does NOT dump raw review bodies or raw thread comments (keeps tokens minimal).
#   2. Parses latest agent-tracked review body for latest_reviewed_head and structured findings.
#   3. Falls back to legacy inline comment markers (e.g. lyreo-review) for backward compatibility.
#   4. Produces structured previous_findings: fingerprint, severity, path, line, title, reviewed_head.
jq -n \
  --arg pr "${owner}/${repo}#${number}" \
  --argjson reviews "$reviews_json" \
  --argjson comments "$comments_json" '
  # Helper to parse agent-pr-review or legacy lyreo-review marker
  def parse_marker(text):
    if (text // "") | test("<!--[\\s\\S]*?(agent-pr-review|lyreo-review)[\\s\\S]*?reviewed-head:\\s*([^\\s>]+)[\\s\\S]*?-->") then
      {
        fingerprint: (if (text // "") | test("fingerprint:\\s*[^\\s>]+") then
          ((text // "") | capture("fingerprint:\\s*(?<fp>[^\\s>]+)") | .fp)
        else null end),
        reviewed_head: ((text // "") | capture("reviewed-head:\\s*(?<head>[^\\s>]+)") | .head)
      }
    else
      { fingerprint: null, reviewed_head: null }
    end;

  def has_agent_marker(text):
    (text // "") | test("<!--[\\s\\S]*?(agent-pr-review|lyreo-review)[\\s\\S]*?reviewed-head:\\s*([^\\s>]+)[\\s\\S]*?-->");

  # 1. Find latest agent review (from review bodies)
  (
    $reviews
    | map(select(has_agent_marker(.body)))
    | sort_by(.submitted_at)
    | reverse
    | .[0] // null
  ) as $latest_review |

  # 2. Extract structured findings from latest agent review body (new format)
  (
    if $latest_review != null then
      ($latest_review.body // "") as $b |
      [
        # Matches: <!-- agent-pr-finding fingerprint: <fp> severity: <sev>(?: lifecycle: <lc>)? path: <path> line: <line> title: <title> -->
        # Supports optional lifecycle (defaulting to NEW if absent) and non-greedy title
        $b | scan("<!-- agent-pr-finding fingerprint: ([^\\s>]+) severity: ([^\\s>]+)(?: lifecycle: ([^\\s>]+))?(?: path: ([^\\s>]+))?(?: line: ([0-9]+))?(?: title: (.*?))?\\s*-->")
        | {
            fingerprint: .[0],
            severity: .[1],
            lifecycle: (.[2] // "NEW"),
            path: (.[3] // "unknown"),
            line: (if .[4] != null and .[4] != "" then (.[4] | tonumber) else 0 end),
            title: (.[5] // "" | sub("^ +"; "") | sub(" +$"; "")),
            reviewed_head: parse_marker($latest_review.body).reviewed_head
          }
      ]
    else
      []
    end
  ) as $findings_from_body |

  # 3. Extract legacy findings from inline comments (like initial PR #15 review)
  (
    [
      $comments[]
      | select(.in_reply_to_id == null and has_agent_marker(.body))
      | {
          id: .id,
          path: .path,
          line: (.line // .original_line // 0),
          fingerprint: parse_marker(.body).fingerprint,
          reviewed_head: parse_marker(.body).reviewed_head,
          severity: (
            if .body | test("\\*\\*BLOCKER\\*\\*") then "BLOCKER"
            elif .body | test("\\*\\*IMPORTANT\\*\\*") then "IMPORTANT"
            elif .body | test("\\*\\*SUGGESTION\\*\\*") then "SUGGESTION"
            else "IMPORTANT" end
          ),
          title: (
            (.body | split("\n")
             | map(select(test("^\\*\\*(BLOCKER|IMPORTANT|SUGGESTION)\\*\\*:\\s*")))
             | first // ""
             | sub("^\\*\\*(BLOCKER|IMPORTANT|SUGGESTION)\\*\\*:\\s*"; "")
             | sub("\\s+$"; ""))
          )
        }
      | select(.fingerprint != null)
    ]
  ) as $legacy_findings |

  # Determine latest reviewed HEAD:
  # Prefer review body head; fall back to inline comments head
  (
    if $latest_review != null and parse_marker($latest_review.body).reviewed_head != null then
      parse_marker($latest_review.body).reviewed_head
    elif ($legacy_findings | length) > 0 then
      ($legacy_findings | map(.reviewed_head) | last)
    else
      null
    end
  ) as $latest_head |

  (
    if $latest_review != null then
      $latest_review.submitted_at
    elif ($comments | length) > 0 then
      ($comments | sort_by(.created_at) | last | .created_at)
    else
      null
    end
  ) as $latest_at |

  # Select previous findings:
  # If findings_from_body has entries, use those.
  # Otherwise if legacy_findings has entries, filter to those matching latest_head.
  (
    if ($findings_from_body | length) > 0 then
      $findings_from_body
    else
      $legacy_findings | map(select(.reviewed_head == $latest_head))
    end
  ) as $previous_findings |

  {
    pr: $pr,
    latest_reviewed_head: $latest_head,
    latest_reviewed_at: $latest_at,
    review_type: (if $latest_head != null then "incremental-re-review" else "first-review" end),
    previous_findings_count: ($previous_findings | length),
    previous_findings: $previous_findings
  }
'
