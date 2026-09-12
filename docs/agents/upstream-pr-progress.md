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

Worktree notes: `...\pr-region-pairs-core\NOTES-pr03.md` (ExtraPath-early rationale).

Grill consensus (settled): full upstreamable surface over time; doc-first batches; Issue+PR together (Draft for large); English for PR/branch surfaces; hard-exclude fork/AI/agent/`-fork.N`/ADR 0013; build/CI as small separate PRs; organize locally and review before a dedicated submit session.

## Status

**Phase:** Organize clean `pr/*` branches on the fork (no upstream Issue/PR yet).

**Overall:** PR-01…04 on `origin`. ExtraPath-early on PR-03 **accepted**. PR-04 is a small stack commit (ADR 0006 + ExtraPath tests). Next organize step: **PR-05** on `pr/extra-paths`.

## Done

| Item | Evidence |
| --- | --- |
| Plan / manifest / progress handoff | `docs/agents/upstream-pr-*.md` |
| `origin/pr/msbuild-script` | `1188d58` |
| `origin/pr/deploy-repo-guard` | `378d430` |
| `origin/pr/region-pairs-core` | `a9ecd04` — ExtraPath-early **accepted** by user |
| `origin/pr/extra-paths` | `8f8fdd2` — ADR 0006 + `TestLiveOverlaySessionExtraPaths` (+ csproj); delta vs PR-03 = 3 files / +740 |
| PR-04 build/tests (agent) | Debug+Release OK; filter `ExtraPath\|DarkScreenSubtitle` → **27 passed** |
| Parent spot-check PR-04 | Forbid grep clean; ADR 0006 present; diff vs PR-03 matches agent report |
| Deferred on purpose | `TestLiveOverlaySessionExtraPathRepeat.cs` needs ActivityLog (PR-06) |

## In progress

| Item | Owner / note |
| --- | --- |
| — | Idle between batches |

## Next actions

1. Build `pr/overlay-layout-per-game` (PR-05) from `origin/pr/extra-paths` per plan/manifest (ApplyGame layout swap + ADR 0009 + persistence tests).
2. Then PR-06 activity-log core (stack on PR-05 tip preferred).
3. Submit session only when user explicitly asks (Ready 01/02; Draft 03+).

## Blockers / confirmations

| When | Confirm before acting |
| --- | --- |
| Before upstream submit | User explicitly asks to open Issues/PRs on `qew21/Genshin-Subtitles` |
| None for PR-05 kickoff | ExtraPath scope already accepted; PR-04 reviewed |

## Branch table (fork `origin`)

| Branch | Tip | Upstream Issue/PR | Notes |
| --- | --- | --- | --- |
| `pr/msbuild-script` | `1188d58` | — | Submit later (Ready) |
| `pr/deploy-repo-guard` | `378d430` | — | Submit later (Ready) |
| `pr/region-pairs-core` | `a9ecd04` | — | Draft later; ExtraPath-early accepted |
| `pr/extra-paths` | `8f8fdd2` | — | Stacked on 03; organize done |
| `pr/overlay-layout-per-game` … | — | — | **Next** |
| `pr/activity-log-*` … `pr/subtitle-idle-timeout` | — | — | Not started |

`master` last known ff of `upstream/master`: `cda7fa6`.

## Last updated

- **When:** 2026-09-12 (ExtraPath accepted; PR-04 finished + parent-reviewed)
- **By:** parent agent
- **personal docs tip:** push with this revision to `origin/personal` in closeout
