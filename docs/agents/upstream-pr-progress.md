# Upstream PR progress (fork-only)

Living handoff for slicing `personal` into clean `pr/*` heads for `qew21/Genshin-Subtitles`.
**Do not** open a PR that adds this file upstream.

## Session close rule (mandatory)

At the **end of every agent/user conversation** that touches upstream-PR work (planning, branch cuts, review, submit):

1. Update **this file** before the turn ends: Status, Done, In progress, Next actions, Blockers / confirmations, Branch table, and “Last updated”.
2. Keep claims honest: only mark Done when tool output or review supports it.
3. If the working tree has progress-doc or plan/manifest edits worth keeping across sessions, **commit and push them to `origin/personal`** (fork only — never write to `upstream`). Mention the commit SHA in “Last updated”.
4. Do **not** open upstream Issues/PRs unless the user explicitly starts a submit session.
5. Point readers at the canonical docs below instead of duplicating long inventories here.

New sessions: read this file first, then the linked docs for the next action.

## Canonical docs (read in order)

| Doc | Role |
| --- | --- |
| [`upstream-pr-progress.md`](upstream-pr-progress.md) | **This file** — status, next step, confirmations |
| [`upstream-pr-plan.md`](upstream-pr-plan.md) | Ground rules, forbid list, PR-01…09 inventory, submit checklist |
| [`upstream-pr-file-manifest.md`](upstream-pr-file-manifest.md) | Precise allow/exclude paths and surgical cuts for PR-03…09 |
| [`git-branches.md`](git-branches.md) | `personal` / `master` / `pr/<topic>` conventions |

Worktree note for PR-03 deviations: `C:\Users\Administrator\.grok\worktrees\csharp-genshin-subtitles\pr-region-pairs-core\NOTES-pr03.md` (untracked local notes).

Grill consensus (already settled): full upstreamable surface over time; doc-first batches; Issue+PR together (Draft for large); English for PR/branch surfaces; hard-exclude fork/AI/agent/`-fork.N`/ADR 0013; build/CI as small separate PRs; organize locally and review before a dedicated submit session.

## Status

**Phase:** Organize clean `pr/*` branches on the fork (no upstream Issue/PR yet).

**Overall:** PR-01…03 on `origin`. User **accepted** PR-03 early ExtraPath scope (2026-09-12). PR-04 organize in progress (stack on `pr/region-pairs-core`: complete/scrub ExtraPath + ADR 0006 + tests — not a greenfield invent).

## Done

| Item | Evidence |
| --- | --- |
| Plan doc | `docs/agents/upstream-pr-plan.md` |
| File transplant manifest PR-03…09 | `docs/agents/upstream-pr-file-manifest.md` |
| Progress handoff + session-close rule | this file |
| `origin/pr/msbuild-script` | `1188d58` — `chore: add VS MSBuild build script` |
| `origin/pr/deploy-repo-guard` | `378d430` — deploy repository guard only |
| `origin/pr/region-pairs-core` | `a9ecd04` — organize complete; ExtraPath-early **accepted** by user |
| PR-03 build/tests (agent) | GI-Subtitles Debug+Release OK; vstest filter `LiveOverlaySession\|HintScreen\|RegionPairSettings` → **75 passed** |
| Parent forbid spot-check on PR-03 | No matches for fork banner / `docs/agents` / ADR 0013 / `ActivityLogWindow` / `SubtitleIdleTimeout` / `RegionAdjustTrace` |
| ExtraPath scope decision | **Accept** — keep ExtraPath on PR-03; PR-04 = narrow/complete/scrub |

## In progress

| Item | Owner / note |
| --- | --- |
| `pr/extra-paths` | Subagent stacking on `origin/pr/region-pairs-core` per manifest PR-04 |

## Next actions

1. Finish + parent-review `pr/extra-paths` (forbid list, ADR 0006, ExtraPath tests, no activity-log/idle/trace).
2. Then PR-05 (layout ApplyGame) stacked on reviewed PR-04 tip (or on PR-03 if 04 is empty of layout work — prefer 04 tip).
3. Submit session remains **blocked** until user explicitly asks.

## Blockers / confirmations

| When | Confirm before acting |
| --- | --- |
| Before upstream submit session | User explicitly asks to open Issues/PRs on `qew21/Genshin-Subtitles` |
| PR-01 local Release on naked `master` | Screenshot was `v4.5.2` on master; PR-03 carries TFM `v4.8` — keep PR-01 narrow |
| Parallel product PRs | 05+ wait until PR-04 reviewed; do not parallelize with in-flight 04 |
## Branch table (fork `origin`)

| Branch | Tip | Upstream Issue/PR | Notes |
| --- | --- | --- | --- |
| `pr/msbuild-script` | `1188d58` | — | Organize done; submit later |
| `pr/deploy-repo-guard` | `378d430` | — | Organize done; submit later |
| `pr/region-pairs-core` | `a9ecd04` | — | Organize complete; ExtraPath-early accepted |
| `pr/extra-paths` | _(pending)_ | — | In progress — stack on `a9ecd04` |
| `pr/overlay-layout-per-game` … `pr/subtitle-idle-timeout` | — | — | Not started |

`master` last known ff of `upstream/master`: `cda7fa6`.

## Last updated

- **When:** 2026-09-12 (user accepted ExtraPath-early; PR-04 subagent started)
- **By:** parent agent
- **personal docs commits:** will push this acceptance + in-progress PR-04 note to `origin/personal` in closeout
