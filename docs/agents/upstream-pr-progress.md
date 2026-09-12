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

**Overall:** PR-01, PR-02, and PR-03 heads are on `origin`. PR-03 parent spot-check passed forbid list / build claim; **scope confirmation needed** on early ExtraPath inclusion before treating PR-03 as submit-ready or starting PR-04 scrub.

## Done

| Item | Evidence |
| --- | --- |
| Plan doc | `docs/agents/upstream-pr-plan.md` |
| File transplant manifest PR-03…09 | `docs/agents/upstream-pr-file-manifest.md` |
| Progress handoff + session-close rule | this file |
| `origin/pr/msbuild-script` | `1188d58` — `chore: add VS MSBuild build script` |
| `origin/pr/deploy-repo-guard` | `378d430` — deploy repository guard only |
| `origin/pr/region-pairs-core` | `a9ecd04` — `feat: region pairs with shared OCR cadence and settings` |
| PR-03 build/tests (agent) | GI-Subtitles Debug+Release OK; vstest filter `LiveOverlaySession\|HintScreen\|RegionPairSettings` → **75 passed** |
| Parent forbid spot-check on PR-03 | No matches for fork banner / `docs/agents` / ADR 0013 / `ActivityLogWindow` / `SubtitleIdleTimeout` / `RegionAdjustTrace` |

## In progress

| Item | Owner / note |
| --- | --- |
| — | None actively building. Waiting on user confirmation for PR-03 ExtraPath scope (see below). |

## Next actions

1. **User confirmation:** accept PR-03 early ExtraPath runtime, or strip back toward master-inline dark-screen/dialogue before submit / before PR-04.
2. If accepted: mark PR-03 organize-complete; start PR-04 as a **narrow/scrub** pass on top of `pr/region-pairs-core` (per agent NOTES), not a greenfield ExtraPath invent.
3. If strip required: new subagent pass on `pr/region-pairs-core` to remove ExtraPath session/UI and restore hybrid MainWindow per manifest.
4. Submit session remains **blocked** until user explicitly asks (then: Ready for 01/02, Draft for 03).

## Blockers / confirmations

| When | Confirm before acting |
| --- | --- |
| **Now** | **PR-03 ExtraPath early landing:** branch includes `ExtraPathSample`/`ExtraPathBody`, DarkScreen/Dialogue session APIs, Settings pin/scan, NotifyIcon box helpers — larger than manifest’s “prefer defer to PR-04”. Accept as Draft scope, or strip first? |
| Before upstream submit session | User explicitly asks to open Issues/PRs on `qew21/Genshin-Subtitles` |
| PR-01 local Release on naked `master` | Screenshot was `v4.5.2` on master; PR-03 carries TFM `v4.8` — keep PR-01 narrow |
| Parallel product PRs | Still do not parallelize 04+ until PR-03 scope is accepted |

## Branch table (fork `origin`)

| Branch | Tip | Upstream Issue/PR | Notes |
| --- | --- | --- | --- |
| `pr/msbuild-script` | `1188d58` | — | Organize done; submit later |
| `pr/deploy-repo-guard` | `378d430` | — | Organize done; submit later |
| `pr/region-pairs-core` | `a9ecd04` | — | Organize done pending ExtraPath scope confirm; 49 files, +8319/−1115 |
| `pr/extra-paths` … | — | — | Not started (expect scrub/narrow if 03 keeps ExtraPath) |
| `pr/overlay-layout-per-game` … `pr/subtitle-idle-timeout` | — | — | Not started |

`master` last known ff of `upstream/master`: `cda7fa6`.

## Last updated

- **When:** 2026-09-12 (end of session: progress doc + PR-03 subagent finished)
- **By:** parent agent after PR-03 push `a9ecd04` and forbid spot-check
- **personal docs commits:** `cc74267` (plan/manifest/progress created); `532fef6` (progress after PR-03). Further edits to this file in the same closeout may add another docs commit on `origin/personal`.
