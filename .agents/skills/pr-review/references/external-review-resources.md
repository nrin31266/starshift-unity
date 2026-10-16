# External Review Resources

> Read this file only when a phase needs current code-review judgment, security guidance, GitHub mechanics, writing/tone rules, or skill-maintenance context. Fetch one URL at a time and return only the applied rule plus the URL.

This standalone URL map replaces bulky in-prompt explanations. Choose the row that matches the immediate question, fetch that source with the available web or documentation tool, apply it, and cite the URL in `Sources checked` or `References fetched`.

## Fetch Policy

1. Prefer official product documentation for GitHub mechanics and dependency behavior.
2. Prefer established engineering references for review judgment and tone.
3. Fetch only the URL needed for the current decision.
4. Keep fetched page contents out of orchestrator output; summarize only the applied rule.
5. If no web tool is available, proceed from the bundled workflow and record a residual risk naming the rule that could not be re-verified.

## Code Review Judgment

| Need | Source |
| --- | --- |
| What reviewers should look for: correctness, design, complexity, tests, naming, comments, style, consistency, docs | https://google.github.io/eng-practices/review/reviewer/looking-for.html |
| Reviewer responsibilities, scope, and general process | https://google.github.io/eng-practices/review/reviewer/ |
| Navigating a change list and deciding inspection order | https://google.github.io/eng-practices/review/reviewer/navigate.html |
| Review speed and when to request changes | https://google.github.io/eng-practices/review/reviewer/speed.html |
| Large-change guidance when partitioning a broad PR into review dimensions | https://google.github.io/eng-practices/review/developer/small-cls.html |
| GitLab high-impact-risk checklist and review process | https://docs.gitlab.com/development/code_review/ |

## Security Review

| Need | Source |
| --- | --- |
| Security-focused code review checklist by topic | https://owasp.org/www-project-code-review-guide/ |
| Application security verification categories for deeper checks | https://owasp.org/www-project-application-security-verification-standard/ |
| OWASP Top 10 risk categories for web applications | https://owasp.org/www-project-top-ten/ |
| Output path safety and path traversal risk when validating `OUTPUT_FILE` | https://owasp.org/www-community/attacks/Path_Traversal |

## Comment Language And Labels

| Need | Source |
| --- | --- |
| Useful, kind, and specific review comments | https://google.github.io/eng-practices/review/reviewer/comments.html |
| Conventional review labels and blocking/non-blocking decorations | https://conventionalcomments.org/ |
| Plain technical writing principles | https://developers.google.com/tech-writing/one/just-enough-grammar |
| Patterns that signal AI-generated prose | https://en.wikipedia.org/wiki/Wikipedia:Signs_of_AI_writing |

## GitHub Review Mechanics

| Need | Source |
| --- | --- |
| Pull request review decisions: comment, approve, request changes | https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/reviewing-changes-in-pull-requests/about-pull-request-reviews |
| Reviewing proposed changes in the GitHub UI | https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/reviewing-changes-in-pull-requests/reviewing-proposed-changes-in-a-pull-request |
| Adding line comments and inline `suggestion` blocks | https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/reviewing-changes-in-pull-requests/commenting-on-a-pull-request |
| Review comment REST fields: `path`, `line`, `side`, `start_line`, `start_side` | https://docs.github.com/en/rest/pulls/comments#create-a-review-comment-for-a-pull-request |
| Create-review REST endpoint: `event`, `body`, `comments[]` | https://docs.github.com/en/rest/pulls/reviews#create-a-review-for-a-pull-request |
| `gh pr review` CLI flags and behavior | https://cli.github.com/manual/gh_pr_review |
| `gh api` for arbitrary REST calls when `gh pr review` is insufficient | https://cli.github.com/manual/gh_api |

## Dependency-Specific Claims

When a finding depends on a library, framework, cloud service, API, SDK, or CLI, fetch current official documentation for that dependency before treating behavior as factual. Cite the exact URL in `Sources checked` or `References fetched`. Treat training-data recall about dependency behavior as a hypothesis until a current source confirms it.

The URL is not only internal bookkeeping: any comment whose claim rests on such an external fact must carry the verifying URL in its posted body, so the reader can confirm the claim without trusting the reviewer. `review-verifier` fails source-less external claims.

## Skill Maintenance And Progressive Disclosure

| Need | Source |
| --- | --- |
| Skill-style progressive disclosure example | https://skills.sh/flpbalada/fb-skills/progressive-disclosure |
| Agent Skills loading model, anatomy, and levels | https://platform.claude.com/docs/en/agents-and-tools/agent-skills/overview |
| Agent Skills authoring best practices | https://docs.claude.com/en/docs/agents-and-tools/agent-skills/best-practices |
| Progressive disclosure as a UX pattern | https://www.nngroup.com/articles/progressive-disclosure/ |
