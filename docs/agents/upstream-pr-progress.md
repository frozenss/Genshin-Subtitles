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

Grill consensus (settled): full upstreamable surface over time; doc-first batches; Issue+PR together (Draft for large); English for PR/branch surfaces; hard-exclude fork/AI/agent/`-fork.N`/ADR 0013; build/CI as small separate PRs; organize locally and review before a dedicated submit session.

## Status

**Phase:** **Organize + gap review complete** for PR-01…09 on the fork. **No upstream Issues/PRs opened yet.**

**Overall:** Independent chores `pr/msbuild-script` + `pr/deploy-repo-guard`. Product stack is linear:

`region-pairs-core` → `extra-paths` → `overlay-layout-per-game` → `activity-log-core` → `activity-log-fold-miss` → `activity-log-copy-tags` → `activity-log-follow-tail` → `region-adjust-polish` → `subtitle-idle-timeout` (`1fa5d9d`).

ExtraPath-early on PR-03 remains **accepted**.

**Gap review verdict:** **No real upstreamable product gaps.** Synthetic tree (`master` + tip PR-03…09 + PR-01 + PR-02) vs `origin/personal` differs only by intentional scrub/format/forbid noise.

## Done

| Item | Tip | Agent validation (parent spot-check where noted) |
| --- | --- | --- |
| `pr/msbuild-script` | `1188d58` | Build script + README build section; no fork banner |
| `pr/deploy-repo-guard` | `378d430` | Deploy repo guard only |
| `pr/region-pairs-core` | `a9ecd04` | 75 tests (pairs/hint/OCR); ExtraPath-early accepted |
| `pr/extra-paths` | `8f8fdd2` | +740 vs 03; 27 ExtraPath/detector tests |
| `pr/overlay-layout-per-game` | `6dfefa4` | +592 vs 04; 40 layout/applied/extra tests |
| `pr/activity-log-core` | `6c5704c` | +2619 vs 05; window ~270 lines; 36 log tests |
| `pr/activity-log-fold-miss` | `d3b7ff4` | +859 vs 06; 18 fold/miss/filter tests |
| `pr/activity-log-copy-tags` | `159e3f4` | +1654 vs 07a; 15 copy/tag tests |
| `pr/activity-log-follow-tail` | `d909b2b` | +1307 vs 07b; 15 follow-tail tests |
| `pr/region-adjust-polish` | `ea48965` | +842 vs 07c; 17+11 adjust tests; Verify-Release script OK |
| `pr/subtitle-idle-timeout` | `1fa5d9d` | +1058 vs 08; 17 idle-timeout tests |
| Synth gap review | throwaway `_review/upstream-synth` (deleted after review) | See “Gap review” below |

Plan/manifest/progress docs live on `origin/personal`.

## Gap review (synth vs `personal`)

**Method:** throwaway worktree from `master` @ `cda7fa6`; merge `origin/pr/subtitle-idle-timeout`, then `pr/msbuild-script`, then `pr/deploy-repo-guard`; diff vs `origin/personal`; classify forbid vs product. Worktree/branch removed after review (do not push synth).

**Identical / aligned (byte or content):** `LiveOverlaySession.cs`, `LiveOverlaySession.OperatorActions.cs`, `MainWindow.xaml(.cs)`, ExtraPath/idle-timeout stores, product ADR set except **0013**, `scripts/Build.ps1`, product file inventory under `GI-Subtitles` / `GI-Test` / `Screenshot` / `scripts` / `docs/adr` (no 0013). csproj Compile/Page **membership** equal (order differs only).

**Intentional diffs only (not gaps):**

| Path | Why OK |
| --- | --- |
| AdjustMouseGuard / RegionAdjustTrace / RegionAdjustDiagnostics / TestRegionAdjustTrace / Verify-Release script | Fork issue `#37`/`#39` comment scrub on synth |
| `ActivityLogWindow.xaml.cs` | Comment / field order / type qualification — behaviorally equivalent |
| Strings `*.xaml` | ActivityLog block indentation only; keys/values identical after whitespace strip |
| `GI-Subtitles.csproj` / `GI-Test.csproj` | Include order only |
| `SettingsWindow.xaml.cs` | Blank-line noise |
| `README*.md` | Personal fork banner only on `personal`; build section on both |
| `.github/workflows/release.yml` | Personal-only `-fork.*` release-notes prepend (forbid); deploy guard on both |
| `.gitignore` | Personal AI / `.scratch` / `tmp` entries |

**Hard-forbid ADDs on personal only:** `docs/agents/**`, `AGENTS.md`, `CONTEXT.md`, ADR **0013**.

**Real gaps:** **none.**

**Optional polish before submit (non-blocking):** normalize ActivityLog string indentation on stack tip; optional later scrub of remaining `#18`/`#36`/`#39` mentions inside shared ADR narrative (present on both trees identically — not a synth↔personal delta).

## In progress

| Item | Owner / note |
| --- | --- |
| — | Idle; waiting for explicit submit session |

## Next actions

1. **Submit session** (only when user explicitly asks): open upstream Issues + PRs per plan checklist — Ready for 01/02; Draft for large product PRs; cross-link; English bodies with Summary/Validation.
2. Optional non-blocking polish: ActivityLog string indent on tip; ADR fork-issue wording scrub if desired before open.
3. After upstream merges, ff `master` ← `upstream/master` and merge into `personal` as usual.

## Blockers / confirmations

| When | Confirm before acting |
| --- | --- |
| **Before any upstream Issue/PR** | User explicitly starts a submit session |
| None for organize / gap review | Inventory PR-01…09 heads are on `origin`; gap review clean |

## Branch table (fork `origin`)

| Branch | Tip | Upstream Issue/PR | Notes |
| --- | --- | --- | --- |
| `pr/msbuild-script` | `1188d58` | — | Independent; Ready later |
| `pr/deploy-repo-guard` | `378d430` | — | Independent; Ready later |
| `pr/region-pairs-core` | `a9ecd04` | — | Stack root; Draft later |
| `pr/extra-paths` | `8f8fdd2` | — | |
| `pr/overlay-layout-per-game` | `6dfefa4` | — | |
| `pr/activity-log-core` | `6c5704c` | — | |
| `pr/activity-log-fold-miss` | `d3b7ff4` | — | |
| `pr/activity-log-copy-tags` | `159e3f4` | — | |
| `pr/activity-log-follow-tail` | `d909b2b` | — | |
| `pr/region-adjust-polish` | `ea48965` | — | |
| `pr/subtitle-idle-timeout` | `1fa5d9d` | — | **Stack tip** |

`master` last known ff of `upstream/master`: `cda7fa6`.

## Last updated

- **When:** 2026-09-12 (gap review: synth tip+01+02 vs `personal`)
- **By:** parent agent after synth review; throwaway `_review/upstream-synth` deleted
- **personal docs tip:** commit+push this file to `origin/personal` in closeout
